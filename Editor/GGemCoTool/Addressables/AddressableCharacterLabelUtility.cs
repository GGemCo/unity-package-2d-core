using System.Collections.Generic;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace GGemCo2DCoreEditor
{
    internal enum AddressableCharacterType
    {
        Monster,
        Npc
    }

    /// <summary>
    /// 맵 리젠 데이터(regen_monster / regen_npc)와 웨이브 스폰 데이터(wave_spawn)를 기반으로,
    /// 등장 캐릭터 프리팹(Addressables Entry)에 맵 라벨을 부여하는 공용 유틸리티입니다.
    /// - <see cref="SettingMap"/>, <see cref="SettingCharacters"/> 등 여러 툴에서 재사용할 수 있습니다.
    /// </summary>
    internal static class AddressableCharacterLabelUtility
    {
        /// <summary>
        /// 리젠(JSON) 파일을 읽고, 등장 캐릭터 프리팹 Addressables Entry에 맵 라벨을 부여합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="mapFolderName">맵 폴더 이름입니다.</param>
        /// <param name="regenJsonAssetPath">리젠 JSON 에셋 경로입니다.</param>
        /// <param name="type">라벨을 적용할 캐릭터 타입입니다.</param>
        /// <param name="clearExistingLabel">기존 맵 라벨 제거 후 다시 적용할지 여부입니다.</param>
        internal static void ApplyMapLabelFromRegen(
            AddressableAssetSettings settings,
            string mapFolderName,
            string regenJsonAssetPath,
            AddressableCharacterType type,
            bool clearExistingLabel)
        {
            if (!TryPrepareMapLabel(settings, mapFolderName, type, clearExistingLabel, out string labelName))
                return;

            string content = AssetDatabaseLoaderManager.LoadFileJson(regenJsonAssetPath);
            if (string.IsNullOrEmpty(content)) return;

            var regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
            if (regenDataList?.CharacterRegenDatas == null || regenDataList.CharacterRegenDatas.Count == 0) return;

            foreach (var regenData in regenDataList.CharacterRegenDatas)
            {
                int uid = regenData.Uid;
                if (uid <= 0) continue;

                ApplyMapLabelToCharacter(settings, uid, type, labelName);
            }
        }

        /// <summary>
        /// 웨이브 스폰(JSON) 파일을 읽고, 웨이브로 등장하는 몬스터 프리팹 Addressables Entry에 맵 라벨을 부여합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="mapFolderName">맵 폴더 이름입니다.</param>
        /// <param name="waveSpawnJsonAssetPath">웨이브 스폰 JSON 에셋 경로입니다.</param>
        /// <param name="clearExistingLabel">기존 몬스터 맵 라벨 제거 후 다시 적용할지 여부입니다.</param>
        internal static void ApplyMapLabelFromWaveSpawn(
            AddressableAssetSettings settings,
            string mapFolderName,
            string waveSpawnJsonAssetPath,
            bool clearExistingLabel)
        {
            if (!TryPrepareMapLabel(
                    settings,
                    mapFolderName,
                    AddressableCharacterType.Monster,
                    clearExistingLabel,
                    out string labelName))
            {
                return;
            }

            string content = AssetDatabaseLoaderManager.LoadFileJson(waveSpawnJsonAssetPath);
            if (string.IsNullOrEmpty(content)) return;

            var waveSpawnDataList = JsonConvert.DeserializeObject<MapWaveSpawnDataList>(content);
            if (waveSpawnDataList?.WaveScenarios == null || waveSpawnDataList.WaveScenarios.Count == 0) return;

            HashSet<int> monsterUids = CollectWaveMonsterUids(waveSpawnDataList);
            foreach (int monsterUid in monsterUids)
            {
                ApplyMapLabelToCharacter(settings, monsterUid, AddressableCharacterType.Monster, labelName);
            }
        }

        /// <summary>
        /// 맵 라벨 적용 전 공통 유효성 검사와 기존 라벨 제거를 수행합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="mapFolderName">맵 폴더 이름입니다.</param>
        /// <param name="type">라벨을 적용할 캐릭터 타입입니다.</param>
        /// <param name="clearExistingLabel">기존 맵 라벨 제거 여부입니다.</param>
        /// <param name="labelName">생성된 맵 라벨 이름입니다.</param>
        /// <returns>라벨 적용 준비가 완료되었으면 true입니다.</returns>
        private static bool TryPrepareMapLabel(
            AddressableAssetSettings settings,
            string mapFolderName,
            AddressableCharacterType type,
            bool clearExistingLabel,
            out string labelName)
        {
            labelName = string.Empty;

            if (settings == null) return false;
            if (string.IsNullOrEmpty(mapFolderName)) return false;

            labelName = ConfigAddressableMap.GetLabel(mapFolderName);
            if (string.IsNullOrEmpty(labelName)) return false;

            if (clearExistingLabel)
            {
                RemoveMapLabelFromAllCharacters(settings, type, labelName);
            }

            return true;
        }

        /// <summary>
        /// 웨이브 스폰 데이터에서 중복을 제거한 몬스터 UID 목록을 수집합니다.
        /// </summary>
        /// <param name="waveSpawnDataList">웨이브 스폰 루트 데이터입니다.</param>
        /// <returns>웨이브에 등장하는 몬스터 UID 집합입니다.</returns>
        private static HashSet<int> CollectWaveMonsterUids(MapWaveSpawnDataList waveSpawnDataList)
        {
            HashSet<int> monsterUids = new HashSet<int>();
            if (waveSpawnDataList?.WaveScenarios == null) return monsterUids;

            foreach (MapWaveScenarioData scenario in waveSpawnDataList.WaveScenarios)
            {
                if (scenario?.Groups == null) continue;

                foreach (MapWaveGroupData group in scenario.Groups)
                {
                    if (group?.Monsters == null) continue;

                    foreach (MapWaveMonsterSpawnData monsterSpawnData in group.Monsters)
                    {
                        if (monsterSpawnData == null || monsterSpawnData.MonsterUid <= 0) continue;
                        monsterUids.Add(monsterSpawnData.MonsterUid);
                    }
                }
            }

            return monsterUids;
        }

        /// <summary>
        /// 캐릭터 UID를 Addressables 프리팹 경로로 변환한 뒤 맵 라벨을 적용합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="characterUid">몬스터 또는 NPC 테이블 UID입니다.</param>
        /// <param name="type">캐릭터 타입입니다.</param>
        /// <param name="labelName">적용할 맵 라벨 이름입니다.</param>
        private static void ApplyMapLabelToCharacter(
            AddressableAssetSettings settings,
            int characterUid,
            AddressableCharacterType type,
            string labelName)
        {
            if (settings == null) return;
            if (characterUid <= 0) return;
            if (string.IsNullOrEmpty(labelName)) return;

            int animationUid = GetAnimationUidByCharacterUid(characterUid, type);
            if (animationUid <= 0) return;

            var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(animationUid);
            if (infoAnimation == null) return;

            string prefabPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, useExt: true);
            if (string.IsNullOrEmpty(prefabPath)) return;

            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
            entry?.SetLabel(labelName, true, true);
        }

        /// <summary>
        /// 캐릭터 UID에 해당하는 애니메이션 UID를 조회합니다.
        /// </summary>
        /// <param name="characterUid">몬스터 또는 NPC 테이블 UID입니다.</param>
        /// <param name="type">캐릭터 타입입니다.</param>
        /// <returns>캐릭터가 사용하는 애니메이션 UID입니다.</returns>
        private static int GetAnimationUidByCharacterUid(int characterUid, AddressableCharacterType type)
        {
            if (type == AddressableCharacterType.Monster)
            {
                var info = TableLoaderManager.LoadMonsterTable().GetDataByUid(characterUid);
                return info?.AnimationUid ?? 0;
            }

            var npc = TableLoaderManager.LoadNpcTable().GetDataByUid(characterUid);
            return npc?.AnimationUid ?? 0;
        }

        /// <summary>
        /// 기존에 설정된 특정 맵 라벨을 몬스터/ NPC 전체에서 제거합니다.
        /// - 리젠 기준으로 라벨을 "갱신"할 때, 잔재 제거를 위해 사용합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="type">라벨을 제거할 캐릭터 타입입니다.</param>
        /// <param name="labelName">제거할 맵 라벨 이름입니다.</param>
        private static void RemoveMapLabelFromAllCharacters(
            AddressableAssetSettings settings,
            AddressableCharacterType type,
            string labelName)
        {
            if (settings == null) return;
            if (string.IsNullOrEmpty(labelName)) return;

            if (type == AddressableCharacterType.Monster)
            {
                Dictionary<int, StruckTableMonster> datas = TableLoaderManager.LoadMonsterTable().GetDatas();
                foreach (var pair in datas)
                {
                    var info = pair.Value;
                    if (info == null) continue;

                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;

                    string prefabPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, useExt: true);
                    if (string.IsNullOrEmpty(prefabPath)) continue;

                    var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
                    entry?.SetLabel(labelName, false, true);
                }
            }
            else
            {
                Dictionary<int, StruckTableNpc> datas = TableLoaderManager.LoadNpcTable().GetDatas();
                foreach (var pair in datas)
                {
                    var info = pair.Value;
                    if (info == null) continue;

                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;

                    string prefabPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, useExt: true);
                    if (string.IsNullOrEmpty(prefabPath)) continue;

                    var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
                    entry?.SetLabel(labelName, false, true);
                }
            }
        }
    }
}
