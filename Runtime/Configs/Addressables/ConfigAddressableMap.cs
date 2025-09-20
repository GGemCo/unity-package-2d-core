using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 관련 Addressables 규칙/레지스트리.
    /// - 폴더별(Label)과 자산별(Key/Path) 생성 규칙을 단일화
    /// - 공용 오브젝트(워프 등) 프리로드 목록 제공
    /// </summary>
    public enum MapAssetType
    {
        TilemapPrefab,    // tilemap.prefab
        RegenNpcJson,     // regen_npc.json
        RegenMonsterJson, // regen_monster.json
        WarpJson          // warp.json
    }

    public static class ConfigAddressableMap
    {
        private const string KeyNameTilemap    = "tilemap";
        private const string KeyNameRegenNpc   = "regen_npc";
        private const string KeyNameRegenMonster= "regen_monster";
        private const string KeyNameWarp       = "warp";

        private const string ExtPrefab = ".prefab";
        private const string ExtJson   = ".json";

        /// <summary>라벨: {SDK}_Map_{folder}</summary>
        public static string GetLabel(string folderName)
        {
            var f = ConfigAddressablePath.Normalize(folderName);
            return $"{ConfigDefine.NameSDK}_Map_{f}";
        }

        /// <summary>키: {SDK}_Map_{folder}_{assetName}</summary>
        public static string GetKey(string folderName, MapAssetType type)
        {
            var f = ConfigAddressablePath.Normalize(folderName);
            return $"{ConfigDefine.NameSDK}_Map_{f}_{GetAssetName(type)}";
        }

        /// <summary>경로: Assets/{SDK}/DataAddressable/Maps/{folder}/{fileName}</summary>
        public static string GetAssetPath(string folderName, MapAssetType type)
        {
            var dir = ConfigAddressablePath.Maps.Folder(folderName);
            return $"{dir}/{GetFileName(type)}";
        }

        // 기존 헬퍼 호환용 (선택)
        public static string GetKeyTileMap(string folderName)          => GetKey(folderName, MapAssetType.TilemapPrefab);
        public static string GetAssetPathTileMap(string folderName)    => GetAssetPath(folderName, MapAssetType.TilemapPrefab);
        public static string GetKeyJsonWarp(string folderName)         => GetKey(folderName, MapAssetType.WarpJson);
        public static string GetAssetPathWarp(string folderName)       => GetAssetPath(folderName, MapAssetType.WarpJson);
        public static string GetKeyJsonRegenNpc(string folderName)     => GetKey(folderName, MapAssetType.RegenNpcJson);
        public static string GetAssetPathRegenNpc(string folderName)   => GetAssetPath(folderName, MapAssetType.RegenNpcJson);
        public static string GetKeyJsonRegenMonster(string folderName) => GetKey(folderName, MapAssetType.RegenMonsterJson);
        public static string GetAssetPathRegenMonster(string folderName)=> GetAssetPath(folderName, MapAssetType.RegenMonsterJson);

        public static string GetAssetName(MapAssetType type) => type switch
        {
            MapAssetType.TilemapPrefab    => KeyNameTilemap,
            MapAssetType.RegenNpcJson     => KeyNameRegenNpc,
            MapAssetType.RegenMonsterJson => KeyNameRegenMonster,
            MapAssetType.WarpJson         => KeyNameWarp,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static string GetFileName(MapAssetType type) => type switch
        {
            MapAssetType.TilemapPrefab    => KeyNameTilemap + ExtPrefab,
            MapAssetType.RegenNpcJson     => KeyNameRegenNpc + ExtJson,
            MapAssetType.RegenMonsterJson => KeyNameRegenMonster + ExtJson,
            MapAssetType.WarpJson         => KeyNameWarp + ExtJson,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        // 공용 오브젝트 (예: Warp prefab)
        public static readonly AddressableAssetInfo ObjectWarp = new(
            key: $"{ConfigDefine.NameSDK}_Map_Warp",
            path: $"{ConfigAddressablePath.Maps.Common}/ObjectWarp.prefab",
            label: ConfigAddressableLabel.PreLoadGamePrefabs
        );

        /// <summary>로딩 씬 프리로드 대상(읽기 전용)</summary>
        public static readonly ReadOnlyCollection<AddressableAssetInfo> NeedLoadInLoadingScene
            = new(new List<AddressableAssetInfo> { ObjectWarp });

        /// <summary>
        /// 캐릭터 프리팹 경로 생성기(기존 사용 패턴 호환)
        /// </summary>
        public static string GetPathCharacter(StruckTableAnimation infoAnimation, bool useExt = false)
        {
            string basePath = infoAnimation.Type switch
            {
                CharacterConstants.Type.Monster => ConfigAddressablePath.Characters.Monster,
                CharacterConstants.Type.Npc     => ConfigAddressablePath.Characters.Npc,
                CharacterConstants.Type.Player  => ConfigAddressablePath.Characters.Player,
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(infoAnimation.PrefabName))
                return string.Empty;

            var path = $"{basePath}/{infoAnimation.PrefabName}";
            return useExt ? path + ExtPrefab : path;
        }
    }
}
