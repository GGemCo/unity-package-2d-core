using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 실행 환경에서 디버그 기능 허용 여부를 판정하는 공용 게이트입니다.
    /// 모든 런타임 디버그 기능은 ScriptableObject의 원본 값을 직접 사용하지 말고,
    /// 반드시 이 클래스 또는 <see cref="DebugOptionRuntimeUtility"/>를 통해 최종 값을 판정해야 합니다.
    /// </summary>
    public static class GGemCoBuildFlags
    {
        /// <summary>
        /// 현재 런타임이 사용할 빌드/테스트 모드를 반환합니다.
        /// 에디터에서는 <see cref="BuildModeOverrideRegistry"/>에 등록된 공급자를 우선 사용하고,
        /// 플레이어에서는 Unity의 Development Build 심볼을 기준으로 판정합니다.
        /// </summary>
        public static GGemCoBuildMode CurrentMode
        {
            get
            {
#if UNITY_EDITOR
                if (BuildModeOverrideRegistry.TryGetMode(out GGemCoBuildMode editorMode))
                    return editorMode;

                return GGemCoBuildMode.Development;
#elif DEVELOPMENT_BUILD
                return GGemCoBuildMode.Development;
#else
                return GGemCoBuildMode.Release;
#endif
            }
        }

        /// <summary>
        /// 디버그 기능 허용 여부입니다.
        /// Development 모드에서만 true이며, ReleaseSimulation과 Release에서는 false입니다.
        /// </summary>
        public static bool AllowDebugFeatures => CurrentMode == GGemCoBuildMode.Development;

        /// <summary>
        /// 현재 모드가 릴리즈와 같은 제약을 적용해야 하는지 여부입니다.
        /// 에디터 ReleaseSimulation과 실제 Release 빌드에서 true입니다.
        /// </summary>
        public static bool IsReleaseLike =>
            CurrentMode == GGemCoBuildMode.ReleaseSimulation ||
            CurrentMode == GGemCoBuildMode.Release;

        /// <summary>
        /// 실제 플레이어 빌드가 릴리즈 모드로 실행 중인지 여부입니다.
        /// 에디터 ReleaseSimulation은 포함하지 않습니다.
        /// </summary>
        public static bool IsReleasePlayerBuild
        {
            get
            {
#if UNITY_EDITOR
                return false;
#else
                return Application.isPlaying && CurrentMode == GGemCoBuildMode.Release;
#endif
            }
        }
    }
}
