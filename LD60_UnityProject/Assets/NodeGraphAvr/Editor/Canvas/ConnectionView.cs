using UnityEditor;
using UnityEngine;

namespace NodeGraphAvr.Editor
{
    /// <summary>
    /// Canvas view of one connection: draws the line as a chain of bezier segments
    /// through the connection's waypoints (smooth tangents by default, straight
    /// segments in sharp-bends mode), handles click selection, relink-by-drag of
    /// either end, waypoint dragging, double-click point insertion, and the context
    /// menus. Pure presentation — every model change goes through the graph's
    /// mutation API via the owning window.
    /// </summary>
    public class ConnectionView
    {
        public enum RelinkEnd
        {
            None,
            Source,
            Target
        }

        private const float RelinkSnapDistance = 20f;
        private const float CurveHitDistance = 10f;
        private const int CurveHitSamplesPerSegment = 30;
        private const float TangentMultiplier = 0.8f;
        private const float TangentMinimum = 25f;
        private const float MidNorm = 0.55f;
        private const float DefaultLineWidth = 3f;
        private const float HighlightLineWidth = 5f;
        private const float WaypointHandleSize = 10f;
        private const int MaxLabelLineChars = 25;

        private static readonly Color _restingColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
        private static readonly Color _highlightColor = new Color(0.35f, 0.65f, 1f, 1f);

        public Connection Connection { get; }

        public RelinkEnd ActiveRelinkEnd => _relinkSnapped ? _armedEnd : RelinkEnd.None;

        private readonly GraphEditorWindow _owner;
        private Vector2? _relinkClickPos;
        private bool _relinkSnapped;
        private RelinkEnd _armedEnd;
        private int _dragPointIndex = -1;
        private Vector2[] _anchors = new Vector2[2];
        private int _anchorCount;
        private Rect _startRect;
        private Rect _endRect;
        private Rect _midRect;
        private string _cachedLabel;
        private readonly GUIContent _labelContent = new GUIContent();
        private static GUIStyle _labelStyle;
        private static readonly Color _labelBackdropColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);

        public ConnectionView(Connection connection, GraphEditorWindow owner)
        {
            Connection = connection;
            _owner = owner;
        }

        public void Draw(Vector2 fromPos, Vector2 toPos, FlowDirection flowDirection, Vector2 pan, string label = null)
        {
            BuildAnchors(fromPos, toPos, pan);
            _startRect = CenteredRect(fromPos, 12f);
            _endRect = CenteredRect(toPos, 14f);
            _midRect = CenteredRect(PosAlongChain(MidNorm, flowDirection), 16f);

            HandleEvents(flowDirection, pan);

            var mousePosition = Event.current.mousePosition;
            var highlighted = _owner.IsSelected(Connection) ||
                              _owner.IsSelected(Connection.Source) || _owner.IsSelected(Connection.Target) ||
                              _startRect.Contains(mousePosition) || _endRect.Contains(mousePosition);
            var color = highlighted ? _highlightColor : _restingColor;

            if (_relinkSnapped)
            {
                var previewFrom = _armedEnd == RelinkEnd.Source ? mousePosition : fromPos;
                var previewTo = _armedEnd == RelinkEnd.Target ? mousePosition : toPos;
                ResolveTangents(previewFrom, previewTo, flowDirection, out var previewFromTangent, out var previewToTangent);
                Handles.DrawBezier(previewFrom, previewTo, previewFrom + previewFromTangent, previewTo + previewToTangent, color, null, DefaultLineWidth);
                return;
            }

            var width = highlighted ? HighlightLineWidth : DefaultLineWidth;
            for (var i = 0; i < _anchorCount - 1; i++)
            {
                SegmentTangents(_anchors[i], _anchors[i + 1], flowDirection, out var fromTangent, out var toTangent);
                Handles.DrawBezier(_anchors[i], _anchors[i + 1], _anchors[i] + fromTangent, _anchors[i + 1] + toTangent, color, null, width);
            }

            EditorGUI.DrawRect(CenteredRect(toPos, 8f), color);
            Handles.color = color;
            for (var i = 0; i < Connection.Waypoints.Count; i++)
                Handles.DrawSolidDisc(_anchors[i + 1], Vector3.forward, WaypointHandleSize * 0.5f);
            Handles.color = Color.white;

            DrawLabel(label, flowDirection);
        }

        private void DrawLabel(string label, FlowDirection flowDirection)
        {
            if (string.IsNullOrEmpty(label) || Event.current.type != EventType.Repaint)
                return;

            if (_labelStyle == null)
                _labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                };

            if (_cachedLabel != label)
            {
                _cachedLabel = label;
                _labelContent.text = Wrap(label, MaxLabelLineChars);
            }

            var size = _labelStyle.CalcSize(_labelContent);
            var center = PosAlongChain(0.5f, flowDirection);
            var rect = new Rect(center.x - size.x * 0.5f - 3f, center.y - size.y * 0.5f - 1f, size.x + 6f, size.y + 2f);
            for (var i = 0; i < Connection.Waypoints.Count; i++)
            {
                var handleRect = WaypointHandleRect(i);
                if (!rect.Overlaps(handleRect))
                    continue;
                rect.y = handleRect.yMin - rect.height - 2f;
                break;
            }

            EditorGUI.DrawRect(rect, _labelBackdropColor);
            _labelStyle.Draw(rect, _labelContent, false, false, false, false);
        }

        /// <summary>
        /// Breaks a line label into rows of at most <paramref name="maxChars"/> characters so a long
        /// option text stops stretching across the canvas. Breaks on the last space that fits, and
        /// mid-word only when a single word is longer than a row. Runs once per label change, not
        /// per repaint, so building a string here is fine.
        /// </summary>
        private static string Wrap(string text, int maxChars)
        {
            if (text.Length <= maxChars)
                return text;

            var builder = new System.Text.StringBuilder(text.Length + 8);
            var lineStart = 0;
            while (text.Length - lineStart > maxChars)
            {
                var space = text.LastIndexOf(' ', lineStart + maxChars, maxChars);
                var lineEnd = space > lineStart ? space : lineStart + maxChars;
                builder.Append(text, lineStart, lineEnd - lineStart).Append('\n');
                lineStart = space > lineStart ? lineEnd + 1 : lineEnd;
            }

            return builder.Append(text, lineStart, text.Length - lineStart).ToString();
        }

        private void BuildAnchors(Vector2 fromPos, Vector2 toPos, Vector2 pan)
        {
            var waypoints = Connection.Waypoints;
            _anchorCount = waypoints.Count + 2;
            if (_anchors.Length < _anchorCount)
                _anchors = new Vector2[_anchorCount];
            _anchors[0] = fromPos;
            for (var i = 0; i < waypoints.Count; i++)
                _anchors[i + 1] = waypoints[i] + pan;
            _anchors[_anchorCount - 1] = toPos;
        }

        private Rect WaypointHandleRect(int index)
        {
            return CenteredRect(_anchors[index + 1], WaypointHandleSize);
        }

        private void HandleEvents(FlowDirection flowDirection, Vector2 pan)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && _owner.AllowCanvasClick)
                HandleMouseDown(e, flowDirection, pan);

            if (_dragPointIndex >= 0)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    _owner.MoveConnectionPoint(Connection, _dragPointIndex, e.mousePosition - pan);
                    e.Use();
                }
                else if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    _dragPointIndex = -1;
                    _owner.ConnectionGestureActive = false;
                    e.Use();
                    _owner.Repaint();
                }
            }

            if (_relinkClickPos != null)
            {
                if (!_relinkSnapped && Vector2.Distance(_relinkClickPos.Value, e.mousePosition) > RelinkSnapDistance)
                    _relinkSnapped = true;

                if (e.type == EventType.MouseDrag && e.button == 0)
                    _owner.Repaint();

                if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    if (_relinkSnapped)
                        _owner.OnRelinkDrop(Connection, _armedEnd, e.mousePosition);
                    _relinkClickPos = null;
                    _relinkSnapped = false;
                    _armedEnd = RelinkEnd.None;
                    _owner.ConnectionGestureActive = false;
                    e.Use();
                    _owner.Repaint();
                }
            }

            if (e.type == EventType.ContextClick && _owner.AllowCanvasClick)
                HandleContextClick(e, flowDirection, pan);
        }

        private void HandleMouseDown(Event e, FlowDirection flowDirection, Vector2 pan)
        {
            for (var i = 0; i < Connection.Waypoints.Count; i++)
            {
                if (!WaypointHandleRect(i).Contains(e.mousePosition))
                    continue;
                _owner.OnConnectionMouseDown(Connection);
                _dragPointIndex = i;
                _owner.ConnectionGestureActive = true;
                e.Use();
                return;
            }

            var onStart = _startRect.Contains(e.mousePosition);
            var onEnd = _endRect.Contains(e.mousePosition);
            var onMid = _midRect.Contains(e.mousePosition);
            var norm = 0f;
            var segmentIndex = -1;
            var onCurve = !onStart && !onEnd && !onMid &&
                          IsPosAlongChain(e.mousePosition, flowDirection, out norm, out segmentIndex);
            if (!onStart && !onEnd && !onMid && !onCurve)
                return;

            if (e.clickCount >= 2 && onCurve)
            {
                _owner.AddConnectionPoint(Connection, segmentIndex, e.mousePosition - pan);
                e.Use();
                return;
            }

            _owner.OnConnectionMouseDown(Connection);
            _relinkClickPos = e.mousePosition;
            _relinkSnapped = false;
            _owner.ConnectionGestureActive = true;
            if (onStart)
                _armedEnd = RelinkEnd.Source;
            else if (onEnd)
                _armedEnd = RelinkEnd.Target;
            else if (onMid)
                _armedEnd = e.shift ? RelinkEnd.Source : RelinkEnd.Target;
            else
                _armedEnd = norm <= MidNorm || e.shift ? RelinkEnd.Source : RelinkEnd.Target;
            e.Use();
        }

        private void HandleContextClick(Event e, FlowDirection flowDirection, Vector2 pan)
        {
            for (var i = 0; i < Connection.Waypoints.Count; i++)
            {
                if (!WaypointHandleRect(i).Contains(e.mousePosition))
                    continue;
                _owner.ShowConnectionPointContextMenu(Connection, i);
                e.Use();
                return;
            }

            var insertIndex = -1;
            if (IsPosAlongChain(e.mousePosition, flowDirection, out _, out var segmentIndex))
                insertIndex = segmentIndex;
            if (insertIndex >= 0 || _midRect.Contains(e.mousePosition) ||
                _startRect.Contains(e.mousePosition) || _endRect.Contains(e.mousePosition))
            {
                _owner.ShowConnectionContextMenu(Connection, insertIndex, e.mousePosition - pan);
                e.Use();
            }
        }

        private void SegmentTangents(Vector2 from, Vector2 to, FlowDirection flowDirection, out Vector2 fromTangent, out Vector2 toTangent)
        {
            if (Connection.SharpBends)
            {
                fromTangent = Vector2.zero;
                toTangent = Vector2.zero;
                return;
            }

            ResolveTangents(from, to, flowDirection, out fromTangent, out toTangent);
        }

        private Vector2 PosAlongChain(float norm, FlowDirection flowDirection)
        {
            var segments = _anchorCount - 1;
            var scaled = Mathf.Clamp01(norm) * segments;
            var segment = Mathf.Min(Mathf.FloorToInt(scaled), segments - 1);
            var t = scaled - segment;
            SegmentTangents(_anchors[segment], _anchors[segment + 1], flowDirection, out var fromTangent, out var toTangent);
            return GetPosAlongCurve(_anchors[segment], _anchors[segment + 1], fromTangent, toTangent, t);
        }

        private bool IsPosAlongChain(Vector2 position, FlowDirection flowDirection, out float norm, out int segmentIndex)
        {
            var segments = _anchorCount - 1;
            for (var i = 0; i < segments; i++)
            {
                SegmentTangents(_anchors[i], _anchors[i + 1], flowDirection, out var fromTangent, out var toTangent);
                if (IsPosAlongCurve(_anchors[i], _anchors[i + 1], fromTangent, toTangent, position, out var t))
                {
                    norm = (i + t) / segments;
                    segmentIndex = i;
                    return true;
                }
            }

            norm = 0f;
            segmentIndex = -1;
            return false;
        }

        internal static void ResolveTangents(Vector2 from, Vector2 to, FlowDirection flowDirection, out Vector2 fromTangent, out Vector2 toTangent)
        {
            if (flowDirection == FlowDirection.Vertical)
            {
                var tangentY = Mathf.Max(Mathf.Abs(from.y - to.y) * TangentMultiplier, TangentMinimum);
                fromTangent = new Vector2(0f, tangentY);
                toTangent = new Vector2(0f, -tangentY);
            }
            else
            {
                var tangentX = Mathf.Max(Mathf.Abs(from.x - to.x) * TangentMultiplier, TangentMinimum);
                fromTangent = new Vector2(tangentX, 0f);
                toTangent = new Vector2(-tangentX, 0f);
            }
        }

        private static Vector2 GetPosAlongCurve(Vector2 from, Vector2 to, Vector2 fromTangent, Vector2 toTangent, float t)
        {
            var u = 1f - t;
            var tt = t * t;
            var uu = u * u;
            var result = uu * u * from;
            result += 3f * uu * t * (from + fromTangent);
            result += 3f * u * tt * (to + toTangent);
            result += tt * t * to;
            return result;
        }

        private static bool IsPosAlongCurve(Vector2 from, Vector2 to, Vector2 fromTangent, Vector2 toTangent, Vector2 position, out float norm)
        {
            norm = 0f;
            var bounds = Rect.MinMaxRect(
                Mathf.Min(from.x, to.x) - CurveHitDistance, Mathf.Min(from.y, to.y) - CurveHitDistance,
                Mathf.Max(from.x, to.x) + CurveHitDistance, Mathf.Max(from.y, to.y) + CurveHitDistance);
            if (!bounds.Contains(position))
                return false;

            for (var i = 0; i <= CurveHitSamplesPerSegment; i++)
            {
                var t = (float)i / CurveHitSamplesPerSegment;
                if (Vector2.Distance(position, GetPosAlongCurve(from, to, fromTangent, toTangent, t)) < CurveHitDistance)
                {
                    norm = t;
                    return true;
                }
            }

            return false;
        }

        private static Rect CenteredRect(Vector2 center, float size)
        {
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }
    }
}
