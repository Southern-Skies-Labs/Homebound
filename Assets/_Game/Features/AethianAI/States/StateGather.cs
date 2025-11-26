using System;
using UnityEngine;
using Homebound.Core;
using Homebound.Features.Economy;

namespace Homebound.Features.AethianAI
{
    public class StateGather : AethianState
    {
        //Variables
        private enum GatherPhase{ MovingToNode, Chopping, ReturningToBase}

        private GatherPhase _phase;
        private IGatherable _currentNode;
        private float _chopTimer;
        private const float CHOP_INTERVAL = 1.0f;
        private const float CHOP_POWER = 20f;
        
        //Referencias cacheadas
        private UnitInventory _myInventory;
        private CityInventory _cityInventory;
        
        public StateGather(AethianBot bot) : base(bot) {}
        
        //Metodos
        // ReSharper disable Unity.PerformanceAnalysis
        public override void Enter()
        {
            _myInventory = _bot.GetComponent<UnitInventory>();
            _cityInventory = ServiceLocator.Get<CityInventory>();

            if (_bot.CurrentJob == null || _bot.CurrentJob.TargetObject == null)
            {
                _bot.ChangeState(_bot.StateIdle);
                return;
            }
            
            _currentNode = _bot.CurrentJob.TargetObject.GetComponent<IGatherable>();

            if (_currentNode == null)
            {
                Debug.LogError("[StateGather] El objetivo no es recolectable");
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            _phase = GatherPhase.MovingToNode;
            _bot.MoveTo(_currentNode.GetPosition());
            Debug.Log($"[Gather] Yendo a talar: {_currentNode.Name}");
        }

        public override void Tick()
        {
            switch (_phase)
            {
                case GatherPhase.MovingToNode:
                    if (_bot.HasReachedDestination())
                    {
                        StartChopping();
                    }
                    break;
                case GatherPhase.Chopping:
                    ProcessChopping();
                    break;
                case GatherPhase.ReturningToBase:
                    if (_bot.HasReachedDestination())
                    {
                        DepositResources();
                    }
                    break;
            }
        }

        private void StartChopping()
        {
            _phase = GatherPhase.Chopping;
            _bot.StopMoving();
            _chopTimer = 0f;
            Debug.Log($"[Gather] Comenzando a talar: {_currentNode.Name}");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void ProcessChopping()
        {
            if (_currentNode == null || _currentNode.Transform == null)
            {
                ReturnToBase();
                return;
            }

            _chopTimer += Time.deltaTime;
            while (_chopTimer >= CHOP_INTERVAL)
            {
                _chopTimer -= CHOP_INTERVAL;
                PerformChopHit();

                if (_currentNode == null || _currentNode.Transform == null) return;
            }
        }

        private void PerformChopHit()
        {
            bool destroyed = _currentNode.Gather(CHOP_POWER);
            if (destroyed)
            {
                InventorySlot loot = _currentNode.GetDrop();
                if (_myInventory != null)
                {
                    int leftOver = _myInventory.Add(loot.Item, loot.Amount);
                    if (leftOver > 0) Debug.Log("Mochila llena! Se desperdiciaron recursos");
                    else Debug.Log($"[Gather] Recogido {loot.Amount} de {loot.Item.DisplayName}");
                }

                ReturnToBase();
            }
        }

        private void ReturnToBase()
        {
            _phase = GatherPhase.ReturningToBase;

            if (_cityInventory != null)
            {
                Vector3 basePos = _cityInventory.transform.position;
                
                Vector3 approachPos = basePos + (_bot.transform.position - basePos).normalized * 2f;
                approachPos.y = _bot.transform.position.y;
                
                _bot.MoveTo(approachPos);
                Debug.Log("[Gather] Mochila llena -  Trabajo listo. Volviendo al almacen");
            }
            else
            {
                _bot.ChangeState(_bot.StateIdle);
            }
        }
        
        private void DepositResources()
        {
            Debug.Log("[Gather] Recursos depositados en la ciudad");
            _bot.CurrentJob = null;
            _bot.ChangeState(_bot.StateIdle);
        }
    }

}
