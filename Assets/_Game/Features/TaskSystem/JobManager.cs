using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Homebound.Core;


namespace Homebound.Features.TaskSystem
{
    public class JobManager : MonoBehaviour
    {
        private List<JobRequest> _allJobs = new List<JobRequest>();

        private void Awake() => ServiceLocator.Register(this);
        private void OnDestroy() => ServiceLocator.Unregister<JobManager>();

        // --- API DE GESTIÓN ---

        public void PostJob(JobRequest job)
        {
            if (job == null) return;
            _allJobs.Add(job);
            // Debug.Log($"[JobManager] Nueva tarea: {job.JobName} (Prio: {job.Priority})");
        }

        public void CancelJob(JobRequest job)
        {
            if (job == null) return;
            
            job.Cancel();
            
            // Si tiene dueño, el dueño se enterará en su siguiente Tick o podemos forzarlo aquí.
            // Por simplicidad y desacoplamiento, dejamos que el bot detecte la cancelación.
            
            _allJobs.Remove(job);
            Debug.Log($"[JobManager] Tarea cancelada: {job.JobName}");
        }

        // --- API DE MATCHMAKING (CEREBRO) ---

        // El Bot pide: "Dame el mejor trabajo para MÍ"
        public JobRequest GetBestJobFor(IJobWorker bot)
        {
            if (_allJobs.Count == 0) return null;

            JobRequest bestJob = null;
            float bestScore = float.MinValue;
            Vector3 botPos = bot.Position;

            // Recorremos todas las tareas (Optimización futura: Spatial Partitioning)
            // Usamos un bucle for reverso por si necesitamos limpiar tareas nulas
            for (int i = _allJobs.Count - 1; i >= 0; i--)
            {
                JobRequest job = _allJobs[i];

                // 1. Limpieza: Si fue cancelada o completada externamente, la sacamos
                if (job.IsCancelled || job.IsCompleted) 
                {
                    _allJobs.RemoveAt(i);
                    continue;
                }

                // 2. Filtros Duros (Hard Constraints)
                if (job.IsClaimed) continue; // Ya tiene dueño
                if (job.RequiredClass != bot.Class) continue; // No es mi clase (Villager vs Builder)

                // 3. Cálculo de Puntuación (Score)
                // Score = Prioridad - Distancia
                // Un punto de Prioridad vale mucho más que 1 metro de distancia.
                // Ajustamos los pesos: Prioridad * 10 vs Distancia * 1.
                
                float distance = Vector3.Distance(botPos, job.Position);
                float score = (job.Priority * 50.0f) - distance; 

                // Guardamos el mejor
                if (score > bestScore)
                {
                    bestScore = score;
                    bestJob = job;
                }
            }

            // Si encontramos uno, lo asignamos
            if (bestJob != null)
            {
                bestJob.Claim(bot);
            }

            return bestJob;
        }

        // Método para devolver una tarea (si falló el camino)
        public void ReturnJob(JobRequest job)
        {
            if (job != null && !job.IsCancelled)
            {
                job.ReturnToQueue();
                // Opcional: Bajar prioridad temporalmente o marcar como "inalcanzable" para este bot
            }
        }
    }
}