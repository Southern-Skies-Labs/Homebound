using UnityEngine;
using System;

namespace Homebound.Features.AethianAI
{
    public enum ActivityType
    {
        Sleep = 0,
        Work = 1,
        Leisure = 2 
    }

    [CreateAssetMenu(fileName = "NewSchedule", menuName = "Homebound/AI/Schedule Profile")]
    public class ScheduleProfile : ScriptableObject
    {
        [Header("Rutina Diaria (00:00 a 23:00)")]
        [Tooltip("Define la actividad para cada hora del día.")]
        public ActivityType[] HourlySchedule = new ActivityType[24];

        
        public ActivityType GetActivityAt(int hour)
        {
            int safeHour = Mathf.Clamp(hour, 0, 23);
            return HourlySchedule[safeHour];
        }

        
        [ContextMenu("Set Default Villager Schedule")]
        public void SetDefaultSchedule()
        {
            HourlySchedule = new ActivityType[24];
            
            // 00:00 - 05:00 -> Dormir (6 horas)
            for (int i = 0; i <= 5; i++) HourlySchedule[i] = ActivityType.Sleep;
            
            // 06:00 - 07:00 -> Ocio (Desayuno/Mañana)
            for (int i = 6; i <= 7; i++) HourlySchedule[i] = ActivityType.Leisure;
            
            // 08:00 - 18:00 -> Trabajo (11 horas)
            for (int i = 8; i <= 18; i++) HourlySchedule[i] = ActivityType.Work;
            
            // 18:00 - 21:00 -> Ocio (Tarde social)
            for (int i = 19; i <= 21; i++) HourlySchedule[i] = ActivityType.Leisure;
            
            // 22:00 - 23:00 -> Dormir
            for (int i = 22; i <= 23; i++) HourlySchedule[i] = ActivityType.Sleep;
        }
    }
}