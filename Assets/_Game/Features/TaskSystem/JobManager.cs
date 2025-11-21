using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Homebound.Core;

namespace Homebound.Features.TaskSystem
{
    public class JobManager : MonoBehaviour
    {
        //Variables
        private List<JobRequest> _pendingJobs = new List<JobRequest>();
        
        private void Awake()
        {
            //Registro del servicio para acceso global
            ServiceLocator.Register<JobManager>(this);
            Debug.Log("[JobManager] Inicializado y registrado");
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Unregister<JobManager>();
            Debug.Log("[JobManager] Desregistrado");
        }
        
        // --- API PUBLICA ---

        public void PostJob(JobRequest newJob)
        {
            if (newJob == null) return;
            _pendingJobs.Add(newJob);
            //Debug.Log($"[JobManager] Se agrego la orden {newJob.JobName}");
        }
        
        public JobRequest GetAvailableJob()
        {
            //Logica de prioridad: 1.- Filtrar no reclamados. 2.- Ordenar por prio descendente. 3.- Tomar el primero.
            var job = _pendingJobs
                .Where(j => !j.IsClaimed)
                .OrderByDescending(j => j.Priority)
                .FirstOrDefault();
            if (job != null)
            {
                job.IsClaimed = true; //Con esto bloqueamos la tarea
            }
            return job;
        }

        public void CancelJob(JobRequest job)
        {
            if (_pendingJobs.Contains(job))
            {
                _pendingJobs.Remove(job);
            }
        }

        public void CompleteJob(JobRequest job)
        {
            if (_pendingJobs.Contains(job))
            {
                _pendingJobs.Remove(job);
                
            }
        }
    }
}
