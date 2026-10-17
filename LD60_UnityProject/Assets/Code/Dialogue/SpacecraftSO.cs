using NodeGraphAvr.Dialogue;
using UnityEngine;

namespace LD60.Dialogue
{
    [CreateAssetMenu(fileName = "Spacecraft", menuName = "LD60/Spacecraft")]
    public class SpacecraftSO : ActorData
    {
        [field: Tooltip("Avatar shown next to this spacecraft's lines.")]
        [field: SerializeField] public Sprite Portrait { get; private set; }

        [field: Tooltip("Color of this spacecraft's name and signal on the orrery.")]
        [field: SerializeField] public Color Accent { get; private set; } = Color.white;

        [field: Tooltip("Voyager 1. Exactly one spacecraft in the project should have this set.")]
        [field: SerializeField] public bool IsPlayer { get; private set; }

        [field: Tooltip("Year of launch. Zero means it predates the run.")]
        [field: SerializeField] public int LaunchYear { get; private set; }

        [field: Tooltip("Year of last contact. Zero means still transmitting.")]
        [field: SerializeField] public int LastContactYear { get; private set; }

        [field: Tooltip("Whether this spacecraft can dictate a track onto the record.")]
        [field: SerializeField] public bool GivesRecordTrack { get; private set; } = true;

        [field: Tooltip("Shown on the final screen in place of the track that was never collected.")]
        [field: SerializeField, TextArea(2, 4)] public string Epitaph { get; private set; }

        [field: Tooltip("Distance from the Sun, for craft that are not anchored to a body.")]
        [field: SerializeField] public float FreeFlightDistanceAu { get; private set; }

        [field: Tooltip("Heliocentric angle, for craft that are not anchored to a body.")]
        [field: SerializeField] public float FreeFlightAngleDegrees { get; private set; }

        [field: Tooltip("Size of the marker drawn on the orrery.")]
        [field: SerializeField] public float DisplayDiameter { get; private set; } = 0.28f;

        public bool ExistsIn(int year)
        {
            return LaunchYear <= 0 || year >= LaunchYear;
        }

        public bool IsSilentIn(int year)
        {
            return LastContactYear > 0 && year > LastContactYear;
        }
    }
}
