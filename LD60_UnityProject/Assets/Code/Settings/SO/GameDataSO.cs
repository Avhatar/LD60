using System;
using UnityEngine;

namespace LD60
{
    public abstract class GameDataSO : ScriptableObject
    {
        public static event Action Changed;

        public static void NotifyChanged()
        {
            if (Changed != null)
                Changed.Invoke();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            NotifyChanged();
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void SubscribeToUndo()
        {
            UnityEditor.Undo.undoRedoPerformed -= NotifyChanged;
            UnityEditor.Undo.undoRedoPerformed += NotifyChanged;
        }
#endif
    }
}
