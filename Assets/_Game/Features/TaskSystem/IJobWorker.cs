using UnityEngine;

namespace Homebound.Features.TaskSystem
{
    public interface IJobWorker
    {
        Vector3 Position { get; }
        UnitClassDefinition Class { get; }
    }
}