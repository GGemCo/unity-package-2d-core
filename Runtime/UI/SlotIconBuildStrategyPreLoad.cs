using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    public class SlotIconBuildStrategyPreLoad : ISlotIconBuildStrategy
    {
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount,
            IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (AddressableSettingsLoader.Instance == null) return;
            if (maxCount <= 0) return;
            GameObject iconItem = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconItem);
            if (iconItem == null) return;
            for (int i = 0; i < maxCount; i++)
            {
                GameObject slotObject = window.preLoadSlots[i];
                if (slotObject == null) continue;
                
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                uiSlot.Initialize(window, window.uid, i, slotSize);
                slots[i] = slotObject;
                
                GameObject icon = Object.Instantiate(iconItem, slotObject.transform);
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                uiIcon.Initialize(window, window.uid, i, i, iconSize, slotSize);
                icons[i] = icon;
            }
        }
    }
}