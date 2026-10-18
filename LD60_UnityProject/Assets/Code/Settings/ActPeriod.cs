using System;
using UnityEngine;
using LD60.Space;

namespace LD60.Settings
{
    [Serializable]
    public class ActPeriod
    {
        [field: SerializeField] public string Name { get; private set; } = "";
        [field: SerializeField] public GameDate Start { get; private set; } = new GameDate();
        [field: SerializeField] public GameDate End { get; private set; } = new GameDate();
        [field: SerializeField] public float RealSecondsDuration { get; private set; } = 1200f;

        [field: Tooltip("Watts handed to the transmitter when this act opens, on top of whatever survived the act before. Clamped to the transmitter maximum on the Watt asset.")]
        [field: SerializeField] public float BudgetWatts { get; private set; } = 120f;

        [field: Tooltip("Where every body stands on its orbit when this act begins.")]
        [field: SerializeField] public ActBodyPlacement[] Placements { get; private set; } = new ActBodyPlacement[0];

        [field: Tooltip("Spacecraft present in this act, one asset each.")]
        [field: SerializeField] public ActSpacecraftSO[] Spacecraft { get; private set; } = new ActSpacecraftSO[0];

        public float StartAngleFor(CelestialBodySO body)
        {
            for (int i = 0; i < Placements.Length; i++)
            {
                if (Placements[i] != null && Placements[i].Body == body)
                    return Placements[i].StartAngleDegrees;
            }

            return 0f;
        }

        public double StartDaysSinceJ2000
        {
            get { return Start == null ? 0d : Start.DaysSinceJ2000; }
        }

        public double EndDaysSinceJ2000
        {
            get { return End == null ? 0d : End.DaysSinceJ2000; }
        }

        public double SpanDays
        {
            get { return EndDaysSinceJ2000 - StartDaysSinceJ2000; }
        }

        public double DaysPerSecond
        {
            get
            {
                if (RealSecondsDuration <= 0f)
                    return 0d;

                return SpanDays / RealSecondsDuration;
            }
        }
    }
}
