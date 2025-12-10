using UnityEngine;

namespace Homebound.Features.Navigation.FailSafe
{
    /// <summary>
    /// Esto define el comportamiento de seguridad especifico para cada entidad del juego. Por ejemplo, si tiene
    /// inteligencia o la capacidad de usar escaleras y si puede o no utilizar el teleport en caso de fallo catastrofico
    /// en relación con su posicionamiento.
    /// </summary>
    public interface IFailSafeStrategy
    {
        bool CanUseEmergencyLadders { get; }
        Vector3 GetSafePosition();
    }
}