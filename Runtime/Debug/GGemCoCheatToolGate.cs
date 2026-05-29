namespace GGemCo2DCore
{
    /// <summary>
    /// 치트 도구 코드와 UI의 최종 사용 가능 여부를 판정하는 공용 런타임 게이트입니다.
    /// 컴파일 심볼은 코드 포함 여부만 결정하고, 실제 실행 가능 여부는 현재 빌드 모드의 디버그 기능 허용 상태까지 함께 확인합니다.
    /// </summary>
    public static class GGemCoCheatToolGate
    {
        /// <summary>
        /// 현재 실행 환경에서 치트 도구를 사용할 수 있는지 여부입니다.
        /// <see cref="GGemCoScriptingDefineSymbols.EnableCheatTools"/> 심볼이 없으면 항상 false이며,
        /// 심볼이 있어도 Development 모드가 아니면 false입니다.
        /// </summary>
        public static bool CanUseCheatTools
        {
            get
            {
#if GGEMCO_ENABLE_CHEAT_TOOLS
                return GGemCoBuildFlags.AllowDebugFeatures;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 사용자가 요청한 치트 UI 표시 상태와 현재 빌드 모드를 함께 고려하여 실제 표시 가능 여부를 반환합니다.
        /// Release Simulation에서는 심볼이 남아 있어도 <see cref="GGemCoBuildFlags.AllowDebugFeatures"/>가 false이므로 치트 UI가 표시되지 않습니다.
        /// </summary>
        /// <param name="requestedVisible">설정 또는 입력에서 요청한 치트 UI 표시 여부입니다.</param>
        /// <returns>치트 UI를 실제로 표시할 수 있으면 true입니다.</returns>
        public static bool CanShowCheatUi(bool requestedVisible)
        {
            return requestedVisible && CanUseCheatTools;
        }
    }
}
