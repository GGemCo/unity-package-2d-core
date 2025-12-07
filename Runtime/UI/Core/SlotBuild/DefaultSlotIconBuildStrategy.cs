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
            if (maxCount > 0 && container == null)
            {
                GcLogger.LogError("아이콘을 담을 Container Icon 항목을 설정해주세요.");
                return;
            }
            if (AddressableLoaderPrefabCommon.Instance == null) return;
            GameObject iconPrefab = window.iconPrefab != null ? window.iconPrefab : IconConstants.LoadByIconType(iconType);
            GameObject slotPrefab = window.slotPrefab != null ? window.slotPrefab : ConfigResources.Slot.Load();

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
                
                // container 의 Cell Size 와 아이콘의 Width/Height 를 비교하여 아이콘 스케일 조정
                // 슬롯은 Grid Layout 으로 인해, Cell Size 값으로 Width, Height 값이 자동으로 적용됨
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                Vector2 cellSize = container.cellSize;
                Vector2 iconRectSize = iconRect.sizeDelta;

                if (iconRectSize.x > 0f && iconRectSize.y > 0f)
                {
                    float scaleX = cellSize.x / iconRectSize.x;
                    float scaleY = cellSize.y / iconRectSize.y;

                    // 비율 왜곡을 막기 위해 가장 작은 값으로 균일 스케일 적용
                    float uniformScale = Mathf.Min(scaleX, scaleY);
                    iconObj.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                }
            }
        }
    }
}