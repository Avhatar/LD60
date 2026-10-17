using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Project-committed overrides of the authoring look of node, action and condition types:
    /// the code attributes are the defaults, this asset is what the project decided instead.
    /// Only overridden types are stored — the full list an author picks from is built from
    /// code in this asset's own inspector, so adding or deleting a type never rewrites the
    /// file and a deleted type leaves no residue. Editor-only chrome: nothing at runtime
    /// reads it, and a project without the asset simply uses the code defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "NodeGraphAvr/Node Style Config", fileName = "NodeStyleConfig")]
    public class NodeStyleConfig : ScriptableObject
    {
        [Tooltip("One entry per overridden type, keyed by [GraphTypeId]. Edited through this asset's inspector, which lists every type in the project and writes an entry only where the author changed something.")]
        [SerializeField] private List<NodeStyleOverride> _overrides = new List<NodeStyleOverride>();

        public IReadOnlyList<NodeStyleOverride> Overrides => _overrides;

        internal List<NodeStyleOverride> OverrideListInternal => _overrides;
    }
}
