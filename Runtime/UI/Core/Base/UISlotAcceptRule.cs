using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 슬롯 수용 규칙의 적용 방식을 정의합니다.
    /// </summary>
    public enum UISlotAcceptMode
    {
        Inherit,
        AllowAll,
        DenyAll,
        Rule
    }

    /// <summary>
    /// 슬롯이 허용할 아이콘/아이템 조건을 정의합니다.
    /// 배열이 비어 있으면 해당 조건은 제한하지 않습니다.
    /// </summary>
    [Serializable]
    public class UISlotAcceptRule
    {
        [Tooltip("슬롯 수용 규칙의 적용 방식입니다.")]
        public UISlotAcceptMode mode = UISlotAcceptMode.Inherit;

        [Tooltip("허용할 아이콘 타입 목록입니다. 비어 있으면 제한하지 않습니다.")]
        public IconConstants.Type[] allowedIconTypes;

        [Tooltip("허용할 아이템 타입 목록입니다. 비어 있으면 제한하지 않습니다.")]
        public ItemConstants.Type[] allowedItemTypes;

        [Tooltip("허용할 아이템 카테고리 목록입니다. 비어 있으면 제한하지 않습니다.")]
        public ItemConstants.Category[] allowedItemCategories;

        [Tooltip("허용할 아이템 서브 카테고리 목록입니다. 비어 있으면 제한하지 않습니다.")]
        public ItemConstants.SubCategory[] allowedItemSubCategories;

        [Tooltip("허용할 장비 부위 목록입니다. 비어 있으면 제한하지 않습니다.")]
        public ItemConstants.PartsType[] allowedPartsTypes;

        [Tooltip("규칙에 맞지 않을 때 노출할 시스템 메시지 키입니다.")]
        public string failMessageKey = "Slot_ItemNotAllowed";
    }

    /// <summary>
    /// 특정 슬롯 index 에만 적용할 규칙 오버라이드입니다.
    /// </summary>
    [Serializable]
    public class UISlotAcceptRuleOverride
    {
        [Tooltip("규칙을 덮어쓸 슬롯 index 입니다.")]
        public int slotIndex;

        [Tooltip("해당 슬롯에만 적용할 규칙입니다.")]
        public UISlotAcceptRule rule = new UISlotAcceptRule();
    }

    /// <summary>
    /// 슬롯 수용 규칙과 아이콘 메타데이터를 비교해 드롭 가능 여부를 판정합니다.
    /// </summary>
    public static class UISlotAcceptRuleEvaluator
    {
        public static bool CanAccept(UISlotAcceptRule rule, UIIcon icon, out string failMessageKey)
        {
            failMessageKey = rule?.failMessageKey;

            if (icon == null || icon.uid <= 0)
                return false;

            if (rule == null)
                return true;

            switch (rule.mode)
            {
                case UISlotAcceptMode.AllowAll:
                    return true;

                case UISlotAcceptMode.DenyAll:
                    return false;

                case UISlotAcceptMode.Inherit:
                    return true;

                case UISlotAcceptMode.Rule:
                    return MatchesRule(rule, icon);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 규칙의 각 필드를 순서대로 검사합니다.
        /// 하나라도 불일치하면 해당 슬롯에는 배치할 수 없습니다.
        /// </summary>
        private static bool MatchesRule(UISlotAcceptRule rule, UIIcon icon)
        {
            if (!ContainsOrEmpty(rule.allowedIconTypes, icon.GetIconType()))
                return false;

            if (icon.GetIconType() != IconConstants.Type.Item)
                return true;

            if (!ContainsOrEmpty(rule.allowedItemTypes, icon.GetItemType()))
                return false;

            if (!ContainsOrEmpty(rule.allowedItemCategories, icon.GetItemCategory()))
                return false;

            if (!ContainsOrEmpty(rule.allowedItemSubCategories, icon.GetItemSubCategory()))
                return false;

            if (!ContainsOrEmpty(rule.allowedPartsTypes, icon.GetItemPartsType()))
                return false;

            return true;
        }

        /// <summary>
        /// 배열이 비어 있으면 제한 없음으로 처리하고,
        /// 값이 있으면 그 안에 target 이 포함되어야만 통과시킵니다.
        /// </summary>
        private static bool ContainsOrEmpty<T>(T[] values, T target)
        {
            if (values == null || values.Length == 0)
                return true;

            for (int i = 0; i < values.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(values[i], target))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 실제 배치/교체 전에 윈도우 규칙을 검사하는 공용 헬퍼입니다.
    /// </summary>
    public static class UISlotPlacementValidator
    {
        public static bool CanPlace(UIWindow window, UIIcon icon, int slotIndex, out string failMessageKey)
        {
            failMessageKey = null;
            return window != null && window.CanAcceptIcon(icon, slotIndex, out failMessageKey);
        }

        public static bool CanSwap(UIIcon dropped, UIIcon target, out string failMessageKey)
        {
            failMessageKey = null;

            if (dropped == null || target == null)
                return false;

            if (ReferenceEquals(dropped, target))
                return true;

            // 1) 드롭 대상 슬롯이 dropped 아이콘을 받을 수 있어야 합니다.
            if (!CanPlace(target.window, dropped, target.slotIndex, out failMessageKey))
                return false;

            // 2) 교체가 발생한다면, 원래 자리도 target 아이콘을 받아야 합니다.
            if (target.uid > 0 && !CanPlace(dropped.window, target, dropped.slotIndex, out failMessageKey))
                return false;

            return true;
        }
    }
}
