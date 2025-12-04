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
                
                // container 의 Cell Size 와 슬롯의 Width/Height 를 비교하여 슬롯 스케일 조정
                RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                Vector2 cellSize = container.cellSize;
                Vector2 slotRectSize = slotRect.sizeDelta;

                if (slotRectSize.x > 0f && slotRectSize.y > 0f)
                {
                    float scaleX = cellSize.x / slotRectSize.x;
                    float scaleY = cellSize.y / slotRectSize.y;

                    // 비율 왜곡을 막기 위해 가장 작은 값으로 균일 스케일 적용
                    float uniformScale = Mathf.Min(scaleX, scaleY);
                    slotObj.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                }
            }
        }
    }
}