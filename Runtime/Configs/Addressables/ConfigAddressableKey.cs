namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables Key 네이밍 규칙(개별 에셋 식별자).
    /// </summary>
    public static class ConfigAddressableKey
    {
        // Character
        public const string Character       = ConfigDefine.NameSDK + "_Character";
        public const string PrefabMonster   = Character + "_Monster";
        public const string PrefabNpc       = Character + "_Npc";
        public const string PrefabPlayer    = Character + "_Player";

        // Character Thumbnails
        public const string CharacterThumbnail        = ConfigDefine.NameSDK + "_CharacterThumbnail";
        public const string CharacterThumbnailNpc     = CharacterThumbnail + "_Npc";
        public const string CharacterThumbnailMonster = CharacterThumbnail + "_Monster";
        public const string CharacterThumbnailPlayer = CharacterThumbnail + "_Player";
        
        // Character Image Name
        public const string CharacterImageName        = ConfigDefine.NameSDK + "_CharacterImageName";
        public const string CharacterImageNameNpc     = CharacterImageName + "_Npc";
        public const string CharacterImageNameMonster = CharacterImageName + "_Monster";

        // Dialogue / Quest / Cutscene
        public const string Dialogue = ConfigDefine.NameSDK + "_Dialogue";
        public const string Quest    = ConfigDefine.NameSDK + "_Quest";
        public const string Cutscene = ConfigDefine.NameSDK + "_Cutscene";

        // Icons
        public const string ItemIcon  = ConfigDefine.NameSDK + "_Item_Icon";

        // Sound
        public const string Sound = ConfigDefine.NameSDK + "_Sound";

        // UI Effect Timeline
        public const string UIEffectRuntimeSequence = ConfigDefine.NameSDK + "_UIEffect_RuntimeSequence";
        
        public const string Table = ConfigDefine.NameSDK + "_Table";
        public const string TablePack = ConfigDefine.NameSDK + "_TablePack";
        
        public const string SimulationTool = ConfigDefine.NameSDK + "_SimulationTool";
        public const string SimulationGrowth = ConfigDefine.NameSDK + "_SimulationGrowth";
        // 씨앗 심는 도구에 사용
        public const string SimulationSeed = ConfigDefine.NameSDK + "_SimulationSeed";

        /// <summary>
        /// UI Effect UID를 기준으로 RuntimeSequence Addressables Key를 생성합니다.
        /// </summary>
        /// <param name="uid">UI Effect UID입니다.</param>
        /// <returns>RuntimeSequence Addressables Key입니다.</returns>
        public static string GetUIEffectRuntimeSequenceKey(int uid)
        {
            return $"{UIEffectRuntimeSequence}_{uid}";
        }

        public static string GetKeyThumbnailNpc(string npcThumbnailFileName)
        {
            return $"{CharacterThumbnailNpc}_{npcThumbnailFileName}";
        }
    }
}
