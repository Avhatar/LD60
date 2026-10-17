using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    public static class CanvasShapes
    {
        private const int MinRadius = 2;
        private const int MaxRadius = 32;
        private const int TitleTopMargin = 3;
        private const float BodyEdgeThickness = 1f;
        private const float OutlineThickness = 1.6f;
        private const float SeparatorThickness = 1f;

        private static readonly Dictionary<int, GUIStyle> _windowStyles = new Dictionary<int, GUIStyle>();
        private static readonly Dictionary<int, GUIStyle> _centeredWindowStyles = new Dictionary<int, GUIStyle>();
        private static readonly Dictionary<int, GUIStyle> _outlineStyles = new Dictionary<int, GUIStyle>();
        private static readonly Dictionary<int, GUIStyle> _bodyPanelStyles = new Dictionary<int, GUIStyle>();
        private static bool _builtForProSkin;
        private static bool _skinKnown;

        private static readonly Color _bodyShadeDark = new Color(0f, 0f, 0f, 0.26f);
        private static readonly Color _bodyShadeLight = new Color(0f, 0f, 0f, 0.12f);
        private static readonly Color _separatorDark = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color _separatorLight = new Color(0f, 0f, 0f, 0.3f);

        public const int NodeHeaderHeight = 20;

        public static GUIStyle WindowStyle(int radius, bool centeredTitle)
        {
            var cache = centeredTitle ? _centeredWindowStyles : _windowStyles;
            var key = Mathf.Clamp(radius, 0, MaxRadius);
            DiscardOnSkinChange();

            if (cache.TryGetValue(key, out var style))
                return style;

            style = new GUIStyle(GUI.skin.window);
            if (key >= MinRadius)
            {
                style.border = new RectOffset(key, key, key, key);
                style.overflow = new RectOffset(0, 0, 0, 0);
                ApplyBackground(style, BuildBodyTexture(key));
            }

            if (centeredTitle)
            {
                style.alignment = TextAnchor.MiddleCenter;
                style.padding = new RectOffset(6, 6, 4, 4);
                style.contentOffset = Vector2.zero;
            }
            else
            {
                style.alignment = TextAnchor.UpperCenter;
                style.padding = new RectOffset(6, 6, NodeHeaderHeight, 4);
                style.contentOffset = new Vector2(0f, -(NodeHeaderHeight - TitleTopMargin));
            }

            ApplyTitleColor(style);
            cache.Add(key, style);
            return style;
        }

        private static void ApplyTitleColor(GUIStyle style)
        {
            var color = style.normal.textColor;
            var states = new[]
            {
                style.onNormal, style.hover, style.onHover,
                style.active, style.onActive, style.focused, style.onFocused,
            };

            for (var i = 0; i < states.Length; i++)
                states[i].textColor = color;
        }

        public static void DrawRoundedOutline(Rect rect, Color color, int radius)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, RoundedOutlineStyle(radius));
            GUI.color = previous;
        }

        public static void DrawNodeBody(Rect rect, int radius)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var pro = EditorGUIUtility.isProSkin;
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            GUI.color = pro ? _bodyShadeDark : _bodyShadeLight;
            GUI.backgroundColor = Color.white;

            if (radius >= MinRadius)
                GUI.Box(rect, GUIContent.none, BodyPanelStyle(radius));
            else
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, SeparatorThickness),
                pro ? _separatorDark : _separatorLight);
        }

        private static GUIStyle BodyPanelStyle(int radius)
        {
            radius = Mathf.Clamp(radius, MinRadius, MaxRadius);
            DiscardOnSkinChange();

            if (_bodyPanelStyles.TryGetValue(radius, out var style))
                return style;

            style = new GUIStyle
            {
                border = new RectOffset(radius, radius, 1, radius),
                overflow = new RectOffset(0, 0, 0, 0),
            };
            ApplyBackground(style, BuildBodyPanelTexture(radius));
            _bodyPanelStyles.Add(radius, style);
            return style;
        }

        private static GUIStyle RoundedOutlineStyle(int radius)
        {
            radius = Mathf.Clamp(radius, MinRadius, MaxRadius);
            DiscardOnSkinChange();

            if (_outlineStyles.TryGetValue(radius, out var style))
                return style;

            style = new GUIStyle
            {
                border = new RectOffset(radius, radius, radius, radius),
                overflow = new RectOffset(0, 0, 0, 0),
            };
            ApplyBackground(style, BuildOutlineTexture(radius));
            _outlineStyles.Add(radius, style);
            return style;
        }

        private static void ApplyBackground(GUIStyle style, Texture2D texture)
        {
            var states = new[]
            {
                style.normal, style.onNormal, style.hover, style.onHover,
                style.active, style.onActive, style.focused, style.onFocused,
            };

            for (var i = 0; i < states.Length; i++)
            {
                states[i].background = texture;
                states[i].scaledBackgrounds = new Texture2D[0];
            }
        }

        private static Texture2D BuildBodyTexture(int radius)
        {
            var pro = EditorGUIUtility.isProSkin;
            var fill = pro ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
            var edge = pro ? new Color(0.13f, 0.13f, 0.13f, 1f) : new Color(0.55f, 0.55f, 0.55f, 1f);
            return BuildTexture(radius, fill, edge, BodyEdgeThickness, false);
        }

        private static Texture2D BuildOutlineTexture(int radius)
        {
            return BuildTexture(radius, Color.clear, Color.white, OutlineThickness, true);
        }

        private static Texture2D BuildBodyPanelTexture(int radius)
        {
            var width = radius * 2 + 2;
            var height = radius + 2;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var distance = BottomBoundaryDistance(x + 0.5f, y + 0.5f, width, radius);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D BuildTexture(int radius, Color fill, Color edge, float edgeThickness, bool hollow)
        {
            var size = radius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = BoundaryDistance(x + 0.5f, y + 0.5f, size, radius);
                    var coverage = Mathf.Clamp01(0.5f - distance);
                    var edgeAmount = Mathf.Clamp01(distance + edgeThickness + 0.5f);
                    var color = Color.Lerp(fill, edge, edgeAmount);
                    color.a = hollow ? edge.a * coverage * edgeAmount : Mathf.Lerp(fill.a, edge.a, edgeAmount) * coverage;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static float BoundaryDistance(float x, float y, int size, int radius)
        {
            var cx = Mathf.Clamp(x, radius, size - radius);
            var cy = Mathf.Clamp(y, radius, size - radius);
            return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius;
        }

        private static float BottomBoundaryDistance(float x, float y, int width, int radius)
        {
            var cx = Mathf.Clamp(x, radius, width - radius);
            var cy = Mathf.Max(y, radius);
            return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius;
        }

        private static void DiscardOnSkinChange()
        {
            var pro = EditorGUIUtility.isProSkin;
            if (_skinKnown && _builtForProSkin == pro)
                return;

            _skinKnown = true;
            _builtForProSkin = pro;
            Discard(_windowStyles);
            Discard(_centeredWindowStyles);
            Discard(_outlineStyles);
            Discard(_bodyPanelStyles);
        }

        private static void Discard(Dictionary<int, GUIStyle> styles)
        {
            foreach (var pair in styles)
            {
                if (pair.Key < MinRadius)
                    continue;

                var texture = pair.Value.normal.background;
                if (texture != null)
                    Object.DestroyImmediate(texture);
            }

            styles.Clear();
        }
    }
}
