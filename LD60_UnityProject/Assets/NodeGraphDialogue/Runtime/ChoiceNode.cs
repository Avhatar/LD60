using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Dialogue
{
    [Name("Choice")]
    [Description("Branches the dialogue: the player picks one authored option and the walk continues along the line that option owns.")]
    [NodeColor("9A7BC8")]
    [GraphTypeId("dlg.choice")]
    [System.Serializable]
    public class ChoiceNode : DialogueNode
    {
        [Tooltip("Whose agent the option conditions are evaluated on. Leave empty to evaluate on whoever started the dialogue.")]
        [ActorPicker]
        [SerializeField] private ActorData _actor;

        [Tooltip("The selectable options of this branch, in presentation order. Each option may own one outgoing line.")]
        [SerializeField] private List<ChoiceOption> _options = new List<ChoiceOption>();

        public ActorData Actor
        {
            get { return _actor; }
            internal set { _actor = value; }
        }

        public IReadOnlyList<ChoiceOption> Options => _options;

        internal List<ChoiceOption> OptionListInternal => _options;

        public override int MaxOutConnections => _options.Count;

        protected internal override ActorData ActionActor => _actor;

        internal int GetOptionLineIndex(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _options.Count)
                return -1;

            var uid = _options[optionIndex].LineUid;
            if (string.IsNullOrEmpty(uid))
                return -1;
            return FindLineIndex(uid);
        }

        internal void MoveLineToOption(Connection connection, int optionIndex)
        {
            if (connection == null || optionIndex < 0 || optionIndex >= _options.Count)
            {
                Debug.LogError($"{nameof(ChoiceNode)}: cannot move a line to option {optionIndex} of node '{Name}' in {nameof(MoveLineToOption)}. " +
                               $"The node has {_options.Count} option(s) and the line must not be null. " +
                               "Fix: pass a line of this node and an option index that exists.");
                return;
            }

            var previousIndex = FindOptionIndexForLine(connection);
            if (previousIndex == optionIndex)
                return;

            var displaced = _options[optionIndex].LineUid;
            _options[optionIndex].LineUid = connection.Uid;
            if (previousIndex >= 0)
                _options[previousIndex].LineUid = displaced;
        }

        internal int FindOptionIndexForLine(Connection connection)
        {
            if (connection == null)
                return -1;

            for (var i = 0; i < _options.Count; i++)
                if (_options[i].LineUid == connection.Uid)
                    return i;
            return -1;
        }

        internal override void Enter(DialogueRunner runner)
        {
            if (_options.Count == 0)
            {
                Debug.LogError($"{nameof(ChoiceNode)}: node '{Name}' has no options in {nameof(Enter)}. " +
                               "A branch with nothing to choose cannot continue; the dialogue stops as failed. " +
                               "Fix: author at least one option on the node in the inspector panel.");
                runner.FinishFromNode(false);
                return;
            }

            var context = runner.ContextForNode(this);
            var visible = new bool[_options.Count];
            var visibleCount = 0;
            for (var i = 0; i < _options.Count; i++)
            {
                var condition = _options[i].VisibleIf;
                visible[i] = condition == null || condition.Evaluate(context);
                if (visible[i])
                    visibleCount++;
            }

            if (visibleCount == 0)
            {
                Debug.LogError($"{nameof(ChoiceNode)}: every option of node '{Name}' is hidden by its condition in {nameof(Enter)}. " +
                               "There is nothing the player could pick, so the dialogue stops as failed. " +
                               "Fix: relax an option's condition, or author one option without a condition as a fallback.");
                runner.FinishFromNode(false);
                return;
            }

            var texts = new LD60.LocalizedString[visibleCount];
            var lineIndices = new int[visibleCount];
            var next = 0;
            for (var i = 0; i < _options.Count; i++)
            {
                if (!visible[i])
                    continue;

                texts[next] = _options[i].Line;
                lineIndices[next] = GetOptionLineIndex(i);
                next++;
            }

            runner.RaiseChoice(new ChoiceRequest(runner, texts, lineIndices));
        }

        protected internal override void OnRuntimeStateRebuilt()
        {
            for (var i = 0; i < _options.Count; i++)
            {
                var option = _options[i];
                if (!string.IsNullOrEmpty(option.LineUid) && FindLineIndex(option.LineUid) < 0)
                    option.LineUid = "";
            }

            var outs = OutConnections;
            for (var j = 0; j < outs.Count; j++)
            {
                if (IsLineClaimed(outs[j].Uid))
                    continue;

                for (var i = 0; i < _options.Count; i++)
                {
                    if (!string.IsNullOrEmpty(_options[i].LineUid))
                        continue;
                    _options[i].LineUid = outs[j].Uid;
                    break;
                }
            }
        }

        private int FindLineIndex(string lineUid)
        {
            var outs = OutConnections;
            for (var i = 0; i < outs.Count; i++)
                if (outs[i].Uid == lineUid)
                    return i;
            return -1;
        }

        private bool IsLineClaimed(string uid)
        {
            for (var i = 0; i < _options.Count; i++)
                if (_options[i].LineUid == uid)
                    return true;
            return false;
        }
    }
}
