using UnityEngine;
using LD60.Settings;
using LD60.Signals;

namespace LD60.UI
{
    public class ReadyRings : MonoBehaviour
    {
        [SerializeField] private ForeignSignalDirector _director;
        [SerializeField] private Material _ringMaterial;

        private InterceptMarkerSO _settings;
        private LineRenderer[] _rings = new LineRenderer[0];
        private LineRenderer _traceLine;
        private LineRenderer _traceRing;
        private MaterialPropertyBlock _block;
        private bool _dataDirty;
        private int _traceSender = -1;
        private Vector2 _tracePoint;
        private float _traceAlpha;

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
            if (_director == null)
            {
                Debug.LogError($"{nameof(ReadyRings)}: {nameof(_director)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to know which senders have enough caught dots and are waiting to be shot. " +
                               "Fix: assign the Foreign Signals object to the Director slot on this object.", this);
                enabled = false;
                return;
            }

            _block = new MaterialPropertyBlock();
            _traceRing = CreateTrace("Speaking Ring", true);
            _traceLine = CreateTrace("Speaking Line", false);
            SyncData();
        }

        private void SyncData()
        {
            _dataDirty = false;

            if (Core.Config == null || Core.Config.InterceptMarker == null)
            {
                Debug.LogError($"{nameof(ReadyRings)}: {nameof(InterceptMarkerSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to know the ring colour, width and margin. " +
                               "Fix: assign an InterceptMarker asset to the Intercept Marker slot on the CoreSO asset.", this);
                return;
            }

            _settings = Core.Config.InterceptMarker;

            for (int i = 0; i < _rings.Length; i++)
                ApplyLook(_rings[i]);
        }

        private void LateUpdate()
        {
            if (_dataDirty)
                SyncData();

            if (_settings == null)
                return;

            Resize(_director.SenderCount);

            for (int i = 0; i < _rings.Length; i++)
            {
                Transform target = _director.IsAwaitingContact(i) ? _director.SenderObject(i) : null;

                if (target == null || !Core.IsGameMode)
                {
                    if (_rings[i].enabled)
                        _rings[i].enabled = false;

                    continue;
                }

                if (!_rings[i].enabled)
                    _rings[i].enabled = true;

                Draw(_rings[i], target.position, _director.SenderObjectRadius(i) + _settings.ReadyRingMargin);
            }

            DrawSpeakingTrace();
        }

        private void DrawSpeakingTrace()
        {
            if (_traceLine == null || _traceRing == null)
                return;

            int speaking = _director.SpeakingSender;

            if (speaking >= 0)
            {
                _traceSender = speaking;
                _tracePoint = _director.SpeakingCapturePoint;
                _traceAlpha = 1f;
            }
            else if (_traceAlpha > 0f && _settings.DialogueTraceFadeSeconds > 0f)
            {
                _traceAlpha = Mathf.Max(0f, _traceAlpha - Time.unscaledDeltaTime / _settings.DialogueTraceFadeSeconds);
            }
            else
            {
                _traceAlpha = 0f;
            }

            Transform owner = _traceAlpha > 0f && _traceSender >= 0 && _traceSender < _director.SenderCount
                ? _director.SenderObject(_traceSender)
                : null;

            if (owner == null || (!Core.IsGameMode && !Core.IsDialogueMode))
            {
                _traceSender = -1;
                _traceAlpha = 0f;
                Show(_traceRing, false);
                Show(_traceLine, false);
                return;
            }

            Vector3 centre = owner.position;
            float radius = _director.SenderObjectRadius(_traceSender) + _settings.ReadyRingMargin;
            bool ringNeeded = !_director.IsAwaitingContact(_traceSender) || !Core.IsGameMode;

            Show(_traceRing, ringNeeded);

            if (ringNeeded)
            {
                PaintTrace(_traceRing, _settings.ReadyRingWidth);
                Draw(_traceRing, centre, radius);
            }

            Vector2 outward = _tracePoint - (Vector2)centre;
            float distance = outward.magnitude;

            if (distance <= radius)
            {
                Show(_traceLine, false);
                return;
            }

            Vector2 edge = (Vector2)centre + outward / distance * radius;
            float depth = centre.z - _settings.ReadyRingDepthTowardCamera;

            Show(_traceLine, true);
            PaintTrace(_traceLine, _settings.DialogueTraceWidth);

            if (_traceLine.positionCount != 2)
                _traceLine.positionCount = 2;

            _traceLine.SetPosition(0, new Vector3(_tracePoint.x, _tracePoint.y, depth));
            _traceLine.SetPosition(1, new Vector3(edge.x, edge.y, depth));
        }

        private static void Show(LineRenderer line, bool shown)
        {
            if (line.enabled != shown)
                line.enabled = shown;
        }

        private void PaintTrace(LineRenderer line, float width)
        {
            Color color = _settings.DialogueTraceColor;
            color.a *= _traceAlpha;

            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;

            if (_ringMaterial == null)
                return;

            Color emissive = color;
            float emission = Mathf.Max(_settings.DialogueTraceEmission, 0f);
            emissive.r *= emission;
            emissive.g *= emission;
            emissive.b *= emission;

            line.sharedMaterial = _ringMaterial;
            _block.Clear();
            _block.SetColor(Shader.PropertyToID("_BaseColor"), emissive);
            line.SetPropertyBlock(_block);
        }

        private LineRenderer CreateTrace(string label, bool loop)
        {
            GameObject holder = new GameObject(label);
            holder.transform.SetParent(transform, false);

            LineRenderer line = holder.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void Draw(LineRenderer ring, Vector3 centre, float radius)
        {
            int segments = Mathf.Max(8, _settings.ReadyRingSegments);

            if (ring.positionCount != segments)
                ring.positionCount = segments;

            float depth = centre.z - _settings.ReadyRingDepthTowardCamera;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                ring.SetPosition(i, new Vector3(centre.x + Mathf.Cos(angle) * radius,
                                                centre.y + Mathf.Sin(angle) * radius,
                                                depth));
            }
        }

        private void Resize(int count)
        {
            if (_rings.Length == count)
                return;

            for (int i = count; i < _rings.Length; i++)
            {
                if (_rings[i] != null)
                    Destroy(_rings[i].gameObject);
            }

            LineRenderer[] resized = new LineRenderer[count];
            int kept = Mathf.Min(count, _rings.Length);

            for (int i = 0; i < kept; i++)
                resized[i] = _rings[i];

            for (int i = kept; i < count; i++)
                resized[i] = CreateRing();

            _rings = resized;
        }

        private LineRenderer CreateRing()
        {
            GameObject ring = new GameObject("Ready Ring");
            ring.transform.SetParent(transform, false);

            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            ApplyLook(line);
            return line;
        }

        private void ApplyLook(LineRenderer line)
        {
            if (_settings == null || line == null)
                return;

            line.widthMultiplier = _settings.ReadyRingWidth;
            line.startColor = _settings.ReadyRingColor;
            line.endColor = _settings.ReadyRingColor;

            if (_ringMaterial == null)
                return;

            Color emissive = _settings.ReadyRingColor;
            float emission = Mathf.Max(_settings.ReadyRingEmission, 0f);
            emissive.r *= emission;
            emissive.g *= emission;
            emissive.b *= emission;

            line.sharedMaterial = _ringMaterial;
            _block.Clear();
            _block.SetColor(Shader.PropertyToID("_BaseColor"), emissive);
            line.SetPropertyBlock(_block);
        }
    }
}
