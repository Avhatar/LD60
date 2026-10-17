using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Popup browser for the canvas creation menu: folders from the entries' category
    /// paths, a search field that flattens the whole tree by relevance, starred
    /// favorites persisted per graph kind, keyboard navigation, and a footer showing
    /// the hovered entry's description. Selecting an entry invokes its action and
    /// closes the popup.
    /// </summary>
    public class NodeCreationBrowser : PopupWindowContent
    {
        private const string SearchControlName = "NodeCreationBrowserSearch";
        private const string FavoritesPrefsPrefix = "NodeGraphAvr.CreationFavorites.";
        private const float RowHeight = 20f;
        private const float FooterMinHeight = 46f;
        private const float FooterMaxHeight = 180f;
        private const float FooterPadding = 4f;
        private const float HeaderHeight = 52f;
        private const float BackRowHeight = 22f;
        private const float StarWidth = 18f;
        private const float SwatchWidth = 10f;

        private static readonly Color _hoverColor = new Color(0.35f, 0.65f, 1f, 0.25f);
        private static readonly Color _favoriteColor = new Color(1f, 0.8f, 0.25f);
        private static readonly Color _mutedColor = new Color(1f, 1f, 1f, 0.2f);

        private readonly string _title;
        private readonly string _prefsKey;
        private readonly NodeMenuTree _tree;
        private readonly List<string> _favorites = new List<string>();
        private readonly List<NodeMenuTree.Folder> _rowFolders = new List<NodeMenuTree.Folder>();
        private readonly List<NodeMenuTree.Item> _rowItems = new List<NodeMenuTree.Item>();
        private readonly GUIContent _footerContent = new GUIContent();

        private NodeMenuTree.Folder _folder;
        private string _search = "";
        private string _lastSearch;
        private List<NodeMenuTree.Item> _searchResults = new List<NodeMenuTree.Item>();
        private bool _favoritesOnly;
        private bool _focusSearch = true;
        private int _hoverIndex;
        private Vector2 _scroll;

        public NodeCreationBrowser(string title, string prefsKey, IReadOnlyList<NodeMenuTree.Item> items)
        {
            _title = title;
            _prefsKey = prefsKey;
            _tree = new NodeMenuTree(items);
            _folder = _tree.Root;
            LoadFavorites();
        }

        public override Vector2 GetWindowSize() => new Vector2(320f, 400f);

        public override void OnOpen()
        {
            editorWindow.wantsMouseMove = true;
        }

        public override void OnGUI(Rect rect)
        {
            var e = Event.current;
            RebuildRows();
            HandleKeyboard(e);
            _hoverIndex = Mathf.Clamp(_hoverIndex, 0, Mathf.Max(0, RowCount - 1));

            var footerHeight = UpdateFooterContent(rect.width);
            var showBack = IsBrowsingFolders() && _folder.Parent != null;
            var headerHeight = HeaderHeight + (showBack ? BackRowHeight : 0f);
            GUILayout.BeginArea(new Rect(0f, 0f, rect.width, headerHeight));
            DrawHeader(e, showBack);
            GUILayout.EndArea();

            var listRect = new Rect(0f, headerHeight, rect.width, rect.height - headerHeight - footerHeight);
            DrawRows(listRect, e);
            DrawFooter(new Rect(2f, rect.height - footerHeight + 2f, rect.width - 4f, footerHeight - 4f));

            if (e.type == EventType.MouseMove)
                editorWindow.Repaint();
        }

        private void DrawHeader(Event e, bool showBack)
        {
            GUILayout.Space(4f);
            GUILayout.Label(_title, EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(SearchControlName);
            var search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (search != _search)
            {
                _search = search;
                _hoverIndex = 0;
            }

            var favoritesOnly = GUILayout.Toggle(_favoritesOnly, "★", EditorStyles.toolbarButton, GUILayout.Width(24f));
            if (favoritesOnly != _favoritesOnly)
            {
                _favoritesOnly = favoritesOnly;
                _hoverIndex = 0;
            }

            GUILayout.EndHorizontal();

            if (_focusSearch && e.type == EventType.Repaint)
            {
                _focusSearch = false;
                EditorGUI.FocusTextInControl(SearchControlName);
            }

            if (showBack && GUILayout.Button("◄  " + _folder.FullPath, EditorStyles.miniButton))
                GoBack();
        }

        private void RebuildRows()
        {
            if (_search != _lastSearch)
            {
                _lastSearch = _search;
                _searchResults = _tree.Search(_search);
                if (_searchResults.Count > 1)
                    _searchResults.Sort(CompareFavoriteFirst);
            }

            _rowFolders.Clear();
            _rowItems.Clear();

            if (!IsBrowsingFolders())
            {
                var source = string.IsNullOrEmpty(_search) ? CollectAllItems() : _searchResults;
                for (var i = 0; i < source.Count; i++)
                    if (!_favoritesOnly || IsFavorite(source[i]))
                        _rowItems.Add(source[i]);
                return;
            }

            for (var i = 0; i < _folder.Folders.Count; i++)
                _rowFolders.Add(_folder.Folders[i]);
            for (var i = 0; i < _folder.Items.Count; i++)
                _rowItems.Add(_folder.Items[i]);
        }

        private bool IsBrowsingFolders()
        {
            return string.IsNullOrEmpty(_search) && !_favoritesOnly;
        }

        private List<NodeMenuTree.Item> CollectAllItems()
        {
            var all = new List<NodeMenuTree.Item>();
            Collect(_tree.Root, all);
            return all;
        }

        private static void Collect(NodeMenuTree.Folder folder, List<NodeMenuTree.Item> into)
        {
            for (var i = 0; i < folder.Items.Count; i++)
                into.Add(folder.Items[i]);
            for (var i = 0; i < folder.Folders.Count; i++)
                Collect(folder.Folders[i], into);
        }

        private int CompareFavoriteFirst(NodeMenuTree.Item a, NodeMenuTree.Item b)
        {
            var favoriteA = IsFavorite(a);
            var favoriteB = IsFavorite(b);
            if (favoriteA == favoriteB)
                return 0;
            return favoriteA ? -1 : 1;
        }

        private int RowCount => _rowFolders.Count + _rowItems.Count;

        private void DrawRows(Rect listRect, Event e)
        {
            GUILayout.BeginArea(listRect);
            _scroll = GUILayout.BeginScrollView(_scroll);

            if (RowCount == 0)
                GUILayout.Label("Nothing to show.", EditorStyles.centeredGreyMiniLabel);

            for (var i = 0; i < _rowFolders.Count; i++)
                DrawFolderRow(_rowFolders[i], i, e);
            for (var i = 0; i < _rowItems.Count; i++)
                DrawItemRow(_rowItems[i], _rowFolders.Count + i, e);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawFolderRow(NodeMenuTree.Folder folder, int index, Event e)
        {
            var rect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            DrawRowBackground(rect, index, e);
            var labelRect = new Rect(rect.x + StarWidth + SwatchWidth + 4f, rect.y, rect.width - StarWidth - SwatchWidth - 24f, rect.height);
            GUI.Label(labelRect, folder.Name, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.xMax - 18f, rect.y, 16f, rect.height), "►");

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                Enter(folder);
                e.Use();
            }
        }

        private void DrawItemRow(NodeMenuTree.Item item, int index, Event e)
        {
            var rect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            DrawRowBackground(rect, index, e);

            var starRect = new Rect(rect.x + 2f, rect.y + 3f, StarWidth - 4f, rect.height - 6f);
            var favorite = IsFavorite(item);
            GUI.color = favorite ? _favoriteColor : _mutedColor;
            if (GUI.Button(starRect, "★", EditorStyles.label))
                ToggleFavorite(item);
            GUI.color = Color.white;

            if (item.HasColor)
                EditorGUI.DrawRect(new Rect(rect.x + StarWidth, rect.y + 4f, SwatchWidth - 3f, rect.height - 8f), item.Color);

            var labelRect = new Rect(rect.x + StarWidth + SwatchWidth + 4f, rect.y, rect.width - StarWidth - SwatchWidth - 8f, rect.height);
            GUI.Label(labelRect, IsBrowsingFolders() ? item.Name : item.Path);

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                Invoke(item);
                e.Use();
            }
        }

        private void DrawRowBackground(Rect rect, int index, Event e)
        {
            if (e.type == EventType.MouseMove && rect.Contains(e.mousePosition))
                _hoverIndex = index;
            if (_hoverIndex == index)
                EditorGUI.DrawRect(rect, _hoverColor);
        }

        private float UpdateFooterContent(float windowWidth)
        {
            var hovered = HoveredItem();
            var text = hovered == null ? "" : string.IsNullOrEmpty(hovered.Description) ? hovered.Path : hovered.Description;
            if (!string.Equals(_footerContent.text, text, System.StringComparison.Ordinal))
                _footerContent.text = text;

            var textHeight = EditorStyles.wordWrappedMiniLabel.CalcHeight(_footerContent, FooterTextWidth(windowWidth));
            return Mathf.Clamp(textHeight + FooterPadding * 2f, FooterMinHeight, FooterMaxHeight);
        }

        private static float FooterTextWidth(float windowWidth)
        {
            return windowWidth - 4f - FooterPadding * 2f;
        }

        private void DrawFooter(Rect footerRect)
        {
            EditorGUI.DrawRect(footerRect, new Color(0f, 0f, 0f, 0.2f));
            GUI.Label(new Rect(footerRect.x + FooterPadding, footerRect.y + 2f, FooterTextWidth(footerRect.width + 4f), footerRect.height - 4f),
                _footerContent, EditorStyles.wordWrappedMiniLabel);
        }

        private NodeMenuTree.Item HoveredItem()
        {
            var itemIndex = _hoverIndex - _rowFolders.Count;
            return itemIndex >= 0 && itemIndex < _rowItems.Count ? _rowItems[itemIndex] : null;
        }

        private void HandleKeyboard(Event e)
        {
            if (e.type != EventType.KeyDown)
                return;

            switch (e.keyCode)
            {
                case KeyCode.DownArrow:
                    GUIUtility.keyboardControl = 0;
                    _hoverIndex = Mathf.Min(_hoverIndex + 1, Mathf.Max(0, RowCount - 1));
                    _focusSearch = true;
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    GUIUtility.keyboardControl = 0;
                    _hoverIndex = Mathf.Max(_hoverIndex - 1, 0);
                    _focusSearch = true;
                    e.Use();
                    break;
                case KeyCode.RightArrow:
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    GUIUtility.keyboardControl = 0;
                    ActivateHovered(e.keyCode == KeyCode.RightArrow);
                    e.Use();
                    break;
                case KeyCode.LeftArrow:
                    if (IsBrowsingFolders() && _folder.Parent != null)
                    {
                        GUIUtility.keyboardControl = 0;
                        GoBack();
                        _focusSearch = true;
                        e.Use();
                    }

                    break;
                case KeyCode.Escape:
                    editorWindow.Close();
                    e.Use();
                    break;
            }
        }

        private void ActivateHovered(bool foldersOnly)
        {
            if (_hoverIndex < _rowFolders.Count)
            {
                Enter(_rowFolders[_hoverIndex]);
                return;
            }

            if (foldersOnly)
                return;

            var item = HoveredItem();
            if (item != null)
                Invoke(item);
        }

        private void Enter(NodeMenuTree.Folder folder)
        {
            _folder = folder;
            _hoverIndex = 0;
            _scroll = Vector2.zero;
            _focusSearch = true;
            editorWindow.Repaint();
        }

        private void GoBack()
        {
            _folder = _folder.Parent ?? _tree.Root;
            _hoverIndex = 0;
            _scroll = Vector2.zero;
            editorWindow.Repaint();
        }

        private void Invoke(NodeMenuTree.Item item)
        {
            if (item.Invoke == null)
            {
                Debug.LogError($"{nameof(NodeCreationBrowser)}: entry '{item.Path}' has no action in {nameof(Invoke)}. " +
                               "Every browser entry must carry what to do when picked. Fix: set Item.Invoke when building the menu.");
                return;
            }

            item.Invoke();
            editorWindow.Close();
        }

        private bool IsFavorite(NodeMenuTree.Item item)
        {
            return _favorites.Contains(item.Path);
        }

        private void ToggleFavorite(NodeMenuTree.Item item)
        {
            if (!_favorites.Remove(item.Path))
                _favorites.Add(item.Path);
            EditorPrefs.SetString(FavoritesPrefsPrefix + _prefsKey, string.Join("\n", _favorites.ToArray()));
            _lastSearch = null;
        }

        private void LoadFavorites()
        {
            var stored = EditorPrefs.GetString(FavoritesPrefsPrefix + _prefsKey, "");
            if (string.IsNullOrEmpty(stored))
                return;

            var paths = stored.Split('\n');
            for (var i = 0; i < paths.Length; i++)
                if (!string.IsNullOrEmpty(paths[i]))
                    _favorites.Add(paths[i]);
        }
    }
}
