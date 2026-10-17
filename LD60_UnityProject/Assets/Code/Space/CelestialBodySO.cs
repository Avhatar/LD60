using UnityEngine;
using LD60.Dialogue;

namespace LD60.Space
{
    [CreateAssetMenu(fileName = "Body", menuName = "LD60/Celestial Body")]
    public class CelestialBodySO : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Body";
        [field: SerializeField] public float SemiMajorAxisAu { get; private set; }
        [field: SerializeField] public float OrbitalPeriodDays { get; private set; }
        [field: SerializeField] public float RotationPeriodHours { get; private set; }
        [field: SerializeField] public float MeanLongitudeJ2000Degrees { get; private set; }
        [field: SerializeField] public float DisplayRadiusOverride { get; private set; }
        [field: SerializeField] public float DisplayDiameter { get; private set; } = 0.3f;
        [field: SerializeField] public Color Tint { get; private set; } = Color.white;
        [field: SerializeField] public Texture2D Texture { get; private set; }
        [field: SerializeField] public bool DrawOrbitRing { get; private set; } = true;
        [field: SerializeField] public SpacecraftSO[] Spacecraft { get; private set; } = new SpacecraftSO[0];

#if UNITY_EDITOR
        public void ApplyOrbitalData(string displayName, float semiMajorAxisAu, float orbitalPeriodDays,
            float rotationPeriodHours, float meanLongitudeJ2000Degrees, float displayDiameter, Color tint)
        {
            DisplayName = displayName;
            SemiMajorAxisAu = semiMajorAxisAu;
            OrbitalPeriodDays = orbitalPeriodDays;
            RotationPeriodHours = rotationPeriodHours;
            MeanLongitudeJ2000Degrees = meanLongitudeJ2000Degrees;
            DisplayDiameter = displayDiameter;
            Tint = tint;
            DrawOrbitRing = semiMajorAxisAu > 0f;
        }
#endif
    }
}
