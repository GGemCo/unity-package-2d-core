using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 정의의 노드 목록을 기준으로 슬롯과 아이콘을 생성하는 전략입니다.
    /// </summary>
    public class SlotIconBuildStrategyWorldMap : ISlotIconBuildStrategy
    {
        private readonly TableMap _tableWorldMap;
        
        /// <summary>
        /// 월드맵 슬롯 생성 전략을 초기화합니다.
        /// </summary>
        /// <param name="tableWorldMap">노드의 mapUid를 TableMap 데이터로 해석하기 위한 테이블입니다.</param>
        public SlotIconBuildStrategyWorldMap(TableMap tableWorldMap)
        {
            _tableWorldMap = tableWorldMap;
        }

        /// <summary>
        /// 월드맵 정의의 노드 개수만큼 슬롯과 아이콘을 만들고 정규화 좌표 위치에 배치합니다.
        /// </summary>
        /// <param name="window">대상 UI 윈도우입니다.</param>
        /// <param name="container">기존 슬롯 컨테이너입니다. 월드맵에서는 node layer를 우선 사용합니다.</param>
        /// <param name="maxCount">생성할 최대 슬롯 수입니다.</param>
        /// <param name="iconType">아이콘 타입입니다.</param>
        /// <param name="slotSize">슬롯 크기입니다.</param>
        /// <param name="iconSize">아이콘 크기입니다.</param>
        /// <param name="slots">생성된 슬롯을 저장할 배열입니다.</param>
        /// <param name="icons">생성된 아이콘을 저장할 배열입니다.</param>
        public void BuildSlotsAndIcons(
            UIWindow window,
            GridLayoutGroup container,
            int maxCount,
            IconConstants.Type iconType,
            Vector2 slotSize,
            Vector2 iconSize,
            GameObject[] slots,
            GameObject[] icons)
        {
            UIWindowWorldMap uiWindowWorldMap = window as UIWindowWorldMap;
            WorldMapDefinition definition = uiWindowWorldMap != null ? uiWindowWorldMap.WorldMapDefinition : null;
            if (uiWindowWorldMap == null || definition == null || definition.Nodes == null)
            {
                return;
            }

            GameObject iconWorldMap = window.iconPrefab != null ? window.iconPrefab : ConfigResources.IconWorldMap.Load();
            GameObject slotPrefab = window.slotPrefab != null ? window.slotPrefab : ConfigResources.Slot.Load();
            if (iconWorldMap == null || slotPrefab == null)
            {
                return;
            }

            Transform parent = uiWindowWorldMap.GetWorldMapNodeParent();
            if (parent == null)
            {
                GcLogger.LogError("월드맵 노드를 담을 컨테이너를 설정해주세요.");
                return;
            }

            int usableCount = Mathf.Min(maxCount, definition.Nodes.Count);
            for (int index = 0; index < usableCount; index++)
            {
                WorldMapNodeDefinition node = definition.Nodes[index];
                if (node == null || node.MapUid <= 0)
                {
                    continue;
                }

                GameObject slotObject = window.preLoadSlots != null && window.preLoadSlots.Length > index
                    ? window.preLoadSlots[index]
                    : null;
                if (slotObject == null)
                {
                    slotObject = Object.Instantiate(slotPrefab, parent);
                }
                else
                {
                    slotObject.transform.SetParent(parent, false);
                }
                
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null)
                {
                    continue;
                }

                uiSlot.Initialize(uiWindowWorldMap, uiWindowWorldMap.uid, index, slotSize);
                slots[index] = slotObject;
                
                GameObject iconObj = window.preLoadIcons != null && window.preLoadIcons.Length > index
                    ? window.preLoadIcons[index]
                    : null;
                if (iconObj == null)
                {
                    iconObj = Object.Instantiate(iconWorldMap, slotObject.transform);
                }
                else
                {
                    iconObj.transform.SetParent(slotObject.transform, false);
                }
                
                UIIconWorldMap uiIcon = iconObj.GetComponent<UIIconWorldMap>();
                if (uiIcon == null)
                {
                    continue;
                }

                uiIcon.Initialize(uiWindowWorldMap, uiWindowWorldMap.uid, index, index, iconSize, slotSize);
                Sprite iconSprite = null;
                if (AddressableLoaderWorldMap.Instance != null)
                {
                    AddressableLoaderWorldMap.Instance.TryGetIconSprite(node, out iconSprite);
                }

                uiIcon.SetWorldMapNode(node, _tableWorldMap != null ? _tableWorldMap.GetDataByUid(node.MapUid) : null, iconSprite);
                
                icons[index] = iconObj;
                if (uiWindowWorldMap.IsWorldMapNodeVisible(node))
                {
                    window.ApplyDefaultSlotIconActiveState(slotObject, iconObj);
                }
                else
                {
                    slotObject.SetActive(false);
                    iconObj.SetActive(false);
                }

                uiWindowWorldMap.ApplyWorldMapNodeVisualState(index);
                uiWindowWorldMap.RegisterWorldMapNode(node, uiSlot, uiIcon);
            }
        }
    }
}
