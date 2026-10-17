using UnityEngine;

namespace LD60.Signals
{
    [CreateAssetMenu(fileName = "Signal", menuName = "LD60/Signal")]
    public class SignalSO : GameDataSO
    {
        [field: Tooltip("Speed when the pointer sits on top of the spacecraft.")]
        [field: SerializeField] public float MinSpeed { get; private set; } = 1.5f;

        [field: Tooltip("Speed when the pointer is at or past the far distance below.")]
        [field: SerializeField] public float MaxSpeed { get; private set; } = 12f;

        [field: Tooltip("Pointer distance from the spacecraft that maps to Min Speed.")]
        [field: SerializeField] public float SpeedNearDistance { get; private set; } = 1f;

        [field: Tooltip("Pointer distance from the spacecraft that maps to Max Speed.")]
        [field: SerializeField] public float SpeedFarDistance { get; private set; } = 14f;

        [field: SerializeField] public float MinWidthDegrees { get; private set; } = 1.5f;
        [field: SerializeField] public float MaxWidthDegrees { get; private set; } = 40f;
        [field: SerializeField] public float DefaultWidthDegrees { get; private set; } = 8f;
        [field: SerializeField] public float WidthScrollStepDegrees { get; private set; } = 1f;

        [field: Tooltip("Dots in the arc at the shortest possible hold.")]
        [field: SerializeField] public int MinDots { get; private set; } = 3;

        [field: Tooltip("Dots in the arc at full charge.")]
        [field: SerializeField] public int MaxDots { get; private set; } = 48;

        [field: Tooltip("How long the fire button must be held to reach Max Dots.")]
        [field: SerializeField] public float SecondsToFullPower { get; private set; } = 1.2f;

        [field: SerializeField] public float DotSize { get; private set; } = 0.13f;

        [field: Tooltip("Gap between a landed dot and the visible edge of the body it stuck to.")]
        [field: SerializeField] public float LandedSurfaceOffset { get; private set; } = 0.05f;
        [field: SerializeField] public float PreviewArcRadius { get; private set; } = 2.2f;
        [field: SerializeField] public float MaxTravelRadius { get; private set; } = 26f;
        [field: SerializeField] public bool PauseWhileAiming { get; private set; }

        [field: SerializeField] public float BaseCostWatts { get; private set; } = 1f;
        [field: SerializeField] public float CostPerWidthDegree { get; private set; } = 0.15f;
        [field: SerializeField] public float CostPerSpeedUnit { get; private set; } = 0.4f;
        [field: SerializeField] public float CostPerDot { get; private set; } = 0.25f;

        [field: SerializeField] public Color PreviewColor { get; private set; } = new Color(0.40f, 0.80f, 1f, 0.35f);
        [field: SerializeField] public Color SignalColor { get; private set; } = new Color(0.60f, 0.95f, 1f, 0.95f);
        [field: SerializeField] public Color HitFlashColor { get; private set; } = Color.white;
        [field: SerializeField] public float HitFlashSeconds { get; private set; } = 0.45f;
        [field: SerializeField] public float PreviewLineWidth { get; private set; } = 0.025f;
        [field: SerializeField] public int ArcSegments { get; private set; } = 24;

        public float ClampWidth(float widthDegrees)
        {
            return Mathf.Clamp(widthDegrees, MinWidthDegrees, MaxWidthDegrees);
        }

        public float SpeedForPointerDistance(float distance)
        {
            if (SpeedFarDistance <= SpeedNearDistance)
                return MinSpeed;

            return Mathf.Lerp(MinSpeed, MaxSpeed, Mathf.InverseLerp(SpeedNearDistance, SpeedFarDistance, distance));
        }

        public int DotsForHoldSeconds(float heldSeconds)
        {
            if (SecondsToFullPower <= 0f)
                return MaxDots;

            return Mathf.RoundToInt(Mathf.Lerp(MinDots, MaxDots, Mathf.Clamp01(heldSeconds / SecondsToFullPower)));
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
