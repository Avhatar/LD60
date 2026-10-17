using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Shows the searchable type browser for any polymorphic base type — the same popup
    /// the canvas uses to create nodes, so actions and conditions inherit its folders,
    /// search, favourites and keyboard navigation for free. Entries are described by the
    /// candidate types' own [Name] / [Category] / [Description] attributes; abstract
    /// types and test assemblies never appear. TypeCache is queried when the user opens
    /// the browser, never on a draw path.
    /// </summary>
    public static class TypeBrowserMenu
    {
        public static void Show(Rect activator, string title, string prefsKey, Type baseType, Action<Type> onPicked)
        {
            if (baseType == null || onPicked == null)
            {
                Debug.LogError($"{nameof(TypeBrowserMenu)}: {(baseType == null ? nameof(baseType) : nameof(onPicked))} is null in {nameof(Show)}. " +
                               "The browser needs a base type to list and a callback to report the pick to. " +
                               "Fix: pass both when opening the browser.");
                return;
            }

            PopupWindow.Show(activator, new NodeCreationBrowser(title, prefsKey, BuildItems(baseType, onPicked)));
        }

        public static List<NodeMenuTree.Item> BuildItems(Type baseType, Action<Type> onPicked)
        {
            var items = new List<NodeMenuTree.Item>();
            var types = TypeCache.GetTypesDerivedFrom(baseType);
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;
                if (type.Assembly.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal))
                    continue;

                var info = NodeTypeInfo.For(type);
                var picked = type;
                items.Add(new NodeMenuTree.Item
                {
                    Path = info.MenuPath,
                    Description = info.Description,
                    HasColor = true,
                    Color = NodeTypeInfo.GetTypeColor(type),
                    Invoke = () => onPicked(picked),
                });
            }

            return items;
        }
    }
}
