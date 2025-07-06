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
            
            /// <summary>
            /// 모든 테이블 이름을 배열로 제공합니다.
            /// </summary>
            public static readonly string[] All = new[]
            {
                Common,
                System,
                Scene,
                // Inventory,
                // Dialogue
            };
        }

        /// <summary>
        /// Localization Key 값 정의
        /// </summary>
        public static class Keys
        {
            private const string NameButton = "Button";
            private const string NameText = "Text";
            public static class Intro
            {
                private const string NameIntro = nameof(Intro);
                public static string ButtonNewGame() => $"{NameIntro}_{NameButton}_NewGame";
                public static string ButtonContinue() => $"{NameIntro}_{NameButton}_Continue";
                public static string ButtonLoad() => $"{NameIntro}_{NameButton}_Load";
            }
            public static class Loading
            {
                private const string NameLoading = nameof(Loading);
                public static string TextTypeTables() => $"{NameLoading}_{NameText}_Tables";
                public static string TextTypePrefab() => $"{NameLoading}_{NameText}_Resources";
                public static string TextTypeSaveData() => $"{NameLoading}_{NameText}_SaveData";
                public static string TextTypeEffect() => $"{NameLoading}_{NameText}_Effect";
                public static string TextTypeItem() => $"{NameLoading}_{NameText}_Item";
                public static string TextTypeSkill() => $"{NameLoading}_{NameText}_Skill";
                public static string TextLoadingPercent() => $"{NameLoading}_{NameText}_LoadingPercent";
            }

            // 기타 UI, 시스템 메시지, 팝업 등 계속 확장 가능
        }
    }
}