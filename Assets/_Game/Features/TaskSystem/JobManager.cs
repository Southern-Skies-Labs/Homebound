using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.TaskSystem
{
    public class JobManager : MonoBehaviour
    {
        [Header("Job Settings")]
        [Tooltip("La profesión requerida para trabajos de minería (ej: Villager o Miner)")]
        [SerializeField] private UnitClassDefinition _minerClassDef;

        private List<JobRequest> _allJobs = new List<JobRequest>();

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister<JobManager>();

        // --- MÉTODO CORREGIDO ---
        public void CreateMiningRequest(Vector3 position)
        {
            if (_minerClassDef == null)
            {
                Debug.LogError("[JobManager] Error: No has asignado '_minerClassDef' en el inspector.");
                return;
            }

            // CORRECCIÓN: Usamos el constructor exacto que pide el compilador (6 argumentos)
            // Firma: (string, JobType, Vector3, Transform, int, UnitClassDefinition)
            JobRequest miningJob = new JobRequest(
                "Mining Command",       // 1. Nombre
                JobType.Mine,           // 2. Tipo (Asumiendo que JobType.Mine existe)
                position,               // 3. Posición
                null,                   // 4. Target Transform (Null porque es un bloque estático)
                10,                     // 5. Prioridad
                _minerClassDef          // 6. Clase Requerida
            );

            PostJob(miningJob);
            // Debug.Log($"[JobManager] Orden de minería creada en {position}");
        }
        // ------------------------

        public void PostJob(JobRequest job)
        {
            if (job == null) return;
            _allJobs.Add(job);
        }

        public void CancelJob(JobRequest job)
        {
            if (job == null) return;
            job.Cancel();
            _allJobs.Remove(job);
            Debug.Log($"[JobManager] Tarea cancelada: {job.JobName}");
        }

        public JobRequest GetBestJobFor(IJobWorker bot)
        {
            if (_allJobs.Count == 0) return null;

            JobRequest bestJob = null;
            float bestScore = float.MinValue;
            Vector3 botPos = bot.Position;

            for (int i = _allJobs.Count - 1; i >= 0; i--)
            {
                JobRequest job = _allJobs[i];

                if (job.IsCancelled || job.IsCompleted)
                {
                    _allJobs.RemoveAt(i);
                    continue;
                }

                if (job.IsClaimed) continue;

                if (job.RequiredClass != null && job.RequiredClass != bot.Class) continue;

                float distance = Vector3.Distance(botPos, job.Position);
                float score = (job.Priority * 50.0f) - distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestJob = job;
                }
            }

            if (bestJob != null)
            {
                bestJob.Claim(bot);
            }

            return bestJob;
        }

        public void ReturnJob(JobRequest job)
        {
            if (job != null && !job.IsCancelled)
            {
                job.ReturnToQueue();
            }
        }
    }
}