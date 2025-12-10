using UnityEngine;
using Homebound.Core;
using Homebound.Features.Navigation.FailSafe;
using Homebound.Features.Economy;

namespace Homebound.Features.AethianAI.FailSafe
{
    public class AethianFailSafeStrategy : MonoBehaviour, IFailSafeStrategy
    {
        public bool CanUseEmergencyLadders => true;

        public Vector3 GetSafePosition()
        {
            GameObject banner = GameObject.FindGameObjectWithTag("CityBanner");
            if (banner != null) return banner.transform.position;
            
            //Si mas tarde decido implementar como CityManager, queda listo el codigo para invocarlo
            // var cityManager = ServiceLocator.Get<CityManager>();
            // if (cityManager != null) return cityManager.MainBannerPosition;
            
            Debug.LogWarning($"[AethianFailSafe] No se encontró Banner. Usando posición actual + offset vertical seguro.");

            return transform.position + Vector3.up * 5f;
        }
    }
}
