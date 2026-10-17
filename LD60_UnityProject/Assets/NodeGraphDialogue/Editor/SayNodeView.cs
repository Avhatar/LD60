using NodeGraphAvr.Editor;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Canvas view of a SayNode: a chat-bubble frame whose body is the spoken line itself, as
    /// much of it as the node's size shows. Resizing the node is how an author decides how much
    /// of a long line to read on the canvas.
    /// </summary>
    [CustomNodeView(typeof(SayNode))]
    public class SayNodeView : DialogueBubbleNodeView
    {
        protected override string GetBodyText() => ((SayNode)Node).Text;
    }
}
