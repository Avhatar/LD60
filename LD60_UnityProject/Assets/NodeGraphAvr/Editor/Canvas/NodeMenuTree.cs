using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public class NodeMenuTree
    {
        public class Item
        {
            public string Path;
            public string Description;
            public bool HasColor;
            public Color Color;
            public Action Invoke;

            public string Name { get; internal set; }

            public string Category { get; internal set; }
        }

        public class Folder
        {
            public string Name;
            public string FullPath;
            public Folder Parent;
            public readonly List<Folder> Folders = new List<Folder>();
            public readonly List<Item> Items = new List<Item>();
        }

        private static readonly char[] _pathSeparator = { '/' };
        private static readonly char[] _wordSeparator = { ' ' };

        public Folder Root { get; } = new Folder { Name = "", FullPath = "" };

        private readonly List<Item> _leaves = new List<Item>();

        public NodeMenuTree(IReadOnlyList<Item> items)
        {
            if (items == null)
            {
                Debug.LogError($"{nameof(NodeMenuTree)}: {nameof(items)} is null in the constructor. " +
                               "The tree needs the menu entries to fold into folders. Fix: pass an empty list instead of null.");
                return;
            }

            for (var i = 0; i < items.Count; i++)
                Add(items[i]);
            Sort(Root);
        }

        private static void Sort(Folder folder)
        {
            folder.Folders.Sort(CompareFolders);
            folder.Items.Sort(CompareItems);
            for (var i = 0; i < folder.Folders.Count; i++)
                Sort(folder.Folders[i]);
        }

        private static int CompareFolders(Folder a, Folder b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareItems(Item a, Item b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private void Add(Item item)
        {
            if (item == null || string.IsNullOrEmpty(item.Path))
                return;

            var parts = item.Path.Split(_pathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var folder = Root;
            for (var i = 0; i < parts.Length - 1; i++)
                folder = GetOrAddFolder(folder, parts[i]);

            item.Name = parts[parts.Length - 1];
            item.Category = folder.FullPath;
            folder.Items.Add(item);
            _leaves.Add(item);
        }

        private static Folder GetOrAddFolder(Folder parent, string name)
        {
            for (var i = 0; i < parent.Folders.Count; i++)
                if (parent.Folders[i].Name == name)
                    return parent.Folders[i];

            var folder = new Folder
            {
                Name = name,
                Parent = parent,
                FullPath = string.IsNullOrEmpty(parent.FullPath) ? name : parent.FullPath + "/" + name,
            };
            parent.Folders.Add(folder);
            return folder;
        }

        public List<Item> Search(string input)
        {
            var results = new List<Item>();
            if (string.IsNullOrEmpty(input))
                return results;

            for (var i = 0; i < _leaves.Count; i++)
                if (Matches(input, _leaves[i].Name, _leaves[i].Category))
                    results.Add(_leaves[i]);

            results.Sort(CompareByScore(input));
            return results;
        }

        private Comparison<Item> CompareByScore(string input)
        {
            return (a, b) => Score(input, a.Name, a.Category).CompareTo(Score(input, b.Name, b.Category));
        }

        public static bool Matches(string input, string name, string category)
        {
            if (string.IsNullOrEmpty(input) || name == null)
                return false;

            var words = SplitWords(input);
            if (words.Length == 0)
                return false;

            var fullPath = Normalize(category) + "/" + Normalize(name);
            if (input[input.Length - 1] == '.')
                return Normalize(category).Contains(words[0]);

            var leftover = fullPath;
            for (var i = 0; i < words.Length; i++)
            {
                var at = leftover.IndexOf(words[i], StringComparison.Ordinal);
                if (at < 0)
                    return false;
                leftover = leftover.Substring(at + words[i].Length);
            }

            return true;
        }

        public static float Score(string input, string name, string category)
        {
            if (string.IsNullOrEmpty(input) || name == null)
                return float.PositiveInfinity;

            var words = SplitWords(input);
            if (words.Length == 0)
                return 1f;

            var leaf = Normalize(name);
            var cat = Normalize(category);
            var first = words[0];
            var last = words[words.Length - 1];

            var score = 1f;
            if (cat.Contains(first))
                score *= 0.9f;
            if (leaf == last)
                score *= 0.5f;
            if (leaf.StartsWith(first, StringComparison.Ordinal))
                score *= 0.5f;
            if (leaf.StartsWith(last, StringComparison.Ordinal))
                score *= 0.5f;
            return score;
        }

        private static string[] SplitWords(string input)
        {
            return input.ToUpperInvariant().Replace('.', ' ').Split(_wordSeparator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.ToUpperInvariant().Replace(" ", "");
        }
    }
}
