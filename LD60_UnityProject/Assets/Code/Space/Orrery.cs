using UnityEngine;
using LD60.Settings;
using LD60.Dialogue;

namespace LD60.Space
{
    [DefaultExecutionOrder(-500)]
    public class Orrery : MonoBehaviour
    {
        private class BodyView
        {
            public CelestialBodySO Data;
            public Transform Root;
            public MeshRenderer Renderer;
            public LineRenderer Ring;
            public float OrbitRadius;
            public float FlashRemaining;
            public Color FlashColor;
            public float SpinTurnsPerDay;
            public double SpinTurns;
        }

        private class FreeFlyerView
        {
            public SpacecraftSO Data;
            public Transform Root;
            public Transform Visual;
            public SpriteRenderer Sprite;
            public MeshRenderer Quad;
        }

        [SerializeField] private Material _bodyMaterial;
        [SerializeField] private Material _orbitMaterial;
        [SerializeField] private Material _spacecraftMaterial;

        private SolarSystemSO _system;
        private TimeScaleSO _timeScale;
        private BodyView[] _bodies = new BodyView[0];
        private FreeFlyerView[] _freeFlyers = new FreeFlyerView[0];
        private MaterialPropertyBlock _propertyBlock;
        private Mesh _quad;
        private bool _dataDirty;
        private ActPeriod _syncedAct;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int DiscRadiusId = Shader.PropertyToID("_DiscRadius");
        private static readonly int SpinId = Shader.PropertyToID("_Spin");
        private static readonly int SunDirId = Shader.PropertyToID("_SunDir");
        private static readonly int OrbitRadiusId = Shader.PropertyToID("_OrbitRadius");

        public Transform PlayerAnchor { get; private set; }

        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                    _propertyBlock = new MaterialPropertyBlock();

                return _propertyBlock;
            }
        }

        public int BodyCount
        {
            get { return _bodies.Length; }
        }

        public double CurrentDaysSinceJ2000
        {
            get { return Core.DaysSinceJ2000; }
        }

        public Transform GetBodyTransform(int index)
        {
            if (index < 0 || index >= _bodies.Length || _bodies[index].Data == null)
                return null;

            return _bodies[index].Root;
        }

        public bool IsHitAt(int index, Vector2 worldPoint)
        {
            if (index < 0 || index >= _bodies.Length || _bodies[index].Data == null || _bodies[index].Root == null)
                return false;

            CelestialBodySO data = _bodies[index].Data;
            float radius = data.DisplayDiameter * data.SurfaceDiscRadius;

            if (radius <= 0f)
                return false;

            return ((Vector2)_bodies[index].Root.position - worldPoint).sqrMagnitude <= radius * radius;
        }

        public float GetHitRadius(int index)
        {
            if (index < 0 || index >= _bodies.Length || _bodies[index].Data == null)
                return 0f;

            return _bodies[index].Data.DisplayDiameter * _bodies[index].Data.SurfaceDiscRadius;
        }

        public float GetHitRadiusOf(CelestialBodySO body)
        {
            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i].Data == body)
                    return body.DisplayDiameter * body.SurfaceDiscRadius;
            }

            return 0f;
        }

        public bool TryPredictBodyPosition(CelestialBodySO body, double daysAhead, out Vector2 position)
        {
            position = Vector2.zero;

            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i].Data != body)
                    continue;

                if (_bodies[i].Data.OrbitalPeriodDays <= 0f || _bodies[i].OrbitRadius <= 0f)
                {
                    position = _bodies[i].Root.position;
                    return true;
                }

                ActPeriod act = Core.CurrentAct;
                double elapsed = act == null ? CurrentDaysSinceJ2000 : CurrentDaysSinceJ2000 - act.StartDaysSinceJ2000;
                double startAngle = act == null ? 0d : act.StartAngleFor(body);
                double degrees = startAngle + (elapsed + daysAhead) / _bodies[i].Data.OrbitalPeriodDays * 360d;
                float radians = (float)(degrees % 360d) * Mathf.Deg2Rad;
                position = (Vector2)transform.position
                         + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _bodies[i].OrbitRadius;
                return true;
            }

            return false;
        }

        public Transform FindBodyTransform(CelestialBodySO body)
        {
            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i].Data == body)
                    return _bodies[i].Root;
            }

            return null;
        }

        public CelestialBodySO GetBody(int index)
        {
            if (index < 0 || index >= _bodies.Length)
                return null;

            return _bodies[index].Data;
        }

        public void FlashBody(int index, Color color, float seconds)
        {
            if (index < 0 || index >= _bodies.Length || _bodies[index].Data == null)
                return;

            _bodies[index].FlashRemaining = seconds;
            _bodies[index].FlashColor = color;
            ApplyBodyBlock(_bodies[index]);
        }

        public void RefreshFromData()
        {
            _dataDirty = true;
        }

        private void OnEnable()
        {
            GameDataSO.Changed += OnDataChanged;
        }

        private void OnDisable()
        {
            GameDataSO.Changed -= OnDataChanged;
        }

        private void OnDataChanged()
        {
            _dataDirty = true;
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
                               "Required as the fallback for bodies and spacecraft that do not carry their own material. " +
                               "Fix: assign an unlit material to the Body Material slot on the Orrery object.");
                enabled = false;
                return;
            }

            _quad = CreateQuadMesh();

            SyncData();
            UpdateBodyPositions();
        }

        private void Update()
        {
            if (!ReferenceEquals(Core.CurrentAct, _syncedAct))
                _dataDirty = true;

            if (_dataDirty)
                SyncData();

            double deltaDays = Core.DeltaDays;

            UpdateBodyPositions();
            UpdateFlashes();
            UpdateSpin(deltaDays);
        }

        private void SyncData()
        {
            _dataDirty = false;
            _syncedAct = Core.CurrentAct;

            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(SyncData)}. " +
                               "Required to re-read the solar system and time settings after a data edit. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.");
                return;
            }

            if (Core.Config.SolarSystem == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(SolarSystemSO)} is null in {nameof(SyncData)}. " +
                               "Required to know which bodies to draw and where to place them. " +
                               "Fix: assign a SolarSystem asset to the SolarSystem slot on the CoreSO asset.");
                return;
            }

            if (Core.Config.TimeScale == null)
            {
                Debug.LogError($"{nameof(Orrery)}: {nameof(TimeScaleSO)} is null in {nameof(SyncData)}. " +
                               "Required to advance orbital motion. " +
                               "Fix: assign a TimeScale asset to the TimeScale slot on the CoreSO asset.");
                return;
            }

            _system = Core.Config.SolarSystem;
            _timeScale = Core.Config.TimeScale;

            SyncBodies();
            SyncFreeFlyers();
        }

        private void SyncBodies()
        {
            ResizeBodies(_system.Bodies.Length);

            for (int i = 0; i < _bodies.Length; i++)
            {
                BodyView view = _bodies[i];
                CelestialBodySO data = _system.Bodies[i];

                if (data == null)
                {
                    Debug.LogError($"{nameof(Orrery)}: {nameof(CelestialBodySO)} at index {i} is null in {nameof(SyncBodies)}. " +
                                   "Required to draw a body and its orbit ring. " +
                                   "Fix: remove the empty slot from the Bodies list on the SolarSystem asset, " +
                                   "or run Create Missing Body Assets on it.");

                    view.Data = null;
                    view.OrbitRadius = 0f;
                    view.FlashRemaining = 0f;
                    view.SpinTurnsPerDay = 0f;
                    view.Root.gameObject.SetActive(false);
                    view.Ring.gameObject.SetActive(false);
                    continue;
                }

                view.Data = data;
                view.OrbitRadius = _system.DisplayRadius(data);
                view.Root.gameObject.SetActive(true);
                view.Root.name = data.DisplayName;
                view.Root.localScale = Vector3.one * data.DisplayDiameter;
                view.Renderer.sharedMaterial = data.Material != null ? data.Material : _bodyMaterial;
                view.SpinTurnsPerDay = _system.SpinTurnsPerDay(data);

                ApplyBodyBlock(view);
                SyncOrbitRing(view);
            }
        }

        private void ResizeBodies(int count)
        {
            if (_bodies.Length == count)
                return;

            BodyView[] resized = new BodyView[count];
            int kept = Mathf.Min(count, _bodies.Length);

            for (int i = 0; i < kept; i++)
                resized[i] = _bodies[i];

            for (int i = kept; i < _bodies.Length; i++)
            {
                Destroy(_bodies[i].Ring.gameObject);
                Destroy(_bodies[i].Root.gameObject);
            }

            for (int i = kept; i < count; i++)
                resized[i] = CreateBodyView();

            _bodies = resized;
        }

        private BodyView CreateBodyView()
        {
            BodyView view = new BodyView();

            GameObject body = new GameObject("Body");
            body.transform.SetParent(transform, false);

            MeshFilter filter = body.AddComponent<MeshFilter>();
            filter.sharedMesh = _quad;

            view.Renderer = body.AddComponent<MeshRenderer>();
            view.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.Renderer.receiveShadows = false;
            view.Root = body.transform;

            GameObject ring = new GameObject("Orbit");
            ring.transform.SetParent(transform, false);

            view.Ring = ring.AddComponent<LineRenderer>();
            view.Ring.useWorldSpace = false;
            view.Ring.loop = true;
            view.Ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.Ring.receiveShadows = false;

            return view;
        }

        private void SyncOrbitRing(BodyView view)
        {
            bool visible = view.Data.DrawOrbitRing && view.OrbitRadius > 0f;
            view.Ring.gameObject.SetActive(visible);

            if (!visible)
                return;

            view.Ring.gameObject.name = view.Data.DisplayName + " Orbit";
            view.Ring.widthMultiplier = _system.OrbitRingWidth;
            view.Ring.startColor = _system.OrbitRingColor;
            view.Ring.endColor = _system.OrbitRingColor;

            if (_orbitMaterial != null)
            {
                view.Ring.sharedMaterial = _orbitMaterial;
                PropertyBlock.Clear();
                PropertyBlock.SetColor(BaseColorId, _system.OrbitRingColor);
                PropertyBlock.SetFloat(OrbitRadiusId, view.OrbitRadius);
                view.Ring.SetPropertyBlock(PropertyBlock);
            }

            int segments = Mathf.Max(8, _system.OrbitRingSegments);
            view.Ring.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                view.Ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * view.OrbitRadius, Mathf.Sin(angle) * view.OrbitRadius, 0f));
            }
        }

        private void SyncFreeFlyers()
        {
            ActPeriod act = Core.CurrentAct;
            ResizeFreeFlyers(CountFreeFlyers(act));
            PlayerAnchor = null;

            if (act == null)
                return;

            int slot = 0;

            for (int i = 0; i < act.Spacecraft.Length; i++)
            {
                ActSpacecraftSO entry = act.Spacecraft[i];

                if (entry == null)
                {
                    Debug.LogError($"{nameof(Orrery)}: {nameof(ActSpacecraftSO)} at index {i} is null in {nameof(SyncFreeFlyers)}. " +
                                   "Required to know which spacecraft this act places. " +
                                   $"Fix: remove the empty slot from the Spacecraft list of act '{act.Name}' on the TimeScale asset.");
                    continue;
                }

                if (entry.Body != null)
                    continue;

                if (entry.Spacecraft == null)
                {
                    Debug.LogError($"{nameof(Orrery)}: {nameof(entry.Spacecraft)} is null on '{entry.name}' in {nameof(SyncFreeFlyers)}. " +
                                   "Required to know which spacecraft to draw and whether it is the player. " +
                                   $"Fix: assign a Spacecraft asset to the Spacecraft slot on '{entry.name}'.");
                    continue;
                }

                FreeFlyerView view = _freeFlyers[slot];
                slot++;

                view.Data = entry.Spacecraft;
                view.Root.gameObject.SetActive(true);
                view.Root.name = entry.Spacecraft.DisplayName;

                SyncFreeFlyerVisual(view);

                float radius = _system.DisplayRadiusForAu(entry.DistanceAu);
                float radians = entry.AngleDegrees * Mathf.Deg2Rad;
                view.Root.localPosition = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);

                if (entry.Spacecraft.IsPlayer)
                    PlayerAnchor = view.Root;
            }
        }

        private static int CountFreeFlyers(ActPeriod act)
        {
            if (act == null)
                return 0;

            int count = 0;

            for (int i = 0; i < act.Spacecraft.Length; i++)
            {
                ActSpacecraftSO entry = act.Spacecraft[i];

                if (entry != null && entry.Body == null && entry.Spacecraft != null)
                    count++;
            }

            return count;
        }

        private void ResizeFreeFlyers(int count)
        {
            if (_freeFlyers.Length == count)
                return;

            FreeFlyerView[] resized = new FreeFlyerView[count];
            int kept = Mathf.Min(count, _freeFlyers.Length);

            for (int i = 0; i < kept; i++)
                resized[i] = _freeFlyers[i];

            for (int i = kept; i < _freeFlyers.Length; i++)
                Destroy(_freeFlyers[i].Root.gameObject);

            for (int i = kept; i < count; i++)
                resized[i] = CreateFreeFlyerView();

            _freeFlyers = resized;
        }

        private FreeFlyerView CreateFreeFlyerView()
        {
            FreeFlyerView view = new FreeFlyerView();

            GameObject root = new GameObject("Spacecraft");
            root.transform.SetParent(transform, false);
            view.Root = root.transform;

            return view;
        }

        private void SyncFreeFlyerVisual(FreeFlyerView view)
        {
            SpacecraftSO data = view.Data;
            bool wantsSprite = data.SpaceSprite != null;

            if (view.Visual == null || wantsSprite != (view.Sprite != null))
            {
                if (view.Visual != null)
                    Destroy(view.Visual.gameObject);

                view.Sprite = null;
                view.Quad = null;

                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(view.Root, false);
                view.Visual = visual.transform;

                if (wantsSprite)
                {
                    view.Sprite = visual.AddComponent<SpriteRenderer>();
                    view.Sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    view.Sprite.receiveShadows = false;
                }
                else
                {
                    MeshFilter filter = visual.AddComponent<MeshFilter>();
                    filter.sharedMesh = _quad;

                    view.Quad = visual.AddComponent<MeshRenderer>();
                    view.Quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    view.Quad.receiveShadows = false;
                }
            }

            if (!wantsSprite)
            {
                view.Quad.sharedMaterial = _bodyMaterial;
                view.Visual.localScale = Vector3.one * data.DisplayDiameter;
                ApplyTint(view.Quad, data.Accent);
                return;
            }

            view.Sprite.sprite = data.SpaceSprite;
            view.Sprite.color = data.Accent;

            if (_spacecraftMaterial != null)
                view.Sprite.sharedMaterial = _spacecraftMaterial;

            float extent = Mathf.Max(data.SpaceSprite.bounds.size.x, data.SpaceSprite.bounds.size.y);
            view.Visual.localScale = Vector3.one * (extent > 0f ? data.DisplayDiameter / extent : 1f);
        }

        private void ApplyBodyBlock(BodyView view)
        {
            PropertyBlock.Clear();

            if (view.FlashRemaining > 0f)
                PropertyBlock.SetColor(BaseColorId, view.FlashColor);
            else if (view.Data.Material == null && view.Data.Surface == null)
                PropertyBlock.SetColor(BaseColorId, view.Data.Tint);

            if (view.Data.Surface != null)
            {
                PropertyBlock.SetTexture(BaseMapId, view.Data.Surface);
                PropertyBlock.SetFloat(DiscRadiusId, view.Data.SurfaceDiscRadius);
            }

            PropertyBlock.SetFloat(SpinId, (float)view.SpinTurns);
            PropertyBlock.SetVector(SunDirId, DirectionToStar(view));
            view.Renderer.SetPropertyBlock(PropertyBlock);
        }

        private static Vector4 DirectionToStar(BodyView view)
        {
            Vector3 position = view.Root.localPosition;
            Vector2 toCentre = new Vector2(-position.x, -position.y);

            if (toCentre.sqrMagnitude <= Mathf.Epsilon)
                return Vector4.zero;

            toCentre.Normalize();
            return new Vector4(toCentre.x, toCentre.y, 0f, 0f);
        }

        private void ApplyTint(Renderer renderer, Color color)
        {
            PropertyBlock.Clear();
            PropertyBlock.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(PropertyBlock);
        }

        private void UpdateFlashes()
        {
            for (int i = 0; i < _bodies.Length; i++)
            {
                BodyView view = _bodies[i];

                if (view.FlashRemaining <= 0f)
                    continue;

                view.FlashRemaining -= UnityEngine.Time.deltaTime;

                if (view.FlashRemaining > 0f)
                    continue;

                view.FlashRemaining = 0f;
            }
        }

        private void UpdateSpin(double deltaDays)
        {
            for (int i = 0; i < _bodies.Length; i++)
            {
                BodyView view = _bodies[i];

                if (view.Data == null)
                    continue;

                if (view.SpinTurnsPerDay != 0f)
                {
                    view.SpinTurns += deltaDays * view.SpinTurnsPerDay;
                }

                ApplyBodyBlock(view);
            }
        }

        private void UpdateBodyPositions()
        {
            ActPeriod act = Core.CurrentAct;
            double days = act == null ? CurrentDaysSinceJ2000 : CurrentDaysSinceJ2000 - act.StartDaysSinceJ2000;

            for (int i = 0; i < _bodies.Length; i++)
            {
                BodyView view = _bodies[i];

                if (view.Data == null)
                    continue;

                if (view.Data.OrbitalPeriodDays <= 0f || view.OrbitRadius <= 0f)
                {
                    view.Root.localPosition = Vector3.zero;
                    continue;
                }

                double startAngle = act == null ? 0d : act.StartAngleFor(view.Data);
                double degrees = startAngle + days / view.Data.OrbitalPeriodDays * 360d;
                float radians = (float)(degrees % 360d) * Mathf.Deg2Rad;
                view.Root.localPosition = new Vector3(Mathf.Cos(radians) * view.OrbitRadius, Mathf.Sin(radians) * view.OrbitRadius, 0f);
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
