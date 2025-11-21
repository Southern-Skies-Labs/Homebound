using UnityEngine;
using Homebound.Features.AethianAI;
using Homebound.Features.TaskSystem;
using Homebound.Core;

namespace Homebound.Tests
{
    public class AethianAITest : MonoBehaviour
    {
        [SerializeField] private AethianBot _testBot;

        private void Start()
        {
            // Crear una tarea falsa para ver si el Bot la toma
            Invoke(nameof(CreateTestJob), 2.0f);
        }

        private void CreateTestJob()
        {
            var manager = ServiceLocator.Get<JobManager>();
            if (manager != null)
            {
                Debug.Log("[Test] Publicando tarea de prueba...");
                // Creamos una tarea en la posición (10, 0, 10)
                var job = new JobRequest("Tarea Test Construcción", JobType.Build, new Vector3(10, 0, 10), null, 50);
                manager.PostJob(job);
            }
        }
        
        // Instrucciones para el tester humano:
        // 1. Observa al Bot en Idle.
        // 2. A los 2 segundos, debería tomar el trabajo y moverse.
        // 3. Espera a que su hambre baje de 20. Debería abandonar todo y entrar en Survival.
    }
}