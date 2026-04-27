using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 아이콘 생성 전략
    /// </summary>
    public class DefaultSlotIconBuildStrategy : ISlotIconBuildStrategy
    {
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount, IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (AddressableLoaderPrefabCommon.Instance == null) return;
            GameObject iconPrefab = window.iconPrefab != null ? window.iconPrefab : IconConstants.LoadByIconType(iconType);
            GameObject slotPrefab = window.slotPrefab != null ? window.slotPrefab : ConfigResources.Slot.Load();

            for (int i = 0; i < maxCount; i++)
            {
                // PreLoad Slots을 사용하지만 사용안하는 슬롯은 비워둘 수 있음
                if (window.preLoadSlots.Length > i && window.preLoadSlots[i] == null) continue;
                
                GameObject slotObj = window.preLoadSlots.Length > i ? window.preLoadSlots[i] : null;
                if (slotObj == null)
                {
                    if (container == null)
                    {
                        GcLogger.LogError("아이콘을 담을 Container Icon 항목을 설정해주세요.");
                        return;
                    }
                    slotObj = Object.Instantiate(slotPrefab, container.transform);
                }
                UISlot uiSlot = slotObj.GetComponent<UISlot>();
                uiSlot.Initialize(window, window.uid, i, slotSize);
                slots[i] = slotObj;

                GameObject iconObj = window.preLoadIcons.Length > i ? window.preLoadIcons[i] : null;
                if (iconObj == null)
                {
                    iconObj = Object.Instantiate(iconPrefab, slotObj.transform);    
                }
                UIIcon uiIcon = iconObj.GetComponent<UIIcon>();
                uiIcon.Initialize(window, window.uid, i, i, iconSize, slotSize);
                icons[i] = iconObj;

                window.ApplyDefaultSlotIconActiveState(slotObj, iconObj);
            }
        }
    }
}
