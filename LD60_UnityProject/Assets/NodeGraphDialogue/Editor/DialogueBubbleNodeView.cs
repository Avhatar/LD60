using NodeGraphAvr.Editor;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Shared canvas look of the dialogue nodes that carry spoken content: a rounded
    /// chat-bubble window instead of the square editor one, so a graph reads as a
    /// conversation at a glance. Shape only — behaviour, input, and layout stay the default.
    /// </summary>
    public abstract class DialogueBubbleNodeView : DefaultNodeView
    {
        private const int BubbleCornerRadius = 12;

        protected override int CornerRadius => BubbleCornerRadius;
    }
}
