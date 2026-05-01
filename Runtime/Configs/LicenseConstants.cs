namespace GGemCo2DCore
{
    /// <summary>
    /// 라이센스 시스템에서 공통으로 사용하는 상수와 타입을 정의합니다.
    /// </summary>
    public static class LicenseConstants
    {
        /// <summary>
        /// 라이센스 값의 해석 방식을 나타냅니다.
        /// </summary>
        public enum ValueType
        {
            String,
            Bool,
            Int,
            Float,
        }

        /// <summary>
        /// Bool 라이센스를 획득 상태로 저장할 때 사용하는 기본 문자열 값입니다.
        /// </summary>
        public const string TrueValue = "true";

        /// <summary>
        /// Bool 라이센스를 미획득 상태로 저장할 때 사용하는 기본 문자열 값입니다.
        /// </summary>
        public const string FalseValue = "false";
    }
}
