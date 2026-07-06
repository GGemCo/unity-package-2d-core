namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// GGemCo 커스텀 툴의 Unity 상단 메뉴 경로를 정의합니다.
    /// </summary>
    /// <remarks>
    /// 모든 패키지 Editor 메뉴는 이 클래스의 루트와 패키지별 경로를 조합해
    /// GGemCoTool/{Package}/{Category}/{Tool} 구조를 유지합니다.
    /// </remarks>
    public static class GGemCoToolMenu
    {
        /// <summary>
        /// 모든 GGemCo 커스텀 툴의 최상위 메뉴 루트입니다.
        /// </summary>
        public const string Root = "GGemCoTool/";

        /// <summary>
        /// Core 패키지 메뉴 루트입니다.
        /// </summary>
        public const string Core = Root + "Core/";

        /// <summary>
        /// Control 패키지 메뉴 루트입니다.
        /// </summary>
        public const string Control = Root + "Control/";

        /// <summary>
        /// Skill 패키지 메뉴 루트입니다.
        /// </summary>
        public const string Skill = Root + "Skill/";

        /// <summary>
        /// Affect 패키지 메뉴 루트입니다.
        /// </summary>
        public const string Affect = Root + "Affect/";

        /// <summary>
        /// AI BT 패키지 메뉴 루트입니다.
        /// </summary>
        public const string AiBt = Root + "AI BT/";

        /// <summary>
        /// Quest 패키지 메뉴 루트입니다.
        /// </summary>
        public const string Quest = Root + "Quest/";

        /// <summary>
        /// Tutorial 패키지 메뉴 루트입니다.
        /// </summary>
        public const string Tutorial = Root + "Tutorial/";

        /// <summary>
        /// 설정 도구 카테고리명입니다.
        /// </summary>
        public const string Settings = "설정하기/";

        /// <summary>
        /// 제작/개발 도구 카테고리명입니다.
        /// </summary>
        public const string Development = "개발툴/";

        /// <summary>
        /// 테스트 도구 카테고리명입니다.
        /// </summary>
        public const string Test = "테스트툴/";

        /// <summary>
        /// 디버그 도구 카테고리명입니다.
        /// </summary>
        public const string Debug = "디버그툴/";

        /// <summary>
        /// 기타 도구 카테고리명입니다.
        /// </summary>
        public const string Etc = "기타/";
    }
}
