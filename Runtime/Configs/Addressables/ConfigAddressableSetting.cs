using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// ScriptableObject 기반 설정(Addressables) 레지스트리.
    /// - 파일명 규칙: Assets/{SDK}/Settings/{SDK}{ShortKey}Settings.asset
    /// - Key 규칙:    {SDK}_{Key}
    /// </summary>
    public static class ConfigAddressableSetting
    {
        public static AddressableAssetInfo Make(string key)
        {
            return new AddressableAssetInfo(
                $"{ConfigDefine.NameSDK}_{key}",
                $"Assets/{ConfigDefine.NameSDK}/Settings/{ConfigDefine.NameSDK}{key}.asset"
                );
        }

        public static readonly AddressableAssetInfo Settings       = Make(nameof(Settings));
        public static readonly AddressableAssetInfo PlayerSettings = Make(nameof(PlayerSettings));
        public static readonly AddressableAssetInfo ItemSettings   = Make(nameof(ItemSettings));
        public static readonly AddressableAssetInfo MapSettings    = Make(nameof(MapSettings));
        public static readonly AddressableAssetInfo SaveSettings   = Make(nameof(SaveSettings));
        public static readonly AddressableAssetInfo OptionSettings = Make(nameof(OptionSettings));
        public static readonly AddressableAssetInfo SoundSettings  = Make(nameof(SoundSettings));
        public static readonly AddressableAssetInfo GameTimeSettings  = Make(nameof(GameTimeSettings));
        public static readonly AddressableAssetInfo CutsceneSettings  = Make(nameof(CutsceneSettings));
        public static readonly AddressableAssetInfo MonsterSettings  = Make(nameof(MonsterSettings));
        public static readonly AddressableAssetInfo WorldMapSettings  = Make(nameof(WorldMapSettings));
        public static readonly AddressableAssetInfo DialogueBalloonSettings = Make(nameof(DialogueBalloonSettings));
        public static readonly AddressableAssetInfo NpcInteractionSettings = Make(nameof(NpcInteractionSettings));
        public static readonly AddressableAssetInfo CharacterCollisionSettings = Make(nameof(CharacterCollisionSettings));

        /// <summary>
        /// 로딩 씬에서 로드해야 하는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            Settings,
            PlayerSettings,
            ItemSettings,
            MapSettings,
            SaveSettings,
            OptionSettings,
            SoundSettings,
            MonsterSettings,
            CutsceneSettings,
            WorldMapSettings,
            DialogueBalloonSettings,
            NpcInteractionSettings,
            CharacterCollisionSettings,
#if GGEMCO_USE_INGAME_TIME
            GameTimeSettings,
#endif            
        };
    }
}
