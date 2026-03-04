using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용으로 "일반 최대 HP"를 증가(저장/로드 + 스탯 Provider 반영).
    /// </summary>
    public sealed class ItemUseActionAddMaxHpNormal : IItemUseAction
    {
        private readonly long _amount;

        public ItemUseActionAddMaxHpNormal(int amount)
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
            ctx.Player.AddItemBonusMaxHpNormal(_amount);
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
