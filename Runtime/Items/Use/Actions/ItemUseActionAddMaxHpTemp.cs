using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용으로 "임시 최대 HP"를 증가(저장/로드 + 스탯 Provider 반영).
    /// - 기본 동작: 증가분만큼 임시 HP(Current)도 함께 증가(즉시 체감).
    /// </summary>
    public sealed class ItemUseActionAddMaxHpTemp : IItemUseAction
    {
        private readonly long _amount;

        public ItemUseActionAddMaxHpTemp(int amount)
        {
            _amount = Mathf.Max(0, amount);
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");
            return ResultCommon.SuccessWithIcons(null);
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            ctx.Player.AddItemBonusMaxHpTemp(_amount, fillCurrent: true);
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
