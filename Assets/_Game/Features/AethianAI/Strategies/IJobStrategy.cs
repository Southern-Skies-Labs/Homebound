using UnityEngine;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.AethianAI.Strategies
{
    public interface IJobStrategy
    {
        /// <param name="bot">Referencia al bot que ejecuta la acción</param>
        /// <param name="deltaTime">Tiempo transcurrido</param>
        void Execute(AethianBot bot, float deltaTime);

        void OnCancel(AethianBot bot);
    }
}