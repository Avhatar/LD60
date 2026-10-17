using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    /// <summary>
    /// Marks an ActorData field whose inspector should offer a dropdown restricted to
    /// the owning dialogue graph's declared actors plus the dialogue system config's
    /// global ones, instead of a free object field.
    /// </summary>
    public sealed class ActorPickerAttribute : PropertyAttribute
    {
    }
}
