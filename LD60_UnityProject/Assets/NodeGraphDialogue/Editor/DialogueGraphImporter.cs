using NodeGraphAvr.Editor;
using UnityEditor.AssetImporters;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Claims the .ngdlg extension for the dialogue graph kind; all import logic
    /// lives in the tool's GraphImporter base.
    /// </summary>
    [ScriptedImporter(1, "ngdlg")]
    public class DialogueGraphImporter : GraphImporter
    {
    }
}
