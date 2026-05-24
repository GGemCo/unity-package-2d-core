namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 단위 자동 이동 사용 정책입니다.
    /// </summary>
    public enum MapAutoMovePolicy
    {
        /// <summary>전역 설정(<c>GGemCoSettings.enableAutoMove</c>) 값을 그대로 따릅니다.</summary>
        Inherit,

        /// <summary>현재 맵에서는 자동 이동을 명시적으로 허용합니다.</summary>
        Enabled,

        /// <summary>현재 맵에서는 자동 이동을 명시적으로 금지합니다.</summary>
        Disabled
    }
}
