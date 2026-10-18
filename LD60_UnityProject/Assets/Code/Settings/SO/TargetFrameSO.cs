using UnityEngine;

namespace LD60.Settings
{
    [CreateAssetMenu(fileName = "TargetFrame", menuName = "LD60/Target Frame")]
    public class TargetFrameSO : GameDataSO
    {
        [field: SerializeField, Tooltip("Stroke width of the frame, in UI units at the canvas reference resolution. " +
            "The mesh is rebuilt rather than scaled, so this stays the same however far the frame is stretched.")]
        public float LineThickness { get; private set; } = 2f;

        [field: SerializeField, Range(0f, 0.5f), Tooltip("Length of each corner bracket as a fraction of the side it sits on. " +
            "0.5 closes the four brackets into a solid rectangle, lower values open it into a targeting reticle.")]
        public float CornerLengthFraction { get; private set; } = 0.5f;

        [field: SerializeField, Tooltip("Gap between the edge of the target and the frame, in UI units.")]
        public float Padding { get; private set; } = 12f;

        [field: SerializeField, Tooltip("Smallest side the frame is allowed to shrink to, in UI units.")]
        public float MinSize { get; private set; } = 40f;

        [field: SerializeField, Tooltip("Largest side the frame is allowed to grow to, in UI units.")]
        public float MaxSize { get; private set; } = 700f;

        [field: SerializeField, Tooltip("How fast the frame stretches to a new size. Higher snaps harder.")]
        public float StretchSpeed { get; private set; } = 16f;

        [field: SerializeField, Tooltip("Size the frame opens from, as a multiple of the size it settles at.")]
        public float OpenSizeMultiplier { get; private set; } = 2f;

        [field: SerializeField, Tooltip("How fast the frame fades in and out, in alpha per second.")]
        public float FadeSpeed { get; private set; } = 14f;

        [field: SerializeField, Tooltip("Colour of the frame around our own spacecraft.")]
        public Color PlayerColor { get; private set; } = new Color(0.27f, 1f, 0.45f, 1f);

        [field: SerializeField, Tooltip("Colour of the frame that follows the pointer onto a celestial body, " +
            "and onto a spacecraft we have already talked to.")]
        public Color HoverColor { get; private set; } = new Color(0.85f, 0.94f, 1f, 1f);

        [field: SerializeField, Tooltip("Colour of the frame over a spacecraft we have not talked to yet, " +
            "the one drawn with question marks in place of a name.")]
        public Color UnknownColor { get; private set; } = new Color(1f, 0.86f, 0.2f, 1f);

        [field: SerializeField, Tooltip("Gap between the top edge of the frame and the bottom of its label, in UI units.")]
        public float LabelOffset { get; private set; } = 4f;

        [field: SerializeField, Tooltip("Tint the label with the frame colour. Disable to keep the colour authored on the label itself.")]
        public bool LabelUsesFrameColor { get; private set; } = true;

        [field: SerializeField, Tooltip("Font of the cooldown counter under the player frame.")]
        public TMPro.TMP_FontAsset CooldownFont { get; private set; }

        [field: SerializeField, Tooltip("Size of the cooldown counter, in UI units.")]
        public float CooldownFontSize { get; private set; } = 20f;

        [field: SerializeField, Tooltip("Colour of the cooldown counter.")]
        public Color CooldownColor { get; private set; } = new Color(0.65f, 0.95f, 1f, 0.9f);

        [field: SerializeField, Tooltip("How far below the player frame the counter sits, in UI units.")]
        public float CooldownOffset { get; private set; } = 34f;
    }
}
