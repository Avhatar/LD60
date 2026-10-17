using UnityEngine;
using NodeGraphAvr.Dialogue;
using LD60.Dialogue;

namespace LD60.Space
{
    [CreateAssetMenu(fileName = "ActSpacecraft", menuName = "LD60/Act Spacecraft")]
    public class ActSpacecraftSO : GameDataSO
    {
        [field: Tooltip("Which spacecraft this entry places and configures.")]
        [field: SerializeField] public SpacecraftSO Spacecraft { get; private set; }

        [field: Tooltip("Body it sits on during this act. Leave empty for a spacecraft flying free, and fill the two fields below instead.")]
        [field: SerializeField] public CelestialBodySO Body { get; private set; }

        [field: Tooltip("Distance from the Sun during this act. Used only when Body is empty.")]
        [field: SerializeField] public float DistanceAu { get; private set; }

        [field: Tooltip("Heliocentric angle during this act. Used only when Body is empty.")]
        [field: SerializeField, Range(0f, 360f)] public float AngleDegrees { get; private set; }

        [field: Tooltip("Body it transmits to. Earth in almost every case.")]
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

        [field: Tooltip("Dialogue started when one of its dots is caught.")]
        [field: SerializeField] public DialogueGraph InterceptDialogue { get; private set; }
    }
}
