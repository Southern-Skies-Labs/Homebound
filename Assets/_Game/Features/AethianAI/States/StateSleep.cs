using UnityEngine;
using Homebound.Features.TimeSystem; 

namespace Homebound.Features.AethianAI
{
    public class StateSleep : AethianState
    {
        public StateSleep(AethianBot bot) : base(bot){}
        
        public override void Enter()
        {
            _bot.StopMoving();
        }
        
        public override void Tick()
        {
            _bot.Stats.Energy.Restore(10f * Time.deltaTime);   
            
            var timeManager = Homebound.Core.ServiceLocator.Get<Homebound.Features.TimeSystem.TimeManager>();
            
            // CORRECCIÓN: Usamos .CurrentTime.Hour en lugar de .CurrentHour
            bool isDay = timeManager.CurrentTime.Hour >= 6 && timeManager.CurrentTime.Hour < 20;
            
            if(_bot.Stats.Energy.Value >= 99f && isDay)
            {
                Debug.Log("[Sleep] Despertando con energia a tope");
                _bot.ChangeState(_bot.StateIdle);
            }
        }
    }
}