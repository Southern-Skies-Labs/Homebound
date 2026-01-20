using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;
using Homebound.Features.AethianAI.Strategies;
using Homebound.Features.AethianAI.Strategies.Gathering;

namespace Homebound.Features.AethianAI
{
    public class StateGather : AethianState
    {
        private IGatherStrategy _currentStrategy;
        private StorageContainer _targetStorage;
        private bool _isDepositing; 

        // Configuración
        private const float INTERACTION_RANGE = 2.0f; // Rango para interactuar con almacenes
        private const int MAX_CARRY_AMOUNT = 20; 

        public StateGather(AethianBot bot) : base(bot) { }

        public override void Enter()
        {
            ResetState();

            if (_bot.CurrentJob == null)
            {
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            // Usamos la Factory para obtener la estrategia correcta (Mine vs Chop)
            _currentStrategy = GatherStrategyFactory.Create(_bot.CurrentJob.Type);

            if (_currentStrategy == null || !_currentStrategy.IsJobValid(_bot.CurrentJob))
            {
                Debug.LogWarning($"[StateGather] Estrategia inválida o trabajo corrupto para {_bot.CurrentJob.Type}");
                CompleteJob();
            }
        }

        public override void Tick()
        {
            if (_isDepositing)
            {
                HandleDepositSequence();
            }
            else
            {
                HandleWorkSequence();
            }
        }

        // --- FASE DE TRABAJO (Delegada a Estrategia) ---

        private void HandleWorkSequence()
        {
            // 1. Verificar validez continua
            if (_bot.CurrentJob == null || !_currentStrategy.IsJobValid(_bot.CurrentJob))
            {
                // Si ya no es válido, puede que se haya completado o cancelado externamente
                StartDepositSequence(); // Intentamos depositar lo que tengamos
                return;
            }

            // 2. Movimiento hacia el objetivo de trabajo
            Vector3 targetPos = _currentStrategy.GetWorkPosition(_bot.CurrentJob, _bot.Position);
            float dist = Vector3.Distance(_bot.Position, targetPos);

            // Nota: Usamos una distancia pequeña para "llegar", pero confiamos en HasReachedDestination
            // Si el bot está parado y cerca, ejecutamos.
            if (dist <= INTERACTION_RANGE)
            {
                _bot.StopMoving();

                // 3. Ejecutar trabajo
                bool finished = _currentStrategy.ExecuteWork(_bot, Time.deltaTime);

                // 4. Chequear inventario (Capacidad)
                CheckInventoryCapacity();

                // 5. Si la estrategia dice que terminó (rompió el bloque/árbol)
                if (finished)
                {
                    StartDepositSequence();
                }
            }
            else
            {
                if (!_bot.HasReachedDestination())
                {
                    _bot.MoveTo(targetPos);
                }
            }
        }

        private void CheckInventoryCapacity()
        {
            if (_bot.TryGetComponent(out UnitInventory inventory))
            {
                if (inventory.IsFull || inventory.TotalCount >= MAX_CARRY_AMOUNT)
                {
                    StartDepositSequence();
                }
            }
        }

        // --- FASE DE DEPÓSITO (Común) ---

        private void StartDepositSequence()
        {
            _isDepositing = true;
            _bot.StopMoving();

            // Si tenemos inventario vacío, no tiene sentido ir al almacén, terminamos el trabajo.
            if (_bot.TryGetComponent(out UnitInventory inventory))
            {
                if (inventory.TotalCount == 0)
                {
                    CompleteJob();
                    return;
                }
            }
            
            var economy = ServiceLocator.Get<EconomyManager>();
            if (economy != null)
            {
                _targetStorage = economy.GetNearestStorage(_bot.Position);
            }

            if (_targetStorage != null)
            {
                _bot.MoveTo(_targetStorage.GetDropOffPoint());
            }
            else
            {
                Debug.LogWarning($"[StateGather] {_bot.name} no encontró almacén. Completando trabajo con recursos encima.");
                CompleteJob(); 
            }
        }

        private void HandleDepositSequence()
        {
            if (_targetStorage == null)
            {
                CompleteJob();
                return;
            }
            
            float dist = Vector3.Distance(_bot.Position, _targetStorage.GetDropOffPoint());
            if (dist <= INTERACTION_RANGE + 1.0f) 
            {
                _bot.StopMoving();
                
                // TRANSFERENCIA
                if (_bot.TryGetComponent(out UnitInventory inventory))
                {
                    inventory.TransferAllTo(_targetStorage);
                }
                
                // Trabajo Completado exitosamente
                CompleteJob();
            }
        }

        // --- UTILIDADES ---

        private void CompleteJob()
        {
            if (_bot.CurrentJob != null) _bot.CurrentJob.Complete();
            
            _bot.CurrentJob = null;
            _bot.ChangeState(_bot.StateIdle);
        }

        private void ResetState()
        {
            _isDepositing = false;
            _targetStorage = null;
            _currentStrategy = null;
        }

        public override void Exit()
        {
            if (_currentStrategy != null) _currentStrategy.OnCancel(_bot);
            _bot.StopMoving();
            ResetState();
        }
    }
}
