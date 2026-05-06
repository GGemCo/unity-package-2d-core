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
            GameObject icon = icons[index];
            if (icon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            // 이동하지 못 하는 곳을 클릭했을 때는 아무것도 하지 않는다.
            if (!CanSelectWorldMapNode(index))
            {
                // OnClearedSelectedIcon();
                return;
            }
            
            if (selectedIcon != null)
            {
                selectedIcon.SetSelected(false);
                selectedIcon = null;
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
        /// 월드맵 창의 표시 상태가 바뀔 때 선택 상태와 예약된 중앙 이동 콜백을 정리합니다.
        /// 닫힘 콜백이 누락된 파생 창도 다음 열림 시 선택 이펙트가 새 요청으로 처리되도록 합니다.
        /// </summary>
        private void ResetSelectionStateForWindowLifecycle()
        {
            _dragController?.StopCenteringAnimation();

            if (selectedIcon != null)
            {
                RemoveSelectedIcon();
                return;
            }

            // 선택 아이콘 참조가 이미 사라졌더라도 Presenter에 남은 활성 선택 이미지를 숨깁니다.
            SceneGame?.uIWindowManager?.ShowSelectIconImage(false);
            OnClearedSelectedIcon();
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
                IsCurrentMapIcon(worldMapIcon) &&
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
                IsCurrentMapIcon(worldMapIcon) &&
                prefabSelectedCurrentMap != null)
            {
                return prefabSelectedCurrentMap;
            }

            return base.GetSelectedIconImagePrefab(icon);
        }

        /// <summary>
        /// 월드맵 선택 아이콘 상태에 맞는 선택 이미지 애니메이션 설정을 반환합니다.
        /// 현재 플레이어가 있는 맵을 선택하면 월드맵 전용 현재 맵 애니메이션 설정을 우선 사용합니다.
        /// </summary>
        /// <param name="icon">선택된 아이콘입니다.</param>
        /// <returns>선택 이미지 애니메이션 설정입니다. null이면 UIWindowManager의 기본 설정을 사용합니다.</returns>
        public override UISelectedIconAnimationSettings GetSelectedIconAnimation(UIIcon icon)
        {
            UIIconWorldMap worldMapIcon = icon as UIIconWorldMap;
            if (worldMapIcon != null &&
                IsCurrentMapIcon(worldMapIcon) &&
                overrideSelectedCurrentMapAnimation)
            {
                return selectedCurrentMapAnimation;
            }

            return base.GetSelectedIconAnimation(icon);
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
            ApplySelectedWorldMapFacing();
        }

        /// <summary>
        /// 현재 선택된 월드맵 노드 위치를 기준으로 선택 이미지의 좌우 방향을 갱신합니다.
        /// 현재 맵보다 왼쪽 노드를 선택하면 왼쪽을 바라보도록 선택 이미지 루트 스케일을 반전합니다.
        /// </summary>
        private void ApplySelectedWorldMapFacing()
        {
            if (_selectedUIIconWorldMap == null || SceneGame.Instance == null || SceneGame.Instance.uIWindowManager == null)
            {
                return;
            }

            GameObject selectedIconImageObject = SceneGame.Instance.uIWindowManager.GetActiveSelectedIconImageObject();
            if (selectedIconImageObject == null)
            {
                return;
            }

            UISelectedIconFacingRuntimeAdapter facingAdapter =
                selectedIconImageObject.GetComponent<UISelectedIconFacingRuntimeAdapter>();
            if (facingAdapter == null)
            {
                facingAdapter = selectedIconImageObject.AddComponent<UISelectedIconFacingRuntimeAdapter>();
            }

            facingAdapter.SetFaceLeft(ShouldFaceSelectedWorldMapNodeLeft());
        }

        /// <summary>
        /// 현재 선택된 월드맵 노드가 현재 플레이어 위치 노드보다 왼쪽에 있는지 확인합니다.
        /// 월드맵 그래프 정의의 정규화 X 좌표를 비교하여 중앙 이동 연출과 무관하게 방향을 계산합니다.
        /// </summary>
        /// <returns>현재 맵보다 왼쪽 노드를 선택했으면 true를 반환합니다.</returns>
        private bool ShouldFaceSelectedWorldMapNodeLeft()
        {
            if (_selectedUIIconWorldMap == null)
            {
                return false;
            }

            if (IsCurrentMapIcon(_selectedUIIconWorldMap) || !CanMoveToNode(_selectedUIIconWorldMap.NodeDefinition))
            {
                return false;
            }

            UIIconWorldMap currentMapIcon = GetCurrentWorldMapIcon();
            if (currentMapIcon == null || currentMapIcon.NodeDefinition == null || _selectedUIIconWorldMap.NodeDefinition == null)
            {
                return false;
            }

            return _selectedUIIconWorldMap.NodeDefinition.NormalizedPosition.x <
                   currentMapIcon.NodeDefinition.NormalizedPosition.x;
        }

        /// <summary>
        /// 현재 플레이어가 있는 맵을 표시하는 월드맵 아이콘을 반환합니다.
        /// </summary>
        /// <returns>현재 맵 아이콘입니다. 찾지 못하면 null을 반환합니다.</returns>
        private UIIconWorldMap GetCurrentWorldMapIcon()
        {
            if (icons == null)
            {
                return null;
            }

            for (int i = 0; i < icons.Length; i++)
            {
                GameObject iconObject = icons[i];
                if (iconObject == null)
                {
                    continue;
                }

                UIIconWorldMap icon = iconObject.GetComponent<UIIconWorldMap>();
                if (icon != null && IsCurrentMapIcon(icon))
                {
                    return icon;
                }
            }

            return null;
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

            return CanSelectNode(node);
        }

        /// <summary>
        /// 현재 플레이어가 있는 월드맵 노드를 선택해 viewport 중앙으로 이동시킵니다.
        /// </summary>
        private void SetCurrentMapCenter()
        {
            foreach (var iconObj in icons)
            {
                if (iconObj == null) continue;
                var icon = iconObj.GetComponent<UIIconWorldMap>();
                if (icon == null || icon.NodeDefinition == null) continue;
                if (IsCurrentMapIcon(icon))
                {
                    SetSelectedIcon(icon.index);
                    return;
                }
            }
        }
    }
}
