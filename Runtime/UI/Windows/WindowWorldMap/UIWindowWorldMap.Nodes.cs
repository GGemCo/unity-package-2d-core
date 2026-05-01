using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 노드 표시 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 슬롯 생성 전략에서 생성한 노드 슬롯과 아이콘을 월드맵 윈도우에 등록합니다.
        /// </summary>
        /// <param name="node">등록할 월드맵 노드 정의입니다.</param>
        /// <param name="slot">노드 슬롯 컴포넌트입니다.</param>
        /// <param name="icon">노드 아이콘 컴포넌트입니다.</param>
        public void RegisterWorldMapNode(WorldMapNodeDefinition node, UISlot slot, UIIconWorldMap icon)
        {
            if (node == null || slot == null || icon == null)
            {
                return;
            }

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect == null)
            {
                return;
            }

            _nodeRectById[node.NodeId] = slotRect;
            _nodeIconById[node.NodeId] = icon;
            PositionWorldMapSlot(slotRect, node);
            RefreshWorldMapNodePointState(node, icon);
        }

        /// <summary>
        /// 월드맵 노드의 기본 노출 여부를 유지하면서 슬롯과 아이콘의 시각 상태를 갱신합니다.
        /// 비활성 상태와 미클리어 상태는 이동 가능 여부와 분리해서 표시만 변경합니다.
        /// </summary>
        /// <param name="slotIndex">갱신할 월드맵 노드 슬롯 인덱스입니다.</param>
        public override void RefreshInactiveSlotState(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return;
            }

            WorldMapNodeDefinition node = GetWorldMapNodeBySlotIndex(slotIndex);
            if (node != null && !IsWorldMapNodeVisible(node))
            {
                ApplyWorldMapNodeVisualState(slotIndex, WorldMapNodeVisualState.Hidden);
                return;
            }

            base.RefreshInactiveSlotState(slotIndex);
            ApplyWorldMapNodeVisualState(slotIndex);
        }

        /// <summary>
        /// 저장된 월드맵 진행도와 노드 기본값을 함께 사용해 노드 표시 여부를 계산합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>기본 표시 노드이거나 저장 데이터에서 활성화된 노드이면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeVisible(WorldMapNodeDefinition node)
        {
            return node != null && (node.VisibleByDefault || IsWorldMapNodeActivated(node));
        }

        /// <summary>
        /// 저장된 월드맵 진행도와 노드 기본값을 함께 사용해 비활성 표시 여부를 계산합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>기본 비활성 노드이고 아직 저장 데이터에서 활성화되지 않았으면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeInactive(WorldMapNodeDefinition node)
        {
            return node != null && node.InactiveByDefault && !IsWorldMapNodeActivated(node);
        }

        /// <summary>
        /// 저장 데이터 기준으로 지정한 월드맵 노드가 표시되지만 아직 클리어된 적이 없는지 계산합니다.
        /// 비활성 노드는 기존 비활성 비주얼을 우선 적용하므로 NoInvite 상태에서 제외합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>노드가 표시 중이고 비활성이 아니며 클리어 기록이 없으면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeNoInvite(WorldMapNodeDefinition node)
        {
            return node != null &&
                   IsWorldMapNodeVisible(node) &&
                   !IsWorldMapNodeInactive(node) &&
                   !IsWorldMapNodeCleared(node);
        }

        /// <summary>
        /// 월드맵 노드의 표시, 비활성, 미클리어 조건을 종합해 아이콘 시각 상태를 반환합니다.
        /// 상태 우선순위는 숨김, 비활성, 미클리어, 일반 순서입니다.
        /// </summary>
        /// <param name="node">상태를 계산할 월드맵 노드입니다.</param>
        /// <returns>월드맵 노드에 적용할 시각 상태입니다.</returns>
        public WorldMapNodeVisualState GetWorldMapNodeVisualState(WorldMapNodeDefinition node)
        {
            if (node == null || !IsWorldMapNodeVisible(node))
            {
                return WorldMapNodeVisualState.Hidden;
            }

            if (IsWorldMapNodeInactive(node))
            {
                return WorldMapNodeVisualState.Inactive;
            }

            return IsWorldMapNodeNoInvite(node)
                ? WorldMapNodeVisualState.NoInvite
                : WorldMapNodeVisualState.Normal;
        }

        /// <summary>
        /// 저장 데이터 기준으로 지정한 월드맵 노드가 활성화되었는지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>저장 데이터에 활성 기록이 있으면 true를 반환합니다.</returns>
        private bool IsWorldMapNodeActivated(WorldMapNodeDefinition node)
        {
            return node != null &&
                   SceneGame.Instance?.saveDataManager?.MapProgress != null &&
                   SceneGame.Instance.saveDataManager.MapProgress.IsWorldMapNodeActivated(node.NodeId);
        }

        /// <summary>
        /// 저장 데이터 기준으로 지정한 월드맵 노드의 실제 맵을 클리어한 기록이 있는지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>노드의 mapUid가 클리어 기록에 있으면 true를 반환합니다.</returns>
        private bool IsWorldMapNodeCleared(WorldMapNodeDefinition node)
        {
            return node != null &&
                   SceneGame.Instance?.saveDataManager?.MapProgress != null &&
                   SceneGame.Instance.saveDataManager.MapProgress.IsMapCleared(node.MapUid);
        }

        /// <summary>
        /// 저장 데이터가 바뀐 뒤 월드맵 노드와 연결선의 표시 상태를 다시 계산합니다.
        /// </summary>
        public void RefreshWorldMapProgressStates()
        {
            RefreshInactiveSlotStates();
            RefreshEdgeVisibility();
            RefreshWorldMapNodePointStates();
            RefreshEdgeHighlight();
        }

        /// <summary>
        /// 슬롯 인덱스에 해당하는 월드맵 노드의 시각 상태를 계산해 슬롯과 아이콘에 적용합니다.
        /// </summary>
        /// <param name="slotIndex">시각 상태를 적용할 월드맵 노드 슬롯 인덱스입니다.</param>
        public void ApplyWorldMapNodeVisualState(int slotIndex)
        {
            ApplyWorldMapNodeVisualState(slotIndex, GetWorldMapNodeVisualState(GetWorldMapNodeBySlotIndex(slotIndex)));
        }

        /// <summary>
        /// 계산된 월드맵 노드 시각 상태를 슬롯과 아이콘 GameObject에 적용합니다.
        /// NoInvite 상태는 슬롯을 비활성화하지 않고 아이콘 비주얼만 변경합니다.
        /// </summary>
        /// <param name="slotIndex">시각 상태를 적용할 월드맵 노드 슬롯 인덱스입니다.</param>
        /// <param name="visualState">적용할 월드맵 노드 시각 상태입니다.</param>
        private void ApplyWorldMapNodeVisualState(int slotIndex, WorldMapNodeVisualState visualState)
        {
            bool visible = visualState != WorldMapNodeVisualState.Hidden;
            bool inactive = visualState == WorldMapNodeVisualState.Inactive;
            bool forceVisible = visible && visualState != WorldMapNodeVisualState.Normal;

            if (slots != null && slotIndex < slots.Length)
            {
                GameObject slotObject = slots[slotIndex];
                if (slotObject != null)
                {
                    if (!visible)
                    {
                        slotObject.SetActive(false);
                    }
                    else if (forceVisible)
                    {
                        slotObject.SetActive(true);
                    }

                    slotObject.GetComponent<UISlot>()?.SetInactiveState(visible && inactive);
                }
            }

            if (icons != null && slotIndex < icons.Length)
            {
                GameObject iconObject = icons[slotIndex];
                if (iconObject != null)
                {
                    if (!visible)
                    {
                        iconObject.SetActive(false);
                    }
                    else if (forceVisible)
                    {
                        iconObject.SetActive(true);
                    }

                    UIIconWorldMap worldMapIcon = iconObject.GetComponent<UIIconWorldMap>();
                    if (worldMapIcon != null)
                    {
                        worldMapIcon.SetWorldMapNodeVisualState(visible ? visualState : WorldMapNodeVisualState.Normal);
                    }
                    else
                    {
                        iconObject.GetComponent<UIIcon>()?.SetInactiveVisualState(visible && inactive, false);
                    }
                }
            }
        }

        /// <summary>
        /// 슬롯 인덱스에 해당하는 월드맵 노드 정의를 반환합니다.
        /// 범위를 벗어난 인덱스이거나 월드맵 정의가 없으면 null을 반환합니다.
        /// </summary>
        /// <param name="slotIndex">조회할 월드맵 노드 슬롯 인덱스입니다.</param>
        /// <returns>슬롯 인덱스에 해당하는 월드맵 노드 정의입니다.</returns>
        private WorldMapNodeDefinition GetWorldMapNodeBySlotIndex(int slotIndex)
        {
            return _worldMapDefinition != null &&
                   _worldMapDefinition.Nodes != null &&
                   slotIndex >= 0 &&
                   slotIndex < _worldMapDefinition.Nodes.Count
                ? _worldMapDefinition.Nodes[slotIndex]
                : null;
        }

        /// <summary>
        /// 월드맵 노드 정의의 정규화 좌표를 슬롯의 anchoredPosition으로 변환해 적용합니다.
        /// </summary>
        /// <param name="slotRect">위치를 적용할 슬롯 RectTransform입니다.</param>
        /// <param name="node">위치 값을 가진 월드맵 노드 정의입니다.</param>
        public void PositionWorldMapSlot(RectTransform slotRect, WorldMapNodeDefinition node)
        {
            if (slotRect == null || node == null)
            {
                return;
            }

            RectTransform parentRect = GetNodeLayerRect();
            if (parentRect == null)
            {
                return;
            }

            Rect rect = parentRect.rect;
            slotRect.anchorMin = Vector2.zero;
            slotRect.anchorMax = Vector2.zero;
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(
                node.NormalizedPosition.x * rect.width,
                node.NormalizedPosition.y * rect.height);
        }

        /// <summary>
        /// 월드맵 노드가 들어갈 부모 Transform을 반환합니다.
        /// </summary>
        /// <returns>노드 레이어 Transform입니다.</returns>
        public Transform GetWorldMapNodeParent()
        {
            EnsureWorldMapLayers();
            return containerNodeLayer != null ? containerNodeLayer : containerWorldMap?.transform;
        }

        /// <summary>
        /// 슬롯 위치를 index 기반으로 재배치합니다.
        /// 기존 호출부 호환을 위해 유지하며, 월드맵 정의가 있으면 해당 index의 노드 위치를 사용합니다.
        /// </summary>
        /// <param name="slot">위치를 변경할 슬롯입니다.</param>
        /// <param name="index">월드맵 노드 인덱스입니다.</param>
        public void SetPositionUiSlot(UISlot slot, int index)
        {
            if (slot == null || _worldMapDefinition == null || index < 0 || index >= _worldMapDefinition.Nodes.Count)
            {
                return;
            }

            PositionWorldMapSlot(slot.GetComponent<RectTransform>(), _worldMapDefinition.Nodes[index]);
        }

        /// <summary>
        /// 월드맵 노드/연결선 캐시를 초기화합니다.
        /// </summary>
        private void ClearWorldMapNodeCache()
        {
            _nodeIconById.Clear();
            _nodeRectById.Clear();
        }

        /// <summary>
        /// 노드 위치를 현재 컨테이너 크기에 맞춰 다시 계산합니다.
        /// </summary>
        private void RepositionWorldMapNodes()
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

                if (_nodeRectById.TryGetValue(node.NodeId, out RectTransform slotRect))
                {
                    PositionWorldMapSlot(slotRect, node);
                }
            }
        }
    }
}
