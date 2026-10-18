using UnityEngine;
using TMPro;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "InterceptMarker", menuName = "LD60/Intercept Marker")]
    public class InterceptMarkerSO : GameDataSO
    {
        [field: SerializeField, Tooltip("Font the marker label is drawn with. Should match the body labels.")]
        public TMP_FontAsset Font { get; private set; }

        [field: SerializeField, Tooltip("Icon shown while we know someone is there but not who.")]
        public Sprite UnknownIcon { get; private set; }

        [field: SerializeField, Tooltip("Label size, in UI units at the canvas reference resolution.")]
        public float FontSize { get; private set; } = 22f;

        [field: SerializeField, Tooltip("Side of the square icon, in UI units.")]
        public float IconSize { get; private set; } = 22f;

        [field: SerializeField, Tooltip("Gap between the icon and the label, in UI units.")]
        public float IconGap { get; private set; } = 4f;

        [field: SerializeField, Tooltip("Colour while the sender is found but not yet reachable.")]
        public Color Color { get; private set; } = new Color(0.95f, 0.72f, 0.35f, 0.95f);

        [field: SerializeField, Tooltip("Colour once enough of its signal was caught to talk to it.")]
        public Color UnlockedColor { get; private set; } = new Color(0.45f, 1f, 0.55f, 0.95f);

        [field: SerializeField, Tooltip("How far above the body the marker sits, in UI units.")]
        public Vector2 ScreenOffset { get; private set; } = new Vector2(0f, 30f);

        [field: SerializeField, Tooltip("Vertical step between markers that share one body, in UI units.")]
        public float StackSpacing { get; private set; } = 22f;

        [field: SerializeField, Tooltip("Width of the label box, in UI units.")]
        public float LabelWidth { get; private set; } = 170f;

        [field: SerializeField, Tooltip("Colour of the ring around an object that has enough caught dots and is waiting to be shot.")]
        public Color ReadyRingColor { get; private set; } = new Color(0.45f, 1f, 0.55f, 0.9f);

        [field: SerializeField, Tooltip("Gap between the object's edge and its ring, in world units.")]
        public float ReadyRingMargin { get; private set; } = 0.12f;

        [field: SerializeField, Tooltip("Stroke width of the ring, in world units.")]
        public float ReadyRingWidth { get; private set; } = 0.035f;

        [field: SerializeField, Tooltip("Segments the ring is drawn with.")]
        public int ReadyRingSegments { get; private set; } = 48;

        [field: SerializeField, Tooltip("How much larger the green halo behind a contacted icon is.")]
        public float IconOutlineScale { get; private set; } = 1.25f;
    }
}
