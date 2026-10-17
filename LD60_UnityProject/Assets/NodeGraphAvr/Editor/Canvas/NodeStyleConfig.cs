using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    [CreateAssetMenu(menuName = "NodeGraphAvr/Node Style Config", fileName = "NodeStyleConfig")]
    public class NodeStyleConfig : ScriptableObject
    {
        [Tooltip("One entry per overridden type, keyed by [GraphTypeId]. Edited through this asset's inspector, which lists every type in the project and writes an entry only where the author changed something.")]
        [SerializeField] private List<NodeStyleOverride> _overrides = new List<NodeStyleOverride>();

        public IReadOnlyList<NodeStyleOverride> Overrides => _overrides;

        internal List<NodeStyleOverride> OverrideListInternal => _overrides;
    }
}
