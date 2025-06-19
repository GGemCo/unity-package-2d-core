using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 분해 윈도우 - 아이콘 생성
    /// </summary>
    public class SlotIconBuildStrategyItemSalvage : ISlotIconBuildStrategy
    {
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount,
            IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (maxCount > 0 && container == null)
            {
                GcLogger.LogError("아이콘을 담을 Container Icon 항목을 설정해주세요.");
                return;
            }
            if (AddressablePrefabLoader.Instance == null) return;
            GameObject iconPrefab = iconType == IconConstants.Type.Skill
                ? AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconSkill)
                : AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconItem);
            GameObject slotPrefab = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabSlot);

            UIWindowItemSalvage uiWindowItemSalvage = window as UIWindowItemSalvage;
            if (uiWindowItemSalvage == null) return;
            for (int i = 0; i < maxCount; i++)
            {
                Transform containerTransform = container.transform;
                if (i >= uiWindowItemSalvage.salvageIconCount)
                {
                    containerTransform = uiWindowItemSalvage.containerResult.transform;
                }
                GameObject slotObj = Object.Instantiate(slotPrefab, containerTransform);
                UISlot uiSlot = slotObj.GetComponent<UISlot>();
                uiSlot.Initialize(window, window.uid, i, slotSize);
                slots[i] = slotObj;

                GameObject iconObj = Object.Instantiate(iconPrefab, slotObj.transform);
                UIIcon uiIcon = iconObj.GetComponent<UIIcon>();
                uiIcon.Initialize(window, window.uid, i, i, iconSize, slotSize);
                icons[i] = iconObj;
            }
        }
    }
}