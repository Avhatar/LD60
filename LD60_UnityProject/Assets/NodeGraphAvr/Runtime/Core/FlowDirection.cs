namespace NodeGraphAvr
{
    /// <summary>
    /// Visual flow direction of a graph kind: which way connections run on the canvas.
    /// Declared by each Graph subclass and consumed by the editor for port placement,
    /// connection tangents, and child auto-sorting.
    /// </summary>
    public enum FlowDirection
    {
        Vertical,
        Horizontal
    }
}
