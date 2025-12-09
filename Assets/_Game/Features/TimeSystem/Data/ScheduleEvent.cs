using System;
using UnityEngine;

namespace Homebound.Features.TimeSystem
{
    public class ScheduledEvent
    {
        public string ID;
        public GameDateTime FireTime;
        public Action Callback;
        public bool IsRepeated;
        public GameDateTime RepeatInterval;

        public ScheduledEvent(string id, GameDateTime fireTime, Action callback)
        {
            ID = id;
            FireTime = fireTime;
            Callback = callback;
        }

    }

}