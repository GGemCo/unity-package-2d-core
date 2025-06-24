using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 에서 사용되는 맵 관련 정의
    /// </summary>
    public static class ConfigAddressableMap
    {
        private const string KeyPrefixMap = ConfigDefine.NameSDK+"_Map";
        private const string KeyPrefabWarp = KeyPrefixMap + "_Warp";
        
        private const string KeyNameTilemap = "tilemap";
        private const string KeyNameRegenNpc = "regen_npc";
        private const string KeyNameRegenMonster = "regen_monster";
        private const string KeyNameWarp = "warp";
        
        private const string FileExt = ".json";
        private const string FileNameTilemap = KeyNameTilemap + ".prefab";
        public const string FileNameRegenNpc = KeyNameRegenNpc + FileExt;
        public const string FileNameRegenMonster = KeyNameRegenMonster + FileExt;
        public const string FileNameWarp = KeyNameWarp + FileExt;
        
        public static string GetKeyTileMap(string folderName) => $"{KeyPrefixMap}_{folderName}_{KeyNameTilemap}";
        public static string GetAssetPathTileMap(string folderName) => $"{GetPathJson(folderName)}/{FileNameTilemap}";
        
        public static string GetKeyJsonWarp(string folderName) => $"{KeyPrefixMap}_{folderName}_{KeyNameWarp}";
        public static string GetAssetPathWarp(string folderName) => $"{GetPathJson(folderName)}/{FileNameWarp}";
        
        public static string GetKeyJsonRegenNpc(string folderName) => $"{KeyPrefixMap}_{folderName}_{KeyNameRegenNpc}";
        public static string GetAssetPathRegenNpc(string folderName) => $"{GetPathJson(folderName)}/{FileNameRegenNpc}";
        
        public static string GetKeyJsonRegenMonster(string folderName) => $"{KeyPrefixMap}_{folderName}_{KeyNameRegenMonster}";
        public static string GetAssetPathRegenMonster(string folderName) => $"{GetPathJson(folderName)}/{FileNameRegenMonster}";

        public static string GetLabel(string folderName) => $"{KeyPrefixMap}_{folderName}";

        public static readonly AddressableAssetInfo ObjectWarp = new(
            KeyPrefabWarp,
            $"{ConfigAddressables.Path}/Maps/Common/ObjectWarp.prefab",
            ConfigAddressableLabel.PreLoadGamePrefabs
        );
        /// <summary>
        /// 로딩 씬에서 로드 해야 되는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            ObjectWarp
        };

        public static string GetPathJson(string folderName)
        {
            return $"{ConfigAddressables.Path}/Maps/{folderName}";
        }

        public static string GetPathCharacter(string infoAnimationPrefabPath)
        {
            return $"{ConfigAddressables.Path}/{infoAnimationPrefabPath}";
        }
    }
}