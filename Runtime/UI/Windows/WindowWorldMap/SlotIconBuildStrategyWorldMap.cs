using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 스킬 윈도우 - 아이콘 생성
    /// </summary>
    public class SlotIconBuildStrategyWorldMap : ISlotIconBuildStrategy
    {
        private readonly TableMap _tableWorldMap;
        
        public SlotIconBuildStrategyWorldMap(TableMap tableWorldMap)
        {
            _tableWorldMap = tableWorldMap;
        }
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount,
            IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (AddressableLoaderSettings.Instance == null) return;
            UIWindowWorldMap uiWindowWorldMap = window as UIWindowWorldMap;
            if (uiWindowWorldMap == null) return;
            
            var datas = _tableWorldMap.GetDatas();
            uiWindowWorldMap.maxCountIcon = datas.Count;
            if (datas.Count <= 0) return;
            
            GameObject iconWorldMap = window.iconPrefab != null ? window.iconPrefab : ConfigResources.IconWorldMap.Load();
            GameObject slotPrefab = window.slotPrefab != null ? window.slotPrefab : ConfigResources.Slot.Load();
            
            if (iconWorldMap == null) return;

            int index = 0;
            foreach (var data in datas)
            {
                int mapUid = data.Key;
                if (mapUid <= 0) continue;
                var info = data.Value;

                GameObject parent = uiWindowWorldMap.containerWorldMap;

                GameObject slotObject = window.preLoadSlots.Length > index ? window.preLoadSlots[index] : null;
                if (slotObject == null)
                {
                    if (parent == null)
                    {
                        GcLogger.LogError("아이콘을 담을 Container Icon 항목을 설정해주세요.");
                        return;
                    }
                    slotObject = Object.Instantiate(slotPrefab, parent.transform);
                }
                
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                uiSlot.Initialize(uiWindowWorldMap, uiWindowWorldMap.uid, index, slotSize);
                uiWindowWorldMap.SetPositionUiSlot(uiSlot, index);
                slots[index] = slotObject;
                
                GameObject iconObj = window.preLoadIcons.Length > index ? window.preLoadIcons[index] : null;
                if (iconObj == null)
                {
                    iconObj = Object.Instantiate(iconWorldMap, slotObject.transform);
                }
                
                UIIconWorldMap uiIcon = iconObj.GetComponent<UIIconWorldMap>();
                if (uiIcon == null) continue;
                uiIcon.Initialize(uiWindowWorldMap, uiWindowWorldMap.uid, index, index, iconSize, slotSize);
                // count, 레벨 1로 초기화
                uiIcon.ChangeInfoByUid(mapUid, 1, 1);
                
                icons[index] = iconObj;
                window.ApplyDefaultSlotIconActiveState(slotObject, iconObj);
                index++;
            }
        }
    }
}
