using Homebound.Features.TaskSystem;


namespace Homebound.Features.AethianAI.Strategies
{
    public class JobStrategyFactory
    {
        public static IJobStrategy CreateStrategy(JobType type)
        {
            switch (type)
            {
                case JobType.Mine:
                    return new MiningJobStrategy();

                default:
                    return null;
            }

        }
    }
}
