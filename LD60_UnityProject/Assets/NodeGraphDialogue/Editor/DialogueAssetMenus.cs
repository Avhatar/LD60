using NodeGraphAvr.Editor;
using UnityEditor;

namespace NodeGraphAvr.Dialogue.Editor
{
    /// <summary>
    /// Project-window creation menu for dialogue graph assets.
    /// </summary>
    public static class DialogueAssetMenus
    {
        [MenuItem("Assets/Create/NodeGraphAvr/Dialogue")]
        private static void CreateDialogue()
        {
            GraphAssetCreation.CreateGraphAsset<DialogueGraph>("New Dialogue.ngdlg");
        }
    }
}
