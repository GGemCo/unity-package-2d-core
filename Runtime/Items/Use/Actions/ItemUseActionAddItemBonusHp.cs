using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// "소모형 추가 최대 HP(추가 하트)" 추가
    /// - ItemBonusHpCurrent의 유일한 증가 경로(회복/리젠은 Base HP만 회복)
    /// - 데미지를 먼저 흡수하고, 0이 되면 즉시 소멸
    /// </summary>
    public sealed class ItemUseActionAddItemBonusHp : IItemUseAction
    {
        private readonly long _amount;

        public ItemUseActionAddItemBonusHp(int amount)
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
            ctx.Player.AddItemBonusHp(_amount);
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
