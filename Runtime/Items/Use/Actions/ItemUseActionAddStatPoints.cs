namespace GGemCo2DCore
{
    public sealed class ItemUseActionAddStatPoints : IItemUseAction
    {
        private readonly int _points;

        public ItemUseActionAddStatPoints(int points)
        {
            _points = points;
        }

        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.PlayerData == null) return ResultCommon.Fail("ItemUse_NoPlayerData");
            if (_points <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");
            return ResultCommon.SuccessWithIcons(null);
        }

        public ResultCommon Execute(ItemUseContext ctx)
        {
            ctx.PlayerData.UnspentStatPoints += _points;
            return ResultCommon.SuccessWithIcons(null);
        }
    }
}
