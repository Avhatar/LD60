using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LD60.Settings;

namespace LD60.UI
{
    public class TargetFrame : MaskableGraphic
    {
        [SerializeField] private TMP_Text _label;

        private Vector2 _screenCenter;
        private float _settledSize;
        private float _currentSize;
        private float _alpha;
        private bool _tracking;
        private Color _frameColor = Color.white;
        private UIVertex _vertex = UIVertex.simpleVert;

        public void Track(Camera viewer, Vector3 worldCenter, float worldRadius, string label, Color frameColor)
        {
            TargetFrameSO settings = ResolveSettings();

            if (settings == null)
                return;

            if (viewer == null)
            {
                Debug.LogError($"{nameof(TargetFrame)}: {nameof(viewer)} is null in {nameof(Track)} on '{name}'. " +
                               "Required to turn the target's world position into a screen position. " +
                               "Fix: pass the scene camera from whatever drives this frame.", this);
                enabled = false;
                return;
            }

            Vector3 center = viewer.WorldToScreenPoint(worldCenter);

            if (center.z < 0f)
            {
                Release();
                return;
            }

            Vector3 edge = viewer.WorldToScreenPoint(worldCenter + viewer.transform.right * worldRadius);
            float scale = CanvasScale;
            float radius = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(edge.x, edge.y)) / scale;

            _screenCenter = new Vector2(center.x, center.y);
            _settledSize = Mathf.Clamp(radius * 2f + settings.Padding * 2f, settings.MinSize, settings.MaxSize);
            _frameColor = frameColor;

            if (!_tracking)
            {
                _tracking = true;
                _currentSize = _settledSize * Mathf.Max(1f, settings.OpenSizeMultiplier);
            }

            SetLabel(label);
        }

        public void Release()
        {
            _tracking = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GameDataSO.Changed += SetVerticesDirty;

            if (!Application.isPlaying)
                return;

            raycastTarget = false;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (_label == null)
                return;

            _label.raycastTarget = false;
            _label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _label.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        protected override void OnDisable()
        {
            GameDataSO.Changed -= SetVerticesDirty;
            base.OnDisable();
        }

        private void LateUpdate()
        {
            TargetFrameSO settings = Settings;

            if (settings == null)
                return;

            float delta = Time.unscaledDeltaTime;

            _alpha = Mathf.MoveTowards(_alpha, _tracking ? 1f : 0f, Mathf.Max(0.01f, settings.FadeSpeed) * delta);

            if (_tracking)
                _currentSize = Mathf.Lerp(_currentSize, _settledSize,
                    1f - Mathf.Exp(-Mathf.Max(0.01f, settings.StretchSpeed) * delta));

            bool shown = _alpha > 0f;

            if (_label != null && _label.enabled != shown)
                _label.enabled = shown;

            if (!shown)
            {
                color = WithAlpha(_frameColor, 0f);
                return;
            }

            rectTransform.position = new Vector3(_screenCenter.x, _screenCenter.y, 0f);
            rectTransform.sizeDelta = new Vector2(_currentSize, _currentSize);
            color = WithAlpha(_frameColor, _alpha);

            if (_label == null)
                return;

            _label.rectTransform.anchoredPosition = new Vector2(0f, settings.LabelOffset);

            if (settings.LabelUsesFrameColor)
                _label.color = WithAlpha(_frameColor, _alpha);
            else
                _label.alpha = _alpha;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            TargetFrameSO settings = Settings;

            if (settings == null)
                return;

            Rect rect = GetPixelAdjustedRect();
            float thickness = Mathf.Min(settings.LineThickness, rect.width * 0.5f, rect.height * 0.5f);

            if (thickness <= 0f)
                return;

            float armX = Mathf.Clamp(rect.width * settings.CornerLengthFraction, thickness, rect.width * 0.5f);
            float armY = Mathf.Clamp(rect.height * settings.CornerLengthFraction, thickness, rect.height * 0.5f);
            float xMin = rect.xMin;
            float xMax = rect.xMax;
            float yMin = rect.yMin;
            float yMax = rect.yMax;

            _vertex.color = color;

            AddQuad(vh, xMin, yMax - thickness, xMin + armX, yMax);
            AddQuad(vh, xMax - armX, yMax - thickness, xMax, yMax);
            AddQuad(vh, xMin, yMin, xMin + armX, yMin + thickness);
            AddQuad(vh, xMax - armX, yMin, xMax, yMin + thickness);

            AddQuad(vh, xMin, yMax - armY, xMin + thickness, yMax - thickness);
            AddQuad(vh, xMax - thickness, yMax - armY, xMax, yMax - thickness);
            AddQuad(vh, xMin, yMin + thickness, xMin + thickness, yMin + armY);
            AddQuad(vh, xMax - thickness, yMin + thickness, xMax, yMin + armY);
        }

        private void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax)
        {
            if (xMax - xMin <= 0f || yMax - yMin <= 0f)
                return;

            int index = vh.currentVertCount;

            _vertex.position = new Vector3(xMin, yMin, 0f);
            _vertex.uv0 = new Vector4(0f, 0f, 0f, 0f);
            vh.AddVert(_vertex);

            _vertex.position = new Vector3(xMin, yMax, 0f);
            _vertex.uv0 = new Vector4(0f, 1f, 0f, 0f);
            vh.AddVert(_vertex);

            _vertex.position = new Vector3(xMax, yMax, 0f);
            _vertex.uv0 = new Vector4(1f, 1f, 0f, 0f);
            vh.AddVert(_vertex);

            _vertex.position = new Vector3(xMax, yMin, 0f);
            _vertex.uv0 = new Vector4(1f, 0f, 0f, 0f);
            vh.AddVert(_vertex);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }

        private void SetLabel(string text)
        {
            if (_label == null)
                return;

            string value = text == null ? string.Empty : text;

            if (_label.text != value)
                _label.text = value;
        }

        private static TargetFrameSO Settings
        {
            get { return Core.Config == null ? null : Core.Config.TargetFrame; }
        }

        private TargetFrameSO ResolveSettings()
        {
            TargetFrameSO settings = Settings;

            if (settings != null)
                return settings;

            Debug.LogError($"{nameof(TargetFrame)}: {nameof(TargetFrameSO)} is null in {nameof(ResolveSettings)} on '{name}'. " +
                           "Required for the stroke width, padding, colours and animation of the frame. " +
                           "Fix: assign the TargetFrame asset to the Target Frame slot on the Core asset.", this);
            enabled = false;
            return null;
        }

        private float CanvasScale
        {
            get
            {
                Canvas root = canvas;

                if (root == null || root.scaleFactor <= 0f)
                    return 1f;

                return root.scaleFactor;
            }
        }

        private static Color WithAlpha(Color source, float alpha)
        {
            return new Color(source.r, source.g, source.b, source.a * alpha);
        }
    }
}
