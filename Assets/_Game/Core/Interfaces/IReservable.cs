using UnityEngine;

namespace Homebound.Features.Economy
{
    public interface IReservable
    {
        bool CanReserve { get; }
        int MaxWorkers { get; }
        int CurrentWorkers { get; }

        bool Reserve(GameObject worker);
        void Release(GameObject worker);

    }

}