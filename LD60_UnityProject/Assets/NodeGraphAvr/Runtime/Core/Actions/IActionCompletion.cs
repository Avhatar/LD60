namespace NodeGraphAvr
{
    /// <summary>
    /// Receives the result of one action. An interface rather than a delegate so that
    /// completing an action allocates nothing: runners and action sequences implement
    /// this directly instead of handing out closures.
    /// </summary>
    public interface IActionCompletion
    {
        void OnActionFinished(bool success);
    }
}
