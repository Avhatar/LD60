using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LD60.Settings;

namespace LD60.UI
{
    public class WattUi : MonoBehaviour
    {
        [SerializeField] private Image _bar;
        [SerializeField] private TMP_Text _wattLabel;
        [SerializeField] private RectTransform _changeRoot;
        [SerializeField] private TMP_Text _changeTemplate;

        private WattSO _settings;
        private TMP_Text[] _changes = new TMP_Text[0];
        private RectTransform[] _changeTransforms = new RectTransform[0];
        private float[] _changeAges = new float[0];
        private bool[] _changeLive = new bool[0];
        private Vector2 _changeStart;
        private float _shownFraction;
        private int _shownWatts = int.MinValue;
        private float _pendingDelta;
        private bool _dataDirty;

        private void OnEnable()
        {
            GameDataSO.Changed += OnDataChanged;
            Core.WattsChanged += OnWattsChanged;
        }

        private void OnDisable()
        {
            GameDataSO.Changed -= OnDataChanged;
            Core.WattsChanged -= OnWattsChanged;
        }

        private void OnDataChanged()
        {
            _dataDirty = true;
        }

        private void OnWattsChanged(float delta)
        {
            _pendingDelta += delta;
        }

        private void Start()
        {
            if (_bar == null)
            {
                Debug.LogError($"{nameof(WattUi)}: {nameof(_bar)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to draw how much of the pool is left. " +
                               "Fix: assign the WattBar object to the Bar slot on this object.", this);
                enabled = false;
                return;
            }

            if (_wattLabel == null)
            {
                Debug.LogError($"{nameof(WattUi)}: {nameof(_wattLabel)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to print how many watts we hold. " +
                               "Fix: assign the WattText object to the Watt Label slot on this object.", this);
                enabled = false;
                return;
            }

            if (_changeRoot == null || _changeTemplate == null)
            {
                Debug.LogError($"{nameof(WattUi)}: {nameof(_changeRoot)} or {nameof(_changeTemplate)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to spawn the text that flies up when watts are spent or handed back. " +
                               "Fix: assign WattTextChangeRoot to the Change Root slot and WattTextChange to the Change Template slot.", this);
                enabled = false;
                return;
            }

            if (Core.Config == null || Core.Config.Watt == null)
            {
                Debug.LogError($"{nameof(WattUi)}: {nameof(WattSO)} is null in {nameof(Start)} on '{name}'. " +
                               "Required to know the transmitter maximum, the bar colours and how the change text flies. " +
                               "Fix: assign a Watt asset to the Watt slot on the CoreSO asset.", this);
                enabled = false;
                return;
            }

            _settings = Core.Config.Watt;
            _changeStart = _changeTemplate.rectTransform.anchoredPosition;
            _changeTemplate.gameObject.SetActive(false);
            BuildChangePool();

            _shownFraction = Core.WattsFraction;
            ApplyBar();
            ApplyWattLabel();
        }

        private void LateUpdate()
        {
            if (_dataDirty)
                SyncData();

            if (_settings == null)
                return;

            float deltaSeconds = Time.unscaledDeltaTime;

            if (_pendingDelta != 0f)
            {
                SpawnChange(_pendingDelta);
                _pendingDelta = 0f;
            }

            AdvanceBar(deltaSeconds);
            TickChanges(deltaSeconds);
        }

        private void SyncData()
        {
            _dataDirty = false;

            if (Core.Config == null || Core.Config.Watt == null)
            {
                Debug.LogError($"{nameof(WattUi)}: {nameof(WattSO)} is null in {nameof(SyncData)} on '{name}'. " +
                               "Required to re-read the bar colours and the flight of the change text after a data edit. " +
                               "Fix: assign a Watt asset to the Watt slot on the CoreSO asset.", this);
                return;
            }

            _settings = Core.Config.Watt;
            _shownWatts = int.MinValue;
        }

        private void BuildChangePool()
        {
            int count = Mathf.Max(1, _settings.ChangePoolSize);

            _changes = new TMP_Text[count];
            _changeTransforms = new RectTransform[count];
            _changeAges = new float[count];
            _changeLive = new bool[count];

            for (int i = 0; i < count; i++)
            {
                TMP_Text text = Instantiate(_changeTemplate, _changeRoot);
                text.gameObject.SetActive(false);
                _changes[i] = text;
                _changeTransforms[i] = text.rectTransform;
            }
        }

        private void AdvanceBar(float deltaSeconds)
        {
            float target = Core.WattsFraction;

            if (_settings.BarCatchUpSeconds <= 0f)
                _shownFraction = target;
            else
                _shownFraction = Mathf.MoveTowards(_shownFraction, target, deltaSeconds / _settings.BarCatchUpSeconds);

            ApplyBar();
            ApplyWattLabel();
        }

        private void ApplyBar()
        {
            _bar.rectTransform.localScale = new Vector3(1f, _shownFraction, 1f);
            _bar.color = _settings.ColorFor(_shownFraction);
        }

        private void ApplyWattLabel()
        {
            int shown = Mathf.RoundToInt(_shownFraction * Core.WattsCapacity);

            if (shown == _shownWatts)
                return;

            _shownWatts = shown;
            _wattLabel.text = shown.ToString(_settings.WattsFormat);
        }

        private void SpawnChange(float delta)
        {
            int slot = FreeChangeSlot();
            TMP_Text text = _changes[slot];

            text.text = delta.ToString(_settings.ChangeFormat);
            text.color = delta >= 0f ? _settings.ChangeGainColor : _settings.ChangeSpendColor;
            text.alpha = 1f;

            _changeTransforms[slot].anchoredPosition = _changeStart;
            _changeAges[slot] = 0f;
            _changeLive[slot] = true;
            text.gameObject.SetActive(true);
        }

        private void TickChanges(float deltaSeconds)
        {
            for (int i = 0; i < _changes.Length; i++)
            {
                if (!_changeLive[i])
                    continue;

                _changeAges[i] += deltaSeconds;

                float progress = _settings.ChangeSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(_changeAges[i] / _settings.ChangeSeconds);

                if (progress >= 1f)
                {
                    _changeLive[i] = false;
                    _changes[i].gameObject.SetActive(false);
                    continue;
                }

                float eased = 1f - (1f - progress) * (1f - progress);
                _changeTransforms[i].anchoredPosition = _changeStart + new Vector2(0f, _settings.ChangeRise * eased);
                _changes[i].alpha = FadeFor(progress);
            }
        }

        private float FadeFor(float progress)
        {
            if (_settings.ChangeSolidFraction >= 1f || progress <= _settings.ChangeSolidFraction)
                return 1f;

            return 1f - Mathf.InverseLerp(_settings.ChangeSolidFraction, 1f, progress);
        }

        private int FreeChangeSlot()
        {
            for (int i = 0; i < _changeLive.Length; i++)
                if (!_changeLive[i])
                    return i;

            int oldest = 0;

            for (int i = 1; i < _changeAges.Length; i++)
                if (_changeAges[i] > _changeAges[oldest])
                    oldest = i;

            return oldest;
        }
    }
}
