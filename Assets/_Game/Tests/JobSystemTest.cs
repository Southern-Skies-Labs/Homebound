using System;
using UnityEngine;
using Homebound.Core;

namespace Homebound.Features.TaskSystem
{
    public class JobSystemTest : MonoBehaviour
    {
        private void Start()
        {
            //Esperamos un frame para asegurar que se inicia de manera correcta
            Invoke(nameof(TestLogic), 0.1f);
        }
        
        private void TestLogic()
        {
            var manager = ServiceLocator.Get<JobManager>();
            
            //Usamos esto para crear dos tareas con diferentes prioridades
            var jobLow = new JobRequest("Recolectar items del suelo", JobType.Haul, Vector3.zero, null, 10);
            var jobHigh = new JobRequest("Construir Muro", JobType.Build, Vector3.zero, null, 100);
            
            //Las publicamos en desorden para asegurar que está tomando la bien las prioridades
            manager.PostJob(jobLow);
            manager.PostJob(jobHigh);
            
            //Pedimos la siguiente tarea
            var bestJob = manager.GetAvailableJob();
            
            //Validamos que la tarea sea la correcta
            if (bestJob != null && bestJob.JobName == "Construir Muro")
            {
                Debug.Log("<color=green> El sistema esta priorizando correctamente ya que completo la tarea de alta importancia " + bestJob.JobName);
            }
            else
            {
                Debug.Log($"<color=red> El sistema no esta priorizando correctamente, se esperaba '{jobHigh}' y se recibió '{bestJob.JobName}' ");
            }
        }
    }
}

