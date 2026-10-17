using System;
using UnityEngine;

namespace LD60.Settings
{
    [Serializable]
    public class GameDate
    {
        private const double MaxDaysFromEpoch = 700000d;

        private static readonly DateTime Epoch = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [SerializeField] private int _year = 2000;
        [SerializeField] private int _month = 1;
        [SerializeField] private int _day = 1;

        public GameDate()
        {
        }

        public GameDate(int year, int month, int day)
        {
            _year = year;
            _month = month;
            _day = day;
        }

        public int Year
        {
            get { return _year; }
        }

        public int Month
        {
            get { return _month; }
        }

        public int Day
        {
            get { return _day; }
        }

        public double DaysSinceJ2000
        {
            get { return ToDaysSinceJ2000(ToDateTime()); }
        }

        public DateTime ToDateTime()
        {
            int year = Mathf.Clamp(_year, 1, 9999);
            int month = Mathf.Clamp(_month, 1, 12);
            int day = Mathf.Clamp(_day, 1, DateTime.DaysInMonth(year, month));

            return new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
        }

        public static double ToDaysSinceJ2000(DateTime value)
        {
            return (value - Epoch).TotalDays;
        }

        public static DateTime FromDaysSinceJ2000(double days)
        {
            if (double.IsNaN(days))
                return Epoch;

            double clamped = days;

            if (clamped < -MaxDaysFromEpoch)
                clamped = -MaxDaysFromEpoch;
            else if (clamped > MaxDaysFromEpoch)
                clamped = MaxDaysFromEpoch;

            return Epoch.AddDays(clamped);
        }
    }
}
