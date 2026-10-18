using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LD60.Dialogue;
using LD60.Settings;
using LD60.Signals;
using LD60.Space;

namespace LD60.UI
{
    public class InterceptMarkers : MonoBehaviour
    {
        private class Marker
        {
            public RectTransform Root;
            public Image Halo;
            public Image Icon;
            public Image Check;
            public TMP_Text Label;
        }

        [SerializeField] private ForeignSignalDirector _director;
        [SerializeField] private RectTransform _root;
        [SerializeField] private TargetFrameUi _frames;

        private InterceptMarkerSO _settings;
        private UiTextSO _text;
        private Camera _camera;
        private Marker[] _markers = new Marker[0];
        private bool _dataDirty;

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
                Debug.LogError($"{nameof(InterceptMarkers)}: {nameof(_director)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to know which senders are discovered and how much of each is caught. " +
                               "Fix: assign the Foreign Signals object to the Director slot on this object.", this);
                enabled = false;
                return;
            }

            if (_root == null)
            {
                Debug.LogError($"{nameof(InterceptMarkers)}: {nameof(_root)} is null in {nameof(Start)} on '{name}'. " +
                               "Required as the canvas transform the markers are parented to. " +
                               "Fix: assign a Canvas RectTransform to the Root slot on this object.", this);
                enabled = false;
                return;
            }

            _camera = Camera.main;

            if (_camera == null)
            {
                Debug.LogError($"{nameof(InterceptMarkers)}: {nameof(Camera)}.{nameof(Camera.main)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to turn a body position into a screen position. " +
                               "Fix: tag the scene camera as MainCamera.", this);
                enabled = false;
                return;
            }

            SyncData();
        }

        private void SyncData()
        {
            _dataDirty = false;

            if (Core.Config == null)
            {
                Debug.LogError($"{nameof(InterceptMarkers)}: {nameof(Core)}.{nameof(Core.Config)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to read the marker look and the localized lines. " +
                               "Fix: add a Core object to the scene and assign the CoreSO asset to its Config slot.", this);
                return;
            }

            _settings = Core.Config.InterceptMarker;
            _text = Core.Config.UiText;

            if (_settings == null)
            {
                Debug.LogError($"{nameof(InterceptMarkers)}: {nameof(InterceptMarkerSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to know the font, icon, colours and offsets of the markers. " +
                               "Fix: assign an InterceptMarker asset to the Intercept Marker slot on the CoreSO asset.", this);
                return;
            }

            if (_text == null)
            {
                Debug.LogError($"{nameof(InterceptMarkers)}: {nameof(UiTextSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to read the localized marker lines. " +
                               "Fix: assign the UiText asset to the Ui Text slot on the CoreSO asset.", this);
                return;
            }

            for (int i = 0; i < _markers.Length; i++)
                ApplyLook(_markers[i]);
        }

        private void LateUpdate()
        {
            if (_dataDirty)
                SyncData();

            if (_settings == null || _text == null)
                return;

            Resize(_director.SenderCount);

            for (int i = 0; i < _markers.Length; i++)
            {
                Vector2 world;

                if (!Core.IsGameMode || !_director.IsDiscovered(i) || IsHovered(i)
                    || !_director.TryGetSenderPosition(i, out world))
                {
                    Show(i, false);
                    continue;
                }

                Show(i, true);
                Paint(i);
                _markers[i].Root.position = (Vector3)((Vector2)_camera.WorldToScreenPoint(world)
                                           + _settings.ScreenOffset
                                           + new Vector2(0f, StackOffset(i)));
            }
        }

        private bool IsHovered(int index)
        {
            if (_frames == null)
                return false;

            ActSpacecraftSO sender = _director.GetSender(index);

            if (_frames.HoveredBody != null && _frames.HoveredBody == sender.Body)
                return true;

            return _frames.HoveredCraft != null && _frames.HoveredCraft == sender.Spacecraft;
        }

        private void Paint(int index)
        {
            bool contacted = _director.IsContacted(index);
            bool ready = _director.IsAwaitingContact(index);
            bool onBody = _director.GetSender(index).Body != null;
            SpacecraftSO craft = _director.GetSender(index).Spacecraft;
            Marker marker = _markers[index];

            Sprite craftIcon = craft != null ? IconFor(craft) : null;
            bool showsCraftIcon = contacted && onBody && craftIcon != null;

            Color color = ready || contacted ? _settings.UnlockedColor : _settings.Color;
            marker.Icon.color = showsCraftIcon ? Color.white : color;
            marker.Icon.sprite = PickIcon(contacted, showsCraftIcon, craftIcon);
            marker.Icon.enabled = marker.Icon.sprite != null;

            marker.Check.enabled = showsCraftIcon && _settings.ContactedIcon != null;
            marker.Check.color = _settings.UnlockedColor;

            marker.Halo.enabled = showsCraftIcon && marker.Icon.enabled;
            marker.Halo.sprite = marker.Icon.sprite;
            marker.Halo.color = _settings.ReadyRingColor;

            marker.Label.enabled = !contacted;
            marker.Label.color = color;

            if (contacted)
                return;

            marker.Label.text = string.Format(_text.InterceptProgressFormat.Value,
                _director.CapturedDots(index),
                _director.GetSender(index).DotsToCapture);
        }

        private Sprite PickIcon(bool contacted, bool showsCraftIcon, Sprite craftIcon)
        {
            if (!contacted)
                return _settings.UnknownIcon;

            if (showsCraftIcon)
                return craftIcon;

            return _settings.ContactedIcon != null ? _settings.ContactedIcon : craftIcon;
        }

        private static Sprite IconFor(SpacecraftSO craft)
        {
            return craft.SpaceSprite != null ? craft.SpaceSprite : craft.DialoguePortrait;
        }

        private float StackOffset(int index)
        {
            int above = 0;

            CelestialBodySO body = _director.GetSender(index).Body;

            if (body == null)
                return 0f;

            for (int i = 0; i < index; i++)
            {
                if (_director.IsDiscovered(i) && _director.GetSender(i).Body == body)
                    above++;
            }

            return above * _settings.StackSpacing;
        }

        private void Show(int index, bool shown)
        {
            if (_markers[index].Root.gameObject.activeSelf != shown)
                _markers[index].Root.gameObject.SetActive(shown);
        }

        private void Resize(int count)
        {
            if (_markers.Length == count)
                return;

            for (int i = count; i < _markers.Length; i++)
            {
                if (_markers[i] != null)
                    Destroy(_markers[i].Root.gameObject);
            }

            Marker[] resized = new Marker[count];
            int kept = Mathf.Min(count, _markers.Length);

            for (int i = 0; i < kept; i++)
                resized[i] = _markers[i];

            for (int i = kept; i < count; i++)
                resized[i] = CreateMarker();

            _markers = resized;
        }

        private Marker CreateMarker()
        {
            Marker marker = new Marker();

            GameObject root = new GameObject("Intercept Marker");
            root.transform.SetParent(_root, false);
            marker.Root = root.AddComponent<RectTransform>();

            GameObject halo = new GameObject("Halo");
            halo.transform.SetParent(marker.Root, false);
            marker.Halo = halo.AddComponent<Image>();
            marker.Halo.raycastTarget = false;
            marker.Halo.preserveAspect = true;
            marker.Halo.enabled = false;
            marker.Halo.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            marker.Halo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            marker.Halo.rectTransform.pivot = new Vector2(1f, 0.5f);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(marker.Root, false);
            marker.Icon = icon.AddComponent<Image>();
            marker.Icon.raycastTarget = false;
            marker.Icon.preserveAspect = true;
            marker.Icon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            marker.Icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            marker.Icon.rectTransform.pivot = new Vector2(1f, 0.5f);

            GameObject check = new GameObject("Check");
            check.transform.SetParent(marker.Root, false);
            marker.Check = check.AddComponent<Image>();
            marker.Check.raycastTarget = false;
            marker.Check.preserveAspect = true;
            marker.Check.enabled = false;
            marker.Check.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            marker.Check.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            marker.Check.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            GameObject label = new GameObject("Label");
            label.transform.SetParent(marker.Root, false);
            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Left;
            text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.pivot = new Vector2(0f, 0.5f);
            marker.Label = text;

            ApplyLook(marker);
            return marker;
        }

        private void ApplyLook(Marker marker)
        {
            if (_settings == null)
                return;

            marker.Root.sizeDelta = new Vector2(_settings.LabelWidth + _settings.IconSize + _settings.IconGap,
                                                _settings.IconSize);

            marker.Icon.rectTransform.sizeDelta = new Vector2(_settings.IconSize, _settings.IconSize);
            marker.Icon.rectTransform.anchoredPosition = new Vector2(-_settings.IconGap * 0.5f, 0f);

            float halo = _settings.IconSize * _settings.IconOutlineScale;
            marker.Halo.rectTransform.sizeDelta = new Vector2(halo, halo);
            marker.Halo.rectTransform.anchoredPosition =
                new Vector2(-_settings.IconGap * 0.5f + (halo - _settings.IconSize) * 0.5f, 0f);

            marker.Check.sprite = _settings.ContactedIcon;

            float badge = _settings.IconSize * _settings.ContactedBadgeScale;
            marker.Check.rectTransform.sizeDelta = new Vector2(badge, badge);
            marker.Check.rectTransform.anchoredPosition =
                new Vector2(-_settings.IconGap * 0.5f - badge * 0.3f, -_settings.IconSize * 0.5f + badge * 0.3f);

            marker.Label.font = _settings.Font;
            marker.Label.fontSize = _settings.FontSize;
            marker.Label.rectTransform.sizeDelta = new Vector2(_settings.LabelWidth, _settings.IconSize);
            marker.Label.rectTransform.anchoredPosition = new Vector2(_settings.IconGap * 0.5f, 0f);
        }
    }
}
