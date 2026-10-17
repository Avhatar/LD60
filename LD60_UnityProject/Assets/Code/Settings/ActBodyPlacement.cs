using UnityEngine;
using LD60.Space;

namespace LD60.Settings
{
    [System.Serializable]
    public class ActBodyPlacement
    {
        [field: SerializeField] public CelestialBodySO Body { get; private set; }

        [field: Tooltip("Where this body stands on its orbit when the act begins. Authored by hand to make the shot easier or harder.")]
        [field: SerializeField, Range(0f, 360f)] public float StartAngleDegrees { get; private set; }

#if UNITY_EDITOR
        public void SetBody(CelestialBodySO body)
        {
            Body = body;
        }
#endif
    }
}
