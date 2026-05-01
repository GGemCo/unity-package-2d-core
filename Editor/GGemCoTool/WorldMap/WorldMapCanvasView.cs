using System;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 그래프를 편집하는 중앙 캔버스 뷰입니다.
    /// </summary>
    internal sealed class WorldMapCanvasView
    {
        private const float NodeWidth = 104f;
        private const float NodeHeight = 44f;
        private const float EdgeHitDistance = 8f;

        private static readonly Color CanvasBackgroundColor = new Color(0.13f, 0.13f, 0.14f);
        private static readonly Color GraphBackgroundColor = new Color(0.18f, 0.18f, 0.2f);
        private static readonly Color GraphBorderColor = new Color(0.65f, 0.65f, 0.68f, 0.8f);
        private static readonly Color MinorGridColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color MajorGridColor = new Color(1f, 1f, 1f, 0.16f);

        private string _draggingNodeId;
        private bool _isPanning;
        private Vector2 _lastMousePosition;

        /// <summary>
        /// 현재 캔버스 상태를 기준으로 렌더링/입력 판단에 사용할 프레임 정보를 계산합니다.
        /// </summary>
        /// <param name="canvasRect">중앙 캔버스의 기본 배치 Rect입니다.</param>
        /// <param name="asset">편집 중인 월드맵 그래프 에셋입니다.</param>
        /// <param name="state">선택 및 캔버스 보기 상태입니다.</param>
        /// <returns>현재 캔버스 프레임 정보입니다.</returns>
        public WorldMapCanvasFrame BuildFrame(Rect canvasRect, WorldMapGraphAsset asset, WorldMapSelectionState state)
        {
            if (asset == null)
            {
                return new WorldMapCanvasFrame(canvasRect, canvasRect, canvasRect);
            }

            Rect graphRect = CalculateGraphRect(canvasRect, asset, state);
            Rect interactionRect = CalculateInteractionRect(graphRect);
            return new WorldMapCanvasFrame(canvasRect, graphRect, interactionRect);
        }

        /// <summary>
        /// 중앙 캔버스의 시각 요소를 그립니다.
        /// </summary>
        /// <param name="frame">현재 캔버스 프레임 정보입니다.</param>
        /// <param name="asset">편집 중인 월드맵 그래프 에셋입니다.</param>
        /// <param name="state">선택 및 캔버스 보기 상태입니다.</param>
        /// <param name="gridSettings">Grid 및 Snap 설정입니다.</param>
        /// <param name="tableMap">노드 표시 이름을 찾을 TableMap입니다.</param>
        public void Draw(
            WorldMapCanvasFrame frame,
            WorldMapGraphAsset asset,
            WorldMapSelectionState state,
            WorldMapCanvasGridSettings gridSettings,
            TableMap tableMap)
        {
            if (asset == null)
            {
                return;
            }

            DrawBackground(frame.HostRect, frame.GraphRect, asset, gridSettings);
            DrawEdges(asset, state, frame.GraphRect);
            DrawNodes(asset, state, tableMap, frame.GraphRect);
            DrawCanvasHint(frame.HostRect, gridSettings, state);
        }

        /// <summary>
        /// 중앙 캔버스 입력을 처리하여 패널보다 높은 우선순위의 인터랙션을 적용합니다.
        /// </summary>
        /// <param name="frame">현재 캔버스 프레임 정보입니다.</param>
        /// <param name="asset">편집 중인 월드맵 그래프 에셋입니다.</param>
        /// <param name="state">선택 및 캔버스 보기 상태입니다.</param>
        /// <param name="gridSettings">Grid 및 Snap 설정입니다.</param>
        /// <param name="tableMap">현재 표시 중인 TableMap입니다. 입력 처리 우선순위의 의미를 문서화하기 위한 인자입니다.</param>
        /// <param name="onNodeSelected">노드 선택 콜백입니다.</param>
        /// <param name="onEdgeSelected">연결선 선택 콜백입니다.</param>
        /// <param name="onCreateEdge">연결선 생성 콜백입니다.</param>
        /// <param name="onChanged">그래프 데이터 변경 콜백입니다.</param>
        public void HandleInput(
            WorldMapCanvasFrame frame,
            WorldMapGraphAsset asset,
            WorldMapSelectionState state,
            WorldMapCanvasGridSettings gridSettings,
            TableMap tableMap,
            Action<WorldMapNodeData> onNodeSelected,
            Action<WorldMapEdgeData> onEdgeSelected,
            Action<string, string> onCreateEdge,
            Action onChanged)
        {
            if (asset == null)
            {
                return;
            }

            _ = tableMap;
            HandleEvents(frame, asset, state, gridSettings, onNodeSelected, onEdgeSelected, onCreateEdge, onChanged);
        }

        /// <summary>
        /// 캔버스 영역 안에서 실제 그래프 배경이 차지할 Rect를 계산합니다.
        /// </summary>
        /// <param name="canvasRect">전체 캔버스 영역입니다.</param>
        /// <param name="asset">기준 해상도를 가진 그래프 에셋입니다.</param>
        /// <param name="state">확대/팬 상태입니다.</param>
        /// <returns>그래프 배경 영역입니다.</returns>
        private static Rect CalculateGraphRect(Rect canvasRect, WorldMapGraphAsset asset, WorldMapSelectionState state)
        {
            Vector2 referenceResolution = asset.referenceResolution;
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                referenceResolution = new Vector2(1920f, 1080f);
            }

            float fitScale = Mathf.Min(canvasRect.width / referenceResolution.x, canvasRect.height / referenceResolution.y);
            Vector2 size = referenceResolution * fitScale * state.Zoom;
            Vector2 center = canvasRect.center + state.PanOffset;
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }

        /// <summary>
        /// 캔버스 배경과 월드맵 배경 이미지를 그리고 필요 시 Grid를 겹쳐서 표시합니다.
        /// </summary>
        /// <param name="canvasRect">전체 캔버스 영역입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        /// <param name="asset">배경 Sprite를 가진 그래프 에셋입니다.</param>
        /// <param name="gridSettings">Grid 표시 설정입니다.</param>
        private static void DrawBackground(Rect canvasRect, Rect graphRect, WorldMapGraphAsset asset, WorldMapCanvasGridSettings gridSettings)
        {
            EditorGUI.DrawRect(canvasRect, CanvasBackgroundColor);
            EditorGUI.DrawRect(graphRect, GraphBackgroundColor);

            if (asset.backgroundSprite != null && asset.backgroundSprite.texture != null)
            {
                GUI.DrawTexture(graphRect, asset.backgroundSprite.texture, ScaleMode.StretchToFill);
            }

            if (gridSettings != null && gridSettings.ShowGrid)
            {
                DrawGrid(graphRect, asset.referenceResolution, gridSettings);
            }

            Handles.BeginGUI();
            Handles.color = GraphBorderColor;
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(graphRect.xMin, graphRect.yMin),
                new Vector3(graphRect.xMax, graphRect.yMin),
                new Vector3(graphRect.xMax, graphRect.yMax),
                new Vector3(graphRect.xMin, graphRect.yMax),
                new Vector3(graphRect.xMin, graphRect.yMin));
            Handles.EndGUI();
        }

        /// <summary>
        /// 기준 해상도와 Grid 설정을 이용해 그래프 위에 격자를 그립니다.
        /// </summary>
        /// <param name="graphRect">격자를 그릴 그래프 영역입니다.</param>
        /// <param name="referenceResolution">Grid 기준 해상도입니다.</param>
        /// <param name="gridSettings">Grid 표시 설정입니다.</param>
        private static void DrawGrid(Rect graphRect, Vector2 referenceResolution, WorldMapCanvasGridSettings gridSettings)
        {
            if (gridSettings == null)
            {
                return;
            }

            gridSettings.Sanitize();

            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                referenceResolution = new Vector2(1920f, 1080f);
            }

            Handles.BeginGUI();

            int columnIndex = 1;
            for (int x = gridSettings.GridCellSize.x; x < referenceResolution.x; x += gridSettings.GridCellSize.x, columnIndex++)
            {
                float normalizedX = x / referenceResolution.x;
                float canvasX = Mathf.Lerp(graphRect.xMin, graphRect.xMax, normalizedX);
                Handles.color = columnIndex % gridSettings.MajorLineInterval == 0 ? MajorGridColor : MinorGridColor;
                Handles.DrawLine(new Vector3(canvasX, graphRect.yMin), new Vector3(canvasX, graphRect.yMax));
            }

            int rowIndex = 1;
            for (int y = gridSettings.GridCellSize.y; y < referenceResolution.y; y += gridSettings.GridCellSize.y, rowIndex++)
            {
                float normalizedY = y / referenceResolution.y;
                float canvasY = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedY);
                Handles.color = rowIndex % gridSettings.MajorLineInterval == 0 ? MajorGridColor : MinorGridColor;
                Handles.DrawLine(new Vector3(graphRect.xMin, canvasY), new Vector3(graphRect.xMax, canvasY));
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// 모든 연결선을 캔버스에 그립니다.
        /// </summary>
        /// <param name="asset">연결선 목록을 가진 그래프 에셋입니다.</param>
        /// <param name="state">현재 선택 상태입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        private static void DrawEdges(WorldMapGraphAsset asset, WorldMapSelectionState state, Rect graphRect)
        {
            Handles.BeginGUI();

            for (int i = 0; i < asset.edges.Count; i++)
            {
                WorldMapEdgeData edge = asset.edges[i];
                if (edge == null)
                {
                    continue;
                }

                WorldMapNodeData from = asset.FindNode(edge.fromNodeId);
                WorldMapNodeData to = asset.FindNode(edge.toNodeId);
                if (from == null || to == null)
                {
                    continue;
                }

                Vector2 fromPoint = NormalizedToCanvas(from.normalizedPosition, graphRect);
                Vector2 toPoint = NormalizedToCanvas(to.normalizedPosition, graphRect);
                bool selected = edge.edgeId == state.SelectedEdgeId;
                Handles.color = selected ? new Color(0.3f, 0.75f, 1f, 1f) : GetEdgeColor(edge.edgeType);
                Handles.DrawAAPolyLine(selected ? 6f : 3f, fromPoint, toPoint);

                if (!edge.bidirectional)
                {
                    DrawArrow(fromPoint, toPoint, Handles.color);
                }
            }

            Handles.EndGUI();
        }

        /// <summary>
        /// 단방향 연결선의 방향 화살표를 그립니다.
        /// </summary>
        /// <param name="from">출발 화면 좌표입니다.</param>
        /// <param name="to">도착 화면 좌표입니다.</param>
        /// <param name="color">화살표 색상입니다.</param>
        private static void DrawArrow(Vector2 from, Vector2 to, Color color)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            direction.Normalize();
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 tip = Vector2.Lerp(from, to, 0.62f);
            Vector2 left = tip - direction * 12f + perpendicular * 5f;
            Vector2 right = tip - direction * 12f - perpendicular * 5f;

            Handles.color = color;
            Handles.DrawAAConvexPolygon(tip, left, right);
        }

        /// <summary>
        /// 모든 노드를 캔버스에 그립니다.
        /// </summary>
        /// <param name="asset">노드 목록을 가진 그래프 에셋입니다.</param>
        /// <param name="state">현재 선택 상태입니다.</param>
        /// <param name="tableMap">노드 표시 이름을 찾을 TableMap입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        private static void DrawNodes(WorldMapGraphAsset asset, WorldMapSelectionState state, TableMap tableMap, Rect graphRect)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip,
            };

            for (int i = 0; i < asset.nodes.Count; i++)
            {
                WorldMapNodeData node = asset.nodes[i];
                if (node == null)
                {
                    continue;
                }

                Rect nodeRect = GetNodeRect(node, graphRect);
                bool selected = node.nodeId == state.SelectedNodeId;
                bool linkStart = node.nodeId == state.LinkingFromNodeId;
                Color fill = GetNodeColor(node.nodeType, selected, linkStart);

                EditorGUI.DrawRect(nodeRect, fill);
                GUI.Box(nodeRect, GUIContent.none, EditorStyles.helpBox);
                GUI.Label(nodeRect, BuildNodeLabel(node, tableMap), labelStyle);
            }
        }

        /// <summary>
        /// 캔버스 조작 힌트를 그립니다.
        /// </summary>
        /// <param name="canvasRect">전체 캔버스 영역입니다.</param>
        /// <param name="gridSettings">Grid 및 Snap 설정입니다.</param>
        /// <param name="state">현재 선택 및 보기 상태입니다.</param>
        private static void DrawCanvasHint(Rect canvasRect, WorldMapCanvasGridSettings gridSettings, WorldMapSelectionState state)
        {
            Rect hintRect = new Rect(canvasRect.x + 10f, canvasRect.y + 10f, canvasRect.width - 20f, 38f);
            string hint = state.IsLinking
                ? "연결 생성: 도착 노드를 클릭하세요. 우클릭으로 취소"
                : (gridSettings != null && gridSettings.SnapEnabled)
                    ? "좌클릭 선택/드래그, 휠 확대, 마우스 휠 버튼 또는 Alt+드래그 팬, Snap 활성"
                    : "좌클릭 선택/드래그, 휠 확대, 마우스 휠 버튼 또는 Alt+드래그 팬";

            EditorGUI.LabelField(hintRect, hint, EditorStyles.whiteMiniLabel);
        }

        /// <summary>
        /// 캔버스 안에서 마우스/키보드 입력을 처리합니다.
        /// </summary>
        /// <param name="frame">현재 캔버스 프레임 정보입니다.</param>
        /// <param name="asset">편집 중인 그래프 에셋입니다.</param>
        /// <param name="state">선택 및 보기 상태입니다.</param>
        /// <param name="gridSettings">Grid 및 Snap 설정입니다.</param>
        /// <param name="onNodeSelected">노드 선택 콜백입니다.</param>
        /// <param name="onEdgeSelected">연결선 선택 콜백입니다.</param>
        /// <param name="onCreateEdge">연결선 생성 콜백입니다.</param>
        /// <param name="onChanged">그래프 데이터 변경 콜백입니다.</param>
        private void HandleEvents(
            WorldMapCanvasFrame frame,
            WorldMapGraphAsset asset,
            WorldMapSelectionState state,
            WorldMapCanvasGridSettings gridSettings,
            Action<WorldMapNodeData> onNodeSelected,
            Action<WorldMapEdgeData> onEdgeSelected,
            Action<string, string> onCreateEdge,
            Action onChanged)
        {
            Event current = Event.current;
            Rect inputRect = GetInputCaptureRect(frame);
            if (!inputRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.ScrollWheel)
            {
                float zoomMultiplier = 1f - current.delta.y * 0.04f;
                state.SetZoom(state.Zoom * zoomMultiplier);
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 1 && state.IsLinking)
            {
                state.CancelLinking();
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && (current.button == 2 || (current.button == 0 && current.alt)))
            {
                _isPanning = true;
                _lastMousePosition = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && _isPanning)
            {
                Vector2 delta = current.mousePosition - _lastMousePosition;
                state.PanOffset += delta;
                _lastMousePosition = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                WorldMapNodeData node = FindNodeAt(asset, frame.GraphRect, current.mousePosition);
                if (node != null)
                {
                    if (state.IsLinking)
                    {
                        if (state.LinkingFromNodeId != node.nodeId)
                        {
                            onCreateEdge?.Invoke(state.LinkingFromNodeId, node.nodeId);
                        }
                        else
                        {
                            state.CancelLinking();
                        }
                    }
                    else
                    {
                        state.SelectNode(node.nodeId);
                        onNodeSelected?.Invoke(node);
                        _draggingNodeId = node.nodeId;
                        Undo.RecordObject(asset, "월드맵 노드 이동");
                    }

                    current.Use();
                    return;
                }

                WorldMapEdgeData edge = FindEdgeAt(asset, frame.GraphRect, current.mousePosition);
                if (edge != null)
                {
                    state.SelectEdge(edge.edgeId);
                    onEdgeSelected?.Invoke(edge);
                    current.Use();
                    return;
                }

                state.ClearSelection();
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 0 && !string.IsNullOrEmpty(_draggingNodeId))
            {
                WorldMapNodeData node = asset.FindNode(_draggingNodeId);
                if (node != null)
                {
                    Vector2 normalized = CanvasToNormalized(current.mousePosition, frame.GraphRect);
                    Vector2 snappedNormalized = WorldMapCanvasGridUtility.ApplySnapNormalized(
                        normalized,
                        asset.referenceResolution,
                        gridSettings);
                    node.normalizedPosition = new Vector2(
                        Mathf.Clamp01(snappedNormalized.x),
                        Mathf.Clamp01(snappedNormalized.y));
                    EditorUtility.SetDirty(asset);
                    onChanged?.Invoke();
                }

                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0 && !string.IsNullOrEmpty(_draggingNodeId))
            {
                _draggingNodeId = null;
                current.Use();
            }
        }

        /// <summary>
        /// 그래프 Rect와 노드 외곽 여백을 이용해 실제 오버레이 입력 Rect를 계산합니다.
        /// </summary>
        /// <param name="graphRect">배경 이미지와 Grid가 그려지는 그래프 Rect입니다.</param>
        /// <returns>노드 외곽까지 포함한 입력 우선순위 Rect입니다.</returns>
        private static Rect CalculateInteractionRect(Rect graphRect)
        {
            float horizontalPadding = NodeWidth * 0.5f + EdgeHitDistance;
            float verticalPadding = NodeHeight * 0.5f + EdgeHitDistance;
            return Rect.MinMaxRect(
                graphRect.xMin - horizontalPadding,
                graphRect.yMin - verticalPadding,
                graphRect.xMax + horizontalPadding,
                graphRect.yMax + verticalPadding);
        }

        /// <summary>
        /// 기본 캔버스 영역과 그래프 오버레이 영역을 합쳐 입력을 선점할 Rect를 계산합니다.
        /// </summary>
        /// <param name="frame">현재 캔버스 프레임 정보입니다.</param>
        /// <returns>캔버스가 우선적으로 입력을 가져갈 Rect입니다.</returns>
        private static Rect GetInputCaptureRect(WorldMapCanvasFrame frame)
        {
            return Rect.MinMaxRect(
                Mathf.Min(frame.HostRect.xMin, frame.InteractionRect.xMin),
                Mathf.Min(frame.HostRect.yMin, frame.InteractionRect.yMin),
                Mathf.Max(frame.HostRect.xMax, frame.InteractionRect.xMax),
                Mathf.Max(frame.HostRect.yMax, frame.InteractionRect.yMax));
        }

        /// <summary>
        /// 지정한 마우스 위치 아래에 있는 노드를 찾습니다.
        /// </summary>
        /// <param name="asset">노드 목록을 가진 그래프 에셋입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        /// <param name="mousePosition">마우스 화면 좌표입니다.</param>
        /// <returns>찾은 노드입니다. 없으면 null입니다.</returns>
        private static WorldMapNodeData FindNodeAt(WorldMapGraphAsset asset, Rect graphRect, Vector2 mousePosition)
        {
            for (int i = asset.nodes.Count - 1; i >= 0; i--)
            {
                WorldMapNodeData node = asset.nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (GetNodeRect(node, graphRect).Contains(mousePosition))
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// 지정한 마우스 위치 근처에 있는 연결선을 찾습니다.
        /// </summary>
        /// <param name="asset">연결선 목록을 가진 그래프 에셋입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        /// <param name="mousePosition">마우스 화면 좌표입니다.</param>
        /// <returns>찾은 연결선입니다. 없으면 null입니다.</returns>
        private static WorldMapEdgeData FindEdgeAt(WorldMapGraphAsset asset, Rect graphRect, Vector2 mousePosition)
        {
            for (int i = asset.edges.Count - 1; i >= 0; i--)
            {
                WorldMapEdgeData edge = asset.edges[i];
                if (edge == null)
                {
                    continue;
                }

                WorldMapNodeData from = asset.FindNode(edge.fromNodeId);
                WorldMapNodeData to = asset.FindNode(edge.toNodeId);
                if (from == null || to == null)
                {
                    continue;
                }

                Vector2 fromPoint = NormalizedToCanvas(from.normalizedPosition, graphRect);
                Vector2 toPoint = NormalizedToCanvas(to.normalizedPosition, graphRect);
                if (DistanceToSegment(mousePosition, fromPoint, toPoint) <= EdgeHitDistance)
                {
                    return edge;
                }
            }

            return null;
        }

        /// <summary>
        /// 노드 데이터를 캔버스에서 사용할 Rect로 변환합니다.
        /// </summary>
        /// <param name="node">노드 데이터입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        /// <returns>노드 Rect입니다.</returns>
        private static Rect GetNodeRect(WorldMapNodeData node, Rect graphRect)
        {
            Vector2 center = NormalizedToCanvas(node.normalizedPosition, graphRect);
            return new Rect(center.x - NodeWidth * 0.5f, center.y - NodeHeight * 0.5f, NodeWidth, NodeHeight);
        }

        /// <summary>
        /// 정규화 좌표를 캔버스 화면 좌표로 변환합니다.
        /// </summary>
        /// <param name="normalized">0~1 범위의 정규화 좌표입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        /// <returns>캔버스 화면 좌표입니다.</returns>
        private static Vector2 NormalizedToCanvas(Vector2 normalized, Rect graphRect)
        {
            return new Vector2(
                graphRect.xMin + graphRect.width * normalized.x,
                graphRect.yMax - graphRect.height * normalized.y);
        }

        /// <summary>
        /// 캔버스 화면 좌표를 정규화 좌표로 변환합니다.
        /// </summary>
        /// <param name="canvasPosition">캔버스 화면 좌표입니다.</param>
        /// <param name="graphRect">그래프 배경 영역입니다.</param>
        /// <returns>0~1 범위 기준의 정규화 좌표입니다.</returns>
        private static Vector2 CanvasToNormalized(Vector2 canvasPosition, Rect graphRect)
        {
            if (graphRect.width <= 0f || graphRect.height <= 0f)
            {
                return Vector2.zero;
            }

            return new Vector2(
                (canvasPosition.x - graphRect.xMin) / graphRect.width,
                (graphRect.yMax - canvasPosition.y) / graphRect.height);
        }

        /// <summary>
        /// 점과 선분 사이의 최단 거리를 계산합니다.
        /// </summary>
        /// <param name="point">검사할 점입니다.</param>
        /// <param name="a">선분 시작점입니다.</param>
        /// <param name="b">선분 끝점입니다.</param>
        /// <returns>점과 선분 사이의 거리입니다.</returns>
        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.001f)
            {
                return Vector2.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
            Vector2 projection = a + segment * t;
            return Vector2.Distance(point, projection);
        }

        /// <summary>
        /// 노드에 표시할 짧은 라벨을 만듭니다.
        /// </summary>
        /// <param name="node">표시할 노드입니다.</param>
        /// <param name="tableMap">TableMap 이름 조회용 테이블입니다.</param>
        /// <returns>노드 라벨 문자열입니다.</returns>
        private static string BuildNodeLabel(WorldMapNodeData node, TableMap tableMap)
        {
            string title = node.titleOverride;
            if (string.IsNullOrWhiteSpace(title) && tableMap != null)
            {
                StruckTableMap mapData = tableMap.GetDataByUid(node.mapUid);
                if (mapData != null)
                {
                    title = mapData.Name;
                }
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Map " + node.mapUid;
            }

            return node.mapUid + "\n" + title;
        }

        /// <summary>
        /// 노드 타입과 선택 상태에 맞는 배경색을 반환합니다.
        /// </summary>
        /// <param name="nodeType">노드 타입입니다.</param>
        /// <param name="selected">현재 선택 여부입니다.</param>
        /// <param name="linkStart">연결 생성 시작 노드 여부입니다.</param>
        /// <returns>노드 배경색입니다.</returns>
        private static Color GetNodeColor(WorldMapNodeType nodeType, bool selected, bool linkStart)
        {
            if (linkStart)
            {
                return new Color(0.95f, 0.72f, 0.28f, 0.95f);
            }

            if (selected)
            {
                return new Color(0.28f, 0.62f, 0.95f, 0.95f);
            }

            switch (nodeType)
            {
                case WorldMapNodeType.Start:
                    return new Color(0.36f, 0.78f, 0.48f, 0.9f);
                case WorldMapNodeType.Boss:
                    return new Color(0.85f, 0.32f, 0.36f, 0.9f);
                case WorldMapNodeType.Rest:
                    return new Color(0.42f, 0.64f, 0.88f, 0.9f);
                case WorldMapNodeType.Shop:
                    return new Color(0.9f, 0.68f, 0.34f, 0.9f);
                case WorldMapNodeType.Hidden:
                    return new Color(0.55f, 0.55f, 0.62f, 0.9f);
                default:
                    return new Color(0.72f, 0.72f, 0.74f, 0.9f);
            }
        }

        /// <summary>
        /// 연결선 타입에 맞는 색상을 반환합니다.
        /// </summary>
        /// <param name="edgeType">연결선 타입입니다.</param>
        /// <returns>연결선 색상입니다.</returns>
        private static Color GetEdgeColor(WorldMapEdgeType edgeType)
        {
            switch (edgeType)
            {
                case WorldMapEdgeType.Locked:
                    return new Color(0.95f, 0.62f, 0.24f, 0.9f);
                case WorldMapEdgeType.Secret:
                    return new Color(0.62f, 0.46f, 0.92f, 0.9f);
                default:
                    return new Color(0.86f, 0.86f, 0.88f, 0.88f);
            }
        }
    }
}
