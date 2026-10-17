using UnityEngine;
using LD60.Dialogue;

namespace LD60.Space
{
    [CreateAssetMenu(fileName = "SolarSystem", menuName = "LD60/Solar System")]
    public class SolarSystemSO : ScriptableObject
    {
        [field: SerializeField] public float InnerRadius { get; private set; } = 0.9f;
        [field: SerializeField] public float LogScale { get; private set; } = 4f;
        [field: SerializeField] public float AxisReferenceAu { get; private set; } = 0.25f;
        [field: SerializeField] public int OrbitRingSegments { get; private set; } = 128;
        [field: SerializeField] public float OrbitRingWidth { get; private set; } = 0.015f;
        [field: SerializeField] public Color OrbitRingColor { get; private set; } = new Color(1f, 1f, 1f, 0.18f);
        [field: SerializeField] public CelestialBodySO[] Bodies { get; private set; } = new CelestialBodySO[0];
        [field: SerializeField] public SpacecraftSO[] FreeFlyers { get; private set; } = new SpacecraftSO[0];

        public float DisplayRadiusForAu(float semiMajorAxisAu)
        {
            if (semiMajorAxisAu <= 0f)
                return 0f;

            float reference = AxisReferenceAu > 0f ? AxisReferenceAu : 1f;
            return InnerRadius + LogScale * Mathf.Log10(1f + semiMajorAxisAu / reference);
        }

        public float DisplayRadius(CelestialBodySO body)
        {
            if (body.DisplayRadiusOverride > 0f)
                return body.DisplayRadiusOverride;

            return DisplayRadiusForAu(body.SemiMajorAxisAu);
        }

#if UNITY_EDITOR
        [ContextMenu("Create Missing Body Assets")]
        private void CreateMissingBodyAssets()
        {
            string folder = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this)).Replace("\\", "/") + "/Bodies";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace("\\", "/"), "Bodies");

            string[] names = { "Sun", "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
            float[] axes = { 0f, 0.387f, 0.723f, 1.000f, 1.524f, 5.203f, 9.537f, 19.191f, 30.070f };
            float[] periods = { 0f, 87.97f, 224.70f, 365.26f, 686.98f, 4332.59f, 10759.22f, 30688.5f, 60182f };
            float[] rotations = { 609.12f, 1407.6f, -5832.5f, 23.934f, 24.623f, 9.925f, 10.656f, -17.24f, 16.11f };
            float[] longitudes = { 0f, 252.25f, 181.98f, 100.46f, 355.43f, 34.40f, 49.94f, 313.23f, 304.88f };
            float[] diameters = { 1.1f, 0.18f, 0.26f, 0.28f, 0.22f, 0.62f, 0.55f, 0.40f, 0.39f };
            Color[] tints =
            {
                new Color(1f, 0.85f, 0.45f), new Color(0.62f, 0.58f, 0.54f), new Color(0.89f, 0.78f, 0.52f),
                new Color(0.40f, 0.60f, 0.85f), new Color(0.78f, 0.42f, 0.28f), new Color(0.80f, 0.68f, 0.52f),
                new Color(0.85f, 0.78f, 0.58f), new Color(0.60f, 0.80f, 0.82f), new Color(0.35f, 0.48f, 0.82f)
            };

            CelestialBodySO[] bodies = new CelestialBodySO[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                string path = folder + "/" + names[i] + ".asset";
                CelestialBodySO body = UnityEditor.AssetDatabase.LoadAssetAtPath<CelestialBodySO>(path);

                if (body == null)
                {
                    body = CreateInstance<CelestialBodySO>();
                    body.ApplyOrbitalData(names[i], axes[i], periods[i], rotations[i], longitudes[i], diameters[i], tints[i]);
                    UnityEditor.AssetDatabase.CreateAsset(body, path);
                }

                bodies[i] = body;
            }

            Bodies = bodies;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
