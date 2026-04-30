using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 노드 포인트 상태 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 모든 월드맵 노드 포인트 이미지를 현재 플레이어 위치 기준으로 갱신합니다.
        /// </summary>
        private void RefreshWorldMapNodePointStates()
        {
            if (_worldMapDefinition == null || _worldMapDefinition.Nodes == null)
            {
                return;
            }

            for (int i = 0; i < _worldMapDefinition.Nodes.Count; i++)
            {
                WorldMapNodeDefinition node = _worldMapDefinition.Nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (_nodeIconById.TryGetValue(node.NodeId, out UIIconWorldMap icon))
                {
                    RefreshWorldMapNodePointState(node, icon);
                }
            }
        }

        /// <summary>
        /// 지정한 월드맵 노드의 포인트 이미지를 현재 플레이어 위치 기준으로 갱신합니다.
        /// </summary>
        /// <param name="node">갱신할 월드맵 노드입니다.</param>
        /// <param name="icon">포인트 이미지를 표시할 월드맵 아이콘입니다.</param>
        private void RefreshWorldMapNodePointState(WorldMapNodeDefinition node, UIIconWorldMap icon)
        {
            if (icon == null)
            {
                return;
            }

            icon.SetPointSprite(ResolveNodePointSprite(GetNodePointState(node)));
        }

        /// <summary>
        /// 지정한 월드맵 노드의 포인트 상태를 계산합니다.
        /// </summary>
        /// <param name="node">상태를 계산할 월드맵 노드입니다.</param>
        /// <returns>현재 플레이어 위치 기준의 노드 포인트 상태입니다.</returns>
        private WorldMapNodePointState GetNodePointState(WorldMapNodeDefinition node)
        {
            if (_mapManager == null || _worldMapDefinition == null || node == null || !IsNodeVisible(node))
            {
                return WorldMapNodePointState.None;
            }

            int currentMapUid = _mapManager.GetCurrentMapUid();
            if (node.MapUid == currentMapUid)
            {
                return WorldMapNodePointState.CurrentMap;
            }

            return _worldMapDefinition.TryGetNodeByMapUid(currentMapUid, out WorldMapNodeDefinition currentNode) &&
                   _worldMapDefinition.IsAdjacentNode(currentNode.NodeId, node.NodeId)
                ? WorldMapNodePointState.MovePossible
                : WorldMapNodePointState.MoveImpossible;
        }

        /// <summary>
        /// 월드맵 노드 포인트 상태에 맞는 Sprite를 반환합니다.
        /// </summary>
        /// <param name="state">포인트에 표시할 노드 상태입니다.</param>
        /// <returns>상태에 맞는 Sprite입니다. 표시할 Sprite가 없으면 null을 반환합니다.</returns>
        private Sprite ResolveNodePointSprite(WorldMapNodePointState state)
        {
            switch (state)
            {
                case WorldMapNodePointState.CurrentMap:
                    return spriteCurrentMap;
                case WorldMapNodePointState.MovePossible:
                    return spriteMovePossible;
                case WorldMapNodePointState.MoveImpossible:
                    return spriteMoveImPossible;
                default:
                    return null;
            }
        }
    }
}
