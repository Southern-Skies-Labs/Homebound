using UnityEngine;

namespace Homebound.Features.AethianAI
{
    public class StateWorking : AethianState
    {
        //Variables
        private float _workTimer;
        private bool _isInteracting;
        public StateWorking(AethianBot bot) : base(bot){}
        
        
        
        //Metodos
        public override void Enter()
        {
            _isInteracting = false;
            _workTimer = 0f;
            
            
            if (_bot.CurrentJob != null)
            {
                if (!_bot.IsPathReachable(_bot.CurrentJob.Position))
                {
                    Debug.LogWarning($"[StateWorking] Destino inalcanzable: {_bot.CurrentJob.JobName}. Cancelando tarea");
                    _bot.CurrentJob.IsClaimed = false;
                    _bot.CurrentJob = null;
                    
                    _bot.ChangeState(_bot.StateIdle);
                    return;
                }
                _bot.MoveTo(_bot.CurrentJob.Position);
            }   
        }
        
        public override void Tick()
        {
            if (_bot.CurrentJob == null)
            {
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            if (!_isInteracting)
            {
                if (_bot.HasReachedDestination())
                {
                    _isInteracting = true;
                    _bot.StopMoving();
                }
            }
            else
            {
                _workTimer += Time.deltaTime;
                if (_workTimer >= 3.0f)
                {
                    CompleteJob();
                }
            }
        }

        private void CompleteJob()
        {
            _bot.CurrentJob = null;
            _bot.ChangeState(_bot.StateIdle);
        }
    }
}
