using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용으로 임시 HP를 증가하거나 설정된 목표치까지 충전합니다.
    /// 저장 데이터와 스탯 Provider를 함께 갱신합니다.
    /// </summary>
    public sealed class ItemUseActionAddMaxHpTemp : IItemUseAction, IItemUseActionAvailabilityRule
    {
        private readonly long _amount;
        private readonly ItemTempHpApplyPolicy _applyPolicy;

        /// <summary>
        /// 임시 HP 아이템 사용 액션을 생성합니다.
        /// </summary>
        /// <param name="amount">누적 증가량 또는 충전 목표값입니다.</param>
        /// <param name="applyPolicy">임시 HP 적용 정책입니다.</param>
        public ItemUseActionAddMaxHpTemp(int amount, ItemTempHpApplyPolicy applyPolicy)
        {
            _amount = Mathf.Max(0, amount);
            _applyPolicy = applyPolicy;
        }

        /// <inheritdoc />
        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            if (!IsAvailable(ctx, out string disabledReason))
            {
                return ResultCommon.Fail(disabledReason);
            }

            return ResultCommon.Success();
        }

        /// <inheritdoc />
        public ResultCommon Execute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            switch (_applyPolicy)
            {
                case ItemTempHpApplyPolicy.Add:
                    ctx.Player.AddItemBonusMaxHpTemp(_amount, fillCurrent: true);
                    return ResultCommon.Success();

                case ItemTempHpApplyPolicy.RefillToTarget:
                    return ctx.Player.RefillItemBonusHpTempTo(_amount)
                        ? ResultCommon.Success()
                        : ResultCommon.Fail("ItemUse_CannotExecute");

                default:
                    return ResultCommon.Fail("ItemUse_InvalidValue");
            }
        }

        /// <inheritdoc />
        public bool IsAvailable(ItemUseContext ctx, out string disabledReason)
        {
            disabledReason = null;

            if (ctx?.Player == null || _amount <= 0)
            {
                disabledReason = "ItemUse_CannotExecute";
                return false;
            }

            if (_applyPolicy == ItemTempHpApplyPolicy.RefillToTarget &&
                ctx.Player.GetItemBonusHpTempCurrent() >= _amount)
            {
                disabledReason = "ItemUse_CannotExecute";
                return false;
            }

            return true;
        }
    }
}
