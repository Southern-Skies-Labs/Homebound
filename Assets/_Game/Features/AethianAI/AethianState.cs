using UnityEngine;

namespace Homebound.Features.AethianAI
{
    public abstract class AethianState
    {
        protected AethianBot _bot;
        
        public AethianState(AethianBot bot)
        {
            _bot = bot;
        }
        
        public virtual void Enter(){}

        public abstract void Tick();
        
        public virtual void Exit(){}

    }
}