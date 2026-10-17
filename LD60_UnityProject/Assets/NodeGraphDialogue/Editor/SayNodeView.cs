using NodeGraphAvr.Editor;

namespace NodeGraphAvr.Dialogue.Editor
{
    [CustomNodeView(typeof(SayNode))]
    public class SayNodeView : DialogueBubbleNodeView
    {
        protected override string GetBodyText() => ((SayNode)Node).Text;
    }
}
