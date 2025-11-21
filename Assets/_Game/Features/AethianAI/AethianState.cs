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
        
        //Se ejecuta al entrar al estado
        public virtual void Enter(){}

        //Se ejecuta cada frame
        public abstract void Tick();
        
        //Se ejecuta al salir del estado
        public virtual void Exit(){}

    }
}