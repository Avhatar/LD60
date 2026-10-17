namespace NodeGraphAvr
{
    /// <summary>
    /// Implemented by a node whose whole purpose is to run one action. One node carries
    /// exactly one action — several actions in a row are several nodes in a row, which
    /// keeps ordering visible on the canvas instead of hidden in a list. The editor uses
    /// this to let the assigned action describe the node, most visibly by overriding its
    /// tint with the action type's own [NodeColor].
    /// </summary>
    public interface IActionHost
    {
        GraphAction Action { get; }
    }
}
