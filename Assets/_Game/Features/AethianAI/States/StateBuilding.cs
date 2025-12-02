using UnityEngine;
using Homebound.Features.Construction;
using Homebound.Features.AethianAI.States;

namespace Homebound.Features.AethianAI.States
{
    public class StateBuilding : AethianState
    {
        //Variables
        private ConstructionSite _targetSite;
        private float _workTimer;
        private const float BUILD_INTERVAL = 2.0f;
        
        public StateBuilding(AethianBot bot) : base(bot) {}
        
        
        //Metodos
        public override void Enter()
        {
            if (_bot.CurrentJob != null && _bot.CurrentJob.TargetObject != null)
            {
                _targetSite = _bot.CurrentJob.TargetObject.GetComponent<ConstructionSite>();
            }

            if (_targetSite == null)
            {
                _bot.ChangeState(_bot.StateIdle);
                return;
            }
            
            _bot.MoveTo(_targetSite.transform.position);
        }

        public override void Tick()
        {
            if (_targetSite == null)
            {
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            if (!_bot.HasReachedDestination())
            {
                return;
            }
            
            _bot.StopMoving();

            Vector3 lookDir = _targetSite.transform.position - _bot.transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                _bot.transform.rotation = Quaternion.Slerp(_bot.transform.rotation, Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 5f);
            }
            
            _workTimer += Time.deltaTime;
            if (_workTimer >= BUILD_INTERVAL)
            {
                _workTimer = 0f;
                PerformBuildStep();
            }
        }

        private void PerformBuildStep()
        {
            bool finished = !_targetSite.ConstructBlock();
            Debug.Log($"[Aethian] {_bot.name} colocó un bloque/avanzo la obra");
            _bot.Stats.Energy.Value -= 2f;

            if (finished || _targetSite.IsFinishedOrStalled())
            {
                _bot.CurrentJob.Complete();
                _bot.ChangeState(_bot.StateIdle);
            }
        }
        
        public override void Exit()
        {
            _targetSite = null;
            _workTimer = 0f;
        }
    }
}
