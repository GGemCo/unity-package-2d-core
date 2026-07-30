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
            if (_selectedUIIconWorldMap == null) return;
            if (!CanMoveToNode(_selectedUIIconWorldMap.NodeDefinition)) return;
            if (IsCurrentMapIcon(_selectedUIIconWorldMap)) return;
            _mapManager.LoadMap(_selectedUIIconWorldMap.uid);
            Show(false);
        }

        private void OnClickCancel()
        {
            Show(false);
            SceneGame.uIWindowManager.ShowWindow(UIWindowConstants.WindowUid.TimingBattleExit, true);
        }

        /// <summary>
        /// 지정한 월드맵 노드로 현재 플레이어 위치에서 이동할 수 있는지 표시 정책 기준으로 확인합니다.
        /// </summary>
        /// <param name="node">이동 대상 월드맵 노드입니다.</param>
        /// <returns>현재 표시 정책상 노드로 이동할 수 있으면 true를 반환합니다.</returns>
        private bool CanMoveToNode(WorldMapNodeDefinition node)
        {
            return CanWarpToNode(node);
        }

        /// <summary>
        /// 현재 선택된 월드맵 아이콘이 실제 이동 가능한 목적지인지에 따라 이동 버튼 상태를 갱신합니다.
        /// 현재 맵, 선택 없음, 표시 정책상 이동 불가능한 노드에서는 버튼을 비활성화합니다.
        /// </summary>
        /// <param name="icon">현재 선택된 월드맵 아이콘입니다. 선택이 없으면 null입니다.</param>
        private void RefreshWarpButtonInteractable(UIIconWorldMap icon)
        {
            if (buttonWarp == null)
            {
                return;
            }

            buttonWarp.interactable =
                icon != null &&
                !IsCurrentMapIcon(icon) &&
                CanMoveToNode(icon.NodeDefinition);
        }

        /// <summary>
        /// 지정한 월드맵 노드가 현재 플레이어가 있는 맵인지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>현재 플레이어가 있는 맵 노드이면 true를 반환합니다.</returns>
        private bool IsCurrentMapNode(WorldMapNodeDefinition node)
        {
            if (_mapManager == null || node == null)
            {
                return false;
            }

            int currentMapUid = _mapManager.GetCurrentMapUid();
            return node.MapUid == currentMapUid || GetWorldMapNodeDisplayMapUid(node) == currentMapUid;
        }

        /// <summary>
        /// 지정한 월드맵 아이콘이 현재 플레이어가 있는 맵을 표시하는지 확인합니다.
        /// map_entry_rule로 표시 맵이 대체된 경우 DisplayMapUid도 현재 맵 판정에 포함합니다.
        /// </summary>
        /// <param name="icon">확인할 월드맵 아이콘입니다.</param>
        /// <returns>아이콘의 요청 맵 또는 표시 맵이 현재 맵이면 true를 반환합니다.</returns>
        private bool IsCurrentMapIcon(UIIconWorldMap icon)
        {
            if (_mapManager == null || icon == null)
            {
                return false;
            }

            int currentMapUid = _mapManager.GetCurrentMapUid();
            return icon.uid == currentMapUid || icon.DisplayMapUid == currentMapUid;
        }

        /// <summary>
        /// 월드맵 노드가 플레이어에게 표시되는 상태인지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>노드가 월드맵에 표시되는 상태이면 true를 반환합니다.</returns>
        private bool IsNodeVisible(WorldMapNodeDefinition node)
        {
            return IsWorldMapNodeVisible(node);
        }
    }
}
