using UnityEngine;

namespace LD60.Signals
{
    [CreateAssetMenu(fileName = "Signal", menuName = "LD60/Signal")]
    public class SignalSO : ScriptableObject
    {
        [field: SerializeField] public float BaseSpeed { get; private set; } = 3f;
        [field: SerializeField] public float MinSpeedMultiplier { get; private set; } = 0.5f;
        [field: SerializeField] public float MaxSpeedMultiplier { get; private set; } = 3f;
        [field: SerializeField] public float DefaultSpeedMultiplier { get; private set; } = 1f;
        [field: SerializeField] public float SpeedDragSensitivity { get; private set; } = 0.01f;

        [field: SerializeField] public float MinWidthDegrees { get; private set; } = 1.5f;
        [field: SerializeField] public float MaxWidthDegrees { get; private set; } = 30f;
        [field: SerializeField] public float DefaultWidthDegrees { get; private set; } = 6f;
        [field: SerializeField] public float WidthScrollStepDegrees { get; private set; } = 1f;

        [field: SerializeField] public float MaxTravelRadius { get; private set; } = 26f;

        [field: SerializeField] public float BaseCostWatts { get; private set; } = 2f;
        [field: SerializeField] public float CostPerWidthDegree { get; private set; } = 0.35f;
        [field: SerializeField] public float CostPerSpeedMultiplier { get; private set; } = 1.5f;

        [field: SerializeField] public Color PreviewColor { get; private set; } = new Color(0.40f, 0.80f, 1f, 0.35f);
        [field: SerializeField] public Color SignalColor { get; private set; } = new Color(0.60f, 0.95f, 1f, 0.95f);
        [field: SerializeField] public Color HitFlashColor { get; private set; } = Color.white;
        [field: SerializeField] public float HitFlashSeconds { get; private set; } = 0.45f;
        [field: SerializeField] public float PreviewLineWidth { get; private set; } = 0.025f;
        [field: SerializeField] public float SignalLineWidth { get; private set; } = 0.06f;
        [field: SerializeField] public int ArcSegments { get; private set; } = 24;

        public float CostFor(float widthDegrees, float speedMultiplier)
        {
            return BaseCostWatts + widthDegrees * CostPerWidthDegree + speedMultiplier * CostPerSpeedMultiplier;
        }

        public float ClampWidth(float widthDegrees)
        {
            return Mathf.Clamp(widthDegrees, MinWidthDegrees, MaxWidthDegrees);
        }

        public float ClampSpeedMultiplier(float speedMultiplier)
        {
            return Mathf.Clamp(speedMultiplier, MinSpeedMultiplier, MaxSpeedMultiplier);
        }
    }
}
