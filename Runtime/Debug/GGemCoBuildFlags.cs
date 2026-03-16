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
        /// 디버그 기능 허용 여부입니다.
        /// 에디터에서는 항상 true, 플레이어에서는 Development Build 인 경우에만 true 입니다.
        /// </summary>
        public static bool AllowDebugFeatures
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return Debug.isDebugBuild;
#endif
            }
        }

        /// <summary>
        /// 릴리즈 빌드인지 여부입니다.
        /// 에디터에서는 항상 false 이며, 플레이어에서는 Development Build 가 아닌 경우 true 입니다.
        /// </summary>
        public static bool IsReleasePlayerBuild => Application.isPlaying && !AllowDebugFeatures;
    }
}
