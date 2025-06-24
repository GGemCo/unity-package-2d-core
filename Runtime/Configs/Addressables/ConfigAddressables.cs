using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class ConfigAddressables
    {
        public const string Path = "Assets/"+ConfigDefine.NameSDK+"/DataAddressable";
        
        /// <summary>
        /// 로딩 씬에서 로드 해야 되는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
        };
        
        // 아이템
        public const string PathItemParts = Path + "/Images/Parts";
        
        // 플레이어
        public const string KeyCharacter = ConfigDefine.NameSDK+"_Character";
        
        public const string KeyPrefabMonster = KeyCharacter + "_Monster";
        public const string KeyPrefabNpc = KeyCharacter + "_Npc";
        public const string KeyPrefabPlayer = KeyCharacter + "_Player";
        
    }
}