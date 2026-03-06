namespace GGemCo2DCore
{
    /// <summary>
    /// HP 회복
    /// - Player.AddHp 사용
    /// - 이미 최대 HP면 실패 처리(기존 Potion 정책과 동일)
    /// </summary>
    public sealed class ItemUseActionAddHp : IItemUseAction
    {
        private readonly int _amount;

        public ItemUseActionAddHp(int amount)
        {
            _amount = amount;
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            // Player에 IsMaxHp가 없을 수도 있으므로 안전 호출
            if (ctx.Player.IsMaxHp())
            {
                return ResultCommon.Fail("Item_HealthFull");
            }

            return ResultCommon.Success();
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            ctx.Player.AddHp(_amount);
            return ResultCommon.Success();
        }
    }
}
