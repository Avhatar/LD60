using UnityEngine;
using LD60.Settings;
using LD60.Dialogue;

namespace LD60.Space
{
    [DefaultExecutionOrder(-500)]
    public class Orrery : MonoBehaviour
    {
        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _orbitMaterial;
        [SerializeField] private Material _spacecraftMaterial;

        private SolarSystemSO _system;
        private TimeScaleSO _timeScale;
        private Transform[] _bodyTransforms;
        private MeshRenderer[] _bodyRenderers;
        private float[] _orbitRadii;
        private float[] _flashRemaining;
        private MaterialPropertyBlock _propertyBlock;
        private Mesh _quad;
        private double _elapsedDays;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public Transform PlayerAnchor { get; private set; }

        public int BodyCount
        {
            get { return _bodyTransforms == null ? 0 : _bodyTransforms.Length; }
        }

        public double CurrentDaysSinceJ2000
        {
            get { return _timeScale == null ? 0d : _timeScale.DaysSinceJ2000AtRunStart + _elapsedDays; }
        }

        public Transform GetBodyTransform(int index)
        {
            return _bodyTransforms[index];
        }

        public CelestialBodySO GetBody(int index)
        {
            return _system.Bodies[index];
        }

        public void FlashBody(int index, Color color, float seconds)
        {
            if (index < 0 || index >= _bodyRenderers.Length || _bodyRenderers[index] == null)
                return;

            _flashRemaining[index] = seconds;
            ApplyTint(_bodyRenderers[index], color, _system.Bodies[index].Texture);
        }

        private void Start()
        {
            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(Start)}. " +
                               "Required to read the solar system and time settings. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.");
                enabled = false;
                return;
            }

            _system = Core.Config.SolarSystem;
            _timeScale = Core.Config.TimeScale;

            if (_system == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(SolarSystemSO)} is null in {nameof(Start)}. " +
                               "Required to know which bodies to build and where to place them. " +
                               "Fix: assign a SolarSystem asset to the SolarSystem slot on the CoreSO asset.");
                enabled = false;
                return;
            }

            if (_timeScale == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(TimeScaleSO)} is null in {nameof(Start)}. " +
                               "Required to advance orbital motion. " +
                               "Fix: assign a TimeScale asset to the TimeScale slot on the CoreSO asset.");
                enabled = false;
                return;
            }

            if (_bodyMaterial == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(_bodyMaterial)} is null in {nameof(Start)}. " +
                               "Required to render celestial body and spacecraft quads. " +
                               "Fix: assign an unlit material to the Body Material slot on the Orrery object.");
                enabled = false;
                return;
            }

            _propertyBlock = new MaterialPropertyBlock();
            _quad = CreateQuadMesh();
            BuildBodies();
            BuildFreeFlyers();
            UpdateBodyPositions();
        }

        private void Update()
        {
            _elapsedDays += UnityEngine.Time.deltaTime * _timeScale.DaysPerSecond;
            UpdateBodyPositions();
            UpdateFlashes();
        }

        private void BuildBodies()
        {
            int count = _system.Bodies.Length;
            _bodyTransforms = new Transform[count];
            _bodyRenderers = new MeshRenderer[count];
            _orbitRadii = new float[count];
            _flashRemaining = new float[count];

            for (int i = 0; i < count; i++)
            {
                CelestialBodySO body = _system.Bodies[i];

                if (body == null)
                {
                    Debug.LogError($"{nameof(Orrery)}: {nameof(CelestialBodySO)} at index {i} is null in {nameof(BuildBodies)}. " +
                                   "Required to build the orrery. " +
                                   "Fix: remove the empty slot from the Bodies list on the SolarSystem asset, " +
                                   "or run Create Missing Body Assets on it.");
                    continue;
                }

                _orbitRadii[i] = _system.DisplayRadius(body);
                _bodyRenderers[i] = CreateQuad(body.DisplayName, body.DisplayDiameter, body.Tint, body.Texture);
                _bodyTransforms[i] = _bodyRenderers[i].transform;

                if (body.DrawOrbitRing && _orbitRadii[i] > 0f)
                    BuildOrbitRing(body.DisplayName, _orbitRadii[i]);
            }
        }

        private void BuildFreeFlyers()
        {
            for (int i = 0; i < _system.FreeFlyers.Length; i++)
            {
                SpacecraftSO craft = _system.FreeFlyers[i];

                if (craft == null)
                {
                    Debug.LogError($"{nameof(Orrery)}: {nameof(SpacecraftSO)} at index {i} is null in {nameof(BuildFreeFlyers)}. " +
                                   "Required to place a spacecraft that is not anchored to a body. " +
                                   "Fix: remove the empty slot from the Free Flyers list on the SolarSystem asset.");
                    continue;
                }

                Transform placed;
                if (craft.Portrait != null)
                    placed = CreateSprite(craft.DisplayName, craft.DisplayDiameter, craft.Accent, craft.Portrait);
                else
                    placed = CreateQuad(craft.DisplayName, craft.DisplayDiameter, craft.Accent, null).transform;

                float radius = _system.DisplayRadiusForAu(craft.FreeFlightDistanceAu);
                float radians = craft.FreeFlightAngleDegrees * Mathf.Deg2Rad;
                placed.localPosition = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);

                if (craft.IsPlayer)
                    PlayerAnchor = placed;
            }
        }

        private Transform CreateSprite(string spriteName, float diameter, Color tint, Sprite sprite)
        {
            GameObject placed = new GameObject(spriteName);
            placed.transform.SetParent(transform, false);

            SpriteRenderer renderer = placed.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (_spacecraftMaterial != null)
                renderer.sharedMaterial = _spacecraftMaterial;

            float extent = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            placed.transform.localScale = Vector3.one * (extent > 0f ? diameter / extent : 1f);

            return placed.transform;
        }

        private MeshRenderer CreateQuad(string quadName, float diameter, Color tint, Texture2D texture)
        {
            GameObject quad = new GameObject(quadName);
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = Vector3.one * diameter;

            MeshFilter filter = quad.AddComponent<MeshFilter>();
            filter.sharedMesh = _quad;

            MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _bodyMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ApplyTint(renderer, tint, texture);

            return renderer;
        }

        private void ApplyTint(Renderer renderer, Color color, Texture2D texture)
        {
            _propertyBlock.Clear();
            _propertyBlock.SetColor(BaseColorId, color);
            if (texture != null)
                _propertyBlock.SetTexture(BaseMapId, texture);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private void BuildOrbitRing(string bodyName, float radius)
        {
            GameObject ring = new GameObject(bodyName + " Orbit");
            ring.transform.SetParent(transform, false);

            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = _system.OrbitRingWidth;
            line.startColor = _system.OrbitRingColor;
            line.endColor = _system.OrbitRingColor;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            if (_orbitMaterial != null)
            {
                line.sharedMaterial = _orbitMaterial;
                _propertyBlock.Clear();
                _propertyBlock.SetColor(BaseColorId, _system.OrbitRingColor);
                line.SetPropertyBlock(_propertyBlock);
            }

            int segments = Mathf.Max(8, _system.OrbitRingSegments);
            line.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void UpdateFlashes()
        {
            for (int i = 0; i < _flashRemaining.Length; i++)
            {
                if (_flashRemaining[i] <= 0f)
                    continue;

                _flashRemaining[i] -= UnityEngine.Time.deltaTime;

                if (_flashRemaining[i] > 0f || _bodyRenderers[i] == null)
                    continue;

                _flashRemaining[i] = 0f;
                ApplyTint(_bodyRenderers[i], _system.Bodies[i].Tint, _system.Bodies[i].Texture);
            }
        }

        private void UpdateBodyPositions()
        {
            double days = CurrentDaysSinceJ2000;

            for (int i = 0; i < _bodyTransforms.Length; i++)
            {
                if (_bodyTransforms[i] == null)
                    continue;

                CelestialBodySO body = _system.Bodies[i];

                if (body.OrbitalPeriodDays <= 0f || _orbitRadii[i] <= 0f)
                {
                    _bodyTransforms[i].localPosition = Vector3.zero;
                    continue;
                }

                double degrees = body.MeanLongitudeJ2000Degrees + days / body.OrbitalPeriodDays * 360d;
                float radians = (float)(degrees % 360d) * Mathf.Deg2Rad;
                float radius = _orbitRadii[i];
                _bodyTransforms[i].localPosition = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);
            }
        }

        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "OrreryQuad";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
