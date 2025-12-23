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