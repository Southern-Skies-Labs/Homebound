using UnityEngine;

namespace Homebound.Features.AethianAI
{
    public class StateSurvival : AethianState
    {
        public StateSurvival(AethianBot bot) : base(bot){}

        public override void Enter()
        {
            Debug.Log("[Aethian] Buscando comida desesperadamente...");
            //Logica para buscar comida
            
        }

        public override void Tick()
        {
            //Esto es para simular, ya que el bot recuperará comida magicamente
            _bot.Stats.Hunger += 10f * Time.deltaTime;

            if (_bot.Stats.Hunger >= 90f)
            {
                Debug.Log("[Aethian] Estomago lleno, volviendo al trabajo");
                _bot.ChangeState(_bot.StateIdle);
            }

        }

    }

}