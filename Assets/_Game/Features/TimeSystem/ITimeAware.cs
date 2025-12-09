namespace Homebound.Features.TimeSystem
{
    public interface ITimeAware
    {
        void OnTimeSkipped(long minutesSkipped);

    }

}