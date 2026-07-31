namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용 시 사전 검증과 실제 효과 적용을 수행하는 액션 계약입니다.
    /// </summary>
    public interface IItemUseAction
    {
        /// <summary>
        /// 현재 컨텍스트에서 액션을 실행할 수 있는지 검사합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>실행 가능 여부입니다.</returns>
        ResultCommon CanExecute(ItemUseContext ctx);

        /// <summary>
        /// 아이템 사용 효과를 적용합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <returns>효과 적용 결과입니다.</returns>
        ResultCommon Execute(ItemUseContext ctx);
    }

    /// <summary>
    /// 상점처럼 상태를 반복 조회하는 UI에서 메시지 출력 없이 액션 구매 가능 여부를 검사하는 계약입니다.
    /// 상태에 따라 사용 가능 여부가 달라지는 액션만 선택적으로 구현합니다.
    /// </summary>
    public interface IItemUseActionAvailabilityRule
    {
        /// <summary>
        /// 현재 컨텍스트에서 액션을 적용할 실질적인 이득이 있는지 검사합니다.
        /// </summary>
        /// <param name="ctx">아이템 사용 컨텍스트입니다.</param>
        /// <param name="disabledReason">적용할 수 없을 때 표시할 시스템 메시지 키입니다.</param>
        /// <returns>액션을 적용할 수 있으면 true입니다.</returns>
        bool IsAvailable(ItemUseContext ctx, out string disabledReason);
    }
}
