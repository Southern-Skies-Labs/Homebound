using UnityEngine;
using Homebound.Core;
using Homebound.Features.Economy;
using System.Linq; 

namespace Homebound.Features.AethianAI
{
    public class StateGather : AethianState
    {
        
        private enum GatherPhase { MovingToNode, Working, ReturningToBase, Depositing }

        private GatherPhase _phase;
        
        // Referencias al objetivo actual
        private IGatherable _targetNode;
        private IReservable _targetReservable;
        
        // Configuración de trabajo
        private float _workTimer;
        private const float WORK_INTERVAL = 1.0f; 
        private const float WORK_POWER = 20f;     
        
        // Referencias propias
        private UnitInventory _myInventory;

        public StateGather(AethianBot bot) : base(bot) { }

        public override void Enter()
        {
            _myInventory = _bot.GetComponent<UnitInventory>();

            
            if (_bot.CurrentJob == null || _bot.CurrentJob.Target == null)
            {
                Debug.LogWarning("[StateGather] Tarea inválida o sin objetivo.");
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            
            _targetNode = _bot.CurrentJob.Target.GetComponent<IGatherable>();
            _targetReservable = _bot.CurrentJob.Target.GetComponent<IReservable>();

            if (_targetNode == null)
            {
                Debug.LogError("[StateGather] El objetivo no es 'IGatherable'.");
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            
            if (_targetReservable != null)
            {
                if (!_targetReservable.CanReserve)
                {
                    Debug.Log("[StateGather] El nodo está lleno de trabajadores. Cancelando.");
                    // Aquí idealmente devolveríamos la tarea al JobManager para que otro la tome luego
                    _bot.CurrentJob = null; 
                    _bot.ChangeState(_bot.StateIdle);
                    return;
                }
                
                
                _targetReservable.Reserve(_bot.gameObject);
            }

            // 4. Iniciar movimiento
            _phase = GatherPhase.MovingToNode;
            _bot.MoveTo(_targetNode.Position);
            // Debug.Log($"[StateGather] Viajando a {_targetNode.Name}...");
        }

        public override void Exit()
        {
            
            if (_targetReservable != null)
            {
                _targetReservable.Release(_bot.gameObject);
            }
            
            _bot.StopMoving();
        }

        public override void Tick()
        {
            
            if (_phase != GatherPhase.ReturningToBase && (_targetNode == null || _targetNode.Equals(null)))
            {
                Debug.Log("[StateGather] El nodo desapareció. Volviendo a casa si tengo carga.");
                CheckInventoryAndReturn();
                return;
            }

            switch (_phase)
            {
                case GatherPhase.MovingToNode:
                    if (_bot.HasReachedDestination())
                    {
                        StartWorking();
                    }
                    break;

                case GatherPhase.Working:
                    ProcessWorking();
                    break;

                case GatherPhase.ReturningToBase:
                    if (_bot.HasReachedDestination())
                    {
                        _phase = GatherPhase.Depositing; 
                        DepositLoad();
                    }
                    break;
            }
        }

        private void StartWorking()
        {
            _phase = GatherPhase.Working;
            _bot.StopMoving();
            _workTimer = 0f;
            
            _bot.transform.LookAt(_bot.CurrentJob.Target.position);
        }

        private void ProcessWorking()
        {
            _workTimer += Time.deltaTime;
            
            // Ciclo de golpe
            if (_workTimer >= WORK_INTERVAL)
            {
                _workTimer = 0f;
                PerformGatherHit();
            }
        }

        private void PerformGatherHit()
        {
            if (_targetNode == null || _targetNode.IsDepleted)
            {
                CheckInventoryAndReturn();
                return;
            }

            // 1. Recolección Física (Devuelve int)
            int harvestedAmount = _targetNode.Gather(WORK_POWER);

            // 2. Guardado en Inventario Local
            if (harvestedAmount > 0 && _myInventory != null)
            {
                
                if (_targetNode is ResourceNode concreteNode) 
                {
                    // Hack temporal seguro: obtenemos el tipo del drop configurado
                    var dropInfo = concreteNode.GetDrop(); 
                    int leftovers = _myInventory.Add(dropInfo.Item, harvestedAmount);
                    
                    if (leftovers > 0)
                    {
                        // Mochila llena
                        Debug.Log("[StateGather] ¡Mochila llena!");
                        CheckInventoryAndReturn();
                    }
                }
            }

            
            if (_targetNode.IsDepleted)
            {
                CheckInventoryAndReturn();
            }
        }

        private void CheckInventoryAndReturn()
        {
            
            if (_myInventory != null && !_myInventory.IsEmpty)
            {
                FindAndMoveToStorage();
            }
            else
            {
                CompleteJob();
            }
        }

        private void FindAndMoveToStorage()
        {
            
            
            var allStorages = Object.FindObjectsByType<StorageContainer>(FindObjectsSortMode.None);
            
            
            var nearestStorage = allStorages
                .OrderBy(s => Vector3.Distance(_bot.Position, s.transform.position))
                .FirstOrDefault();

            if (nearestStorage != null)
            {
                _phase = GatherPhase.ReturningToBase;
                _bot.MoveTo(nearestStorage.GetDropOffPoint());
                
            }
            else
            {
                Debug.LogWarning("[StateGather] ¡No hay almacenes! El bot se queda con los recursos.");
                CompleteJob(); 
            }
        }

        private void DepositLoad()
        {
            // Buscamos contenedores cercanos
            var colliders = Physics.OverlapSphere(_bot.Position, 2.5f); // Radio un poco mayor para asegurar contacto
            
            foreach (var col in colliders)
            {
                var container = col.GetComponent<StorageContainer>();
                if (container != null)
                {
                    // ¡ENCONTRADO! Procedemos a transferir
                    Debug.Log($"[StateGather] {_bot.name} depositando en {container.name}...");
                    
                    bool success = _myInventory.TransferAllTo(container);
                    
                    if (success)
                    {
                        Debug.Log($"[StateGather] ✅ Depósito completado con éxito.");
                    }
                    else
                    {
                        Debug.LogWarning($"[StateGather] ⚠️ No se pudo depositar (¿Almacén lleno?).");
                    }
                    
                    break; // Ya encontramos uno, no seguimos buscando
                }
            }

            CompleteJob();
        }

        private void CompleteJob()
        {
            _bot.CurrentJob = null;
            _bot.ChangeState(_bot.StateIdle);
        }
        
        
    }
}