namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 입장 규칙 시스템에서 사용하는 상수와 타입을 정의합니다.
    /// </summary>
    public static class MapEntryRuleConstants
    {
        /// <summary>
        /// 라이센스 값을 비교하는 방식을 나타냅니다.
        /// </summary>
        public enum CompareType
        {
            Exists,
            NotExists,
            Equals,
            NotEquals,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
        }
    }
}
