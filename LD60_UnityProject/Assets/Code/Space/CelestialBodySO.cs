using UnityEngine;
using LD60.Dialogue;

namespace LD60.Space
{
    [CreateAssetMenu(fileName = "Body", menuName = "LD60/Celestial Body")]
    public class CelestialBodySO : GameDataSO
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Body";
        [field: SerializeField] public float SemiMajorAxisAu { get; private set; }
        [field: SerializeField] public float OrbitalPeriodDays { get; private set; }
        [field: SerializeField] public float RotationPeriodHours { get; private set; }
        [field: SerializeField] public float DisplayRadiusOverride { get; private set; }
        [field: SerializeField] public float DisplayDiameter { get; private set; } = 0.3f;
        [field: SerializeField] public Color Tint { get; private set; } = Color.white;
        [field: SerializeField, Tooltip("Disc artwork pushed into the body material as _BaseMap. Leave empty to draw the material's own texture.")]
        public Texture2D Surface { get; private set; }
        [field: SerializeField, Range(0.05f, 0.5f), Tooltip("Radius of the painted globe inside Surface, in UV units from its center. " +
            "The shader treats that circle as the sphere; anything painted outside it (rings, flares) is left unrotated.")]
        public float SurfaceDiscRadius { get; private set; } = 0.37f;
        [field: SerializeField, Tooltip("Material the body quad is drawn with. Leave empty to use the Orrery fallback material tinted by Tint.")]
        public Material Material { get; private set; }
        [field: SerializeField] public bool DrawOrbitRing { get; private set; } = true;

#if UNITY_EDITOR
        public void ApplyOrbitalData(string displayName, float semiMajorAxisAu, float orbitalPeriodDays,
            float rotationPeriodHours, float displayDiameter, Color tint)
        {
            DisplayName = displayName;
            SemiMajorAxisAu = semiMajorAxisAu;
            OrbitalPeriodDays = orbitalPeriodDays;
            RotationPeriodHours = rotationPeriodHours;
            DisplayDiameter = displayDiameter;
            Tint = tint;
            DrawOrbitRing = semiMajorAxisAu > 0f;
        }
#endif
    }
}
