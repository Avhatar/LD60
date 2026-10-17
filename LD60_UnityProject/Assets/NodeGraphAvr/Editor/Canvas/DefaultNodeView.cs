namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Fallback canvas view used for every node type that has no [CustomNodeView]
    /// registration. Adds nothing to the base: header, the base body (a hosted action's name,
    /// when there is one), and the resize corner.
    /// </summary>
    public class DefaultNodeView : NodeView
    {
    }
}
