using UnityEngine;
using System; 

namespace Homebound.Features.TaskSystem
{
    public enum JobType
    {
        Idle = 0,
        Haul = 10,
        Chop = 20,
        Build = 30,
        Craft = 40
    }
    
    [System.Serializable]
    public class JobRequest
    {
        // Variables
        public string JobName;
        public JobType JobType;
        public Vector3 Position;
        public Transform TargetObject; // Tu usas Transform, perfecto.
        public int Priority;
        public bool IsClaimed;
        
        // Callback: Avisa a quien creó la tarea que ya terminó
        public Action<JobRequest> OnCompleted; 

        //Metodos
        public JobRequest(string name, JobType type, Vector3 pos, Transform targetObject, int priority, Action<JobRequest> onCompleted = null)
        {
            JobName = name;
            JobType = type;
            Position = pos;
            TargetObject = targetObject;
            Priority = priority;
            OnCompleted = onCompleted;
            IsClaimed = false;
        }
        
        public void ForceCancel()
        {
            IsClaimed = true; 
            OnCompleted = null; // Rompemos la referencia para evitar errores
        }
        
        public void Complete()
        {
            OnCompleted?.Invoke(this);
        }
    }
}