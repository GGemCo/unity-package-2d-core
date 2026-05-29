#if UNITY_EDITOR
namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 디버그 옵션 검색 범위를 정의합니다.
    /// </summary>
    public enum DebugOptionScanScope
    {
        /// <summary>
        /// 프로젝트에 존재하는 모든 ScriptableObject 에셋을 검사합니다.
        /// 작업자별 개발용 Settings도 포함됩니다.
        /// </summary>
        AllProjectAssets = 0,

        /// <summary>
        /// 릴리즈 빌드 검증에 사용할 후보만 검사합니다.
        /// 작업자별 개발용 Settings와 Editor 전용 경로는 제외합니다.
        /// </summary>
        ReleaseBuildCandidates = 1
    }
}
#endif
