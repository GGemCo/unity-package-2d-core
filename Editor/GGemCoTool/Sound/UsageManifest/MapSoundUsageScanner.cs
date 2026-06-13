using System;
using System.Collections.Generic;
using System.Linq;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치 JSON을 읽어 배치된 몬스터와 NPC의 애니메이션 사운드 사용처를 수집합니다.
    /// </summary>
    internal sealed class MapSoundUsageScanner
    {
        private readonly TableMap _tableMap;
        private readonly TableMonster _tableMonster;
        private readonly TableNpc _tableNpc;
        private readonly TableAnimation _tableAnimation;
        private readonly Dictionary<string, IReadOnlyList<AnimationSoundUsage>> _animationUsageCache =
            new Dictionary<string, IReadOnlyList<AnimationSoundUsage>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 맵 사운드 사용처 분석기를 생성합니다.
        /// </summary>
        /// <param name="tableMap">맵 테이블입니다.</param>
        /// <param name="tableMonster">몬스터 테이블입니다.</param>
        /// <param name="tableNpc">NPC 테이블입니다.</param>
        /// <param name="tableAnimation">캐릭터 애니메이션 테이블입니다.</param>
        public MapSoundUsageScanner(
            TableMap tableMap,
            TableMonster tableMonster,
            TableNpc tableNpc,
            TableAnimation tableAnimation)
        {
            _tableMap = tableMap;
            _tableMonster = tableMonster;
            _tableNpc = tableNpc;
            _tableAnimation = tableAnimation;
        }

        /// <summary>
        /// 모든 맵의 몬스터 및 NPC 배치 JSON을 분석하여 매니페스트 원본 레코드를 추가합니다.
        /// </summary>
        /// <param name="target">발견한 사용처를 추가할 결과 목록입니다.</param>
        /// <param name="result">진단 메시지를 기록할 생성 결과입니다.</param>
        public void Scan(List<SoundUsageManifestBuildRecord> target, SoundUsageManifestBuildResult result)
        {
            if (target == null || _tableMap == null)
                return;

            IReadOnlyDictionary<int, StruckTableMap> maps = _tableMap.GetAll();
            if (maps == null)
                return;

            foreach (KeyValuePair<int, StruckTableMap> pair in maps.OrderBy(item => item.Key))
            {
                StruckTableMap map = pair.Value;
                if (map == null || map.Uid <= 0 || string.IsNullOrWhiteSpace(map.FolderName))
                    continue;

                ScanMonsterPlacements(map, target, result);
                ScanNpcPlacements(map, target, result);
            }
        }

        /// <summary>
        /// 지정한 맵의 regen_monster.json에서 몬스터 UID를 수집하고 캐릭터 프리팹을 분석합니다.
        /// </summary>
        private void ScanMonsterPlacements(
            StruckTableMap map,
            List<SoundUsageManifestBuildRecord> target,
            SoundUsageManifestBuildResult result)
        {
            string jsonPath = ConfigAddressableMap.GetAssetPathRegenMonster(map.FolderName);
            IReadOnlyList<int> monsterUids = LoadPlacementUids(jsonPath, "몬스터", map.Uid, result);
            HashSet<int> uniqueMonsterUids = new HashSet<int>(monsterUids);

            foreach (int monsterUid in uniqueMonsterUids.OrderBy(uid => uid))
            {
                if (!_tableMonster.TryGetDataByUid(monsterUid, out StruckTableMonster monster) || monster == null)
                {
                    result?.AddWarning(
                        $"맵 배치 JSON의 몬스터 UID가 monster 테이블에 없습니다. mapUid={map.Uid}, monsterUid={monsterUid}, path={jsonPath}");
                    continue;
                }

                ScanCharacterAnimation(
                    map,
                    monster.AnimationUid,
                    SoundUsageManifestSourceType.MonsterAnimation,
                    monsterUid,
                    monster.Name,
                    jsonPath,
                    target,
                    result);
            }
        }

        /// <summary>
        /// 지정한 맵의 regen_npc.json에서 NPC UID를 수집하고 캐릭터 프리팹을 분석합니다.
        /// </summary>
        private void ScanNpcPlacements(
            StruckTableMap map,
            List<SoundUsageManifestBuildRecord> target,
            SoundUsageManifestBuildResult result)
        {
            string jsonPath = ConfigAddressableMap.GetAssetPathRegenNpc(map.FolderName);
            IReadOnlyList<int> npcUids = LoadPlacementUids(jsonPath, "NPC", map.Uid, result);
            HashSet<int> uniqueNpcUids = new HashSet<int>(npcUids);

            foreach (int npcUid in uniqueNpcUids.OrderBy(uid => uid))
            {
                if (!_tableNpc.TryGetDataByUid(npcUid, out StruckTableNpc npc) || npc == null)
                {
                    result?.AddWarning(
                        $"맵 배치 JSON의 NPC UID가 npc 테이블에 없습니다. mapUid={map.Uid}, npcUid={npcUid}, path={jsonPath}");
                    continue;
                }

                ScanCharacterAnimation(
                    map,
                    npc.AnimationUid,
                    SoundUsageManifestSourceType.NpcAnimation,
                    npcUid,
                    npc.Name,
                    jsonPath,
                    target,
                    result);
            }
        }

        /// <summary>
        /// 캐릭터 AnimationUid로 프리팹을 조회하고 모든 애니메이션 사운드 이벤트를 현재 맵 범위에 추가합니다.
        /// </summary>
        private void ScanCharacterAnimation(
            StruckTableMap map,
            int animationUid,
            SoundUsageManifestSourceType sourceType,
            int characterUid,
            string characterName,
            string placementJsonPath,
            List<SoundUsageManifestBuildRecord> target,
            SoundUsageManifestBuildResult result)
        {
            if (animationUid <= 0 ||
                !_tableAnimation.TryGetDataByUid(animationUid, out StruckTableAnimation animation) ||
                animation == null)
            {
                result?.AddWarning(
                    $"배치 캐릭터의 AnimationUid를 찾지 못했습니다. mapUid={map.Uid}, characterUid={characterUid}, animationUid={animationUid}");
                return;
            }

            string prefabPathWithoutExtension = ConfigAddressableMap.GetPathCharacter(animation);
            string prefabPath = string.IsNullOrWhiteSpace(prefabPathWithoutExtension)
                ? string.Empty
                : $"{prefabPathWithoutExtension}.prefab";
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                result?.AddWarning(
                    $"캐릭터 프리팹 경로를 만들지 못했습니다. mapUid={map.Uid}, characterUid={characterUid}, animationUid={animationUid}");
                return;
            }

            IReadOnlyList<AnimationSoundUsage> usages = GetOrScanAnimationUsages(prefabPath, result);
            for (int i = 0; i < usages.Count; i++)
            {
                AnimationSoundUsage usage = usages[i];
                if (usage == null || usage.SoundUid <= 0)
                    continue;

                target.Add(new SoundUsageManifestBuildRecord
                {
                    ScopeType = SoundUsageManifestScopeType.Map,
                    ScopeUid = map.Uid,
                    SoundUid = usage.SoundUid,
                    SourceType = sourceType,
                    SourceUid = characterUid,
                    SourcePath = usage.SourcePath,
                    Memo = $"map={map.Name}, character={characterName}, placement={placementJsonPath}, {usage.Memo}",
                });
            }
        }

        /// <summary>
        /// 동일 캐릭터 프리팹을 여러 맵에서 재사용할 때 애니메이션 에셋 분석 결과를 캐시해 반복 비용을 줄입니다.
        /// </summary>
        private IReadOnlyList<AnimationSoundUsage> GetOrScanAnimationUsages(
            string prefabPath,
            SoundUsageManifestBuildResult result)
        {
            if (_animationUsageCache.TryGetValue(prefabPath, out IReadOnlyList<AnimationSoundUsage> cached))
                return cached;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                result?.AddWarning($"캐릭터 프리팹을 찾지 못했습니다. path={prefabPath}");
                IReadOnlyList<AnimationSoundUsage> empty = Array.Empty<AnimationSoundUsage>();
                _animationUsageCache[prefabPath] = empty;
                return empty;
            }

            IReadOnlyList<AnimationSoundUsage> usages =
                AnimationSoundEventScanner.ScanPrefab(prefab, prefabPath, result);
            _animationUsageCache[prefabPath] = usages;
            return usages;
        }

        /// <summary>
        /// 맵 배치 JSON을 읽어 유효한 캐릭터 UID 목록을 반환합니다.
        /// 파일이 없는 맵은 빈 배치로 처리하며, 파싱 오류만 경고로 기록합니다.
        /// </summary>
        /// <param name="jsonPath">regen_monster 또는 regen_npc JSON 에셋 경로입니다.</param>
        /// <param name="label">진단 메시지에 표시할 캐릭터 종류입니다.</param>
        /// <param name="mapUid">분석 중인 맵 UID입니다.</param>
        /// <param name="result">진단 메시지를 기록할 생성 결과입니다.</param>
        /// <returns>JSON에 기록된 캐릭터 UID 목록입니다.</returns>
        private static IReadOnlyList<int> LoadPlacementUids(
            string jsonPath,
            string label,
            int mapUid,
            SoundUsageManifestBuildResult result)
        {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
                return Array.Empty<int>();

            try
            {
                CharacterRegenDataList data =
                    JsonConvert.DeserializeObject<CharacterRegenDataList>(textAsset.text);
                if (data?.CharacterRegenDatas == null)
                    return Array.Empty<int>();

                List<int> resultUids = new List<int>(data.CharacterRegenDatas.Count);
                for (int i = 0; i < data.CharacterRegenDatas.Count; i++)
                {
                    int uid = data.CharacterRegenDatas[i]?.Uid ?? 0;
                    if (uid > 0)
                        resultUids.Add(uid);
                }

                return resultUids;
            }
            catch (Exception ex)
            {
                result?.AddWarning(
                    $"맵 {label} 배치 JSON을 해석하지 못했습니다. mapUid={mapUid}, path={jsonPath}, error={ex.Message}");
                return Array.Empty<int>();
            }
        }
    }
}
