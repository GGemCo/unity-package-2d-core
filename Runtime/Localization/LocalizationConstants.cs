namespace GGemCo2DCore
{
    public static class LocalizationConstants
    {
        /// <summary>
        /// 지원 언어 목록 (확장 가능)
        /// </summary>
        public enum LanguageIndex
        {
            En, // English
            Ko, // Korean
            // Ja, // Japanese
            // Zh, // Chinese
        }

        /// <summary>
        /// 기본 언어
        /// </summary>
        public static readonly LanguageIndex DefaultLanguageIndex = LanguageIndex.En;

        /// <summary>
        /// Localization Table 이름 정의
        /// </summary>
        public static class Tables
        {
            public const string Common = "GGemCo_Common";
            public const string System = "GGemCo_System";
            public const string Scene = "GGemCo_Scene";

            // public const string Inventory = "GGemCo_Inventory";
            // public const string Dialogue = "GGemCo_Dialogue";
        }

        /// <summary>
        /// Localization Key 값 정의
        /// </summary>
        public static class Keys
        {
            public static class Intro
            {
                public const string ButtonNewGame = "Intro_Button_NewGame";
                public const string ButtonContinue = "Intro_Button_Continue";
                public const string ButtonLoad = "Intro_Button_Load";
            }

            // 기타 UI, 시스템 메시지, 팝업 등 계속 확장 가능
        }
    }
}