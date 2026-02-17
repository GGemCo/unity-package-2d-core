namespace GGemCo2DCore
{
    /// <summary>
    /// MP 회복
    /// - Player.AddMp 사용
    /// - 이미 최대 MP면 실패 처리(기존 Potion 정책과 동일)
    /// </summary>
    public sealed class ItemUseActionAddMp : IItemUseAction
    {
        private readonly int _amount;

        public ItemUseActionAddMp(int amount)
        {
            _amount = amount;
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            if (ctx.Player.IsMaxMp())
            {
                return ResultCommon.Fail("Item_ManaFull");
            }

            return ResultCommon.SuccessWithIcons(null);
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            ctx.Player.AddMp(_amount);
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
