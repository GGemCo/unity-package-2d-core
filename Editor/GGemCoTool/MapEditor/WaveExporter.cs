using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치툴에서 웨이브 스폰 데이터를 불러오고 저장하는 에디터 전용 Exporter입니다.
    /// </summary>
    public sealed class WaveExporter
    {
        private readonly MapWaveSpawnDataList _waveSpawnDataList = new MapWaveSpawnDataList();
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;
        private DefaultMap _defaultMap;

        /// <summary>
        /// 현재 편집 중인 웨이브 스폰 데이터입니다.
        /// </summary>
        public MapWaveSpawnDataList DataList => _waveSpawnDataList;

        /// <summary>
        /// 웨이브 Exporter가 사용할 테이블과 현재 맵을 초기화합니다.
        /// </summary>
        /// <param name="tableMonster">몬스터 테이블입니다.</param>
        /// <param name="tableAnimation">애니메이션 테이블입니다.</param>
        /// <param name="defaultMap">현재 편집 중인 맵 루트입니다.</param>
        public void Initialize(TableMonster tableMonster, TableAnimation tableAnimation, DefaultMap defaultMap)
        {
            _tableMonster = tableMonster;
            _tableAnimation = tableAnimation;
            _defaultMap = defaultMap;
        }

        /// <summary>
        /// 현재 편집 중인 맵 루트를 갱신합니다.
        /// 맵을 새로 불러온 뒤 웨이브 스폰 포인트 기본 위치 계산에 사용합니다.
        /// </summary>
        /// <param name="defaultMap">현재 편집 중인 맵 루트입니다.</param>
        public void SetDefaultMap(DefaultMap defaultMap)
        {
            _defaultMap = defaultMap;
        }

        /// <summary>
        /// wave_spawn.json 데이터를 불러와 에디터 편집 상태로 복원합니다.
        /// 파일이 없거나 비어 있으면 빈 웨이브 데이터를 유지합니다.
        /// </summary>
        /// <param name="waveFileName">불러올 wave_spawn.json 에셋 경로입니다.</param>
        public void LoadWaveData(string waveFileName)
        {
            ClearData();

            if (string.IsNullOrEmpty(waveFileName))
            {
                return;
            }

            try
            {
                string content = AssetDatabaseLoaderManager.LoadFileJson(waveFileName);
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                MapWaveSpawnDataList loadedData = JsonConvert.DeserializeObject<MapWaveSpawnDataList>(content);
                if (loadedData?.WaveScenarios == null)
                {
                    return;
                }

                _waveSpawnDataList.WaveScenarios.AddRange(loadedData.WaveScenarios);
                NormalizeAllScenarios(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"웨이브 스폰 데이터 읽기 실패: {waveFileName}\n{ex.Message}");
            }
        }

        /// <summary>
        /// 현재 편집 중인 웨이브 데이터를 wave_spawn.json으로 저장합니다.
        /// 저장 전에 맵 UID와 기본값을 정규화하고, 사용된 몬스터 프리팹에 맵 Addressables 라벨을 부여합니다.
        /// </summary>
        /// <param name="filePath">저장할 폴더 경로입니다.</param>
        /// <param name="fileName">저장할 파일명입니다.</param>
        /// <param name="mapUid">현재 맵 UID입니다.</param>
        /// <param name="struckTableMap">현재 맵 테이블 데이터입니다.</param>
        public void ExportWaveDataToJson(string filePath, string fileName, int mapUid, StruckTableMap struckTableMap)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(fileName))
            {
                Debug.LogWarning("웨이브 스폰 데이터를 저장할 경로가 유효하지 않습니다.");
                return;
            }

            NormalizeAllScenarios(mapUid);
            ApplyMonsterAddressableLabels(struckTableMap);

            Directory.CreateDirectory(filePath);
            string path = Path.Combine(filePath, fileName);
            string json = JsonConvert.SerializeObject(_waveSpawnDataList, Formatting.Indented);
            File.WriteAllText(path, json);
            Debug.Log("웨이브 스폰 data exported to " + path);
        }

        /// <summary>
        /// 현재 맵에 새 웨이브 시나리오를 추가합니다.
        /// </summary>
        /// <param name="mapUid">새 시나리오에 연결할 맵 UID입니다.</param>
        /// <returns>추가된 웨이브 시나리오 데이터입니다.</returns>
        public MapWaveScenarioData AddScenario(int mapUid)
        {
            MapWaveScenarioData scenario = new MapWaveScenarioData
            {
                ScenarioUid = GetNextScenarioUid(),
                MapUid = mapUid,
                Memo = string.Empty,
                AutoStart = true,
                StartDelaySeconds = 0f,
                SuppressNormalMonsterRespawnWhileRunning = true
            };

            _waveSpawnDataList.WaveScenarios.Add(scenario);
            return scenario;
        }

        /// <summary>
        /// 지정 시나리오를 편집 데이터에서 제거합니다.
        /// </summary>
        /// <param name="scenarioUid">제거할 시나리오 UID입니다.</param>
        /// <returns>시나리오를 제거했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool RemoveScenario(int scenarioUid)
        {
            MapWaveScenarioData scenario = FindScenario(scenarioUid);
            return scenario != null && _waveSpawnDataList.WaveScenarios.Remove(scenario);
        }

        /// <summary>
        /// 지정 시나리오 UID에 해당하는 데이터를 찾습니다.
        /// </summary>
        /// <param name="scenarioUid">조회할 시나리오 UID입니다.</param>
        /// <returns>찾은 시나리오 데이터입니다. 없으면 <see langword="null"/>입니다.</returns>
        public MapWaveScenarioData FindScenario(int scenarioUid)
        {
            List<MapWaveScenarioData> scenarios = _waveSpawnDataList.WaveScenarios;
            if (scenarios == null)
            {
                return null;
            }

            for (int i = 0; i < scenarios.Count; i++)
            {
                MapWaveScenarioData scenario = scenarios[i];
                if (scenario != null && scenario.ScenarioUid == scenarioUid)
                {
                    return scenario;
                }
            }

            return null;
        }

        /// <summary>
        /// 지정 시나리오에 스폰 포인트를 추가합니다.
        /// </summary>
        /// <param name="scenario">스폰 포인트를 추가할 시나리오입니다.</param>
        /// <param name="position">맵 기준 스폰 위치입니다.</param>
        /// <returns>추가된 스폰 포인트 데이터입니다.</returns>
        public MapWaveSpawnPointData AddSpawnPoint(MapWaveScenarioData scenario, Vector3 position)
        {
            if (scenario == null)
            {
                return null;
            }

            EnsureScenarioCollections(scenario);

            MapWaveSpawnPointData spawnPoint = new MapWaveSpawnPointData
            {
                PointId = GetNextSpawnPointId(scenario),
                x = position.x,
                y = position.y,
                z = position.z,
                RandomRadius = 0f,
                MapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling
            };

            scenario.SpawnPoints.Add(spawnPoint);
            if (scenario.StartPointId <= 0)
            {
                scenario.StartPointId = spawnPoint.PointId;
            }

            return spawnPoint;
        }

        /// <summary>
        /// 지정 시나리오에서 스폰 포인트를 제거합니다.
        /// 이 스폰 포인트를 참조하던 몬스터 스폰 데이터와 그룹 이동 유도 포인트는 참조 ID를 0으로 되돌립니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <param name="pointId">제거할 스폰 포인트 ID입니다.</param>
        /// <returns>스폰 포인트를 제거했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool RemoveSpawnPoint(MapWaveScenarioData scenario, int pointId)
        {
            MapWaveSpawnPointData spawnPoint = FindSpawnPoint(scenario, pointId);
            if (scenario == null || spawnPoint == null)
            {
                return false;
            }

            bool removed = scenario.SpawnPoints.Remove(spawnPoint);
            if (!removed)
            {
                return false;
            }

            if (scenario.StartPointId == pointId)
            {
                scenario.StartPointId = scenario.SpawnPoints.Count > 0 ? scenario.SpawnPoints[0].PointId : 0;
            }

            if (scenario.Groups != null)
            {
                for (int groupIndex = 0; groupIndex < scenario.Groups.Count; groupIndex++)
                {
                    MapWaveGroupData group = scenario.Groups[groupIndex];
                    if (group == null)
                    {
                        continue;
                    }

                    if (group.NavigationPointId == pointId)
                    {
                        group.NavigationPointId = 0;
                    }

                    if (group.Monsters == null)
                    {
                        continue;
                    }

                    for (int monsterIndex = 0; monsterIndex < group.Monsters.Count; monsterIndex++)
                    {
                        MapWaveMonsterSpawnData monsterSpawn = group.Monsters[monsterIndex];
                        if (monsterSpawn != null && monsterSpawn.SpawnPointId == pointId)
                        {
                            monsterSpawn.SpawnPointId = 0;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 지정 시나리오에서 스폰 포인트를 찾습니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <param name="pointId">조회할 스폰 포인트 ID입니다.</param>
        /// <returns>찾은 스폰 포인트입니다. 없으면 <see langword="null"/>입니다.</returns>
        public MapWaveSpawnPointData FindSpawnPoint(MapWaveScenarioData scenario, int pointId)
        {
            if (scenario?.SpawnPoints == null)
            {
                return null;
            }

            for (int i = 0; i < scenario.SpawnPoints.Count; i++)
            {
                MapWaveSpawnPointData spawnPoint = scenario.SpawnPoints[i];
                if (spawnPoint != null && spawnPoint.PointId == pointId)
                {
                    return spawnPoint;
                }
            }

            return null;
        }

        /// <summary>
        /// 지정 시나리오에 웨이브 그룹을 추가합니다.
        /// </summary>
        /// <param name="scenario">그룹을 추가할 시나리오입니다.</param>
        /// <returns>추가된 웨이브 그룹 데이터입니다.</returns>
        public MapWaveGroupData AddGroup(MapWaveScenarioData scenario)
        {
            if (scenario == null)
            {
                return null;
            }

            EnsureScenarioCollections(scenario);

            MapWaveGroupData group = new MapWaveGroupData
            {
                GroupUid = GetNextGroupUid(scenario),
                Order = scenario.Groups.Count + 1,
                RepeatCount = 1,
                NextPolicy = WaveNextPolicy.WhenAllDead,
                NextAfterSeconds = 0f,
                NextDelaySeconds = 0f,
                NextGroupUid = 0
            };

            scenario.Groups.Add(group);
            return group;
        }

        /// <summary>
        /// 지정 시나리오에서 웨이브 그룹을 제거합니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <param name="groupUid">제거할 그룹 UID입니다.</param>
        /// <returns>그룹을 제거했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool RemoveGroup(MapWaveScenarioData scenario, int groupUid)
        {
            MapWaveGroupData group = FindGroup(scenario, groupUid);
            return scenario?.Groups != null && group != null && scenario.Groups.Remove(group);
        }

        /// <summary>
        /// 지정 시나리오에서 웨이브 그룹을 찾습니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <param name="groupUid">조회할 그룹 UID입니다.</param>
        /// <returns>찾은 그룹 데이터입니다. 없으면 <see langword="null"/>입니다.</returns>
        public MapWaveGroupData FindGroup(MapWaveScenarioData scenario, int groupUid)
        {
            if (scenario?.Groups == null)
            {
                return null;
            }

            for (int i = 0; i < scenario.Groups.Count; i++)
            {
                MapWaveGroupData group = scenario.Groups[i];
                if (group != null && group.GroupUid == groupUid)
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// 웨이브 그룹에 몬스터 스폰 데이터를 추가합니다.
        /// </summary>
        /// <param name="group">대상 웨이브 그룹입니다.</param>
        /// <param name="monsterUid">생성할 몬스터 UID입니다.</param>
        /// <param name="spawnPointId">기준 스폰 포인트 ID입니다.</param>
        /// <returns>추가된 몬스터 스폰 데이터입니다.</returns>
        public MapWaveMonsterSpawnData AddMonsterSpawn(MapWaveGroupData group, int monsterUid, int spawnPointId)
        {
            if (group == null || monsterUid <= 0)
            {
                return null;
            }

            group.Monsters ??= new List<MapWaveMonsterSpawnData>();

            MapWaveMonsterSpawnData monsterSpawn = new MapWaveMonsterSpawnData
            {
                MonsterUid = monsterUid,
                SpawnPointId = spawnPointId,
                Count = 1,
                SpawnIntervalSeconds = 0f,
                DefaultVisible = true,
                CanMoveX = true,
                CanMoveY = true,
                MapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling
            };

            group.Monsters.Add(monsterSpawn);
            return monsterSpawn;
        }

        /// <summary>
        /// 그룹에서 지정 인덱스의 몬스터 스폰 데이터를 제거합니다.
        /// </summary>
        /// <param name="group">대상 웨이브 그룹입니다.</param>
        /// <param name="index">제거할 몬스터 스폰 데이터 인덱스입니다.</param>
        /// <returns>데이터를 제거했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool RemoveMonsterSpawn(MapWaveGroupData group, int index)
        {
            if (group?.Monsters == null || index < 0 || index >= group.Monsters.Count)
            {
                return false;
            }

            group.Monsters.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 현재 씬 선택 상태를 기준으로 새 스폰 포인트에 사용할 기본 위치를 계산합니다.
        /// 선택된 오브젝트가 있으면 그 위치를 사용하고, 없으면 현재 맵 루트 위치를 사용합니다.
        /// </summary>
        /// <returns>스폰 포인트 기본 위치입니다.</returns>
        public Vector3 ResolveDefaultSpawnPointPosition()
        {
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject)
            {
                return selectedObject.transform.position;
            }

            return _defaultMap ? _defaultMap.transform.position : Vector3.zero;
        }

        /// <summary>
        /// 에디터 편집 중인 웨이브 데이터를 모두 비웁니다.
        /// </summary>
        private void ClearData()
        {
            _waveSpawnDataList.WaveScenarios.Clear();
        }

        /// <summary>
        /// 모든 시나리오의 기본값, 참조 컬렉션, 정렬값을 저장 가능한 상태로 정리합니다.
        /// </summary>
        /// <param name="mapUid">적용할 맵 UID입니다. 0이면 기존 값을 유지합니다.</param>
        private void NormalizeAllScenarios(int mapUid)
        {
            if (_waveSpawnDataList.WaveScenarios == null)
            {
                _waveSpawnDataList.WaveScenarios = new List<MapWaveScenarioData>();
            }

            for (int i = _waveSpawnDataList.WaveScenarios.Count - 1; i >= 0; i--)
            {
                MapWaveScenarioData scenario = _waveSpawnDataList.WaveScenarios[i];
                if (scenario == null)
                {
                    _waveSpawnDataList.WaveScenarios.RemoveAt(i);
                    continue;
                }

                NormalizeScenario(scenario, mapUid);
            }

            _waveSpawnDataList.WaveScenarios.Sort(CompareScenario);
        }

        /// <summary>
        /// 단일 시나리오의 기본값과 내부 목록을 정규화합니다.
        /// </summary>
        /// <param name="scenario">정규화할 시나리오입니다.</param>
        /// <param name="mapUid">적용할 맵 UID입니다. 0이면 기존 값을 유지합니다.</param>
        private static void NormalizeScenario(MapWaveScenarioData scenario, int mapUid)
        {
            EnsureScenarioCollections(scenario);

            if (mapUid > 0)
            {
                scenario.MapUid = mapUid;
            }

            if (scenario.ScenarioUid <= 0)
            {
                scenario.ScenarioUid = 1;
            }

            if (scenario.StartDelaySeconds < 0f)
            {
                scenario.StartDelaySeconds = 0f;
            }

            for (int i = scenario.SpawnPoints.Count - 1; i >= 0; i--)
            {
                MapWaveSpawnPointData spawnPoint = scenario.SpawnPoints[i];
                if (spawnPoint == null)
                {
                    scenario.SpawnPoints.RemoveAt(i);
                    continue;
                }

                if (spawnPoint.PointId <= 0)
                {
                    spawnPoint.PointId = i + 1;
                }

                if (spawnPoint.RandomRadius < 0f)
                {
                    spawnPoint.RandomRadius = 0f;
                }
            }

            if (scenario.StartPointId <= 0 && scenario.SpawnPoints.Count > 0)
            {
                scenario.StartPointId = scenario.SpawnPoints[0].PointId;
            }

            for (int i = scenario.Groups.Count - 1; i >= 0; i--)
            {
                MapWaveGroupData group = scenario.Groups[i];
                if (group == null)
                {
                    scenario.Groups.RemoveAt(i);
                    continue;
                }

                NormalizeGroup(group, i);
            }

            scenario.Groups.Sort(CompareGroup);
        }

        /// <summary>
        /// 단일 웨이브 그룹의 기본값과 몬스터 스폰 목록을 정규화합니다.
        /// </summary>
        /// <param name="group">정규화할 그룹입니다.</param>
        /// <param name="index">그룹 목록 내 인덱스입니다.</param>
        private static void NormalizeGroup(MapWaveGroupData group, int index)
        {
            if (group.GroupUid <= 0)
            {
                group.GroupUid = index + 1;
            }

            if (group.Order <= 0)
            {
                group.Order = index + 1;
            }

            if (group.RepeatCount == 0 || group.RepeatCount < -1)
            {
                group.RepeatCount = 1;
            }

            if (group.NextAfterSeconds < 0f)
            {
                group.NextAfterSeconds = 0f;
            }

            if (group.NextDelaySeconds < 0f)
            {
                group.NextDelaySeconds = 0f;
            }

            group.Monsters ??= new List<MapWaveMonsterSpawnData>();
            for (int i = group.Monsters.Count - 1; i >= 0; i--)
            {
                MapWaveMonsterSpawnData monsterSpawn = group.Monsters[i];
                if (monsterSpawn == null || monsterSpawn.MonsterUid <= 0)
                {
                    group.Monsters.RemoveAt(i);
                    continue;
                }

                if (monsterSpawn.Count <= 0)
                {
                    monsterSpawn.Count = 1;
                }

                if (monsterSpawn.SpawnIntervalSeconds < 0f)
                {
                    monsterSpawn.SpawnIntervalSeconds = 0f;
                }

                if (!monsterSpawn.HasCombatProfileUidOverride)
                {
                    monsterSpawn.CombatProfileUidOverride = 0;
                }
                else if (monsterSpawn.CombatProfileUidOverride < 0)
                {
                    monsterSpawn.CombatProfileUidOverride = 0;
                }
            }
        }

        /// <summary>
        /// 시나리오 내부 컬렉션을 null이 아닌 상태로 보정합니다.
        /// </summary>
        /// <param name="scenario">보정할 시나리오입니다.</param>
        private static void EnsureScenarioCollections(MapWaveScenarioData scenario)
        {
            if (scenario == null)
            {
                return;
            }

            scenario.SpawnPoints ??= new List<MapWaveSpawnPointData>();
            scenario.Groups ??= new List<MapWaveGroupData>();
        }

        /// <summary>
        /// 새 시나리오 UID를 계산합니다.
        /// </summary>
        /// <returns>현재 목록에서 사용하지 않는 다음 시나리오 UID입니다.</returns>
        private int GetNextScenarioUid()
        {
            int maxUid = 0;
            List<MapWaveScenarioData> scenarios = _waveSpawnDataList.WaveScenarios;
            if (scenarios != null)
            {
                for (int i = 0; i < scenarios.Count; i++)
                {
                    maxUid = Mathf.Max(maxUid, scenarios[i]?.ScenarioUid ?? 0);
                }
            }

            return maxUid + 1;
        }

        /// <summary>
        /// 지정 시나리오에서 새 스폰 포인트 ID를 계산합니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <returns>현재 시나리오에서 사용하지 않는 다음 스폰 포인트 ID입니다.</returns>
        private static int GetNextSpawnPointId(MapWaveScenarioData scenario)
        {
            int maxId = 0;
            if (scenario?.SpawnPoints != null)
            {
                for (int i = 0; i < scenario.SpawnPoints.Count; i++)
                {
                    maxId = Mathf.Max(maxId, scenario.SpawnPoints[i]?.PointId ?? 0);
                }
            }

            return maxId + 1;
        }

        /// <summary>
        /// 지정 시나리오에서 새 그룹 UID를 계산합니다.
        /// </summary>
        /// <param name="scenario">대상 시나리오입니다.</param>
        /// <returns>현재 시나리오에서 사용하지 않는 다음 그룹 UID입니다.</returns>
        private static int GetNextGroupUid(MapWaveScenarioData scenario)
        {
            int maxUid = 0;
            if (scenario?.Groups != null)
            {
                for (int i = 0; i < scenario.Groups.Count; i++)
                {
                    maxUid = Mathf.Max(maxUid, scenario.Groups[i]?.GroupUid ?? 0);
                }
            }

            return maxUid + 1;
        }

        /// <summary>
        /// 웨이브 데이터에서 사용하는 모든 몬스터 프리팹에 현재 맵 Addressables 라벨을 부여합니다.
        /// </summary>
        /// <param name="struckTableMap">현재 맵 테이블 데이터입니다.</param>
        private void ApplyMonsterAddressableLabels(StruckTableMap struckTableMap)
        {
            if (struckTableMap == null || string.IsNullOrEmpty(struckTableMap.FolderName))
            {
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings || _tableMonster == null || _tableAnimation == null)
            {
                return;
            }

            HashSet<int> monsterUids = CollectMonsterUids();
            string labelName = ConfigAddressableMap.GetLabel(struckTableMap.FolderName);
            foreach (int monsterUid in monsterUids)
            {
                var monsterInfo = _tableMonster.GetDataByUid(monsterUid);
                if (monsterInfo == null)
                {
                    continue;
                }

                var animationInfo = _tableAnimation.GetDataByUid(monsterInfo.AnimationUid);
                if (animationInfo == null)
                {
                    continue;
                }

                string assetPath = ConfigAddressableMap.GetPathCharacter(animationInfo, true);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                entry?.SetLabel(labelName, true, true);
            }
        }

        /// <summary>
        /// 현재 웨이브 데이터에서 참조하는 몬스터 UID 목록을 수집합니다.
        /// </summary>
        /// <returns>중복이 제거된 몬스터 UID 집합입니다.</returns>
        private HashSet<int> CollectMonsterUids()
        {
            HashSet<int> result = new HashSet<int>();
            List<MapWaveScenarioData> scenarios = _waveSpawnDataList.WaveScenarios;
            if (scenarios == null)
            {
                return result;
            }

            for (int scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
            {
                MapWaveScenarioData scenario = scenarios[scenarioIndex];
                if (scenario?.Groups == null)
                {
                    continue;
                }

                for (int groupIndex = 0; groupIndex < scenario.Groups.Count; groupIndex++)
                {
                    MapWaveGroupData group = scenario.Groups[groupIndex];
                    if (group?.Monsters == null)
                    {
                        continue;
                    }

                    for (int monsterIndex = 0; monsterIndex < group.Monsters.Count; monsterIndex++)
                    {
                        int monsterUid = group.Monsters[monsterIndex]?.MonsterUid ?? 0;
                        if (monsterUid > 0)
                        {
                            result.Add(monsterUid);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 시나리오 정렬 순서를 비교합니다.
        /// </summary>
        /// <param name="left">왼쪽 시나리오입니다.</param>
        /// <param name="right">오른쪽 시나리오입니다.</param>
        /// <returns>정렬 비교 결과입니다.</returns>
        private static int CompareScenario(MapWaveScenarioData left, MapWaveScenarioData right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.ScenarioUid.CompareTo(right.ScenarioUid);
        }

        /// <summary>
        /// 그룹 정렬 순서를 비교합니다.
        /// </summary>
        /// <param name="left">왼쪽 그룹입니다.</param>
        /// <param name="right">오른쪽 그룹입니다.</param>
        /// <returns>정렬 비교 결과입니다.</returns>
        private static int CompareGroup(MapWaveGroupData left, MapWaveGroupData right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int orderCompare = left.Order.CompareTo(right.Order);
            return orderCompare != 0
                ? orderCompare
                : left.GroupUid.CompareTo(right.GroupUid);
        }
    }
}
