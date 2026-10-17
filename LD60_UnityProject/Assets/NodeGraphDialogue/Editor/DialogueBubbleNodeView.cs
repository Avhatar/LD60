using NodeGraphAvr.Editor;

namespace NodeGraphAvr.Dialogue.Editor
{
    public abstract class DialogueBubbleNodeView : DefaultNodeView
    {
        private const int BubbleCornerRadius = 12;

        protected override int CornerRadius => BubbleCornerRadius;
    }
}
