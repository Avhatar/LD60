namespace NodeGraphAvr
{
    /// <summary>
    /// Where a group frame's comment is rendered on the canvas relative to the frame.
    /// None keeps the comment hidden behind the hover tooltip; any side renders it as
    /// permanently visible text next to that edge of the frame.
    /// </summary>
    public enum GroupCommentPlacement
    {
        None,
        Top,
        Right,
        Bottom,
        Left
    }
}
