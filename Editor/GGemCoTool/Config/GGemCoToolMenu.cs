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
        /// TestAutomation 패키지 메뉴 루트입니다.
        /// </summary>
        public const string TestAutomation = Root + "TestAutomation/";

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

    /// <summary>
    /// GGemCo 커스텀 툴의 Unity 메뉴 정렬 우선순위를 정의합니다.
    /// </summary>
    /// <remarks>
    /// 패키지 기준값과 카테고리 오프셋을 조합해
    /// 패키지별 메뉴 순서와 카테고리별 메뉴 순서를 한 곳에서 관리합니다.
    /// </remarks>
    public static class GGemCoToolMenuPriority
    {
        /// <summary>
        /// Core 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int Core = 1000;

        /// <summary>
        /// Control 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int Control = 2000;

        /// <summary>
        /// Skill 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int Skill = 3000;

        /// <summary>
        /// Affect 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int Affect = 4000;

        /// <summary>
        /// AI BT 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int AiBt = 5000;

        /// <summary>
        /// Quest 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int Quest = 6000;

        /// <summary>
        /// Tutorial 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int Tutorial = 7000;

        /// <summary>
        /// TestAutomation 패키지 메뉴의 기준 우선순위입니다.
        /// </summary>
        public const int TestAutomation = 8000;

        /// <summary>
        /// 설정하기 카테고리의 우선순위 오프셋입니다.
        /// </summary>
        public const int Settings = 0;

        /// <summary>
        /// 개발툴 카테고리의 우선순위 오프셋입니다.
        /// </summary>
        public const int Development = 100;

        /// <summary>
        /// 테스트툴 카테고리의 우선순위 오프셋입니다.
        /// </summary>
        public const int Test = 200;

        /// <summary>
        /// 디버그툴 카테고리의 우선순위 오프셋입니다.
        /// </summary>
        public const int Debug = 300;

        /// <summary>
        /// 기타 카테고리의 우선순위 오프셋입니다.
        /// </summary>
        public const int Etc = 900;

        /// <summary>
        /// Core 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int CoreSettings = Core + Settings;

        /// <summary>
        /// Core 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int CoreDevelopment = Core + Development;

        /// <summary>
        /// Core 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int CoreTest = Core + Test;

        /// <summary>
        /// Core 패키지 디버그툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int CoreDebug = Core + Debug;

        /// <summary>
        /// Core 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int CoreEtc = Core + Etc;

        /// <summary>
        /// Control 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int ControlSettings = Control + Settings;

        /// <summary>
        /// Control 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int ControlDevelopment = Control + Development;

        /// <summary>
        /// Control 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int ControlTest = Control + Test;

        /// <summary>
        /// Control 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int ControlEtc = Control + Etc;

        /// <summary>
        /// Skill 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int SkillSettings = Skill + Settings;

        /// <summary>
        /// Skill 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int SkillDevelopment = Skill + Development;

        /// <summary>
        /// Skill 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int SkillTest = Skill + Test;

        /// <summary>
        /// Skill 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int SkillEtc = Skill + Etc;

        /// <summary>
        /// Affect 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AffectSettings = Affect + Settings;

        /// <summary>
        /// Affect 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AffectDevelopment = Affect + Development;

        /// <summary>
        /// Affect 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AffectTest = Affect + Test;

        /// <summary>
        /// Affect 패키지 디버그툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AffectDebug = Affect + Debug;

        /// <summary>
        /// Affect 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AffectEtc = Affect + Etc;

        /// <summary>
        /// AI BT 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AiBtSettings = AiBt + Settings;

        /// <summary>
        /// AI BT 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AiBtDevelopment = AiBt + Development;

        /// <summary>
        /// AI BT 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AiBtTest = AiBt + Test;

        /// <summary>
        /// AI BT 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int AiBtEtc = AiBt + Etc;

        /// <summary>
        /// Quest 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int QuestSettings = Quest + Settings;

        /// <summary>
        /// Quest 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int QuestDevelopment = Quest + Development;

        /// <summary>
        /// Quest 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int QuestTest = Quest + Test;

        /// <summary>
        /// Quest 패키지 디버그툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int QuestDebug = Quest + Debug;

        /// <summary>
        /// Quest 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int QuestEtc = Quest + Etc;

        /// <summary>
        /// Tutorial 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TutorialSettings = Tutorial + Settings;

        /// <summary>
        /// Tutorial 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TutorialDevelopment = Tutorial + Development;

        /// <summary>
        /// Tutorial 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TutorialTest = Tutorial + Test;

        /// <summary>
        /// Tutorial 패키지 디버그툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TutorialDebug = Tutorial + Debug;

        /// <summary>
        /// Tutorial 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TutorialEtc = Tutorial + Etc;

        /// <summary>
        /// TestAutomation 패키지 설정하기 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TestAutomationSettings = TestAutomation + Settings;

        /// <summary>
        /// TestAutomation 패키지 개발툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TestAutomationDevelopment = TestAutomation + Development;

        /// <summary>
        /// TestAutomation 패키지 테스트툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TestAutomationTest = TestAutomation + Test;

        /// <summary>
        /// TestAutomation 패키지 디버그툴 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TestAutomationDebug = TestAutomation + Debug;

        /// <summary>
        /// TestAutomation 패키지 기타 카테고리의 기준 우선순위입니다.
        /// </summary>
        public const int TestAutomationEtc = TestAutomation + Etc;
    }
}
