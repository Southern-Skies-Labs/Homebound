using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.AethianAI.States
{
    public class StateIdle : AethianState
    {
        public StateIdle(AethianBot bot) : base(bot) {}
        
        public override void Enter()
        {
         //Detenemos movimiento
         _bot.StopMoving();
        }
        
        public override void Tick()
        {
            //Si tenemos hambre, el Aethianbot forzara el cambio a survival
            var jobManager = ServiceLocator.Get<JobManager>();
            if (jobManager != null)
            {
                var job = jobManager.GetAvailableJob();
                if (job != null)
                {
                    _bot.CurrentJob = job;
                    if (job.JobType == JobType.Chop)
                    {
                        _bot.ChangeState(_bot.StateGather);
                    }
                    else
                    {
                        _bot.ChangeState(_bot.StateWorking);
                    }
                
                }
            }

        }
    }
}

