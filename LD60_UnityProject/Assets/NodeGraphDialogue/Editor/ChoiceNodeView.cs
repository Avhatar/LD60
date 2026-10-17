using NodeGraphAvr.Editor;
using UnityEngine;

namespace NodeGraphAvr.Dialogue.Editor
{
    [CustomNodeView(typeof(ChoiceNode))]
    public class ChoiceNodeView : DialogueBubbleNodeView
    {
        private string[] _cachedTexts = new string[0];
        private GraphCondition[] _cachedConditions = new GraphCondition[0];
        private string _body = "";

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
