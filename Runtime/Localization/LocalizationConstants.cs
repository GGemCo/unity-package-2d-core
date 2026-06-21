using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace GGemCo2DCore
{
    public static class LocalizationConstants
    {
        private static readonly Dictionary<string, string> LanguageNames = new Dictionary<string, string>()
        {
            { "en", "English" },
            { "en-US", "English" },
            { "ko", "한국어" },
            { "ko-KR", "한국어" }
        };

        public static string GetName(Locale locale)
        {
            if (locale == null) return "Unknown";
            var code = locale.Identifier.Code;

            // 매핑 테이블 우선
            if (LanguageNames.TryGetValue(code, out var display))
                return display;

            // 매핑 없으면 LocaleName 사용
            return locale.LocaleName;
        }

        public static Locale GetDefaultLocale()
        {
            return LocalizationSettings.ProjectLocale;
        }
        public static string GetDefaultCode()
        {
            return LocalizationSettings.ProjectLocale.Identifier.Code;
        }

        /// <summary>
        /// Localization Table 이름 정의
        /// </summary>
        public static class Tables
        {
            public const string CommonUI = "GGemCo_Common_UI";
            public const string CommonGame = "GGemCo_Common_Game";
            public const string System = "GGemCo_System";
            public const string Scene = "GGemCo_Scene";
            public const string ItemTaxonomy = "GGemCo_Item_Taxonomy";
            
            public const string UIWindowTitle = "GGemCo_UI_Window_Title";
            public const string UIWindowItemInfo = "GGemCo_UIWindowItemInfo";
            public const string UIWindowItemUpgrade = "GGemCo_UIWindowItemUpgrade";
            public const string UIWindowItemCraft = "GGemCo_UIWindowItemCraft";
            public const string UIWindowOption = "GGemCo_UIWindowOption";
            public const string UIWindowInteractionDialogue = "GGemCo_UIWindowInteractionDialogue";
            public const string UIWindowTcgBattleHud = "GGemCo_UIWindowTcgBattleHud";
            public const string UIWindowPlayerStatInfo = "GGemCo_UIWindowPlayerStatInfo";
            public const string UIWindowShop = "GGemCo_UIWindowShop";
            public const string UIWindowPlayerStatReset = "GGemCo_UIWindowPlayerStatReset";
            
            public const string StatusName = "GGemCo_Status_Name";
            public const string ItemName = "GGemCo_Item_Name";
            public const string ItemDescription = "GGemCo_Item_Description";
            public const string MapName = "GGemCo_Map_Name";
            public const string NpcName = "GGemCo_Npc_Name";
            public const string MonsterName = "GGemCo_Monster_Name";

            /// <summary>
            /// 모든 테이블 이름을 배열로 제공합니다.
            /// </summary>
            public static readonly string[] All = new[]
            {
                CommonUI,
                CommonGame,
                System,
                Scene,
                ItemTaxonomy,
                UIWindowTitle,
                UIWindowItemInfo,
                UIWindowItemUpgrade,
                UIWindowItemCraft,
                UIWindowOption,
                UIWindowInteractionDialogue,
                UIWindowTcgBattleHud,
                StatusName,
                ItemName,
                ItemDescription,
                MapName,
                NpcName,
                MonsterName,
                UIWindowPlayerStatInfo,
                UIWindowShop,
                UIWindowPlayerStatReset,
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
                public static string TextTypeVfx() => $"{NameLoading}_{NameText}_Effect";
                public static string TextTypeItem() => $"{NameLoading}_{NameText}_Item";
                public static string TextTypeSkill() => $"{NameLoading}_{NameText}_Skill";
                public static string TextTypeAffect() => $"{NameLoading}_{NameText}_Affect";
                public static string TextTypeSound() => $"{NameLoading}_{NameText}_Sound";
                public static string TextLoadingPercent() => $"{NameLoading}_{NameText}_LoadingPercent";
                public static string TextTypeSettings() => $"{NameLoading}_{NameText}_Settings";
                public static string TextTypeLocalization() => $"{NameLoading}_{NameText}_Localization";
                public static string TextTypeInputAction() => $"{NameLoading}_{NameText}_InputAction";
                public static string TextTypeCutscene() => $"{NameLoading}_{NameText}_Cutscene";
                public static string TextTypeWorldMap() => $"{NameLoading}_{NameText}_WorldMap";
                public static string TextTypeCharacterThumbnail() => $"{NameLoading}_{NameText}_CharacterThumbnail";
            }

            // 기타 UI, 시스템 메시지, 팝업 등 계속 확장 가능
            public static class Date
            {
                public static string Week() => $"ui_date_weekday";
                public static string Climate() => $"ui_date_climate";

                public static string Day() => $"ui_date_daynumber";
            }
        }
    }
}
