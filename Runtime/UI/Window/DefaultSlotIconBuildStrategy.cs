﻿using UnityEngine;
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
            if (maxCount > 0 && container == null)
            {
                GcLogger.LogError("아이콘을 담을 Container Icon 항목을 설정해주세요.");
                return;
            }
            if (AddressableLoaderPrefabCommon.Instance == null) return;
            GameObject iconPrefab = iconType == IconConstants.Type.Skill
                ? ConfigResources.IconSkill.Load()
                : ConfigResources.IconItem.Load();
            GameObject slotPrefab = ConfigResources.Slot.Load();

            for (int i = 0; i < maxCount; i++)
            {
                GameObject slotObj = Object.Instantiate(slotPrefab, container.transform);
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