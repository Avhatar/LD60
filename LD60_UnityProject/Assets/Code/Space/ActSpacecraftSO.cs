using UnityEngine;
using NodeGraphAvr.Dialogue;
using LD60.Dialogue;

namespace LD60.Space
{
    [CreateAssetMenu(fileName = "ActSpacecraft", menuName = "LD60/Act Spacecraft")]
    public class ActSpacecraftSO : GameDataSO
    {
        private const double DaysPerYear = 365.25d;

        [field: Tooltip("Which spacecraft this entry places and configures.")]
        [field: SerializeField] public SpacecraftSO Spacecraft { get; private set; }

        [field: Tooltip("Body it sits on during this act. Leave empty for a spacecraft flying free, and fill the fields below instead.")]
        [field: SerializeField] public CelestialBodySO Body { get; private set; }

        [field: Tooltip("Distance from the Sun when the act begins. Used only when Body is empty.")]
        [field: SerializeField] public float DistanceAu { get; private set; }

        [field: Tooltip("Heliocentric angle when the act begins. Used only when Body is empty.")]
        [field: SerializeField, Range(0f, 360f)] public float AngleDegrees { get; private set; }

        [field: Tooltip("How much farther out it gets across the whole act, in the same units as Distance Au. " +
            "For our own spacecraft, where the point it reaches by the final date matters more than the speed it keeps. " +
            "Zero hands the radial motion to Au Per Year instead.")]
        [field: SerializeField] public float TravelAuDuringAct { get; private set; }

        [field: Tooltip("Radial speed in Distance Au units per year. Real speed while Motion Scale is one. " +
            "Ignored while Travel Au During Act is non-zero.")]
        [field: SerializeField] public float AuPerYear { get; private set; }

        [field: Tooltip("Heliocentric degrees it sweeps per year. Real rate while Motion Scale is one. " +
            "Negative sweeps clockwise. Values above 360 are laps, which is how a short heliocentric orbit is written.")]
        [field: SerializeField] public float DegreesPerYear { get; private set; }

        [field: Tooltip("Multiplies everything it travels. One is real speed; raise it to make slow motion read on the orrery.")]
        [field: SerializeField] public float MotionScale { get; private set; } = 1f;

        [field: Tooltip("Body it transmits to. Earth in almost every case. Never the body it sits on: that transmission starts and ends in the same place and only draws dots on top of it. Something parked on a body either stays silent and is reached another way, or transmits outward to a different body.")]
        [field: SerializeField] public CelestialBodySO TransmitTo { get; private set; }

        [field: Tooltip("Arc width of its transmissions, in degrees.")]
        [field: SerializeField] public float ArcWidthDegrees { get; private set; } = 10f;

        [field: Tooltip("Dots carried by one of its transmissions.")]
        [field: SerializeField] public int DotCount { get; private set; } = 12;

        [field: Tooltip("Travel speed of its transmissions. Deliberately slower than ours.")]
        [field: SerializeField] public float Speed { get; private set; } = 1.2f;

        [field: Tooltip("Seconds between its transmissions.")]
        [field: SerializeField] public float TransmitPeriodSeconds { get; private set; } = 18f;

        [field: Tooltip("Dots of its signal that have to be caught before it can be talked to directly.")]
        [field: SerializeField] public int DotsToCapture { get; private set; } = 8;

        [field: Tooltip("Starts already found and already unlocked, as if every dot it owes had been caught. Its marker is lit from the first second, and our signal landing on it, or on the body it sits on, opens the conversation straight away. For the one tutorial contact of an act, and for anything switched off that never transmits at all.")]
        [field: SerializeField] public bool KnownFromStart { get; private set; }

        [field: Tooltip("Dialogues this spacecraft can be overheard saying. One is picked at random each time its transmission is caught.")]
        [field: SerializeField] public DialogueGraph[] InterceptDialogues { get; private set; } = new DialogueGraph[0];

        [field: Tooltip("The direct conversation. Played once, when enough of its dots have been caught and our signal then lands on it, or on the body it sits on.")]
        [field: SerializeField] public DialogueGraph ContactDialogue { get; private set; }

        public bool TransmitsOntoItself
        {
            get { return Body != null && TransmitTo == Body; }
        }

        public bool Transmits
        {
            get { return TransmitTo != null && !TransmitsOntoItself && TransmitPeriodSeconds > 0f; }
        }

        public float DistanceAuAt(double elapsedDays, double actSpanDays)
        {
            double travelled;

            if (TravelAuDuringAct != 0f && actSpanDays != 0d)
                travelled = TravelAuDuringAct * elapsedDays / actSpanDays;
            else
                travelled = AuPerYear * elapsedDays / DaysPerYear;

            return Mathf.Max(0f, DistanceAu + (float)(travelled * MotionScale));
        }

        public float AngleDegreesAt(double elapsedDays)
        {
            return AngleDegrees + (float)(DegreesPerYear * MotionScale * elapsedDays / DaysPerYear);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!TransmitsOntoItself)
                return;

            Debug.LogError($"{nameof(ActSpacecraftSO)}: {nameof(TransmitTo)} is the same body as {nameof(Body)} on '{name}'. " +
                           "Required to differ, because a transmission that starts and ends on one body just draws dots on top of it. " +
                           $"Fix: aim {nameof(TransmitTo)} at a different body, or clear it and set {nameof(TransmitPeriodSeconds)} to zero to keep this one silent.");
        }
#endif
    }
}
