using System.Collections.Generic;
using UnityEngine;
using Homebound.Core;
using Homebound.Features.TimeSystem;

namespace Homebound.Features.TimeSystem
{
    public class TimeEventSystem : MonoBehaviour
    {
        private TimeManager _timeManager;
        private List<ScheduledEvent> _pendingEvents = new List<ScheduledEvent>();
        
        private List<ScheduledEvent> _eventsToFire = new List<ScheduledEvent>(); 

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _timeManager = ServiceLocator.Get<TimeManager>();
            if (_timeManager != null)
            {
                _timeManager.OnMinuteChanged += CheckEvents;
            }
            else
            {
                Debug.LogError("[TimeEventSystem] No se encontró TimeManager. El Scheduler no funcionará.");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TimeEventSystem>();
            if (_timeManager != null)
            {
                _timeManager.OnMinuteChanged -= CheckEvents;
            }
        }

        //Api Publica
        
        public void ScheduleEvent(string id, GameDateTime absoluteTime, System.Action callback)
        {
            if (IsTimeInPast(absoluteTime))
            {
                Debug.LogWarning($"[TimeEventSystem] Intentando programar evento '{id}' en el pasado. Se ejecutará inmediatamente.");
                callback?.Invoke();
                return;
            }

            var newEvent = new ScheduledEvent(id, absoluteTime, callback);
            _pendingEvents.Add(newEvent);
            // Debug.Log($"[TimeEventSystem] Evento '{id}' programado para: {absoluteTime}");
        }

        
        public void ScheduleEventRelative(string id, int hoursFromNow, int minutesFromNow, System.Action callback)
        {
            if (_timeManager == null)
            {
                _timeManager = ServiceLocator.Get<TimeManager>();
            }
            
            if (_timeManager == null) 
            {
                Debug.LogError($"[TimeEventSystem] Error al programar '{id}': TimeManager no encontrado.");
                return;
            }
            
            long totalMinutesToAdd = (hoursFromNow * 60) + minutesFromNow;
            GameDateTime targetTime = AddMinutesToCurrent(totalMinutesToAdd);

            ScheduleEvent(id, targetTime, callback);
        }

        public void CancelEvent(string id)
        {
            _pendingEvents.RemoveAll(e => e.ID == id);
        }

        

        private void CheckEvents(GameDateTime currentTime)
        {
            if (_pendingEvents.Count == 0) return;

            _eventsToFire.Clear();
            
            for (int i = _pendingEvents.Count - 1; i >= 0; i--)
            {
                var evt = _pendingEvents[i];
                if (currentTime >= evt.FireTime)
                {
                    _eventsToFire.Add(evt);
                    _pendingEvents.RemoveAt(i);
                }
            }
            
            foreach (var evt in _eventsToFire)
            {
                // Debug.Log($"[TimeEventSystem] Disparando evento: {evt.ID}");
                evt.Callback?.Invoke();
            }
        }

        
        private GameDateTime AddMinutesToCurrent(long minutesToAdd)
        {
            GameDateTime tempTime = _timeManager.CurrentTime;
            
            int currentMin = tempTime.Minute + (int)minutesToAdd;
            int currentHour = tempTime.Hour;
            
            while (currentMin >= 60) { currentMin -= 60; currentHour++; }
            while (currentHour >= 24) { currentHour -= 24; tempTime.Day++; }
            
            return new GameDateTime(tempTime.Year, tempTime.Season, tempTime.Day, currentHour, currentMin);
        }

        private bool IsTimeInPast(GameDateTime target)
        {
            if (_timeManager == null) return false;
            return _timeManager.CurrentTime > target;
        }
    }
}