namespace GGemCo2DCore
{
    /// <summary>
    /// ScriptableObject 디버그 옵션의 최종 런타임 값을 계산하는 유틸리티입니다.
    /// raw 값이 true 여도 릴리즈 빌드에서는 항상 false 를 반환합니다.
    /// </summary>
    public static class DebugOptionRuntimeUtility
    {
        /// <summary>
        /// 원본 디버그 옵션 값을 현재 빌드 상태에 맞는 최종 값으로 변환합니다.
        /// </summary>
        /// <param name="rawValue">ScriptableObject 에 저장된 원본 bool 값입니다.</param>
        /// <returns>디버그 기능이 허용되는 환경이면 원본 값을, 아니면 false 를 반환합니다.</returns>
        public static bool Resolve(bool rawValue)
        {
            return rawValue && GGemCoBuildFlags.AllowDebugFeatures;
        }
    }
}
