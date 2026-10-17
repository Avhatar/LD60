using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    [GraphTypeId("dlg")]
    public class DialogueGraph : Graph
    {
        [Tooltip("Actors this dialogue expects to use. Authoring-only: feeds the speaker dropdown in the inspector together with the dialogue system config; the runtime never reads it.")]
        [SerializeField] private List<ActorData> _actors = new List<ActorData>();

        public IReadOnlyList<ActorData> Actors => _actors;

        internal List<ActorData> ActorListInternal => _actors;

        public override Type BaseNodeType => typeof(DialogueNode);
        public override bool IsTree => true;
        public override FlowDirection FlowDirection => FlowDirection.Vertical;
    }
}
