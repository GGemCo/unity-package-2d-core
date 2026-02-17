namespace GGemCo2DCore
{
    public sealed class ItemUseActionAddExp : IItemUseAction
    {
        private readonly long _exp;

        public ItemUseActionAddExp(long exp)
        {
            _exp = exp;
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.PlayerData == null) return ResultCommon.Fail("ItemUse_NoPlayerData");
            if (_exp <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");
            return ResultCommon.SuccessWithIcons(null);
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            ctx.PlayerData.AddExp(_exp);
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
