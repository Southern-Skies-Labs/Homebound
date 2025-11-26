using UnityEngine;

namespace Homebound.Features.AethianAI
{
    public class StateSleep : AethianState
    {
        public StateSleep(AethianBot bot) : base(bot){}
        
        public override void Enter()
        {
            // Debug.Log("[Sleep] ZzzZzz... Durmiendo en el suelo");
            _bot.StopMoving();
        }
        
        public override void Tick()
        {
            _bot.Stats.Energy.Restore(10f * Time.deltaTime);   
            
            var timeManager = Homebound.Core.ServiceLocator.Get<Homebound.Features.TimeSystem.TimeManager>();
            bool isDay = timeManager.CurrentHour >= 6 && timeManager.CurrentHour < 20;
            
            if(_bot.Stats.Energy.Value >= 99f && isDay)
            {
                Debug.Log("[Sleep] Despertando con energia a tope");
                _bot.ChangeState(_bot.StateIdle);
            }
            
        }
    
    }

}
