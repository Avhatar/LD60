using UnityEngine;
using LD60.Settings;

namespace LD60.Signals
{
    [CreateAssetMenu(fileName = "Signal", menuName = "LD60/Signal")]
    public class SignalSO : GameDataSO
    {
        [field: Tooltip("Speed of the narrowest possible volley. Narrow means fast.")]
        [field: SerializeField] public float MinSpeed { get; private set; } = 1.5f;

        [field: Tooltip("Speed of the widest possible volley. Wide means slow.")]
        [field: SerializeField] public float MaxSpeed { get; private set; } = 12f;

        [field: SerializeField] public float MinWidthDegrees { get; private set; } = 1.5f;
        [field: SerializeField] public float MaxWidthDegrees { get; private set; } = 40f;
        [field: SerializeField] public float DefaultWidthDegrees { get; private set; } = 8f;
        [field: SerializeField] public float WidthScrollStepDegrees { get; private set; } = 1f;

        [field: Tooltip("Dots in the arc at the shortest possible hold.")]
        [field: SerializeField] public int MinDots { get; private set; } = 3;

        [field: Tooltip("Dots in the arc at full charge.")]
        [field: SerializeField] public int MaxDots { get; private set; } = 48;

        [field: Tooltip("Dots packed into one degree of arc. The whole dot count follows from the angle.")]
        [field: SerializeField] public float DotsPerDegree { get; private set; } = 1.2f;

        [field: Tooltip("Quiet period after a dot is caught before the intercept dialogue starts. Every further dot caught from the same transmission restarts it, so the line waits for the whole volley to arrive.")]
        [field: SerializeField] public float DialogueDebounceSeconds { get; private set; } = 0.2f;

        [field: Tooltip("Seconds of cooldown each dot in the volley costs.")]
        [field: SerializeField] public float CooldownSecondsPerDot { get; private set; } = 1f;

        [field: SerializeField] public float DotSize { get; private set; } = 0.13f;

        [field: Tooltip("How soft the rim of a dot is. Zero is a hard-edged circle, one is a blurred blob.")]
        [field: SerializeField, Range(0f, 1f)] public float DotEdgeSoftness { get; private set; } = 0.35f;

        [field: Tooltip("Gap between a landed dot and the visible edge of the body it stuck to.")]
        [field: SerializeField] public float LandedSurfaceOffset { get; private set; } = 0.05f;
        [field: SerializeField] public float PreviewArcRadius { get; private set; } = 2.2f;
        [field: SerializeField] public float MaxTravelRadius { get; private set; } = 26f;

        [field: Tooltip("How long a dot lives while it is still flying and has touched nothing. Long enough to leave the screen.")]
        [field: SerializeField] public float DotFlightLifetimeSeconds { get; private set; } = 30f;

        [field: Tooltip("How far past the edge of the view a dot may drift before it is dropped. Stops captures happening where the player cannot see them.")]
        [field: SerializeField] public float OffscreenMargin { get; private set; } = 1f;

        [field: Tooltip("How long a dot lives after it touched something - landed on an object or caught another dot.")]
        [field: SerializeField] public float DotLifetimeSeconds { get; private set; } = 10f;

        [field: Tooltip("How close our dot has to pass a foreign dot to catch it. Kept generous on purpose, dot-on-dot precision would read as a lottery.")]
        [field: SerializeField] public float CaptureRadius { get; private set; } = 0.22f;

        [field: Tooltip("Played when we transmit.")]
        [field: SerializeField] public SoundContainerSO OursSound { get; private set; }

        [field: Tooltip("Played when another spacecraft transmits.")]
        [field: SerializeField] public SoundContainerSO ForeignSound { get; private set; }

        [field: Tooltip("Played for each dot of a foreign transmission we catch.")]
        [field: SerializeField] public SoundContainerSO ReplySound { get; private set; }

        [field: Tooltip("Played once, on the dot that completes a sender and opens contact.")]
        [field: SerializeField] public SoundContainerSO ContactSound { get; private set; }

        [field: Tooltip("Played on the dot that lands on an unlocked sender and opens the direct conversation.")]
        [field: SerializeField] public SoundContainerSO ContactShotSound { get; private set; }

        [field: Tooltip("How long the whole transmission takes to dim away at the end of its lifetime.")]
        [field: SerializeField] public float DotFadeOutSeconds { get; private set; } = 1.5f;

        [field: Tooltip("How long a dot takes to cross-fade from its travelling colour into its arrived colour.")]
        [field: SerializeField] public float DotColorBlendSeconds { get; private set; } = 0.35f;

        [field: Tooltip("Colour a dot turns when it lands on the body the transmission was addressed to.")]
        [field: SerializeField] public Color ArrivedColor { get; private set; } = new Color(0.45f, 1f, 0.55f, 0.95f);

        [field: Tooltip("Brightness multiplier of an arrived dot. Above one pushes it into bloom.")]
        [field: SerializeField] public float ArrivedDotEmission { get; private set; } = 2f;

        [field: Tooltip("Colour of transmissions sent by other spacecraft.")]
        [field: SerializeField] public Color ForeignColor { get; private set; } = new Color(0.95f, 0.72f, 0.35f, 0.9f);

        [field: Tooltip("Brightness multiplier of dots sent by other spacecraft. Above one pushes them into bloom.")]
        [field: SerializeField] public float ForeignDotEmission { get; private set; } = 1.2f;

        [field: Tooltip("Colour of transmissions from a spacecraft we have already made contact with.")]
        [field: SerializeField] public Color StudiedColor { get; private set; } = new Color(1f, 1f, 1f, 0.9f);

        [field: SerializeField] public float StudiedDotEmission { get; private set; } = 1.2f;

        [field: Tooltip("Colour of transmissions from a spacecraft whose every line we have already heard. Faded on purpose - nothing new to get here.")]
        [field: SerializeField] public Color ExhaustedColor { get; private set; } = new Color(0.85f, 0.85f, 0.9f, 0.35f);

        [field: SerializeField] public float ExhaustedDotEmission { get; private set; } = 0.5f;

        [field: Tooltip("Colour our own dot turns when it catches a transmission that has no lines left.")]
        [field: SerializeField] public Color ExhaustedCaptureColor { get; private set; } = new Color(1f, 1f, 1f, 0.95f);

        [field: Tooltip("Colour a foreign dot turns when it trips over a dot from another transmission. Both dots stop there and fade.")]
        [field: SerializeField] public Color TrippedColor { get; private set; } = new Color(1f, 1f, 1f, 0.95f);

        [field: SerializeField] public float TrippedDotEmission { get; private set; } = 1.6f;

        [field: Tooltip("Dot size of transmissions sent by other spacecraft.")]
        [field: SerializeField] public float ForeignDotSize { get; private set; } = 0.11f;

        [field: Tooltip("How far along its path a foreign transmission first becomes visible. Zero means it leaves its sender in plain sight.")]
        [field: SerializeField, Range(0f, 0.9f)] public float ForeignSpawnFraction { get; private set; }
        [field: SerializeField] public bool PauseWhileAiming { get; private set; }

        [field: SerializeField] public float BaseCostWatts { get; private set; } = 1f;
        [field: SerializeField] public float CostPerWidthDegree { get; private set; } = 0.15f;
        [field: SerializeField] public float CostPerSpeedUnit { get; private set; } = 0.4f;
        [field: SerializeField] public float CostPerDot { get; private set; } = 0.25f;

        [field: SerializeField] public Color PreviewColor { get; private set; } = new Color(0.40f, 0.80f, 1f, 0.35f);

        [field: Tooltip("Brightness multiplier of the aim preview dots.")]
        [field: SerializeField] public float PreviewDotEmission { get; private set; } = 0.8f;

        [field: SerializeField] public Color SignalColor { get; private set; } = new Color(0.60f, 0.95f, 1f, 0.95f);

        [field: Tooltip("Brightness multiplier of your own dots. Above one pushes them into bloom.")]
        [field: SerializeField] public float DotEmission { get; private set; } = 1.4f;

        [field: SerializeField] public Color HitFlashColor { get; private set; } = Color.white;
        [field: SerializeField] public float HitFlashSeconds { get; private set; } = 0.45f;
        [field: SerializeField] public float PreviewLineWidth { get; private set; } = 0.025f;
        [field: SerializeField] public int ArcSegments { get; private set; } = 24;

        public float FadeForRemaining(float secondsLeft)
        {
            if (secondsLeft <= 0f)
                return 0f;

            if (DotFadeOutSeconds <= 0f)
                return 1f;

            return Mathf.Clamp01(secondsLeft / DotFadeOutSeconds);
        }

        public float FadeForAge(float ageSeconds)
        {
            if (DotFadeOutSeconds <= 0f)
                return 1f;

            return Mathf.Clamp01((DotLifetimeSeconds - ageSeconds) / DotFadeOutSeconds);
        }

        public float AdvanceColorBlend(float blend, float deltaSeconds)
        {
            if (DotColorBlendSeconds <= 0f)
                return 1f;

            return Mathf.Min(1f, blend + deltaSeconds / DotColorBlendSeconds);
        }

        public float ClampWidth(float widthDegrees)
        {
            return Mathf.Clamp(widthDegrees, MinWidthDegrees, MaxWidthDegrees);
        }

        public int DotsForWidth(float widthDegrees)
        {
            return Mathf.Clamp(Mathf.RoundToInt(widthDegrees * DotsPerDegree), MinDots, MaxDots);
        }

        public float SpeedForWidth(float widthDegrees)
        {
            if (MaxWidthDegrees <= MinWidthDegrees)
                return MaxSpeed;

            return Mathf.Lerp(MaxSpeed, MinSpeed, Mathf.InverseLerp(MinWidthDegrees, MaxWidthDegrees, widthDegrees));
        }

        public float CooldownForDots(int dots)
        {
            return dots * CooldownSecondsPerDot;
        }

        public float CostFor(float widthDegrees, float speed, int dots)
        {
            return BaseCostWatts
                 + widthDegrees * CostPerWidthDegree
                 + speed * CostPerSpeedUnit
                 + dots * CostPerDot;
        }
    }
}
