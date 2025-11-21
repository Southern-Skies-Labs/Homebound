using UnityEngine;

namespace Homebound.Features.AethianAI
{
    public class StateWorking : AethianState
    {
        public StateWorking(AethianBot bot) : base(bot){}
        
        public override void Enter()
        {
            if (_bot.CurrentJob != null)
            {
                Debug.Log($"[Aethian] Yendo a trabajar: {_bot.CurrentJob.JobName}");
                _bot.Agent.SetDestination(_bot.CurrentJob.Position);
            }   
        }
        
        public override void Tick()
        {
            if (_bot.CurrentJob == null)
            {
                _bot.ChangeState(_bot.StateIdle);
                return;
            }

            if (!_bot.Agent.pathPending && _bot.Agent.remainingDistance <= _bot.Agent.stoppingDistance)
            {
                Debug.Log("[Aethian] Trabajo terminado (Simulado)");
                _bot.CurrentJob = null;
                _bot.ChangeState(_bot.StateIdle);
            }
        }
    }
}
