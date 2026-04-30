using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 맵 이동 판정 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 현재 선택된 월드맵 노드의 mapUid로 맵 이동을 요청합니다.
        /// </summary>
        private void OnClickWarp()
        {
            if (GcLogger.IsNull(_mapManager, nameof(MapManager))) return;
            if (GcLogger.IsNull(_selectedUIIconWorldMap, "선택된 맵이 없습니다.")) return;
            if (!CanMoveToNode(_selectedUIIconWorldMap.NodeDefinition)) return;
            if (_selectedUIIconWorldMap.uid == _mapManager.GetCurrentMapUid()) return;
            _mapManager.LoadMap(_selectedUIIconWorldMap.uid);
        }

        /// <summary>
        /// 지정한 월드맵 노드로 현재 플레이어 위치에서 이동할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="node">이동 대상 월드맵 노드입니다.</param>
        /// <returns>노드가 표시 중이고 현재 맵과 바로 연결되어 있으면 true를 반환합니다.</returns>
        private bool CanMoveToNode(WorldMapNodeDefinition node)
        {
            if (_mapManager == null || _worldMapDefinition == null || node == null)
            {
                return false;
            }

            if (!IsNodeVisible(node))
            {
                return false;
            }

            int currentMapUid = _mapManager.GetCurrentMapUid();
            if (node.MapUid == currentMapUid)
            {
                return false;
            }

            return _worldMapDefinition.TryGetNodeByMapUid(currentMapUid, out WorldMapNodeDefinition currentNode) &&
                   _worldMapDefinition.IsAdjacentNode(currentNode.NodeId, node.NodeId);
        }

        /// <summary>
        /// 월드맵 노드가 플레이어에게 표시되는 상태인지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>노드가 월드맵에 표시되는 상태이면 true를 반환합니다.</returns>
        private static bool IsNodeVisible(WorldMapNodeDefinition node)
        {
            return node != null && node.VisibleByDefault;
        }
    }
}
