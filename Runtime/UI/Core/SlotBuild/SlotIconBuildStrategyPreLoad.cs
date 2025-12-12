using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class SlotIconBuildStrategyPreLoad : ISlotIconBuildStrategy
    {
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount,
            IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (AddressableLoaderSettings.Instance == null) return;
            if (AddressableLoaderPrefabCommon.Instance == null) return;
            if (maxCount <= 0) return;
            
            GameObject iconPrefab = window.iconPrefab != null ? window.iconPrefab : IconConstants.LoadByIconType(iconType);
            
            if (iconPrefab == null) return;
            for (int i = 0; i < maxCount; i++)
            {
                if (i >= window.preLoadSlots.Length) continue;
                GameObject slotObject = window.preLoadSlots[i];
                if (slotObject == null) continue;
                
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                uiSlot.Initialize(window, window.uid, i, slotSize);
                slots[i] = slotObject;
                
                GameObject iconObj = Object.Instantiate(iconPrefab, slotObject.transform);
                UIIcon uiIcon = iconObj.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                uiIcon.Initialize(window, window.uid, i, i, iconSize, slotSize);
                icons[i] = iconObj;
            }
        }
    }
}