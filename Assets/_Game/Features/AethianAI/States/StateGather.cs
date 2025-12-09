using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Economy;

namespace Homebound.Features.AethianAI
{
    public class StateGather : AethianState
    {
        private ResourceNode _targetNode;
        private StorageContainer _targetStorage;
        
        private float _gatherTimer;
        private bool _isGathering;
        private bool _isDepositing; 

        // Configuración
        private const float INTERACTION_RANGE = 2.5f;
        private const float GATHER_INTERVAL = 1.0f;
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

            FindTargetNode();
        }

        public override void Tick()
        {
            if (_isDepositing)
            {
                HandleDepositSequence();
            }
            else
            {
                HandleGatherSequence();
            }
        }

        //SECUENCIA DE RECOLECCIÓN

        private void HandleGatherSequence()
        {
            if (_targetNode == null)
            {
                StartDepositSequence(); 
                return;
            }
            
            if (!_isGathering)
            {
                float distanceFlat = Vector2.Distance(
                    new Vector2(_bot.Position.x, _bot.Position.z),
                    new Vector2(_targetNode.transform.position.x, _targetNode.transform.position.z)
                );

                if (distanceFlat <= INTERACTION_RANGE)
                {
                    _bot.StopMoving();
                    Vector3 lookTarget = _targetNode.transform.position;
                    lookTarget.y = _bot.Position.y;
                    _bot.transform.LookAt(lookTarget);
                    _isGathering = true;
                }
                else
                {
                    if (!_bot.HasReachedDestination()) 
                        _bot.MoveTo(_targetNode.transform.position);
                }
            }
            else
            {
                _gatherTimer += Time.deltaTime;
                if (_gatherTimer >= GATHER_INTERVAL)
                {
                    _gatherTimer = 0f;
                    PerformGatherHit();
                }
            }
        }

        private void PerformGatherHit()
        {
            if (_targetNode == null) return;
            
            float damage = _bot.Stats.GatheringPower; 
            int amount = _targetNode.Gather(damage); 

            if (amount > 0)
            {
                if (_bot.TryGetComponent(out UnitInventory inventory))
                {
                    ItemData itemType = _targetNode.GetDrop().Item; 
                    inventory.Add(itemType, amount);
                    
                    int currentCount = inventory.Count(itemType);
                    if (currentCount >= MAX_CARRY_AMOUNT)
                    {
                        // Debug.Log($"[StateGather] Inventario lleno ({currentCount}). Regresando a depositar.");
                        StartDepositSequence();
                        return;
                    }
                }
            }
            
            if (_targetNode.IsDepleted)
            {
                StartDepositSequence();
            }
        }

        //SECUENCIA DE DEPÓSITO

        private void StartDepositSequence()
        {
            _isDepositing = true;
            _isGathering = false;
            _bot.StopMoving();
            
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
                Debug.LogWarning($"[StateGather] {_bot.name} no encontró almacén. Se queda con los recursos.");
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
                    bool transferSuccess = inventory.TransferAllTo(_targetStorage);
                    if (transferSuccess)
                    {
                        // Debug.Log($"[StateGather] Recursos depositados en {_targetStorage.name}.");
                    }
                }
                
                if (_targetNode != null && !_targetNode.IsDepleted)
                {
                    _isDepositing = false;
                    _targetStorage = null;
                    _bot.MoveTo(_targetNode.transform.position);
                }
                else
                {
                    CompleteJob();
                }
            }
        }

        //UTILIDADES

        private void FindTargetNode()
        {
            Collider[] hits = Physics.OverlapSphere(_bot.CurrentJob.Position, 1.0f);
            foreach (var hit in hits)
            {
                var node = hit.GetComponentInParent<ResourceNode>();
                if (node != null)
                {
                    _targetNode = node;
                    break;
                }
            }

            if (_targetNode == null)
            {
                CompleteJob();
                return;
            }

            _bot.MoveTo(_targetNode.transform.position);
        }

        private void CompleteJob()
        {
            if (_bot.CurrentJob != null) _bot.CurrentJob.Complete();
            
            _bot.CurrentJob = null;
            _targetNode = null;
            _bot.ChangeState(_bot.StateIdle);
        }

        private void ResetState()
        {
            _isGathering = false;
            _isDepositing = false;
            _gatherTimer = 0f;
            _targetNode = null;
            _targetStorage = null;
        }

        public override void Exit()
        {
            _bot.StopMoving();
            ResetState();
        }
    }
}