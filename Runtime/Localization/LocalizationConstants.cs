using System.Collections.Generic;

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

        public static readonly Dictionary<LanguageIndex, string> LanguageNames = new Dictionary<LanguageIndex, string>()
        {
            { LanguageIndex.En, "English" },
            { LanguageIndex.Ko, "한국어" },
        };

        /// <summary>
        /// Localization Table 이름 정의
        /// </summary>
        public static class Tables
        {
            public const string CommonUI = "GGemCo_Common_UI";
            public const string CommonGame = "GGemCo_Common_Game";
            public const string System = "GGemCo_System";
            public const string Scene = "GGemCo_Scene";
            
            public const string UIWindowTitle = "GGemCo_UI_Window_Title";
            public const string UIWindowItemInfo = "GGemCo_UIWindowItemInfo";
            public const string UIWindowSkill = "GGemCo_UIWindowSkill";
            public const string UIWindowSkillInfo = "GGemCo_UIWindowSkillInfo";
            public const string UIWindowItemUpgrade = "GGemCo_UIWindowItemUpgrade";
            public const string UIWindowItemCraft = "GGemCo_UIWindowItemCraft";
            public const string UIWindowQuestReward = "GGemCo_UIWindowQuestReward";
            
            public const string StatusName = "GGemCo_Status_Name";
            public const string ItemName = "GGemCo_Item_Name";
            public const string ItemDescription = "GGemCo_Item_Description";
            public const string MapName = "GGemCo_Map_Name";
            public const string SkillName = "GGemCo_Skill_Name";
            public const string NpcName = "GGemCo_Npc_Name";
            public const string MonsterName = "GGemCo_Monster_Name";
            public const string AffectName = "GGemCo_Affect_Name";

            /// <summary>
            /// 모든 테이블 이름을 배열로 제공합니다.
            /// </summary>
            public static readonly string[] All = new[]
            {
                CommonUI,
                CommonGame,
                System,
                Scene,
                UIWindowTitle,
                UIWindowItemInfo,
                UIWindowSkill,
                UIWindowSkillInfo,
                UIWindowItemUpgrade,
                UIWindowItemCraft,
                StatusName,
                ItemName,
                ItemDescription,
                MapName,
                SkillName,
                NpcName,
                MonsterName,
                AffectName,
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
                public static string ButtonOption() => $"{NameIntro}_{NameButton}_Option";
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
                public static string TextTypeAffect() => $"{NameLoading}_{NameText}_Affect";
                public static string TextTypeSound() => $"{NameLoading}_{NameText}_Sound";
                public static string TextLoadingPercent() => $"{NameLoading}_{NameText}_LoadingPercent";
                public static string TextTypeSettings() => $"{NameLoading}_{NameText}_Settings";
                public static string TextTypeLocalization() => $"{NameLoading}_{NameText}_Localization";
                public static string TextTypeInputAction() => $"{NameLoading}_{NameText}_InputAction";
            }

            // 기타 UI, 시스템 메시지, 팝업 등 계속 확장 가능
        }
    }
}