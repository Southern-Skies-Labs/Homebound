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

                case JobType.Build:
                    return new BuildJobStrategy();

                default:
                    return null;
            }

        }
    }
}
