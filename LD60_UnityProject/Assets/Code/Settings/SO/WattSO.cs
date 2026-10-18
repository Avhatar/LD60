using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "Watt", menuName = "LD60/Watt")]
    public class WattSO : GameDataSO
    {
        [field: Tooltip("Everything our transmitter can hold. The bar is full at this value and every grant is clamped to it.")]
        [field: SerializeField] public float MaxWatts { get; private set; } = 120f;

        [field: Tooltip("Watts one dot of our own volley costs. The whole volley is charged as one number.")]
        [field: SerializeField] public float WattsPerDot { get; private set; } = 1f;

        [field: Tooltip("Watts handed back for every foreign dot one of our dots catches.")]
        [field: SerializeField] public float RefundPerCaughtDot { get; private set; } = 1f;

        [field: Tooltip("Seconds the bar needs to travel its whole length while catching up with the pool. Zero snaps.")]
        [field: SerializeField] public float BarCatchUpSeconds { get; private set; } = 0.35f;

        [field: Tooltip("Colour of a full pool.")]
        [field: SerializeField] public Color FullColor { get; private set; } = new Color(0.40f, 0.78f, 0.31f, 0.77f);

        [field: Tooltip("Colour at the share of the pool set below. The green and the red run into it from both sides.")]
        [field: SerializeField] public Color MidColor { get; private set; } = new Color(0.93f, 0.85f, 0.29f, 0.77f);

        [field: Tooltip("Colour of an empty pool.")]
        [field: SerializeField] public Color EmptyColor { get; private set; } = new Color(0.90f, 0.26f, 0.22f, 0.77f);

        [field: Tooltip("Share of the pool that reads as the middle colour.")]
        [field: SerializeField, Range(0f, 1f)] public float MidFraction { get; private set; } = 0.5f;

        [field: Tooltip("Number format of the watts we hold.")]
        [field: SerializeField] public string WattsFormat { get; private set; } = "0";

        [field: Tooltip("Number format of the flying change text. The second half is used for losses.")]
        [field: SerializeField] public string ChangeFormat { get; private set; } = "+0;-0";

        [field: Tooltip("How far the change text flies up before it is gone, in UI units.")]
        [field: SerializeField] public float ChangeRise { get; private set; } = 80f;

        [field: Tooltip("How long the change text lives.")]
        [field: SerializeField] public float ChangeSeconds { get; private set; } = 1.1f;

        [field: Tooltip("Share of the flight the text stays solid for. The rest of it fades out.")]
        [field: SerializeField, Range(0f, 1f)] public float ChangeSolidFraction { get; private set; } = 0.3f;

        [field: Tooltip("Colour of a gain.")]
        [field: SerializeField] public Color ChangeGainColor { get; private set; } = new Color(0.45f, 1f, 0.55f, 1f);

        [field: Tooltip("Colour of a spend.")]
        [field: SerializeField] public Color ChangeSpendColor { get; private set; } = new Color(1f, 0.37f, 0.37f, 1f);

        [field: Tooltip("How many change texts can fly at once. The oldest is recycled when they all do.")]
        [field: SerializeField] public int ChangePoolSize { get; private set; } = 6;

        public Color ColorFor(float fraction)
        {
            if (fraction >= MidFraction)
            {
                float toFull = MidFraction >= 1f ? 1f : Mathf.InverseLerp(MidFraction, 1f, fraction);
                return Color.Lerp(MidColor, FullColor, toFull);
            }

            float toMid = MidFraction <= 0f ? 1f : Mathf.InverseLerp(0f, MidFraction, fraction);
            return Color.Lerp(EmptyColor, MidColor, toMid);
        }
    }
}
