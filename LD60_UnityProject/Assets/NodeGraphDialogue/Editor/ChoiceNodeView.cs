using NodeGraphAvr.Editor;
using UnityEngine;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Canvas view of a ChoiceNode: draws it as a chat bubble whose body lists the options —
    /// the first one on a small node, more of them as the author grows it — and maps every
    /// outgoing line to the option that owns it (port slot = option index, label = option text),
    /// keeping the ownership as lines are created, adopted by relink, or removed, so any option
    /// can be wired in any order and the port squares always mirror the option list.
    /// </summary>
    [CustomNodeView(typeof(ChoiceNode))]
    public class ChoiceNodeView : DialogueBubbleNodeView
    {
        private string[] _cachedTexts = new string[0];
        private GraphCondition[] _cachedConditions = new GraphCondition[0];
        private string _body = "";

        /// <summary>
        /// The option texts, one numbered line each — the number is the option's own position, so
        /// the body reads in step with the port squares below it and with the option list in the
        /// panel. An option that is gated names its condition in square brackets, so a graph shows
        /// which branches are not always offered. Rebuilt only when an option's text or condition
        /// instance actually changes — this runs every repaint, so the comparison must not allocate.
        /// </summary>
        protected override string GetBodyText()
        {
            var options = ((ChoiceNode)Node).Options;
            if (!OptionsChanged(options))
                return _body;

            _cachedTexts = new string[options.Count];
            _cachedConditions = new GraphCondition[options.Count];
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < options.Count; i++)
            {
                _cachedTexts[i] = options[i].Text;
                _cachedConditions[i] = options[i].VisibleIf;
                if (i > 0)
                    builder.Append('\n');
                builder.Append(i + 1).Append(". ");
                builder.Append(string.IsNullOrEmpty(options[i].Text) ? "(empty)" : options[i].Text);
                if (options[i].VisibleIf != null)
                    builder.Append(" [").Append(NodeTypeInfo.For(options[i].VisibleIf.GetType()).Name).Append(']');
            }

            _body = builder.ToString();
            return _body;
        }

        /// <summary>True when the option that owns this out-connection is gated by a condition.</summary>
        public override bool IsOutSlotConditional(int slot)
        {
            var options = ((ChoiceNode)Node).Options;
            return slot >= 0 && slot < options.Count && options[slot].VisibleIf != null;
        }

        private bool OptionsChanged(System.Collections.Generic.IReadOnlyList<ChoiceOption> options)
        {
            if (options.Count != _cachedTexts.Length)
                return true;

            for (var i = 0; i < options.Count; i++)
                if (!ReferenceEquals(options[i].Text, _cachedTexts[i]) ||
                    !ReferenceEquals(options[i].VisibleIf, _cachedConditions[i]))
                    return true;
            return false;
        }

        public override string GetOutConnectionLabel(int index)
        {
            var choice = (ChoiceNode)Node;
            if (index < 0 || index >= choice.OutConnections.Count)
                return null;

            var optionIndex = choice.FindOptionIndexForLine(choice.OutConnections[index]);
            if (optionIndex < 0)
                return null;
            return choice.Options[optionIndex].Text;
        }

        public override int GetOutConnectionSlot(int connectionIndex)
        {
            var choice = (ChoiceNode)Node;
            if (connectionIndex < 0 || connectionIndex >= choice.OutConnections.Count)
                return connectionIndex;

            var optionIndex = choice.FindOptionIndexForLine(choice.OutConnections[connectionIndex]);
            return optionIndex >= 0 ? optionIndex : connectionIndex;
        }

        public override bool CanReassignOutConnectionSlot => true;

        public override void SetOutConnectionSlot(Connection connection, int slot)
        {
            ((ChoiceNode)Node).MoveLineToOption(connection, slot);
        }

        public override void OnOutConnectionCreated(Connection connection, int slot)
        {
            var options = ((ChoiceNode)Node).OptionListInternal;
            if (slot >= 0 && slot < options.Count && string.IsNullOrEmpty(options[slot].LineUid))
            {
                options[slot].LineUid = connection.Uid;
                return;
            }

            for (var i = 0; i < options.Count; i++)
            {
                if (!string.IsNullOrEmpty(options[i].LineUid))
                    continue;
                options[i].LineUid = connection.Uid;
                return;
            }
        }

        public override void OnOutConnectionRemoved(Connection connection)
        {
            var choice = (ChoiceNode)Node;
            var optionIndex = choice.FindOptionIndexForLine(connection);
            if (optionIndex >= 0)
                choice.OptionListInternal[optionIndex].LineUid = "";
        }
    }
}
