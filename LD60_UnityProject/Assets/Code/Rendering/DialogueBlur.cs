using UnityEngine;
using UnityEngine.Rendering;

namespace LD60.Rendering
{
    [RequireComponent(typeof(Volume))]
    public class DialogueBlur : MonoBehaviour
    {
        private Volume _volume;
        private float _target;

        private void Awake()
        {
            _volume = GetComponent<Volume>();

            if (_volume == null)
            {
                Debug.LogError($"{nameof(DialogueBlur)}: {nameof(_volume)} is null in {nameof(Awake)}. " +
                               "Required as the volume whose weight fades the dialogue blur in and out. " +
                               "Fix: add a Volume component to this object and give it a profile holding a Blur Vignette override.", this);
                enabled = false;
                return;
            }

            _volume.weight = 0f;
        }

        private void OnEnable()
        {
            Core.ModeChanged += OnModeChanged;
            _target = Core.IsDialogueMode ? 1f : 0f;
            _volume.weight = _target;
        }

        private void OnDisable()
        {
            Core.ModeChanged -= OnModeChanged;
        }

        private void Update()
        {
            if (_volume.weight == _target)
                return;

            float fadeSeconds = FadeSeconds;

            if (fadeSeconds <= 0f)
            {
                _volume.weight = _target;
                return;
            }

            _volume.weight = Mathf.MoveTowards(_volume.weight, _target, Time.unscaledDeltaTime / fadeSeconds);
        }

        private void OnModeChanged(GameMode mode)
        {
            _target = mode == GameMode.Dialogue ? 1f : 0f;
        }

        private static float FadeSeconds
        {
            get
            {
                if (Core.Config == null || Core.Config.Dialogue == null)
                    return 0f;

                return Core.Config.Dialogue.BlurFadeSeconds;
            }
        }
    }
}
