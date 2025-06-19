using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 스킬 윈도우 - 아이콘 생성
    /// </summary>
    public class SlotIconBuildStrategySkill : ISlotIconBuildStrategy
    {
        public void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount,
            IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons)
        {
            if (AddressableSettingsLoader.Instance == null || window.containerIcon == null) return;
            UIWindowSkill uiWindowSkill = window as UIWindowSkill;
            if (uiWindowSkill == null) return;
            GameObject prefabUIElementSkill = uiWindowSkill.prefabUIElementSkill;
            if (prefabUIElementSkill == null)
            {
                GcLogger.LogError("UIElementSkill 프리팹이 없습니다.");
                return;
            }
            var datas = uiWindowSkill.TableSkill.GetSkills();
            uiWindowSkill.maxCountIcon = datas.Count;
            if (datas.Count <= 0) return;
            
            GameObject iconSkill = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconSkill);
            GameObject slot = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabSlot);
            if (iconSkill == null) return;

            int index = 0;
            foreach (var data in datas)
            {
                int skillUid = data.Key;
                if (skillUid <= 0) continue;
                var info = data.Value;

                GameObject parent = uiWindowSkill.gameObject;
                // UI Element 프리팹이 있으면 만든다.
                if (prefabUIElementSkill != null)
                {
                    parent = Object.Instantiate(prefabUIElementSkill, uiWindowSkill.containerIcon.gameObject.transform);
                    if (parent == null) continue;
                    UIElementSkill uiElementSkill = parent.GetComponent<UIElementSkill>();
                    if (uiElementSkill == null) continue;
                    uiElementSkill.Initialize(uiWindowSkill, index, info);
                    uiWindowSkill.UIElementSkills.TryAdd(index, uiElementSkill);
                }

                GameObject slotObject = Object.Instantiate(slot, parent.transform);
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                uiSlot.Initialize(uiWindowSkill, uiWindowSkill.uid, index, slotSize);
                uiWindowSkill.SetPositionUiSlot(uiSlot, index);
                slots[index] = slotObject;
                
                GameObject icon = Object.Instantiate(iconSkill, slotObject.transform);
                UIIconSkill uiIcon = icon.GetComponent<UIIconSkill>();
                if (uiIcon == null) continue;
                uiIcon.Initialize(uiWindowSkill, uiWindowSkill.uid, index, index, iconSize, slotSize);
                // count, 레벨 1로 초기화
                uiIcon.ChangeInfoByUid(skillUid, 1, 1);
                
                icons[index] = icon;
                index++;
            }
        }
    }
}