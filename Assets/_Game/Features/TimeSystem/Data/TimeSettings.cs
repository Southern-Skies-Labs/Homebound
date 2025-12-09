using UnityEngine;

namespace Homebound.Features.TimeSystem
{
    [CreateAssetMenu(fileName = "TimeSettings", menuName = "Homebound/Time/Time Settings")]
    public class TimeSettings : ScriptableObject
    {
        //Variables
        [Header("Time Flow Config")] 
        [Tooltip("Duración en segundos reales de una hora en el juego")] [Min(0.1f)]
        public float RealSecondsPerGameHour = 30f;

        [Header("Calendar Config")] 
        [Tooltip("Cuantos días tiene una estación")] [Min(1)]
        public int DaysPerSeason = 30;

        [Tooltip("A que hora comienza el amanecer (0 a 23 hrs")] [Range(0, 23)]
        public int SunriseHour = 6;

        [Tooltip("A que hora comienza el anochecer (0 a 23 hrs")] [Range(0, 23)]
        public int SunsetHour = 18;

        [Header("Starting Point")] 
        public int StartYear = 1;
        public Season StartSeason = Season.Spring;
        public int StartDay = 1;
        public int StartHour = 8;

        public int MinutesPerDay => 24 * 60;
        public int DaysPerYear => DaysPerSeason * 4;
    }

}