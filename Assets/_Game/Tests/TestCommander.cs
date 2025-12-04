using UnityEngine;
using Homebound.Core;
using Homebound.Features.TaskSystem;
using Homebound.Features.Navigation;

namespace Homebound.Tests
{
    public class TestCommander : MonoBehaviour
    {
        [Header("Configuración de Test")]
        [SerializeField] private Transform _moveTarget; // Arrastra un objeto vacío aquí como destino
        [SerializeField] private Transform _gatherTarget; // Arrastra un árbol aquí

        private void Update()
        {
            // TEST 1: Movimiento (Tecla M)
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (_moveTarget == null) 
                {
                    Debug.LogWarning("[Test] Asigna un _moveTarget en el inspector.");
                    return;
                }

                SendMoveJob(_moveTarget.position);
            }

            // TEST 2: Recolección (Tecla T)
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (_gatherTarget == null)
                {
                    Debug.LogWarning("[Test] Asigna un _gatherTarget en el inspector.");
                    return;
                }
                
                SendChopJob(_gatherTarget);
            }
        }

        private void SendMoveJob(Vector3 pos)
        {
            var jobManager = ServiceLocator.Get<JobManager>();
            if (jobManager == null)
            {
                Debug.LogError("[Test] JobManager no encontrado.");
                return;
            }

            // Creamos la tarea con el nuevo formato TaskSystem 2.0
            JobRequest job = new JobRequest(
                "Test Mover",
                JobType.Move, // Asegúrate que este tipo exista en JobDefinitions, o usa Haul
                pos,
                null,
                50, // Prioridad alta
                UnitClass.Villager
            );

            jobManager.PostJob(job);
            Debug.Log($"[Test] Orden de movimiento enviada a {pos}");
        }

        private void SendChopJob(Transform target)
        {
            var jobManager = ServiceLocator.Get<JobManager>();
            if (jobManager == null) return;

            JobRequest job = new JobRequest(
                "Test Talar",
                JobType.Chop,
                target.position,
                target,
                50,
                UnitClass.Villager
            );

            jobManager.PostJob(job);
            Debug.Log($"[Test] Orden de talar enviada para {target.name}");
        }
    }
}