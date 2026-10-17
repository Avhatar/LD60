using NodeGraphAvr.Editor;
using UnityEditor;

namespace NodeGraphAvr.Dialogue.Editor
{
    public static class DialogueAssetMenus
    {
        [MenuItem("Assets/Create/NodeGraphAvr/Dialogue")]
        private static void CreateDialogue()
        {
            GraphAssetCreation.CreateGraphAsset<DialogueGraph>("New Dialogue.ngdlg");
        }
    }
}
