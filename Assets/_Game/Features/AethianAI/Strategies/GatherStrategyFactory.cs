using Homebound.Features.TaskSystem;
using Homebound.Features.AethianAI.Strategies.Gathering;

namespace Homebound.Features.AethianAI.Strategies
{
    public static class GatherStrategyFactory
    {
        public static IGatherStrategy Create(JobType type)
        {
            switch (type)
            {
                case JobType.Mine:
                    return new MiningJobStrategy();

                case JobType.Chop: // Talar y Recolectar usan la misma lógica de "Entidad" por ahora
                case JobType.Gather:
                    return new ResourceGatherStrategy();

                default:
                    // Fallback seguro o null
                    return null;
            }
        }
    }
}
