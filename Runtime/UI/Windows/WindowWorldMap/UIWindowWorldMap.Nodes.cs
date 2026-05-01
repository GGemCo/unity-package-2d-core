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
        /// 월드맵 노드의 기본 노출 여부를 유지하면서 슬롯과 아이콘의 비활성 표시 상태를 갱신합니다.
        /// </summary>
        /// <param name="slotIndex">갱신할 월드맵 노드 슬롯 인덱스입니다.</param>
        public override void RefreshInactiveSlotState(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return;
            }

            WorldMapNodeDefinition node = _worldMapDefinition != null &&
                                          _worldMapDefinition.Nodes != null &&
                                          slotIndex < _worldMapDefinition.Nodes.Count
                ? _worldMapDefinition.Nodes[slotIndex]
                : null;
            if (node != null && !IsWorldMapNodeVisible(node))
            {
                if (slots != null && slotIndex < slots.Length)
                {
                    slots[slotIndex]?.SetActive(false);
                }

                if (icons != null && slotIndex < icons.Length)
                {
                    icons[slotIndex]?.SetActive(false);
                }

                return;
            }

            base.RefreshInactiveSlotState(slotIndex);
            if (node != null && IsWorldMapNodeInactive(node))
            {
                ApplyWorldMapNodeInactiveVisual(slotIndex);
            }
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
        /// 월드맵 노드를 보이는 상태로 유지하면서 슬롯과 아이콘에 비활성 비주얼을 적용합니다.
        /// </summary>
        /// <param name="slotIndex">비활성 비주얼을 적용할 월드맵 노드 슬롯 인덱스입니다.</param>
        private void ApplyWorldMapNodeInactiveVisual(int slotIndex)
        {
            if (slots != null && slotIndex < slots.Length)
            {
                GameObject slotObject = slots[slotIndex];
                if (slotObject != null)
                {
                    slotObject.SetActive(true);
                    slotObject.GetComponent<UISlot>()?.SetInactiveState(true);
                }
            }

            if (icons != null && slotIndex < icons.Length)
            {
                GameObject iconObject = icons[slotIndex];
                if (iconObject != null)
                {
                    iconObject.SetActive(true);
                    iconObject.GetComponent<UIIcon>()?.SetInactiveVisualState(true, false);
                }
            }
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
