using UnityEngine;

namespace Homebound.Features.TaskSystem
{
    public enum JobType
    {
        Idle,
        Move,
        Gather,
        Build,
        Haul,
        Craft,
        Chop,
        Mine
    }

    [System.Serializable]
    public class JobRequest
    {
        public string JobName;
        public JobType JobType;

        public UnitClassDefinition RequiredClass;

        public Vector3 Position;
        public Transform Target;
        public int Priority;

        public IJobWorker Owner { get; private set; }

        public bool IsClaimed => Owner != null;
        public bool IsCancelled { get; private set; }
        public bool IsCompleted { get; private set; }

        public JobRequest(string name, JobType type, Vector3 pos, Transform target, int priority, UnitClassDefinition requiredClass)
        {
            JobName = name;
            JobType = type;
            Position = pos;
            Target = target;
            Priority = priority;
            RequiredClass = requiredClass;

            IsCancelled = false;
            IsCompleted = false;
            Owner = null;
        }

        public void Claim(IJobWorker worker)
        {
            Owner = worker;
        }

        public void ReturnToQueue()
        {
            Owner = null;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }

        public void Complete()
        {
            IsCompleted = true;
        }

    }
}