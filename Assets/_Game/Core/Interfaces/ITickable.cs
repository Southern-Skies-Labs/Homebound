using UnityEngine;

namespace Homebound.Core
{
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}

