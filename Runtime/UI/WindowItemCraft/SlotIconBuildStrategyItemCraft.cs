using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 제작 윈도우 - 아이콘 생성
    /// </summary>
    public class SlotIconBuildStrategyItemCraft : ISlotIconBuildStrategy
    {
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount,
            IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (AddressableSettingsLoader.Instance == null || window.containerIcon == null) return;
            UIWindowItemCraft uiWindowItemCraft = window as UIWindowItemCraft;
            if (uiWindowItemCraft == null) return;
            GameObject prefabUIElementSkill = uiWindowItemCraft.prefabUIElementCraft;
            if (prefabUIElementSkill == null)
            {
                GcLogger.LogError("UIElementSkill 프리팹이 없습니다.");
                return;
            }
            var datas = uiWindowItemCraft.TableItemCraft.GetDatas();
            if (datas.Count <= 0) return;
            uiWindowItemCraft.maxCountIcon = datas.Count;
            slots = new GameObject[uiWindowItemCraft.maxCountIcon];
            icons = new GameObject[uiWindowItemCraft.maxCountIcon];
            
            GameObject iconItem = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconItem);
            GameObject slot = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabSlot);
            if (iconItem == null) return;

            int index = 0;
            foreach (var data in datas)
            {
                int craftUid = data.Key;
                if (craftUid <= 0) continue;
                var info = uiWindowItemCraft.TableItemCraft.GetDataByUid(craftUid);
                if (info == null)
                {
                    GcLogger.LogError("item_craft 테이블에 정보가 없습니다. craft uid : " + craftUid);
                    continue;
                }

                GameObject parent = uiWindowItemCraft.gameObject;
                // UI Element 프리팹이 있으면 만든다.
                if (prefabUIElementSkill != null)
                {
                    parent = Object.Instantiate(prefabUIElementSkill, uiWindowItemCraft.containerIcon.gameObject.transform);
                    if (parent == null) continue;
                    UIElementItemCraft uiElementItemCraft = parent.GetComponent<UIElementItemCraft>();
                    if (uiElementItemCraft == null) continue;
                    uiElementItemCraft.Initialize(uiWindowItemCraft, index, info);
                    uiWindowItemCraft.UIElementItemCrafts.TryAdd(index, uiElementItemCraft);
                }

                GameObject slotObject = Object.Instantiate(slot, parent.transform);
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                uiSlot.Initialize(uiWindowItemCraft, uiWindowItemCraft.uid, index, slotSize);
                uiWindowItemCraft.SetPositionUiSlot(uiSlot, index);
                slots[index] = slotObject;
                
                GameObject icon = Object.Instantiate(iconItem, slotObject.transform);
                UIIconItem uiIcon = icon.GetComponent<UIIconItem>();
                if (uiIcon == null) continue;
                uiIcon.Initialize(uiWindowItemCraft, uiWindowItemCraft.uid, index, index, iconSize, slotSize);
                // count, 레벨 1로 초기화
                uiIcon.ChangeInfoByUid(info.ResultItemUid, 1);
                // element 에서 마우스 이벤트 처리
                uiIcon.SetRaycastTarget(false);
                uiIcon.RemoveLockImage();
                
                icons[index] = icon;
                index++;
            }
        }
    }
}