using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 맵의 웨이브 스폰 데이터를 로드하고 웨이브 시나리오 진행을 관리합니다.
    /// </summary>
    public sealed class MapWaveSpawnController
    {
        private readonly Dictionary<int, MapWaveScenarioRuntime> _scenarioByUid =
            new Dictionary<int, MapWaveScenarioRuntime>();
        private readonly Dictionary<int, MapWaveScenarioData> _loadedScenarioDataByUid =
            new Dictionary<int, MapWaveScenarioData>();
        private readonly HashSet<int> _scheduledScenarioUids = new HashSet<int>();
        private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();
        private readonly WaveMonsterOwnershipRegistry _ownershipRegistry = new WaveMonsterOwnershipRegistry();

        private const string DebugLogPrefix = "[MapWave]";

        private MapManager _mapManager;
        private MapLoadCharacters _mapLoadCharacters;
        private MapTileCommon _mapTileCommon;
        private StruckTableMap _currentMapTableData;
        private int _nextGroupInstanceId;

        /// <summary>
        /// 현재 실행 중인 웨이브 시나리오가 하나라도 있는지 여부입니다.
        /// </summary>
        public bool HasRunningScenario => _scenarioByUid.Count > 0;

        /// <summary>
        /// 웨이브 진행 추적 로그를 Unity Console에 출력할지 여부입니다.
        /// </summary>
        public bool DebugLogEnabled { get; set; } = true;

        /// <summary>
        /// 웨이브 스폰 컨트롤러에 맵 관리자와 캐릭터 스폰 서비스를 연결합니다.
        /// </summary>
        /// <param name="mapManager">현재 맵을 관리하는 맵 매니저입니다.</param>
        /// <param name="mapLoadCharacters">몬스터 생성 API를 제공하는 맵 캐릭터 로더입니다.</param>
        public void Initialize(MapManager mapManager, MapLoadCharacters mapLoadCharacters)
        {
            _mapManager = mapManager;
            _mapLoadCharacters = mapLoadCharacters;
        }

        /// <summary>
        /// 현재 맵의 웨이브 진행 상태와 소유권 정보를 모두 초기화합니다.
        /// </summary>
        public void Reset()
        {
            StopTrackedCoroutines();
            _scenarioByUid.Clear();
            _loadedScenarioDataByUid.Clear();
            _scheduledScenarioUids.Clear();
            _ownershipRegistry.Clear();
            _mapTileCommon = null;
            _currentMapTableData = null;
            _nextGroupInstanceId = 0;
        }

        /// <summary>
        /// 현재 맵의 wave_spawn.json을 로드하고 AutoStart 시나리오를 예약합니다.
        /// </summary>
        /// <param name="mapTileCommon">웨이브 몬스터를 배치할 현재 맵 루트입니다.</param>
        /// <param name="currentMapTableData">현재 맵 테이블 데이터입니다.</param>
        public async Task LoadWaveSpawnAsync(MapTileCommon mapTileCommon, StruckTableMap currentMapTableData)
        {
            Reset();

            if (mapTileCommon == null || currentMapTableData == null)
            {
                return;
            }

            _mapTileCommon = mapTileCommon;
            _currentMapTableData = currentMapTableData;

            MapWaveSpawnDataList dataList = await LoadWaveSpawnDataAsync(currentMapTableData);
            if (dataList?.WaveScenarios == null || dataList.WaveScenarios.Count == 0)
            {
                LogDebug($"웨이브 스폰 데이터가 없습니다. mapUid:{currentMapTableData.Uid}, folder:{currentMapTableData.FolderName}");
                return;
            }

            int validScenarioCount = 0;
            foreach (MapWaveScenarioData scenarioData in dataList.WaveScenarios)
            {
                if (!IsScenarioValidForCurrentMap(scenarioData, currentMapTableData.Uid))
                {
                    continue;
                }

                _loadedScenarioDataByUid[scenarioData.ScenarioUid] = scenarioData;
                validScenarioCount++;

                if (scenarioData.AutoStart)
                {
                    StartScenario(scenarioData, Mathf.Max(0f, scenarioData.StartDelaySeconds));
                }
            }

            LogDebug($"웨이브 스폰 데이터 로드 완료. mapUid:{currentMapTableData.Uid}, scenarioCount:{validScenarioCount}");
        }

        /// <summary>
        /// 지정 시나리오 UID의 웨이브를 수동으로 시작합니다.
        /// </summary>
        /// <param name="scenarioUid">시작할 웨이브 시나리오 UID입니다.</param>
        /// <returns>시나리오 시작 요청을 등록했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryStartScenario(int scenarioUid)
        {
            if (scenarioUid <= 0 || IsScenarioAlreadyScheduledOrRunning(scenarioUid))
            {
                return false;
            }

            MapWaveScenarioData scenarioData = FindScenarioData(scenarioUid);
            if (scenarioData == null)
            {
                return false;
            }

            LogDebug($"웨이브 시나리오 수동 시작 요청. scenarioUid:{scenarioUid}");
            StartScenario(scenarioData, Mathf.Max(0f, scenarioData.StartDelaySeconds));
            return true;
        }


        /// <summary>
        /// 실행 중인 웨이브 시나리오가 일반 배치 몬스터 리젠 억제를 요구하는지 확인합니다.
        /// </summary>
        /// <returns>일반 몬스터 리젠을 억제해야 하면 <see langword="true"/>를 반환합니다.</returns>
        public bool ShouldSuppressNormalMonsterRespawn()
        {
            foreach (MapWaveScenarioRuntime scenarioRuntime in _scenarioByUid.Values)
            {
                if (scenarioRuntime != null &&
                    !scenarioRuntime.IsCompleted &&
                    scenarioRuntime.Data.SuppressNormalMonsterRespawnWhileRunning)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 웨이브 소유 몬스터 사망을 컨트롤러에 반영합니다.
        /// </summary>
        /// <param name="monsterVid">사망한 몬스터 VID입니다.</param>
        /// <returns>웨이브 소유 몬스터로 처리했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryHandleWaveMonsterDead(int monsterVid)
        {
            if (!_ownershipRegistry.TryGet(monsterVid, out WaveMonsterOwnership ownership))
            {
                return false;
            }

            _ownershipRegistry.Unregister(monsterVid);
            LogDebug($"웨이브 몬스터 사망 감지. monsterVid:{monsterVid}, scenarioUid:{ownership.ScenarioUid}, groupUid:{ownership.GroupUid}, groupInstanceId:{ownership.GroupInstanceId}");

            if (!_scenarioByUid.TryGetValue(ownership.ScenarioUid, out MapWaveScenarioRuntime scenarioRuntime))
            {
                LogDebug($"사망 몬스터의 웨이브 시나리오가 이미 종료되었습니다. monsterVid:{monsterVid}, scenarioUid:{ownership.ScenarioUid}");
                return true;
            }

            if (!scenarioRuntime.TryGetActiveGroup(ownership.GroupInstanceId, out MapWaveGroupRuntime groupRuntime))
            {
                LogDebug($"사망 몬스터의 웨이브 그룹이 이미 정리되었습니다. monsterVid:{monsterVid}, groupInstanceId:{ownership.GroupInstanceId}");
                return true;
            }

            bool removed = groupRuntime.MarkMonsterDead(monsterVid);
            LogDebug($"웨이브 그룹 생존 수 갱신. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, removed:{removed}, alive:{groupRuntime.AliveCount}");
            HandleGroupClearedIfNeeded(scenarioRuntime, groupRuntime);
            return true;
        }

        /// <summary>
        /// wave_spawn.json 데이터를 Addressables에서 읽어 역직렬화합니다.
        /// </summary>
        /// <param name="currentMapTableData">현재 맵 테이블 데이터입니다.</param>
        /// <returns>로드된 웨이브 스폰 데이터입니다. 파일이 없거나 비어 있으면 null입니다.</returns>
        private static async Task<MapWaveSpawnDataList> LoadWaveSpawnDataAsync(StruckTableMap currentMapTableData)
        {
            string key = ConfigAddressableMap.GetKeyJsonWaveSpawn(currentMapTableData.FolderName);
            try
            {
                if (!await HasAddressableLocationAsync<TextAsset>(key))
                {
                    return null;
                }

                TextAsset textFile = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);
                if (textFile == null || string.IsNullOrEmpty(textFile.text))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<MapWaveSpawnDataList>(textFile.text);
            }
            catch (Exception ex)
            {
                // 웨이브 파일은 선택 데이터이므로 맵 로드를 실패시키지 않고 현재 맵의 웨이브만 건너뜁니다.
                GcLogger.LogWarning($"웨이브 스폰 json 로드를 건너뜁니다. file {key}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 선택 Addressables 리소스가 현재 빌드/에디터 카탈로그에 존재하는지 확인합니다.
        /// </summary>
        /// <typeparam name="T">조회할 Unity 리소스 타입입니다.</typeparam>
        /// <param name="key">조회할 Addressables 키입니다.</param>
        /// <returns>로드 가능한 위치가 하나 이상 있으면 <see langword="true"/>를 반환합니다.</returns>
        private static async Task<bool> HasAddressableLocationAsync<T>(string key) where T : UnityEngine.Object
        {
            AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle =
                Addressables.LoadResourceLocationsAsync(key, typeof(T));
            await handle.Task;

            bool hasLocation = handle.Status == AsyncOperationStatus.Succeeded &&
                               handle.Result != null &&
                               handle.Result.Count > 0;
            Addressables.Release(handle);
            return hasLocation;
        }

        /// <summary>
        /// 시나리오 데이터가 현재 맵에서 실행 가능한지 검사합니다.
        /// </summary>
        /// <param name="scenarioData">검사할 시나리오 데이터입니다.</param>
        /// <param name="currentMapUid">현재 맵 UID입니다.</param>
        /// <returns>현재 맵에서 실행 가능하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsScenarioValidForCurrentMap(MapWaveScenarioData scenarioData, int currentMapUid)
        {
            if (scenarioData == null || scenarioData.ScenarioUid <= 0)
            {
                return false;
            }

            return scenarioData.MapUid <= 0 || scenarioData.MapUid == currentMapUid;
        }

        /// <summary>
        /// 로드된 현재 맵 데이터에서 지정 시나리오 UID를 검색합니다.
        /// </summary>
        /// <param name="scenarioUid">검색할 시나리오 UID입니다.</param>
        /// <returns>시나리오 데이터입니다. 없으면 null입니다.</returns>
        private MapWaveScenarioData FindScenarioData(int scenarioUid)
        {
            return _loadedScenarioDataByUid.TryGetValue(scenarioUid, out MapWaveScenarioData scenarioData)
                ? scenarioData
                : null;
        }

        /// <summary>
        /// 웨이브 시나리오 시작 코루틴을 등록합니다.
        /// </summary>
        /// <param name="scenarioData">시작할 시나리오 데이터입니다.</param>
        /// <param name="delaySeconds">시작 전 대기 시간입니다.</param>
        private void StartScenario(MapWaveScenarioData scenarioData, float delaySeconds)
        {
            if (_mapManager == null || scenarioData == null || IsScenarioAlreadyScheduledOrRunning(scenarioData.ScenarioUid))
            {
                return;
            }

            _scheduledScenarioUids.Add(scenarioData.ScenarioUid);
            LogDebug($"웨이브 시나리오 시작 예약. scenarioUid:{scenarioData.ScenarioUid}, delay:{delaySeconds:F2}");
            Coroutine routine = _mapManager.StartCoroutine(StartScenarioCoroutine(scenarioData, delaySeconds));
            TrackCoroutine(routine);
        }

        /// <summary>
        /// 시작 지연 후 첫 번째 웨이브 그룹을 실행합니다.
        /// </summary>
        /// <param name="scenarioData">시작할 시나리오 데이터입니다.</param>
        /// <param name="delaySeconds">시작 전 대기 시간입니다.</param>
        private IEnumerator StartScenarioCoroutine(MapWaveScenarioData scenarioData, float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            if (!CanRunWave())
            {
                LogDebug($"웨이브 시나리오 시작 취소. 필수 맵 참조가 없습니다. scenarioUid:{scenarioData.ScenarioUid}");
                _scheduledScenarioUids.Remove(scenarioData.ScenarioUid);
                yield break;
            }

            MapWaveScenarioRuntime scenarioRuntime = new MapWaveScenarioRuntime(scenarioData);
            MapWaveGroupData firstGroup = scenarioRuntime.GetFirstGroup();
            if (firstGroup == null)
            {
                GcLogger.LogWarning($"웨이브 시나리오에 실행할 그룹이 없습니다. scenarioUid: {scenarioData.ScenarioUid}");
                _scheduledScenarioUids.Remove(scenarioData.ScenarioUid);
                yield break;
            }

            _scenarioByUid[scenarioData.ScenarioUid] = scenarioRuntime;
            LogDebug($"웨이브 시나리오 시작. scenarioUid:{scenarioData.ScenarioUid}, firstGroupUid:{firstGroup.GroupUid}");
            StartGroup(scenarioRuntime, firstGroup, 0);
        }

        /// <summary>
        /// 웨이브 그룹 스폰 코루틴을 시작합니다.
        /// </summary>
        /// <param name="scenarioRuntime">그룹이 속한 시나리오 런타임입니다.</param>
        /// <param name="groupData">실행할 그룹 데이터입니다.</param>
        /// <param name="repeatIndex">반복 실행 회차입니다.</param>
        private void StartGroup(MapWaveScenarioRuntime scenarioRuntime, MapWaveGroupData groupData, int repeatIndex)
        {
            if (!CanRunWave() || scenarioRuntime == null || groupData == null)
            {
                return;
            }

            int groupInstanceId = ++_nextGroupInstanceId;
            MapWaveGroupRuntime groupRuntime = new MapWaveGroupRuntime(
                groupInstanceId,
                scenarioRuntime,
                groupData,
                repeatIndex);

            scenarioRuntime.AddActiveGroup(groupRuntime);
            LogDebug($"웨이브 그룹 시작. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupData.GroupUid}, instanceId:{groupInstanceId}, repeatIndex:{repeatIndex}, policy:{groupData.NextPolicy}");

            Coroutine spawnRoutine = _mapManager.StartCoroutine(SpawnGroupCoroutine(scenarioRuntime, groupRuntime));
            TrackCoroutine(spawnRoutine);

            if (UsesTimeBasedNextPolicy(groupData))
            {
                LogDebug($"웨이브 그룹 시간 전환 타이머 시작. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupData.GroupUid}, instanceId:{groupInstanceId}, wait:{Mathf.Max(0f, groupData.NextAfterSeconds):F2}");
                Coroutine timerRoutine = _mapManager.StartCoroutine(WaitTimeThenRequestNextGroup(scenarioRuntime, groupRuntime));
                TrackCoroutine(timerRoutine);
            }
        }

        /// <summary>
        /// 그룹에 설정된 몬스터들을 순서대로 스폰합니다.
        /// </summary>
        /// <param name="scenarioRuntime">그룹이 속한 시나리오 런타임입니다.</param>
        /// <param name="groupRuntime">스폰할 그룹 런타임입니다.</param>
        private IEnumerator SpawnGroupCoroutine(MapWaveScenarioRuntime scenarioRuntime, MapWaveGroupRuntime groupRuntime)
        {
            List<MapWaveMonsterSpawnData> monsters = groupRuntime.Data.Monsters;
            LogDebug($"웨이브 그룹 스폰 시작. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, entryCount:{monsters?.Count ?? 0}");
            if (monsters != null)
            {
                foreach (MapWaveMonsterSpawnData spawnData in monsters)
                {
                    if (spawnData == null)
                    {
                        continue;
                    }

                    int count = Mathf.Max(0, spawnData.Count);
                    for (int i = 0; i < count; i++)
                    {
                        SpawnWaveMonster(scenarioRuntime, groupRuntime, spawnData);

                        if (spawnData.SpawnIntervalSeconds > 0f && i < count - 1)
                        {
                            yield return new WaitForSeconds(spawnData.SpawnIntervalSeconds);
                        }
                    }
                }
            }

            groupRuntime.MarkSpawnCompleted();
            LogDebug($"웨이브 그룹 스폰 완료. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, spawned:{groupRuntime.SpawnedCount}, alive:{groupRuntime.AliveCount}");
            HandleGroupClearedIfNeeded(scenarioRuntime, groupRuntime);
        }

        /// <summary>
        /// 단일 웨이브 몬스터를 생성하고 웨이브 소유권을 등록합니다.
        /// </summary>
        /// <param name="scenarioRuntime">몬스터가 속한 시나리오 런타임입니다.</param>
        /// <param name="groupRuntime">몬스터가 속한 그룹 런타임입니다.</param>
        /// <param name="spawnData">몬스터 스폰 데이터입니다.</param>
        private void SpawnWaveMonster(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime groupRuntime,
            MapWaveMonsterSpawnData spawnData)
        {
            CharacterRegenData regenData = BuildWaveMonsterRegenData(scenarioRuntime.Data, spawnData);
            if (regenData == null)
            {
                return;
            }

            Monster monster = _mapLoadCharacters.SpawnMonster(
                spawnData.MonsterUid,
                regenData,
                _mapTileCommon,
                _currentMapTableData,
                MonsterSpawnRegistrationPolicy.WaveManaged);

            if (monster == null)
            {
                GcLogger.LogWarning(
                    $"웨이브 몬스터 스폰에 실패했습니다. scenarioUid: {scenarioRuntime.Data.ScenarioUid}, " +
                    $"groupUid: {groupRuntime.Data.GroupUid}, monsterUid: {spawnData.MonsterUid}");
                return;
            }

            groupRuntime.RegisterMonster(monster.vid);
            _ownershipRegistry.Register(monster.vid, new WaveMonsterOwnership(
                scenarioRuntime.Data.ScenarioUid,
                groupRuntime.Data.GroupUid,
                groupRuntime.InstanceId));
            LogDebug($"웨이브 몬스터 스폰. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, monsterUid:{spawnData.MonsterUid}, vid:{monster.vid}, alive:{groupRuntime.AliveCount}");
        }

        /// <summary>
        /// 웨이브 몬스터 스폰 데이터를 기존 캐릭터 리젠 데이터 형식으로 변환합니다.
        /// </summary>
        /// <param name="scenarioData">웨이브 시나리오 데이터입니다.</param>
        /// <param name="spawnData">몬스터 스폰 데이터입니다.</param>
        /// <returns>몬스터 생성에 사용할 리젠 데이터입니다. 스폰 포인트가 없으면 null입니다.</returns>
        private CharacterRegenData BuildWaveMonsterRegenData(
            MapWaveScenarioData scenarioData,
            MapWaveMonsterSpawnData spawnData)
        {
            if (spawnData == null || spawnData.MonsterUid <= 0)
            {
                return null;
            }

            MapWaveSpawnPointData spawnPoint = FindSpawnPoint(scenarioData, spawnData.SpawnPointId);
            if (spawnPoint == null)
            {
                GcLogger.LogWarning(
                    $"웨이브 스폰 포인트를 찾을 수 없습니다. scenarioUid: {scenarioData.ScenarioUid}, " +
                    $"spawnPointId: {spawnData.SpawnPointId}, monsterUid: {spawnData.MonsterUid}");
                return null;
            }

            Vector3 spawnPosition = ResolveSpawnPosition(spawnPoint, spawnData);
            return new CharacterRegenData(
                spawnData.MonsterUid,
                spawnPosition,
                spawnData.IsFlip,
                _currentMapTableData.Uid,
                spawnData.DefaultVisible,
                spawnData.MoveStep,
                spawnData.MoveSpeed,
                spawnData.CanMoveX,
                spawnData.CanMoveY,
                null,
                ResolveVisibilityPolicy(spawnPoint, spawnData));
        }

        /// <summary>
        /// 시나리오에서 지정 스폰 포인트를 검색합니다.
        /// </summary>
        /// <param name="scenarioData">검색 대상 시나리오 데이터입니다.</param>
        /// <param name="spawnPointId">검색할 스폰 포인트 ID입니다.</param>
        /// <returns>검색된 스폰 포인트입니다. 없으면 null입니다.</returns>
        private static MapWaveSpawnPointData FindSpawnPoint(MapWaveScenarioData scenarioData, int spawnPointId)
        {
            if (scenarioData?.SpawnPoints == null)
            {
                return null;
            }

            foreach (MapWaveSpawnPointData spawnPoint in scenarioData.SpawnPoints)
            {
                if (spawnPoint != null && spawnPoint.PointId == spawnPointId)
                {
                    return spawnPoint;
                }
            }

            return null;
        }

        /// <summary>
        /// 스폰 포인트와 몬스터 오프셋을 합산하여 최종 생성 위치를 계산합니다.
        /// </summary>
        /// <param name="spawnPoint">기준 스폰 포인트입니다.</param>
        /// <param name="spawnData">몬스터 스폰 데이터입니다.</param>
        /// <returns>최종 생성 위치입니다.</returns>
        private static Vector3 ResolveSpawnPosition(MapWaveSpawnPointData spawnPoint, MapWaveMonsterSpawnData spawnData)
        {
            Vector2 randomOffset = Vector2.zero;
            if (spawnPoint.RandomRadius > 0f)
            {
                randomOffset = Random.insideUnitCircle * spawnPoint.RandomRadius;
            }

            return new Vector3(
                spawnPoint.x + spawnData.OffsetX + randomOffset.x,
                spawnPoint.y + spawnData.OffsetY + randomOffset.y,
                spawnPoint.z + spawnData.OffsetZ);
        }

        /// <summary>
        /// 몬스터별 표시 정책을 우선하고, 기본값이면 스폰 포인트 표시 정책을 사용합니다.
        /// </summary>
        /// <param name="spawnPoint">스폰 포인트 데이터입니다.</param>
        /// <param name="spawnData">몬스터 스폰 데이터입니다.</param>
        /// <returns>최종 맵 표시 정책입니다.</returns>
        private static MapCharacterVisibilityPolicy ResolveVisibilityPolicy(
            MapWaveSpawnPointData spawnPoint,
            MapWaveMonsterSpawnData spawnData)
        {
            return spawnData.MapVisibilityPolicy != MapCharacterVisibilityPolicy.DefaultCulling
                ? spawnData.MapVisibilityPolicy
                : spawnPoint.MapVisibilityPolicy;
        }

        /// <summary>
        /// 시간 기반 다음 그룹 전환 정책의 대기 시간이 끝나면 다음 그룹 전환을 요청합니다.
        /// </summary>
        /// <param name="scenarioRuntime">그룹이 속한 시나리오 런타임입니다.</param>
        /// <param name="groupRuntime">전환 기준 그룹 런타임입니다.</param>
        private IEnumerator WaitTimeThenRequestNextGroup(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime groupRuntime)
        {
            float waitSeconds = Mathf.Max(0f, groupRuntime.Data.NextAfterSeconds);
            if (waitSeconds > 0f)
            {
                yield return new WaitForSeconds(waitSeconds);
            }

            LogDebug($"웨이브 그룹 시간 전환 조건 만족. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, wait:{waitSeconds:F2}");
            RequestNextGroup(scenarioRuntime, groupRuntime, WaveNextTriggerReason.TimerElapsed);
        }

        /// <summary>
        /// 그룹 클리어 조건을 확인하고 정책에 맞게 다음 그룹 전환 또는 정리를 처리합니다.
        /// </summary>
        /// <param name="scenarioRuntime">그룹이 속한 시나리오 런타임입니다.</param>
        /// <param name="groupRuntime">상태를 검사할 그룹 런타임입니다.</param>
        private void HandleGroupClearedIfNeeded(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime groupRuntime)
        {
            if (scenarioRuntime == null || groupRuntime == null || !groupRuntime.IsCleared)
            {
                return;
            }

            if (!groupRuntime.IsNextRequested && ShouldRequestNextWhenAllDead(groupRuntime.Data.NextPolicy))
            {
                LogDebug($"웨이브 그룹 전체 처치 조건 만족. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}");
                RequestNextGroup(scenarioRuntime, groupRuntime, WaveNextTriggerReason.AllMonstersDead);
            }

            if (CanRemoveClearedGroup(groupRuntime))
            {
                RemoveGroupRuntime(scenarioRuntime, groupRuntime);
                TryCompleteScenario(scenarioRuntime);
            }
        }

        /// <summary>
        /// 다음 그룹 전환 코루틴을 요청합니다.
        /// </summary>
        /// <param name="scenarioRuntime">전환할 시나리오 런타임입니다.</param>
        /// <param name="groupRuntime">전환 기준 그룹 런타임입니다.</param>
        /// <param name="reason">전환을 요청한 원인입니다.</param>
        private void RequestNextGroup(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime groupRuntime,
            WaveNextTriggerReason reason)
        {
            if (scenarioRuntime == null || groupRuntime == null || groupRuntime.IsNextRequested)
            {
                return;
            }

            groupRuntime.MarkNextRequested();
            scenarioRuntime.IncrementPendingTransition();
            LogDebug($"웨이브 다음 그룹 전환 요청. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, reason:{reason}, pending:{scenarioRuntime.PendingTransitionCount}");

            MapWaveGroupData nextGroup = ResolveNextGroupForTransition(scenarioRuntime, groupRuntime);
            NotifyWaveTransitionRequested(scenarioRuntime, groupRuntime, nextGroup, reason);

            Coroutine routine = _mapManager.StartCoroutine(ProceedToNextGroupCoroutine(scenarioRuntime, groupRuntime));
            TrackCoroutine(routine);

            if (groupRuntime.IsCleared)
            {
                RemoveGroupRuntime(scenarioRuntime, groupRuntime);
            }
        }

        /// <summary>
        /// 반복 설정과 명시 다음 그룹 설정을 고려하여 실제 다음 실행 그룹을 계산합니다.
        /// </summary>
        /// <param name="scenarioRuntime">전환할 시나리오 런타임입니다.</param>
        /// <param name="previousGroup">전환 기준 이전 그룹입니다.</param>
        /// <returns>다음에 실행할 그룹이며, 시나리오가 종료되면 null입니다.</returns>
        private static MapWaveGroupData ResolveNextGroupForTransition(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime previousGroup)
        {
            if (scenarioRuntime == null || previousGroup == null)
            {
                return null;
            }

            if (TryResolveRepeatedGroup(previousGroup, out MapWaveGroupData repeatedGroup, out _))
            {
                return repeatedGroup;
            }

            return scenarioRuntime.GetNextGroup(previousGroup.Data);
        }

        /// <summary>
        /// 다음 그룹과 이동 유도 위치를 계산하여 맵 매니저의 범용 웨이브 전환 이벤트를 발행합니다.
        /// </summary>
        /// <param name="scenarioRuntime">전환할 시나리오 런타임입니다.</param>
        /// <param name="previousGroup">전환 기준 이전 그룹입니다.</param>
        /// <param name="nextGroup">다음에 실행할 그룹입니다.</param>
        /// <param name="reason">전환을 요청한 원인입니다.</param>
        private void NotifyWaveTransitionRequested(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime previousGroup,
            MapWaveGroupData nextGroup,
            WaveNextTriggerReason reason)
        {
            if (_mapManager == null || scenarioRuntime == null || previousGroup?.Data == null)
            {
                return;
            }

            bool hasNavigationPosition = TryResolveGroupNavigationPosition(
                scenarioRuntime.Data,
                nextGroup,
                out Vector3 navigationPosition);
            MapWaveTransitionContext context = new MapWaveTransitionContext(
                scenarioRuntime.Data.ScenarioUid,
                previousGroup.Data.GroupUid,
                nextGroup?.GroupUid ?? 0,
                previousGroup.Data.NextPolicy,
                reason,
                previousGroup.Data.NextDelaySeconds,
                hasNavigationPosition,
                navigationPosition);
            _mapManager.NotifyWaveTransitionRequested(context);
        }

        /// <summary>
        /// 그룹의 명시 이동 유도 포인트를 우선 사용하고, 없으면 몬스터 스폰 위치의 가중 평균을 계산합니다.
        /// </summary>
        /// <param name="scenarioData">스폰 포인트 목록을 가진 시나리오입니다.</param>
        /// <param name="groupData">이동 유도 위치를 계산할 다음 그룹입니다.</param>
        /// <param name="navigationPosition">계산된 이동 유도 위치입니다.</param>
        /// <returns>유효한 위치를 계산했으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryResolveGroupNavigationPosition(
            MapWaveScenarioData scenarioData,
            MapWaveGroupData groupData,
            out Vector3 navigationPosition)
        {
            navigationPosition = Vector3.zero;
            if (scenarioData == null || groupData == null)
            {
                return false;
            }

            if (groupData.NavigationPointId > 0)
            {
                MapWaveSpawnPointData navigationPoint =
                    FindSpawnPoint(scenarioData, groupData.NavigationPointId);
                if (navigationPoint != null)
                {
                    navigationPosition = new Vector3(
                        navigationPoint.x,
                        navigationPoint.y,
                        navigationPoint.z);
                    return true;
                }
            }

            if (groupData.Monsters == null)
            {
                return false;
            }

            Vector3 weightedPositionSum = Vector3.zero;
            int totalCount = 0;
            for (int i = 0; i < groupData.Monsters.Count; i++)
            {
                MapWaveMonsterSpawnData monsterSpawn = groupData.Monsters[i];
                if (monsterSpawn == null)
                {
                    continue;
                }

                MapWaveSpawnPointData spawnPoint =
                    FindSpawnPoint(scenarioData, monsterSpawn.SpawnPointId);
                if (spawnPoint == null)
                {
                    continue;
                }

                int count = Mathf.Max(1, monsterSpawn.Count);
                Vector3 spawnPosition = new Vector3(
                    spawnPoint.x + monsterSpawn.OffsetX,
                    spawnPoint.y + monsterSpawn.OffsetY,
                    spawnPoint.z + monsterSpawn.OffsetZ);
                weightedPositionSum += spawnPosition * count;
                totalCount += count;
            }

            if (totalCount <= 0)
            {
                return false;
            }

            navigationPosition = weightedPositionSum / totalCount;
            return true;
        }

        /// <summary>
        /// 전환 지연 후 반복 그룹 또는 다음 그룹을 실행합니다.
        /// </summary>
        /// <param name="scenarioRuntime">전환할 시나리오 런타임입니다.</param>
        /// <param name="previousGroup">전환 기준 이전 그룹입니다.</param>
        private IEnumerator ProceedToNextGroupCoroutine(
            MapWaveScenarioRuntime scenarioRuntime,
            MapWaveGroupRuntime previousGroup)
        {
            float delaySeconds = Mathf.Max(0f, previousGroup.Data.NextDelaySeconds);
            if (delaySeconds > 0f)
            {
                LogDebug($"웨이브 다음 그룹 전환 지연 시작. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{previousGroup.Data.GroupUid}, instanceId:{previousGroup.InstanceId}, delay:{delaySeconds:F2}");
                yield return new WaitForSeconds(delaySeconds);
            }

            scenarioRuntime.DecrementPendingTransition();
            LogDebug($"웨이브 다음 그룹 전환 실행. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, previousGroupUid:{previousGroup.Data.GroupUid}, instanceId:{previousGroup.InstanceId}, pending:{scenarioRuntime.PendingTransitionCount}");

            if (!CanRunWave() || scenarioRuntime.IsCompleted)
            {
                yield break;
            }

            if (TryResolveRepeatedGroup(previousGroup, out MapWaveGroupData repeatedGroup, out int repeatIndex))
            {
                LogDebug($"웨이브 그룹 반복 실행. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{repeatedGroup.GroupUid}, nextRepeatIndex:{repeatIndex}");
                StartGroup(scenarioRuntime, repeatedGroup, repeatIndex);
                yield break;
            }

            MapWaveGroupData nextGroup = scenarioRuntime.GetNextGroup(previousGroup.Data);
            if (nextGroup != null)
            {
                LogDebug($"웨이브 다음 그룹 시작. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, nextGroupUid:{nextGroup.GroupUid}");
                StartGroup(scenarioRuntime, nextGroup, 0);
                yield break;
            }

            TryCompleteScenario(scenarioRuntime);
        }

        /// <summary>
        /// 현재 그룹 반복 실행이 필요한지 계산합니다.
        /// </summary>
        /// <param name="groupRuntime">반복 여부를 확인할 그룹 런타임입니다.</param>
        /// <param name="groupData">반복 실행할 그룹 데이터입니다.</param>
        /// <param name="nextRepeatIndex">다음 반복 회차입니다.</param>
        /// <returns>반복 실행이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryResolveRepeatedGroup(
            MapWaveGroupRuntime groupRuntime,
            out MapWaveGroupData groupData,
            out int nextRepeatIndex)
        {
            groupData = null;
            nextRepeatIndex = 0;

            if (groupRuntime == null || groupRuntime.Data == null)
            {
                return false;
            }

            int repeatCount = groupRuntime.Data.RepeatCount;
            if (repeatCount == 0)
            {
                repeatCount = 1;
            }

            bool shouldRepeat = repeatCount < 0 || groupRuntime.RepeatIndex + 1 < repeatCount;
            if (!shouldRepeat)
            {
                return false;
            }

            groupData = groupRuntime.Data;
            nextRepeatIndex = groupRuntime.RepeatIndex + 1;
            return true;
        }

        /// <summary>
        /// 지정 그룹 정책이 시간 기반 전환을 사용하는지 확인합니다.
        /// </summary>
        /// <param name="groupData">검사할 그룹 데이터입니다.</param>
        /// <returns>시간 기반 전환을 사용하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool UsesTimeBasedNextPolicy(MapWaveGroupData groupData)
        {
            return groupData != null &&
                   (groupData.NextPolicy == WaveNextPolicy.AfterSeconds ||
                    groupData.NextPolicy == WaveNextPolicy.AllDeadOrAfterSeconds);
        }

        /// <summary>
        /// 모든 몬스터 처치 시 다음 그룹 전환을 요청하는 정책인지 확인합니다.
        /// </summary>
        /// <param name="policy">검사할 다음 그룹 전환 정책입니다.</param>
        /// <returns>모두 처치 조건으로 전환해야 하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool ShouldRequestNextWhenAllDead(WaveNextPolicy policy)
        {
            return policy == WaveNextPolicy.WhenAllDead ||
                   policy == WaveNextPolicy.AllDeadOrAfterSeconds;
        }

        /// <summary>
        /// 클리어된 그룹 인스턴스를 활성 목록에서 제거할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="groupRuntime">검사할 그룹 런타임입니다.</param>
        /// <returns>제거할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool CanRemoveClearedGroup(MapWaveGroupRuntime groupRuntime)
        {
            if (groupRuntime == null || !groupRuntime.IsCleared)
            {
                return false;
            }

            // 시간 기반 정책은 시간이 지나기 전에 모두 처치되어도 타이머가 다음 전환을 담당해야 합니다.
            if (groupRuntime.Data.NextPolicy == WaveNextPolicy.AfterSeconds && !groupRuntime.IsNextRequested)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 활성 그룹 목록에서 그룹을 제거하고 생성 몬스터 소유권을 정리합니다.
        /// </summary>
        /// <param name="scenarioRuntime">그룹이 속한 시나리오 런타임입니다.</param>
        /// <param name="groupRuntime">제거할 그룹 런타임입니다.</param>
        private void RemoveGroupRuntime(MapWaveScenarioRuntime scenarioRuntime, MapWaveGroupRuntime groupRuntime)
        {
            scenarioRuntime.RemoveActiveGroup(groupRuntime.InstanceId);
            LogDebug($"웨이브 그룹 정리. scenarioUid:{scenarioRuntime.Data.ScenarioUid}, groupUid:{groupRuntime.Data.GroupUid}, instanceId:{groupRuntime.InstanceId}, spawned:{groupRuntime.SpawnedCount}");

            List<int> spawnedVids = groupRuntime.GetSpawnedMonsterVids();
            foreach (int monsterVid in spawnedVids)
            {
                _ownershipRegistry.Unregister(monsterVid);
            }
        }

        /// <summary>
        /// 시나리오에 남은 활성 그룹과 전환 대기가 없으면 완료 처리합니다.
        /// </summary>
        /// <param name="scenarioRuntime">완료 여부를 검사할 시나리오 런타임입니다.</param>
        private void TryCompleteScenario(MapWaveScenarioRuntime scenarioRuntime)
        {
            if (scenarioRuntime == null || scenarioRuntime.IsCompleted)
            {
                return;
            }

            if (scenarioRuntime.ActiveGroupCount > 0 || scenarioRuntime.PendingTransitionCount > 0)
            {
                return;
            }

            scenarioRuntime.MarkCompleted();
            LogDebug($"웨이브 시나리오 완료. scenarioUid:{scenarioRuntime.Data.ScenarioUid}");
            _scenarioByUid.Remove(scenarioRuntime.Data.ScenarioUid);
            _scheduledScenarioUids.Remove(scenarioRuntime.Data.ScenarioUid);
        }


        /// <summary>
        /// 시나리오가 이미 시작 예약되었거나 실행 중인지 확인합니다.
        /// </summary>
        /// <param name="scenarioUid">검사할 시나리오 UID입니다.</param>
        /// <returns>이미 예약 또는 실행 중이면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsScenarioAlreadyScheduledOrRunning(int scenarioUid)
        {
            return _scheduledScenarioUids.Contains(scenarioUid) || _scenarioByUid.ContainsKey(scenarioUid);
        }

        /// <summary>
        /// 웨이브 실행에 필요한 맵 참조가 유효한지 확인합니다.
        /// </summary>
        /// <returns>웨이브를 실행할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool CanRunWave()
        {
            return _mapManager != null &&
                   _mapLoadCharacters != null &&
                   _mapTileCommon != null &&
                   _currentMapTableData != null;
        }

        /// <summary>
        /// 웨이브 디버그 로그가 활성화되어 있으면 공통 접두사를 붙여 로그를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 로그 본문입니다.</param>
        private void LogDebug(string message)
        {
            if (!DebugLogEnabled || string.IsNullOrEmpty(message))
            {
                return;
            }

            GcLogger.Log($"{DebugLogPrefix} {message}");
        }

        /// <summary>
        /// 실행 중인 코루틴을 추적 목록에 추가합니다.
        /// </summary>
        /// <param name="routine">추적할 코루틴입니다.</param>
        private void TrackCoroutine(Coroutine routine)
        {
            if (routine != null)
            {
                _runningCoroutines.Add(routine);
            }
        }

        /// <summary>
        /// 컨트롤러가 시작한 코루틴들을 중지하고 추적 목록을 비웁니다.
        /// </summary>
        private void StopTrackedCoroutines()
        {
            if (_mapManager != null)
            {
                foreach (Coroutine routine in _runningCoroutines)
                {
                    if (routine != null)
                    {
                        _mapManager.StopCoroutine(routine);
                    }
                }
            }

            _runningCoroutines.Clear();
        }
    }
}
