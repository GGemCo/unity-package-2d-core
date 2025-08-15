using System.Collections.Generic;

namespace GGemCo2DCore
{
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
        public static readonly AddressableAssetInfo MapSettings    = Make(nameof(MapSettings));
        public static readonly AddressableAssetInfo SaveSettings   = Make(nameof(SaveSettings));
        public static readonly AddressableAssetInfo OptionSettings = Make(nameof(OptionSettings));
        public static readonly AddressableAssetInfo SoundSettings  = Make(nameof(SoundSettings));

        /// <summary>
        /// 로딩 씬에서 로드해야 하는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            Settings,
            PlayerSettings,
            MapSettings,
            SaveSettings,
            OptionSettings,
            SoundSettings
        };
    }
}
