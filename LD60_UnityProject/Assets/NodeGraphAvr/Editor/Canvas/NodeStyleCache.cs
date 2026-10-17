using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public static class NodeStyleCache
    {
        private static NodeStyleConfig _config;
        private static bool _searched;
        private static Dictionary<Type, Color> _colorByType;

        public static NodeStyleConfig Config
        {
            get
            {
                if (_searched)
                    return _config;

                _searched = true;
                var guids = AssetDatabase.FindAssets("t:" + nameof(NodeStyleConfig));
                if (guids.Length > 0)
                    _config = AssetDatabase.LoadAssetAtPath<NodeStyleConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
                return _config;
            }
        }

        public static bool TryGetColor(Type type, out Color color)
        {
            color = default;
            if (type == null)
                return false;

            EnsureMap();
            return _colorByType.TryGetValue(type, out color);
        }

        public static void Invalidate()
        {
            _config = null;
            _searched = false;
            _colorByType = null;
            EnsureMap();

            var windows = Resources.FindObjectsOfTypeAll<GraphEditorWindow>();
            for (var i = 0; i < windows.Length; i++)
                windows[i].Repaint();
        }

        internal static void UseConfig(NodeStyleConfig config)
        {
            _config = config;
            _searched = config != null;
            _colorByType = null;
        }

        private static void EnsureMap()
        {
            if (_colorByType != null)
                return;

            _colorByType = new Dictionary<Type, Color>();
            var config = Config;
            if (config == null)
                return;

            var overrides = config.Overrides;
            for (var i = 0; i < overrides.Count; i++)
            {
                var entry = overrides[i];
                if (entry == null || !GraphTypeRegistry.TryGetType(entry.TypeId, out var type))
                    continue;
                _colorByType[type] = entry.Color;
            }
        }
    }
}
