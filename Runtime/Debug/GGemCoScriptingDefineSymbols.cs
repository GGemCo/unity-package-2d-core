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
        /// Release Simulation에서는 반복 테스트 시간을 줄이기 위해 심볼을 자동 제거하지 않고,
        /// 실제 Release 빌드 준비 및 릴리즈 검증 단계에서 제거 또는 차단합니다.
        /// </summary>
        public const string EnableCheatTools = "GGEMCO_ENABLE_CHEAT_TOOLS";
    }
}
