using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Locates the project's NodeStyleConfig once per domain and resolves its overrides into
    /// a type-keyed map the canvas can read while drawing. The asset search and the id-to-type
    /// resolution happen once, never per repaint; an override edit calls Invalidate, which
    /// drops the map and repaints every open graph window so the canvas follows the config
    /// immediately.
    /// </summary>
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

        /// <summary>
        /// Color this type is overridden to, if the project overrides it. The declared
        /// [NodeColor] is not consulted here — layering the two is NodeTypeInfo's job.
        /// </summary>
        public static bool TryGetColor(Type type, out Color color)
        {
            color = default;
            if (type == null)
                return false;

            EnsureMap();
            return _colorByType.TryGetValue(type, out color);
        }

        /// <summary>
        /// Re-resolves the config and its overrides, then repaints open graph windows. Call
        /// after any edit of the config asset — including the edit that creates the very first
        /// one, which is why the asset search is redone here rather than trusted from before.
        /// The search runs on this explicit call, never lazily from a repaint.
        /// </summary>
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

        /// <summary>
        /// Pins the config instance instead of locating one by asset search. Exists for tests,
        /// which must not depend on what the project happens to hold; passing null restores the
        /// search.
        /// </summary>
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
