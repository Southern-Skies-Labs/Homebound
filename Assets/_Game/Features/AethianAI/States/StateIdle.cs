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
            
            _bot.StopMoving();
        }
        
        public override void Tick()
        {
            
            var jobManager = ServiceLocator.Get<JobManager>();
            if (jobManager != null)
            {
                var job = jobManager.GetAvailableJob();
                
                if (job != null)
                {
                    
                    _bot.CurrentJob = job;

                    
                    switch (job.JobType)
                    {
                        case JobType.Chop:
                            _bot.ChangeState(_bot.StateGather);
                            break;

                        case JobType.Build:
                            
                            _bot.ChangeState(_bot.StateBuilding); 
                            break;

                        default:
                            
                            _bot.ChangeState(_bot.StateWorking);
                            break;
                    }
                }
            }
        }
    }
}