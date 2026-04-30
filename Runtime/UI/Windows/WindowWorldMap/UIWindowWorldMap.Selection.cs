using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 선택 처리 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 월드맵 전용 선택 규칙을 적용합니다.
        /// </summary>
        /// <param name="index">선택할 월드맵 노드 슬롯 인덱스입니다.</param>
        public override void SetSelectedIcon(int index)
        {
            if (selectedIcon != null)
            {
                selectedIcon.SetSelected(false);
                selectedIcon = null;
            }

            if (!CanSelectWorldMapNode(index))
            {
                OnClearedSelectedIcon();
                return;
            }

            GameObject icon = icons[index];
            if (icon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            selectedIcon = icon.GetComponent<UIIcon>();
            if (selectedIcon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            selectedIcon.SetSelected(true, false);
            OnSelectedIcon(selectedIcon);
        }

        /// <summary>
        /// 월드맵 전용 선택 참조를 기본 selectedIcon 흐름과 동기화합니다.
        /// 버튼 액션은 이 참조를 사용하므로 선택 변경 시 함께 갱신합니다.
        /// </summary>
        /// <param name="icon">선택된 아이콘입니다.</param>
        protected override void OnSelectedIcon(UIIcon icon)
        {
            base.OnSelectedIcon(icon);
            _selectedUIIconWorldMap = icon as UIIconWorldMap;
            RefreshEdgeHighlight();
            MoveSelectedWorldMapIconToCenter();
        }

        /// <summary>
        /// 월드맵 아이콘 선택이 해제되었을 때 선택 참조와 연결선 강조를 정리합니다.
        /// </summary>
        protected override void OnClearedSelectedIcon()
        {
            base.OnClearedSelectedIcon();
            _selectionCenteringRequestId++;
            _selectedUIIconWorldMap = null;
            RefreshEdgeHighlight();
        }

        /// <summary>
        /// 월드맵 선택 아이콘 상태에 맞는 선택 이미지 Sprite를 반환합니다.
        /// 현재 플레이어가 있는 맵을 선택하면 월드맵 전용 현재 맵 선택 이미지를 우선 사용합니다.
        /// </summary>
        /// <param name="icon">선택된 아이콘입니다.</param>
        /// <returns>선택 이미지에 사용할 Sprite입니다. null이면 UIWindowManager의 기본 Sprite를 사용합니다.</returns>
        public override Sprite GetSelectedIconImageSprite(UIIcon icon)
        {
            UIIconWorldMap worldMapIcon = icon as UIIconWorldMap;
            if (worldMapIcon != null &&
                IsCurrentMapNode(worldMapIcon.NodeDefinition) &&
                spriteSelectedCurrentMap != null)
            {
                return spriteSelectedCurrentMap;
            }

            return base.GetSelectedIconImageSprite(icon);
        }

        /// <summary>
        /// 월드맵 선택 아이콘 상태에 맞는 선택 이미지 Prefab을 반환합니다.
        /// 현재 플레이어가 있는 맵을 선택하면 월드맵 전용 현재 맵 선택 프리팹을 우선 사용합니다.
        /// </summary>
        /// <param name="icon">선택된 아이콘입니다.</param>
        /// <returns>선택 이미지에 사용할 Prefab입니다. null이면 UIWindowManager의 기본 Prefab을 사용합니다.</returns>
        public override GameObject GetSelectedIconImagePrefab(UIIcon icon)
        {
            UIIconWorldMap worldMapIcon = icon as UIIconWorldMap;
            if (worldMapIcon != null &&
                IsCurrentMapNode(worldMapIcon.NodeDefinition) &&
                prefabSelectedCurrentMap != null)
            {
                return prefabSelectedCurrentMap;
            }

            return base.GetSelectedIconImagePrefab(icon);
        }

        /// <summary>
        /// 선택된 월드맵 아이콘이 viewport 중앙에 오도록 월드맵 컨테이너 이동을 요청합니다.
        /// </summary>
        private void MoveSelectedWorldMapIconToCenter()
        {
            if (_selectedUIIconWorldMap == null)
            {
                return;
            }

            RectTransform selectedRect = _selectedUIIconWorldMap.GetComponent<RectTransform>();
            if (selectedRect == null)
            {
                return;
            }

            int requestId = ++_selectionCenteringRequestId;
            if (_dragController == null)
            {
                ShowSelectedWorldMapIconImage(requestId);
                return;
            }

            _dragController.MoveTargetToViewportCenter(
                selectedRect,
                selectedNodeCenteringOptions,
                () => ShowSelectedWorldMapIconImage(requestId));
        }

        /// <summary>
        /// 현재 선택 요청이 유효할 때 월드맵 선택 이미지를 표시합니다.
        /// </summary>
        /// <param name="requestId">선택 중앙 이동 요청 ID입니다.</param>
        private void ShowSelectedWorldMapIconImage(int requestId)
        {
            if (requestId != _selectionCenteringRequestId)
            {
                return;
            }

            _selectedUIIconWorldMap?.RefreshSelectedIconImage();
        }

        /// <summary>
        /// 지정한 슬롯 인덱스의 월드맵 노드를 선택할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="index">확인할 월드맵 노드 슬롯 인덱스입니다.</param>
        /// <returns>노드가 월드맵에 표시 중이면 true를 반환합니다.</returns>
        private bool CanSelectWorldMapNode(int index)
        {
            if (icons == null || index < 0 || index >= icons.Length)
            {
                return false;
            }

            if (_worldMapDefinition == null || _worldMapDefinition.Nodes == null || index >= _worldMapDefinition.Nodes.Count)
            {
                return false;
            }

            WorldMapNodeDefinition node = _worldMapDefinition.Nodes[index];

            int currentMapUid = _mapManager.GetCurrentMapUid();
            if (node.MapUid != currentMapUid)
            {
                return CanMoveToNode(node);
            }

            return IsNodeVisible(node);
        }

        /// <summary>
        /// 현재 플레이어가 있는 월드맵 노드를 선택해 viewport 중앙으로 이동시킵니다.
        /// </summary>
        private void SetCurrentMapCenter()
        {
            int currentMapUid = _mapManager.GetCurrentMapUid();
            foreach (var iconObj in icons)
            {
                var icon = iconObj.GetComponent<UIIconWorldMap>();
                if (icon == null || icon.NodeDefinition == null) continue;
                if (currentMapUid == icon.NodeDefinition.MapUid)
                {
                    SetSelectedIcon(icon.index);
                    return;
                }
            }
        }
    }
}
