using System;
using Homebound.Features.TimeSystem;
using UnityEngine;

namespace Homebound.Features.TimeSystem
{
    [Serializable]
    public class GameDateTime : IComparable<GameDateTime>, IEquatable<GameDateTime>
    {
        //variables
        public int Year;
        public Season Season;
        public int Day;
        public int Hour;
        public int Minute;
        
        //Constructor
        public GameDateTime(int year, Season season, int day, int hour, int minute)
        {
            Year = year;
            Season = season;
            Day = day;
            Hour = hour;
            Minute = minute;
        }
        
        //Metodos & Helpers
        public long TotalGameMinutes
        {
            get
            {
                long total = 0;
                total += Minute;
                total += Hour * 60;
                total += (Day - 1) * 24 * 60;
                total += (int)Season * 30 * 24 * 60;
                total += Year * 4 * 30 * 24 * 60;
                return total;
            }
        }

        #region Operators & Comparison

        public int CompareTo(GameDateTime other)
        {
            return TotalGameMinutes.CompareTo(other.TotalGameMinutes);
        }

        public bool Equals(GameDateTime other)
        {
            return TotalGameMinutes == other.TotalGameMinutes;
        }
        
        public static bool operator >(GameDateTime a, GameDateTime b) => a.TotalGameMinutes > b.TotalGameMinutes;
        public static bool operator <(GameDateTime a, GameDateTime b) => a.TotalGameMinutes < b.TotalGameMinutes;
        public static bool operator >=(GameDateTime a, GameDateTime b) => a.TotalGameMinutes >= b.TotalGameMinutes;
        public static bool operator <=(GameDateTime a, GameDateTime b) => a.TotalGameMinutes <= b.TotalGameMinutes;
        public static bool operator ==(GameDateTime a, GameDateTime b) => a.TotalGameMinutes == b.TotalGameMinutes;
        public static bool operator !=(GameDateTime a, GameDateTime b) => a.TotalGameMinutes != b.TotalGameMinutes;

        public override bool Equals(object obj)
        {
            return obj is GameDateTime other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Season, Day, Hour, Minute);
        }

        public override string ToString()
        {
            return $"Y{Year} {Season} D{Day} - {Hour:D2}:{Minute:D2}";
        }

        #endregion
        
    }
}