using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public static class ConfigAddressableSetting
    {
        // 기본 설정 Scriptable Object
        private const string KeySettings = ConfigDefine.NameSDK+"_Settings";
        private const string KeyPlayerSettings = ConfigDefine.NameSDK+"_PlayerSettings";
        private const string KeyMapSettings = ConfigDefine.NameSDK+"_MapSettings";
        private const string KeySaveSettings = ConfigDefine.NameSDK+"_SaveSettings";

        public static readonly AddressableAssetInfo Settings = new(
            KeySettings,
            $"Assets/{ConfigDefine.NameSDK}/Settings/{ConfigDefine.NameSDK}Settings.asset"
        );

        public static readonly AddressableAssetInfo PlayerSettings = new(
            KeyPlayerSettings,
            $"Assets/{ConfigDefine.NameSDK}/Settings/{ConfigDefine.NameSDK}PlayerSettings.asset"
        );

        public static readonly AddressableAssetInfo MapSettings = new(
            KeyMapSettings,
            $"Assets/{ConfigDefine.NameSDK}/Settings/{ConfigDefine.NameSDK}MapSettings.asset"
        );

        public static readonly AddressableAssetInfo SaveSettings = new(
            KeySaveSettings,
            $"Assets/{ConfigDefine.NameSDK}/Settings/{ConfigDefine.NameSDK}SaveSettings.asset"
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