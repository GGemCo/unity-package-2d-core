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
            RefreshWorldMapNodeDisplay(slotIndex, node);
            ApplyWorldMapNodeVisualState(slotIndex);
        }

        /// <summary>
        /// 저장된 월드맵 진행도와 노드 기본값을 함께 사용해 노드 표시 여부를 계산합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>기본 표시 노드이거나 저장 데이터에서 표시 또는 활성화된 노드이면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeVisible(WorldMapNodeDefinition node)
        {
            return node != null &&
                   (node.VisibleByDefault ||
                    IsWorldMapNodeVisibleByProgress(node) ||
                    IsWorldMapNodeActivated(node));
        }

        /// <summary>
        /// 월드맵 아이콘에 표시할 실제 맵 UID를 반환합니다.
        /// map_entry_rule 조건에 매칭되면 대상 맵 UID를, 매칭되지 않으면 원본 노드의 mapUid를 사용합니다.
        /// </summary>
        /// <param name="node">표시 맵 UID를 계산할 원본 월드맵 노드입니다.</param>
        /// <returns>월드맵 아이콘에 표시할 TableMap UID입니다.</returns>
        public int GetWorldMapNodeDisplayMapUid(WorldMapNodeDefinition node)
        {
            if (node == null)
            {
                return 0;
            }

            MapEntryRuleResolveResult result = ResolveWorldMapNodeEntryRule(node);
            return result.TargetMapUid > 0 ? result.TargetMapUid : node.MapUid;
        }

        /// <summary>
        /// 원본 월드맵 노드와 map_entry_rule 결과를 사용해 아이콘의 표시 정보를 갱신합니다.
        /// 위치, 연결선, 이동 요청은 원본 노드를 유지하고 이름과 이미지만 표시 맵 기준으로 교체합니다.
        /// </summary>
        /// <param name="icon">표시 정보를 적용할 월드맵 아이콘입니다.</param>
        /// <param name="node">이동 요청과 그래프 판정에 사용할 원본 월드맵 노드입니다.</param>
        /// <param name="fallbackTableMap">윈도우 테이블 캐시가 없을 때 사용할 TableMap입니다.</param>
        public void ApplyWorldMapNodeDisplay(
            UIIconWorldMap icon,
            WorldMapNodeDefinition node,
            TableMap fallbackTableMap = null)
        {
            if (icon == null)
            {
                return;
            }

            WorldMapNodeDefinition displayNode = ResolveWorldMapNodeDisplayNode(node);
            StruckTableMap displayMapData = ResolveWorldMapNodeDisplayMapData(node, fallbackTableMap);
            Sprite displayIconSprite = ResolveWorldMapNodeDisplayIconSprite(node, displayNode);
            Sprite displayInactiveSprite = ResolveWorldMapNodeDisplayInactiveSprite(node, displayNode);
            WorldMapNodeDecorationRuntimeData displayDecoration = ResolveWorldMapNodeDisplayDecoration(node, displayNode);
            icon.SetWorldMapNode(node, displayNode, displayMapData, displayIconSprite, displayInactiveSprite, displayDecoration);
        }

        /// <summary>
        /// 지정한 슬롯의 월드맵 아이콘 표시 정보를 현재 map_entry_rule 결과 기준으로 다시 적용합니다.
        /// 저장 데이터나 라이선스 상태가 바뀐 뒤에도 아이콘 이름과 이미지가 실제 입장 맵을 따라가게 합니다.
        /// </summary>
        /// <param name="slotIndex">갱신할 월드맵 노드 슬롯 인덱스입니다.</param>
        /// <param name="node">이동 요청과 그래프 판정에 사용할 원본 월드맵 노드입니다.</param>
        private void RefreshWorldMapNodeDisplay(int slotIndex, WorldMapNodeDefinition node)
        {
            if (icons == null || slotIndex < 0 || slotIndex >= icons.Length || icons[slotIndex] == null)
            {
                return;
            }

            UIIconWorldMap icon = icons[slotIndex].GetComponent<UIIconWorldMap>();
            ApplyWorldMapNodeDisplay(icon, node, _tableMap);
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
                ? WorldMapNodeVisualState.NoClear
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
        /// 저장 데이터 기준으로 지정한 월드맵 노드가 표시 상태인지 확인합니다.
        /// 활성화 여부와 분리되어 있으므로 비활성 노드는 비활성 표시를 유지할 수 있습니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>저장 데이터에 표시 기록이 있으면 true를 반환합니다.</returns>
        private bool IsWorldMapNodeVisibleByProgress(WorldMapNodeDefinition node)
        {
            return node != null &&
                   SceneGame.Instance?.saveDataManager?.MapProgress != null &&
                   SceneGame.Instance.saveDataManager.MapProgress.IsWorldMapNodeVisible(node.NodeId);
        }

        /// <summary>
        /// 원본 월드맵 노드의 입장 규칙을 현재 세이브 데이터 기준으로 계산합니다.
        /// 규칙을 적용할 수 없으면 원본 mapUid를 그대로 사용하는 결과를 반환합니다.
        /// </summary>
        /// <param name="node">입장 규칙을 계산할 원본 월드맵 노드입니다.</param>
        /// <returns>맵 입장 규칙 적용 결과입니다.</returns>
        private MapEntryRuleResolveResult ResolveWorldMapNodeEntryRule(WorldMapNodeDefinition node)
        {
            if (node == null)
            {
                return new MapEntryRuleResolveResult(0, 0, null);
            }

            MapEntryRuleResolver resolver = CreateWorldMapEntryRuleResolver();
            return resolver != null
                ? resolver.ResolveTargetMap(node.MapUid)
                : new MapEntryRuleResolveResult(node.MapUid, node.MapUid, null);
        }

        /// <summary>
        /// 월드맵 표시 정보 계산에 사용할 맵 입장 규칙 해석기를 생성합니다.
        /// 라이선스 상태가 바뀐 뒤에도 최신 세이브 매니저를 사용하도록 호출 시점마다 의존성을 다시 연결합니다.
        /// </summary>
        /// <returns>맵 입장 규칙 해석기입니다. 테이블 로더가 없으면 null입니다.</returns>
        private static MapEntryRuleResolver CreateWorldMapEntryRuleResolver()
        {
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            if (tableLoaderManager == null)
            {
                return null;
            }

            LicenseManager licenseManager = SceneGame.Instance?.saveDataManager?.LicenseManager;
            return new MapEntryRuleResolver(tableLoaderManager, licenseManager);
        }

        /// <summary>
        /// map_entry_rule 결과에 해당하는 표시용 월드맵 노드를 찾습니다.
        /// 대상 맵 UID를 가진 노드가 없으면 원본 노드를 반환하여 위치와 연결 관계를 유지합니다.
        /// </summary>
        /// <param name="node">원본 월드맵 노드입니다.</param>
        /// <returns>표시에 사용할 월드맵 노드입니다.</returns>
        private WorldMapNodeDefinition ResolveWorldMapNodeDisplayNode(WorldMapNodeDefinition node)
        {
            if (node == null || _worldMapDefinition == null)
            {
                return node;
            }

            int displayMapUid = GetWorldMapNodeDisplayMapUid(node);
            return displayMapUid > 0 &&
                   displayMapUid != node.MapUid &&
                   _worldMapDefinition.TryGetNodeByMapUid(displayMapUid, out WorldMapNodeDefinition displayNode)
                ? displayNode
                : node;
        }

        /// <summary>
        /// map_entry_rule 결과에 해당하는 표시용 TableMap 데이터를 반환합니다.
        /// 대상 맵 데이터가 없으면 원본 노드의 맵 데이터를 반환합니다.
        /// </summary>
        /// <param name="node">원본 월드맵 노드입니다.</param>
        /// <param name="fallbackTableMap">윈도우 테이블 캐시가 없을 때 사용할 TableMap입니다.</param>
        /// <returns>표시에 사용할 TableMap 데이터입니다.</returns>
        private StruckTableMap ResolveWorldMapNodeDisplayMapData(
            WorldMapNodeDefinition node,
            TableMap fallbackTableMap)
        {
            if (node == null)
            {
                return null;
            }

            TableMap tableMap = _tableMap ?? fallbackTableMap ?? TableLoaderManager.Instance?.TableMap;
            int displayMapUid = GetWorldMapNodeDisplayMapUid(node);
            return tableMap?.GetDataByUid(displayMapUid) ?? tableMap?.GetDataByUid(node.MapUid);
        }

        /// <summary>
        /// 표시용 노드에 맞는 월드맵 아이콘 Sprite를 반환합니다.
        /// 표시용 노드 Sprite가 없으면 원본 노드 Sprite를 사용합니다.
        /// </summary>
        /// <param name="node">원본 월드맵 노드입니다.</param>
        /// <param name="displayNode">map_entry_rule 결과로 선택된 표시용 노드입니다.</param>
        /// <returns>표시에 사용할 월드맵 아이콘 Sprite입니다.</returns>
        private static Sprite ResolveWorldMapNodeDisplayIconSprite(
            WorldMapNodeDefinition node,
            WorldMapNodeDefinition displayNode)
        {
            if (AddressableLoaderWorldMap.Instance == null)
            {
                return null;
            }

            if (displayNode != null &&
                AddressableLoaderWorldMap.Instance.TryGetIconSprite(displayNode, out Sprite displaySprite))
            {
                return displaySprite;
            }

            return node != null &&
                   node != displayNode &&
                   AddressableLoaderWorldMap.Instance.TryGetIconSprite(node, out Sprite fallbackSprite)
                ? fallbackSprite
                : null;
        }

        /// <summary>
        /// 표시용 노드에 맞는 월드맵 비활성 Sprite를 반환합니다.
        /// 표시용 노드 비활성 Sprite가 없으면 원본 노드 비활성 Sprite를 사용합니다.
        /// </summary>
        /// <param name="node">원본 월드맵 노드입니다.</param>
        /// <param name="displayNode">map_entry_rule 결과로 선택된 표시용 노드입니다.</param>
        /// <returns>비활성 상태에 사용할 월드맵 아이콘 Sprite입니다.</returns>
        private static Sprite ResolveWorldMapNodeDisplayInactiveSprite(
            WorldMapNodeDefinition node,
            WorldMapNodeDefinition displayNode)
        {
            if (AddressableLoaderWorldMap.Instance == null)
            {
                return null;
            }

            if (displayNode != null &&
                AddressableLoaderWorldMap.Instance.TryGetInactiveSprite(displayNode, out Sprite displaySprite))
            {
                return displaySprite;
            }

            return node != null &&
                   node != displayNode &&
                   AddressableLoaderWorldMap.Instance.TryGetInactiveSprite(node, out Sprite fallbackSprite)
                ? fallbackSprite
                : null;
        }

        /// <summary>
        /// 표시용 노드에 맞는 데코레이션 override 데이터를 반환합니다.
        /// 표시용 노드에 override가 없으면 원본 노드 override를 사용하고, 둘 다 없으면 비어 있는 값을 반환합니다.
        /// </summary>
        /// <param name="node">원본 월드맵 노드입니다.</param>
        /// <param name="displayNode">map_entry_rule 결과로 선택된 표시용 노드입니다.</param>
        /// <returns>UIIconWorldMap에 전달할 데코레이션 런타임 데이터입니다.</returns>
        private static WorldMapNodeDecorationRuntimeData ResolveWorldMapNodeDisplayDecoration(
            WorldMapNodeDefinition node,
            WorldMapNodeDefinition displayNode)
        {
            WorldMapNodeDefinition decorationNode = ResolveWorldMapNodeDecorationNode(node, displayNode);
            if (decorationNode == null)
            {
                return WorldMapNodeDecorationRuntimeData.Empty;
            }

            Sprite decorationSprite = null;
            RuntimeAnimatorController decorationAnimatorController = null;
            if (AddressableLoaderWorldMap.Instance != null)
            {
                AddressableLoaderWorldMap.Instance.TryGetDecorationSprite(decorationNode, out decorationSprite);
                AddressableLoaderWorldMap.Instance.TryGetDecorationAnimatorController(
                    decorationNode,
                    out decorationAnimatorController);
            }

            return new WorldMapNodeDecorationRuntimeData(
                decorationSprite,
                decorationAnimatorController,
                decorationNode.DecorationAnimationName,
                decorationNode.DecorationLoop,
                decorationNode.DecorationOffset,
                decorationNode.DecorationSize,
                decorationNode.DecorationScale);
        }

        /// <summary>
        /// 데코레이션 override 값을 제공할 노드를 결정합니다.
        /// 표시용 노드를 먼저 확인하고, 없으면 원본 노드의 override를 fallback으로 사용합니다.
        /// </summary>
        /// <param name="node">원본 월드맵 노드입니다.</param>
        /// <param name="displayNode">표시용 월드맵 노드입니다.</param>
        /// <returns>데코레이션 override 값을 제공할 노드입니다.</returns>
        private static WorldMapNodeDefinition ResolveWorldMapNodeDecorationNode(
            WorldMapNodeDefinition node,
            WorldMapNodeDefinition displayNode)
        {
            if (HasWorldMapNodeDecorationOverride(displayNode))
            {
                return displayNode;
            }

            return node != null && node != displayNode && HasWorldMapNodeDecorationOverride(node)
                ? node
                : displayNode ?? node;
        }

        /// <summary>
        /// 노드가 스프라이트, 애니메이터, 재생 이름, 오프셋 중 하나 이상의 데코레이션 override 값을 갖는지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>데코레이션 override 값이 있으면 true를 반환합니다.</returns>
        private static bool HasWorldMapNodeDecorationOverride(WorldMapNodeDefinition node)
        {
            return node != null &&
                   (!string.IsNullOrWhiteSpace(node.DecorationAnimatorControllerAddress) ||
                    !string.IsNullOrWhiteSpace(node.DecorationSpriteAddress) ||
                    !string.IsNullOrWhiteSpace(node.DecorationAnimationName) ||
                    node.DecorationOffset != Vector2.zero ||
                    (node.DecorationSize != Vector2.zero && node.DecorationSize != Vector2.one) ||
                    (node.DecorationScale != Vector2.zero && node.DecorationScale != Vector2.one));
        }

        /// <summary>
        /// 저장 데이터 기준으로 지정한 월드맵 노드의 표시 맵을 클리어한 기록이 있는지 확인합니다.
        /// </summary>
        /// <param name="node">확인할 월드맵 노드입니다.</param>
        /// <returns>표시용 mapUid가 클리어 기록에 있으면 true를 반환합니다.</returns>
        private bool IsWorldMapNodeCleared(WorldMapNodeDefinition node)
        {
            int displayMapUid = GetWorldMapNodeDisplayMapUid(node);
            return node != null &&
                   displayMapUid > 0 &&
                   SceneGame.Instance?.saveDataManager?.MapProgress != null &&
                   SceneGame.Instance.saveDataManager.MapProgress.IsMapCleared(displayMapUid);
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
            WorldMapNodeDefinition node = GetWorldMapNodeBySlotIndex(slotIndex);
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

            ApplyWorldMapNodePresentationState(slotIndex, node, visible);
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
