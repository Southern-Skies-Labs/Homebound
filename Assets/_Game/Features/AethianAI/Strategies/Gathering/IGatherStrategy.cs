using UnityEngine;
using Homebound.Features.TaskSystem;

namespace Homebound.Features.AethianAI.Strategies.Gathering
{
    public interface IGatherStrategy
    {
        /// <summary>
        /// Determina si el trabajo sigue siendo válido (ej: el árbol sigue ahí, el bloque no se ha roto).
        /// </summary>
        bool IsJobValid(JobRequest job);

        /// <summary>
        /// Obtiene la posición a la que debe ir el bot para ejecutar el trabajo.
        /// Puede ser diferente a job.Position (ej: pararse al lado, no encima).
        /// </summary>
        Vector3 GetWorkPosition(JobRequest job, Vector3 botPosition);

        /// <summary>
        /// Ejecuta la acción de trabajo (talar, picar).
        /// </summary>
        /// <returns>True si el trabajo ha terminado (se rompió el bloque/árbol).</returns>
        bool ExecuteWork(AethianBot bot, float deltaTime);

        /// <summary>
        /// Llamado cuando se cancela el trabajo para limpiar temporizadores o estados.
        /// </summary>
        void OnCancel(AethianBot bot);
    }
}
