using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public static class ConfigAddressables
    {
        public const string Path = "Assets/GGemCo/DataAddressable";
        
        /// <summary>
        /// 로딩 씬에서 로드 해야 되는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
        };
        
        // 플레이어
        public const string KeyPrefabPlayer = "GGemCo_Character_Player";
        public const string KeyCharacter = "GGemCo_Character";
    }
}