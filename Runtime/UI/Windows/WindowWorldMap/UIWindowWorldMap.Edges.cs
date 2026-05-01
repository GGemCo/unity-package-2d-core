using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 연결선 표시 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 생성되어 있는 모든 연결선 UI를 제거합니다.
        /// </summary>
        private void ClearEdgeLines()
        {
            for (int i = _edgeLines.Count - 1; i >= 0; i--)
            {
                WorldMapLineRenderer line = _edgeLines[i];
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }

            _edgeLines.Clear();
        }

        /// <summary>
        /// 월드맵 정의의 edge 목록을 기준으로 연결선 UI를 생성합니다.
        /// </summary>
        private void BuildEdgeLines()
        {
            ClearEdgeLines();
            EnsureWorldMapLayers();

            if (_worldMapDefinition == null || _worldMapDefinition.Edges == null || containerLineLayer == null)
            {
                return;
            }

            for (int i = 0; i < _worldMapDefinition.Edges.Count; i++)
            {
                WorldMapEdgeDefinition edge = _worldMapDefinition.Edges[i];
                if (edge == null)
                {
                    continue;
                }

                if (!_nodeRectById.TryGetValue(edge.FromNodeId, out RectTransform from) ||
                    !_nodeRectById.TryGetValue(edge.ToNodeId, out RectTransform to))
                {
                    continue;
                }

                GameObject lineObject = new GameObject("Edge_" + edge.EdgeId, typeof(RectTransform), typeof(Image), typeof(WorldMapLineRenderer));
                RectTransform lineRect = lineObject.GetComponent<RectTransform>();
                lineRect.SetParent(containerLineLayer, false);
                lineRect.anchorMin = Vector2.zero;
                lineRect.anchorMax = Vector2.zero;

                WorldMapLineRenderer line = lineObject.GetComponent<WorldMapLineRenderer>();
                line.Initialize(
                    edge,
                    from,
                    to,
                    GetEdgeColor(edge.EdgeType),
                    edgeColorHighlighted,
                    edgeThickness,
                    ResolveEdgeSprite(edge),
                    edgeSpriteHighlighted,
                    edgeSpriteDrawMode);
                lineObject.SetActive(IsEdgeVisible(edge));
                _edgeLines.Add(line);
            }

            RefreshEdgeHighlight();
        }

        /// <summary>
        /// 모든 연결선 UI의 위치와 회전을 즉시 갱신합니다.
        /// </summary>
        private void RefreshEdgeLines()
        {
            for (int i = 0; i < _edgeLines.Count; i++)
            {
                if (_edgeLines[i] != null)
                {
                    _edgeLines[i].Refresh();
                }
            }
        }

        /// <summary>
        /// 현재 선택된 노드와 현재 플레이어 위치를 기준으로 강조할 연결선을 갱신합니다.
        /// </summary>
        private void RefreshEdgeHighlight()
        {
            for (int i = 0; i < _edgeLines.Count; i++)
            {
                WorldMapLineRenderer line = _edgeLines[i];
                if (line != null)
                {
                    line.SetHighlighted(ShouldHighlightEdge(line));
                }
            }
        }

        /// <summary>
        /// 저장된 월드맵 진행도 기준으로 모든 연결선의 표시 상태를 갱신합니다.
        /// </summary>
        private void RefreshEdgeVisibility()
        {
            for (int i = 0; i < _edgeLines.Count; i++)
            {
                WorldMapLineRenderer line = _edgeLines[i];
                if (line != null)
                {
                    line.gameObject.SetActive(IsEdgeVisible(line.EdgeDefinition));
                }
            }
        }

        /// <summary>
        /// 지정한 연결선을 현재 선택 상태에서 강조해야 하는지 확인합니다.
        /// 현재 맵을 선택한 경우에는 강조하지 않고, 이동 가능한 다른 맵을 선택한 경우 현재 맵과 선택 맵을 직접 연결하는 선만 강조합니다.
        /// </summary>
        /// <param name="line">강조 여부를 확인할 연결선 렌더러입니다.</param>
        /// <returns>연결선을 강조해야 하면 true를 반환합니다.</returns>
        private bool ShouldHighlightEdge(WorldMapLineRenderer line)
        {
            if (line == null || _selectedUIIconWorldMap == null)
            {
                return false;
            }

            WorldMapNodeDefinition selectedNode = _selectedUIIconWorldMap.NodeDefinition;
            if (selectedNode == null || IsCurrentMapNode(selectedNode) || !CanMoveToNode(selectedNode))
            {
                return false;
            }

            if (_mapManager == null ||
                _worldMapDefinition == null ||
                !_worldMapDefinition.TryGetNodeByMapUid(_mapManager.GetCurrentMapUid(), out WorldMapNodeDefinition currentNode))
            {
                return false;
            }

            return line.ConnectsNode(currentNode.NodeId, selectedNode.NodeId);
        }

        /// <summary>
        /// 연결선 타입에 맞는 기본 색상을 반환합니다.
        /// </summary>
        /// <param name="edgeType">연결선 타입입니다.</param>
        /// <returns>연결선 색상입니다.</returns>
        private Color GetEdgeColor(WorldMapEdgeType edgeType)
        {
            switch (edgeType)
            {
                case WorldMapEdgeType.Locked:
                    return edgeColorLocked;
                case WorldMapEdgeType.Secret:
                    return edgeColorSecret;
                default:
                    return edgeColorNormal;
            }
        }

        /// <summary>
        /// 연결선 정의와 타입별 기본값을 기준으로 사용할 연결선 스프라이트를 반환합니다.
        /// </summary>
        /// <param name="edge">스프라이트를 결정할 연결선 정의입니다.</param>
        /// <returns>사용할 연결선 스프라이트입니다. 없으면 null을 반환합니다.</returns>
        private Sprite ResolveEdgeSprite(WorldMapEdgeDefinition edge)
        {
            if (edge != null &&
                AddressableLoaderWorldMap.Instance != null &&
                AddressableLoaderWorldMap.Instance.TryGetEdgeSprite(edge, out Sprite edgeSprite))
            {
                return edgeSprite;
            }

            return edge != null ? GetDefaultEdgeSprite(edge.EdgeType) : edgeSpriteNormal;
        }

        /// <summary>
        /// 연결선 타입에 맞는 기본 스프라이트를 반환합니다.
        /// </summary>
        /// <param name="edgeType">연결선 타입입니다.</param>
        /// <returns>타입별 기본 연결선 스프라이트입니다.</returns>
        private Sprite GetDefaultEdgeSprite(WorldMapEdgeType edgeType)
        {
            switch (edgeType)
            {
                case WorldMapEdgeType.Locked:
                    return edgeSpriteLocked != null ? edgeSpriteLocked : edgeSpriteNormal;
                case WorldMapEdgeType.Secret:
                    return edgeSpriteSecret != null ? edgeSpriteSecret : edgeSpriteNormal;
                default:
                    return edgeSpriteNormal;
            }
        }

        /// <summary>
        /// edge의 양 끝 노드가 기본 표시 대상인지 확인합니다.
        /// </summary>
        /// <param name="edge">표시 여부를 확인할 연결선 정의입니다.</param>
        /// <returns>양 끝 노드가 표시 대상이면 true입니다.</returns>
        private bool IsEdgeVisible(WorldMapEdgeDefinition edge)
        {
            if (_worldMapDefinition == null || edge == null)
            {
                return false;
            }

            return _worldMapDefinition.TryGetNode(edge.FromNodeId, out WorldMapNodeDefinition from) &&
                   _worldMapDefinition.TryGetNode(edge.ToNodeId, out WorldMapNodeDefinition to) &&
                   IsWorldMapNodeVisible(from) &&
                   IsWorldMapNodeVisible(to);
        }
    }
}
