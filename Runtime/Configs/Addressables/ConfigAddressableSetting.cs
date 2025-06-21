using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public static class ConfigAddressableSetting
    {
        // 기본 설정 Scriptable Object
        private const string KeySettings = "GGemCo_Settings";
        private const string KeyPlayerSettings = "GGemCo_PlayerSettings";
        private const string KeyMapSettings = "GGemCo_MapSettings";
        private const string KeySaveSettings = "GGemCo_SaveSettings";

        public static readonly AddressableAssetInfo Settings = new(
            KeySettings,
            "Assets/GGemCo/Settings/GGemCoSettings.asset"
        );

        public static readonly AddressableAssetInfo PlayerSettings = new(
            KeyPlayerSettings,
            "Assets/GGemCo/Settings/GGemCoPlayerSettings.asset"
        );

        public static readonly AddressableAssetInfo MapSettings = new(
            KeyMapSettings,
            "Assets/GGemCo/Settings/GGemCoMapSettings.asset"
        );

        public static readonly AddressableAssetInfo SaveSettings = new(
            KeySaveSettings,
            "Assets/GGemCo/Settings/GGemCoSaveSettings.asset"
        );
        /// <summary>
        /// 로딩 씬에서 로드 해야 되는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            Settings,
            PlayerSettings,
            MapSettings,
            SaveSettings,
        };
    }
}