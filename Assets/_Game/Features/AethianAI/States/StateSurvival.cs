using UnityEngine;
using Homebound.Core;
using Homebound.Features.Economy;

namespace Homebound.Features.AethianAI
{
    public class StateSurvival : AethianState
    {
        private CityInventory _cityInventory;
        
        public StateSurvival(AethianBot bot) : base(bot){}

        // ReSharper disable Unity.PerformanceAnalysis
        public override void Enter()
        {
            _cityInventory = ServiceLocator.Get<CityInventory>();
            Debug.Log("[Aethian] Buscando comida desesperadamente...");
            //Logica para buscar comida

            TryEatFromCity();

        }

        // ReSharper disable Unity.PerformanceAnalysis
        public override void Tick()
        {

            if (!_bot.Stats.Hunger.IsCritical())
            {
                Debug.Log("[Aethian] Estomago lleno, volviendo al trabajo");
                _bot.ChangeState(_bot.StateIdle);
            }

        }
        
        private void TryEatFromCity()
        {
            Debug.Log("[Survival] Comiendo ración de emergencia...");
            _bot.Stats.Hunger.Restore(50f);
        }

    }

}