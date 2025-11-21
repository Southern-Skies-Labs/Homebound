using UnityEngine;


namespace  Homebound.Features.TaskSystem
{
    public enum JobType
    {
        Idle = 0,
        Haul = 10, //Transportar
        Chop = 20, // Talar
        Build = 30, // Construir
        Craft = 40 // Fabricar
    }
    
    /// <summary>
    /// DTO que representan las ordenes de trabajo
    /// </summary>
    
    [System.Serializable]
    public class JobRequest
    {
        
        //Variables
        public string JobName;
        public JobType JobType;
        public Vector3 Position;
        public Transform TargetObject;
        public int Priority;
        public bool IsClaimed;
        
        //Metodos
        public JobRequest(string name, JobType type, Vector3 pos, Transform targetObject, int priority)
        {
            JobName = name;
            JobType = type;
            Position = pos;
            TargetObject = targetObject;
            Priority = priority;
            IsClaimed = false;
        }
    }
}