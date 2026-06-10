namespace GGemCo2DCore
{
    /// <summary>
    /// MP 회복 아이템 사용 액션입니다.
    /// </summary>
    /// <remarks>
    /// <para>기본 동작은 <see cref="Player.AddMp"/>와 <see cref="Player.IsMaxMp"/>를 사용합니다.</para>
    /// <para>게임별 MP 상한 규칙이 필요한 경우 <see cref="IItemUseMpReceiver"/> 구현체를 우선 사용합니다.</para>
    /// </remarks>
    public sealed class ItemUseActionAddMp : IItemUseAction
    {
        private readonly int _amount;

        public ItemUseActionAddMp(int amount)
        {
            _amount = amount;
        }

        /// <summary>
        /// MP 회복 액션을 실행할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>실행 가능 여부와 실패 메시지 키입니다.</returns>
        public ResultCommon CanExecute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            IItemUseMpReceiver mpReceiver = ctx.MpReceiver;
            if (mpReceiver != null)
            {
                // 게임별 MP 상한 규칙이 있으면 Core의 MaxMp 기준 대신 수신자 정책을 우선 적용합니다.
                return mpReceiver.CanAddMp(_amount)
                    ? ResultCommon.Success()
                    : ResultCommon.Fail("Item_ManaFull");
            }

            if (ctx.Player.IsMaxMp())
            {
                return ResultCommon.Fail("Item_ManaFull");
            }

            return ResultCommon.Success();
        }

        /// <summary>
        /// MP 회복 액션을 실행합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>실행 결과입니다.</returns>
        public ResultCommon Execute(ItemUseContext ctx)
        {
            if (ctx?.Player == null) return ResultCommon.Fail("ItemUse_NoPlayer");
            if (_amount <= 0) return ResultCommon.Fail("ItemUse_InvalidValue");

            IItemUseMpReceiver mpReceiver = ctx.MpReceiver;
            if (mpReceiver != null)
            {
                // CanExecute와 Execute 사이에 MP가 변경될 수 있으므로 실제 실행 시에도 성공 여부를 확인합니다.
                return mpReceiver.TryAddMp(_amount)
                    ? ResultCommon.Success()
                    : ResultCommon.Fail("Item_ManaFull");
            }

            ctx.Player.AddMp(_amount);
            return ResultCommon.Success();
        }
    }
}
