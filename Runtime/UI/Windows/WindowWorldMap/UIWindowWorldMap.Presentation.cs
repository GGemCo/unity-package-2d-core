using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 표시 정책과 화면별 판정 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 현재 윈도우가 기본으로 사용할 표시 정책 모드를 반환합니다.
        /// 파생 윈도우는 이 값을 바꿔 같은 월드맵 렌더링 파이프라인에 다른 정책을 적용할 수 있습니다.
        /// </summary>
        /// <returns>윈도우 기본 표시 정책 모드입니다.</returns>
        protected virtual WorldMapWindowMode ResolveDefaultPresentationMode()
        {
            return WorldMapWindowMode.Default;
        }

        /// <summary>
        /// 표시 정책 옵션이 비어 있으면 기본값을 만들고, 파생 윈도우의 기본 모드가 있으면 초기 정책을 적용합니다.
        /// </summary>
        private void EnsurePresentationOptions()
        {
            if (presentationOptions == null)
            {
                presentationOptions = WorldMapWindowPresentationOptions.CreateDefault();
            }

            WorldMapWindowMode defaultMode = ResolveDefaultPresentationMode();
            if (presentationOptions.mode == WorldMapWindowMode.Default &&
                defaultMode != WorldMapWindowMode.Default)
            {
                presentationOptions.ApplyPreset(defaultMode);
            }

            _activePresentationOptions ??= presentationOptions;
        }

        /// <summary>
        /// 다음번 월드맵 표시 한 번에 적용할 표시 정책 모드를 예약합니다.
        /// 맵 클리어처럼 같은 Window UID를 다른 진입 목적에 맞게 표시할 때 사용합니다.
        /// </summary>
        /// <param name="mode">다음 표시에서 사용할 월드맵 표시 정책 모드입니다.</param>
        public void SetNextPresentationMode(WorldMapWindowMode mode)
        {
            _nextPresentationMode = mode;
            _hasNextPresentationMode = true;
        }

        /// <summary>
        /// 지정한 모드로 예약된 다음 표시 정책을 취소합니다.
        /// 지연 표시 요청이 취소된 뒤 해당 정책이 다른 월드맵 오픈에 남는 것을 방지합니다.
        /// </summary>
        /// <param name="mode">취소할 예약 표시 정책 모드입니다.</param>
        /// <returns>일치하는 예약 표시 정책을 취소했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool CancelNextPresentationMode(WorldMapWindowMode mode)
        {
            if (!_hasNextPresentationMode || _nextPresentationMode != mode)
            {
                return false;
            }

            _hasNextPresentationMode = false;
            _nextPresentationMode = default;
            return true;
        }

        /// <summary>
        /// 현재 월드맵 오픈에 적용할 표시 정책을 확정하고 일회성 예약을 소비합니다.
        /// 예약이 없으면 Inspector에 직렬화된 기본 표시 정책을 사용합니다.
        /// </summary>
        private void ApplyPresentationOptionsForShow()
        {
            EnsurePresentationOptions();
            if (_hasNextPresentationMode)
            {
                _activePresentationOptions =
                    WorldMapWindowPresentationOptions.Create(_nextPresentationMode);
                _hasNextPresentationMode = false;
                _nextPresentationMode = default;
            }
            else
            {
                _activePresentationOptions = presentationOptions;
            }

            if (buttonCancel != null)
            {
                buttonCancel.gameObject.SetActive(_activePresentationOptions.showCancelButton);
            }
        }

        /// <summary>
        /// 지정한 노드가 강조 표시 대상인지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>노드가 강조 대상이면 true입니다.</returns>
        private bool ShouldEmphasizeNode(WorldMapNodeDefinition node)
        {
            return node != null &&
                   WorldMapWindowPresentationOptions.ContainsNodeType(
                       _activePresentationOptions.emphasizedNodeTypes,
                       node.NodeType);
        }

        /// <summary>
        /// 표시 정책에 맞춰 노드 루트에 적용할 투명도를 반환합니다.
        /// </summary>
        /// <param name="node">투명도를 계산할 월드맵 노드입니다.</param>
        /// <returns>슬롯 루트에 적용할 alpha 값입니다.</returns>
        private float GetNodeAlpha(WorldMapNodeDefinition node)
        {
            return ShouldEmphasizeNode(node)
                ? 1f
                : Mathf.Clamp01(_activePresentationOptions.dimmedNodeAlpha);
        }

        /// <summary>
        /// 지정한 슬롯 루트에 표시 정책의 alpha 값을 적용합니다.
        /// CanvasGroup이 없으면 런타임에 추가하여 슬롯, 아이콘, 포인트, 데코레이션을 함께 흐리게 합니다.
        /// </summary>
        /// <param name="slotObject">alpha를 적용할 슬롯 루트 GameObject입니다.</param>
        /// <param name="alpha">적용할 alpha 값입니다.</param>
        private static void ApplySlotRootAlpha(GameObject slotObject, float alpha)
        {
            if (slotObject == null)
            {
                return;
            }

            CanvasGroup canvasGroup = slotObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = slotObject.AddComponent<CanvasGroup>();
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        /// <summary>
        /// 지정한 슬롯 인덱스에 표시 정책의 노드 alpha를 적용합니다.
        /// </summary>
        /// <param name="slotIndex">alpha를 적용할 슬롯 인덱스입니다.</param>
        /// <param name="node">슬롯에 연결된 월드맵 노드입니다.</param>
        /// <param name="visible">노드가 현재 표시 상태이면 true입니다.</param>
        private void ApplyWorldMapNodePresentationState(int slotIndex, WorldMapNodeDefinition node, bool visible)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            GameObject slotObject = slots[slotIndex];
            float alpha = visible ? GetNodeAlpha(node) : 1f;
            ApplySlotRootAlpha(slotObject, alpha);
        }

        /// <summary>
        /// 지정한 노드를 선택할 수 있는지 표시 정책과 이동 정책을 함께 확인합니다.
        /// </summary>
        /// <param name="node">선택 가능 여부를 확인할 월드맵 노드입니다.</param>
        /// <returns>노드를 선택할 수 있으면 true입니다.</returns>
        private bool CanSelectNode(WorldMapNodeDefinition node)
        {
            if (node == null ||
                !WorldMapWindowPresentationOptions.ContainsNodeType(
                    _activePresentationOptions.selectableNodeTypes,
                    node.NodeType))
            {
                return false;
            }

            return IsCurrentMapNode(node)
                ? IsNodeVisible(node)
                : CanMoveToNode(node);
        }

        /// <summary>
        /// 표시 정책상 지정한 노드로 이동할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="node">이동 가능 여부를 확인할 월드맵 노드입니다.</param>
        /// <returns>정책상 이동 가능한 노드이면 true입니다.</returns>
        private bool CanWarpToNode(WorldMapNodeDefinition node)
        {
            return IsWarpDestinationAvailable(node, false);
        }

        /// <summary>
        /// 지정한 노드가 현재 월드맵 표시 정책에서 워프 목적지로 노출 가능한지 확인합니다.
        /// 목록 표시에서는 현재 맵을 포함할 수 있고, 실제 이동 판정에서는 현재 맵을 제외할 수 있습니다.
        /// </summary>
        /// <param name="node">워프 목적지 가능 여부를 확인할 월드맵 노드입니다.</param>
        /// <param name="includeCurrentMap">현재 플레이어가 있는 맵도 목적지 목록에 포함할지 여부입니다.</param>
        /// <returns>현재 표시 정책에서 워프 목적지로 사용할 수 있으면 <see langword="true"/>입니다.</returns>
        protected bool IsWarpDestinationAvailable(
            WorldMapNodeDefinition node,
            bool includeCurrentMap)
        {
            if (_mapManager == null || _worldMapDefinition == null || node == null)
            {
                return false;
            }

            if (!IsNodeVisible(node) || IsWorldMapNodeInactive(node))
            {
                return false;
            }

            if (!WorldMapWindowPresentationOptions.ContainsNodeType(
                    _activePresentationOptions.warpableNodeTypes,
                    node.NodeType))
            {
                return false;
            }

            if (_activePresentationOptions.requireVisitedToWarp && !IsWorldMapNodeVisited(node))
            {
                return false;
            }

            bool isCurrentMap = IsCurrentMapNode(node);
            if (isCurrentMap)
            {
                return includeCurrentMap;
            }

            return !_activePresentationOptions.requireAdjacencyToWarp || IsAdjacentToCurrentMapNode(node);
        }

        /// <summary>
        /// 지정한 노드가 현재 맵 노드와 직접 연결되어 있는지 확인합니다.
        /// </summary>
        /// <param name="node">연결 여부를 확인할 월드맵 노드입니다.</param>
        /// <returns>현재 맵 노드와 직접 연결되어 있으면 true입니다.</returns>
        private bool IsAdjacentToCurrentMapNode(WorldMapNodeDefinition node)
        {
            if (_mapManager == null || _worldMapDefinition == null || node == null)
            {
                return false;
            }

            int currentMapRequestUid = _mapManager.GetCurrentMapRequestUid();
            return _worldMapDefinition.TryGetNodeByMapUid(
                       currentMapRequestUid,
                       out WorldMapNodeDefinition currentNode) &&
                   _worldMapDefinition.IsAdjacentNode(currentNode.NodeId, node.NodeId);
        }

        /// <summary>
        /// 지정한 노드가 방문한 노드인지 확인합니다.
        /// 현재 저장 구조에서는 월드맵 노드 전용 방문 기록이 런타임 UI에 연결되어 있지 않으므로 표시 맵의 클리어 기록을 방문 기록으로 사용합니다.
        /// </summary>
        /// <param name="node">방문 여부를 확인할 월드맵 노드입니다.</param>
        /// <returns>방문한 노드로 판단되면 true입니다.</returns>
        private bool IsWorldMapNodeVisited(WorldMapNodeDefinition node)
        {
            return IsWorldMapNodeCleared(node);
        }

        /// <summary>
        /// 표시 정책상 지정한 연결선을 보여줄 수 있는지 확인합니다.
        /// </summary>
        /// <param name="edge">표시 여부를 확인할 연결선 정의입니다.</param>
        /// <returns>연결선을 보여줄 수 있으면 true입니다.</returns>
        private bool ShouldShowEdge(WorldMapEdgeDefinition edge)
        {
            return edge != null && !_activePresentationOptions.hideAllEdges;
        }

        /// <summary>
        /// 표시 정책상 선택 연결선 강조를 사용할지 확인합니다.
        /// </summary>
        /// <returns>선택 연결선 강조를 사용하면 true입니다.</returns>
        private bool ShouldHighlightSelectedEdges()
        {
            return _activePresentationOptions.highlightSelectedEdges;
        }

        /// <summary>
        /// 표시 정책상 지정한 노드의 포인트 상태 이미지를 보여줄지 확인합니다.
        /// </summary>
        /// <param name="node">포인트 표시 여부를 확인할 월드맵 노드입니다.</param>
        /// <returns>노드 포인트 상태 이미지를 표시하면 true입니다.</returns>
        private bool ShouldShowNodePointState(WorldMapNodeDefinition node)
        {
            return node != null &&
                   _activePresentationOptions.showNodePointState &&
                   WorldMapWindowPresentationOptions.ContainsNodeType(
                       _activePresentationOptions.pointStateNodeTypes,
                       node.NodeType);
        }
    }
}
