namespace GGemCo2DCore
{
    /// <summary>
    /// GGemCo 패키지에서 사용하는 커스텀 Scripting Define Symbol 이름을 모아둔 상수 모음입니다.
    /// 문자열 오타를 줄이고 Editor 검증기와 Runtime 코드가 동일한 심볼 이름을 참조하도록 합니다.
    /// </summary>
    public static class GGemCoScriptingDefineSymbols
    {
        /// <summary>
        /// 골드 추가, 레벨업, 데이터 초기화 같은 개발/QA 치트 도구 코드를 컴파일에 포함할 때 사용하는 심볼입니다.
        /// Release Simulation과 Release 모드에서는 Build Profile 도구와 릴리즈 검증기가 이 심볼을 제거하거나 빌드를 차단합니다.
        /// </summary>
        public const string EnableCheatTools = "GGEMCO_ENABLE_CHEAT_TOOLS";
    }
}
