using System.Text.RegularExpressions;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 관련 모든 경로 규칙의 단일 소스(SoT).
    /// - 경로는 반드시 여기서만 생성/관리합니다.
    /// - OS 간 슬래시/특수문자 정규화를 제공합니다.
    /// - 다른 Config* 클래스는 본 클래스를 통해 경로를 참조하세요.
    /// </summary>
    public static class ConfigAddressablePath
    {
        // -------------------------
        // Root
        // -------------------------
        /// <summary>Assets/{SDK}/DataAddressable</summary>
        public static string Root => EnsureForwardSlashes($"Assets/{ConfigDefine.NameSDK}/DataAddressable");

        /// <summary>Assets/{SDK}/Settings</summary>
        public static string SettingsRoot => EnsureForwardSlashes($"Assets/{ConfigDefine.NameSDK}/Settings");

        /// <summary>SpriteAtlas 루트</summary>
        public static string SpriteAtlas => Combine(Root, "SpriteAtlas");

        // -------------------------
        // Tables / Sounds
        // -------------------------
        /// <summary>테이블 파일(.txt) 루트</summary>
        public static string Tables => Combine(Root, "Tables");

        /// <summary>런타임 테이블 팩(.bytes) 루트</summary>
        public static string TablePacks => Combine(Root, "TablePacks");

        /// <summary>사운드 루트</summary>
        public static string Sounds => Combine(Root, "Sounds");

        /// <summary>
        /// 사운드 경로 생성기
        /// - Type/SubType 조합에 따라 폴더를 만듭니다.
        /// - 둘 다 None이면 빈 문자열을 반환합니다.
        /// </summary>
        public static string BuildSoundPath(SoundConstants.Type type, SoundConstants.SubType subType)
        {
            if (type == SoundConstants.Type.None && subType == SoundConstants.SubType.None)
                return string.Empty;

            if (type != SoundConstants.Type.None && subType != SoundConstants.SubType.None)
                return Combine(Sounds, type.ToString(), subType.ToString());

            if (type != SoundConstants.Type.None)
                return Combine(Sounds, type.ToString());

            return string.Empty;
        }

        // -------------------------
        // Characters
        // -------------------------
        public static class Characters
        {
            /// <summary>Assets/{SDK}/DataAddressable/Characters</summary>
            private static string RootCharacter => Combine(Root, "Characters");

            /// <summary>Characters/Npc</summary>
            public static string Npc => Combine(RootCharacter, "Npc");

            /// <summary>Characters/Monster</summary>
            public static string Monster => Combine(RootCharacter, "Monster");

            /// <summary>Characters/Player</summary>
            public static string Player => Combine(RootCharacter, "Player");

            public static class Thumbnails
            {
                /// <summary>Images/Thumbnail</summary>
                private static string RootThumbnail => Combine(Images.RootImage, "Thumbnail");

                /// <summary>Images/Thumbnail/Npc</summary>
                public static string Npc => Combine(RootThumbnail, "Npc");

                /// <summary>Images/Thumbnail/Monster</summary>
                public static string Monster => Combine(RootThumbnail, "Monster");

                /// <summary>Images/Thumbnail/Player</summary>
                public static string Player => Combine(RootThumbnail, "Player");
            }

            public static class ImageName
            {
                /// <summary>Images/CharacterName</summary>
                private static string RootImageName => Combine(Images.RootImage, "CharacterName");
                /// <summary>Images/CharacterName/Npc</summary>
                public static string Npc => Combine(RootImageName, "Npc");
                
                /// <summary>Images/CharacterName/Monster</summary>
                public static string Monster => Combine(RootImageName, "Monster");
            }
        }

        // -------------------------
        // Images (Icons, Parts, etc.)
        // -------------------------
        public static class Images
        {
            /// <summary>Assets/{SDK}/DataAddressable/Images</summary>
            public static string RootImage => Combine(Root, "Images");

            /// <summary>Images/Parts (아이템 파츠)</summary>
            public static string Parts => Combine(RootImage, "Parts");

            public static class Icon
            {
                /// <summary>Images/Icon</summary>
                public static string RootIcon => Combine(RootImage, "Icon");

                /// <summary>Images/Icon/Item</summary>
                public static string Item => Combine(RootIcon, "Item");

                /// <summary>Images/Icon/Item/Drop</summary>
                public static string ItemDrop => Combine(Item, "Drop");

                /// <summary>Images/Icon/Item/Equip</summary>
                public static string ItemEquip => Combine(Item, "Equip");

                /// <summary>Images/Icon/Item/Icon</summary>
                public static string ItemIcon => Combine(Item, "Icon");
            }
        }

        // -------------------------
        // Vfx
        // -------------------------
        public static class Vfx
        {
            /// <summary>Assets/{SDK}/DataAddressable/Vfx</summary>
            public static string RootVfx => Combine(Root, "Vfx");
        }

        // -------------------------
        // UI Effect Timeline
        // -------------------------
        public static class UIEffect
        {
            /// <summary>Assets/{SDK}/DataAddressable/UIEffect</summary>
            private static string RootUIEffect => Combine(Root, "UIEffect");

            /// <summary>Assets/{SDK}/DataAddressable/UIEffect/RuntimeSequences</summary>
            public static string RuntimeSequence => Combine(RootUIEffect, "RuntimeSequences");
        }

        // -------------------------
        // Dialogue / Quest / Cutscene
        // -------------------------
        public static class Narrative
        {
            /// <summary>Assets/{SDK}/DataAddressable/Dialogue</summary>
            public static string Dialogue => Combine(Root, "Dialogue");

            /// <summary>Assets/{SDK}/DataAddressable/Quests</summary>
            public static string Quests => Combine(Root, "Quests");

            /// <summary>Assets/{SDK}/DataAddressable/Cutscene</summary>
            public static string Cutscene => Combine(Root, "Cutscene");
        }

        // -------------------------
        // Maps
        // -------------------------
        public static class Maps
        {
            /// <summary>Assets/{SDK}/DataAddressable/Maps</summary>
            private static string RootMap => Combine(Root, "Maps");

            /// <summary>Assets/{SDK}/DataAddressable/Maps/Common</summary>
            public static string Common => Combine(RootMap, "Common");

            /// <summary>
            /// 특정 폴더(스테이지/씬) 경로
            /// - 폴더명은 Normalize 규칙을 거칩니다.
            /// </summary>
            public static string Folder(string folderName)
            {
                var f = Normalize(folderName);
                return Combine(RootMap, f);
            }
        }
        // -------------------------
        // Prefab
        // -------------------------
        public static class Prefab
        {
            /// <summary>Assets/{SDK}/DataAddressable/Prefab</summary>
            public static string RootPrefab => Combine(Root, "Prefab");
        }

        public static class Simulation
        {
            /// <summary>Assets/{SDK}/DataAddressable/Simulation</summary>
            private static string RootSimulation => Combine(Root, "Simulation");

            /// <summary>Assets/{SDK}/DataAddressable/Simulation/ToolDefinition</summary>
            public static string ToolDefinition => Combine(RootSimulation, "ToolDefinition");
            
            /// <summary>Assets/{SDK}/DataAddressable/Simulation/ToolTargeting</summary>
            public static string ToolTargeting => Combine(RootSimulation, "ToolTargeting");
            
            /// <summary>Assets/{SDK}/DataAddressable/Simulation/Growth</summary>
            public static string Growth => Combine(RootSimulation, "Growth");
        }

        // -------------------------
        // Utility
        // -------------------------

        /// <summary>
        /// 경로 결합 + 슬래시 정규화.
        /// </summary>
        public static string Combine(params string[] parts)
        {
            if (parts == null || parts.Length == 0) return string.Empty;
            var joined = string.Join("/", parts);
            return EnsureForwardSlashes(joined);
        }

        /// <summary>
        /// 역슬래시(\)를 슬래시(/)로 바꾸고, 연속 슬래시를 1개로 축약합니다.
        /// </summary>
        public static string EnsureForwardSlashes(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            var s = path.Replace('\\', '/');
            // "Assets://" 같은 스킴이 없으므로 단순 축약 가능
            s = Regex.Replace(s, "/{2,}", "/");
            return s;
        }

        /// <summary>
        /// Addressables 키/경로 안전성을 위한 폴더명 정규화.
        /// - Trim, 공백→'_', 허용문자 외 '_' 대체, 연속 '_' 축약, 선두/말미 '_' 제거.
        /// - 현재는 [A-Za-z0-9_-]만 허용합니다. (한글 허용 필요 시 정규식 확장)
        /// </summary>
        public static string Normalize(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return string.Empty;

            var s = folderName.Trim();
            s = Regex.Replace(s, @"\s+", "_");                // 공백 → '_'
            s = Regex.Replace(s, @"[^A-Za-z0-9_\-]", "_");    // 허용 외 문자 → '_'
            s = Regex.Replace(s, @"_+", "_");                 // 연속 '_' 축약
            s = s.Trim('_');                                  // 선두/말미 '_' 제거
            return s;
        }
    }
}
