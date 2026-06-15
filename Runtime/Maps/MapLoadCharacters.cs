using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    public class MapLoadCharacters
    {
        private int _characterVid;
        private MapManager _mapManager;
        private TableMonster _tableMonster;
        private TableLoaderManager _tableLoaderManager;
        private float _defaultMonsterRegenTimeSec;
        private readonly Dictionary<int, CharacterRegenData> _monsterRegenDataByVid = new Dictionary<int, CharacterRegenData>();
        private readonly HashSet<int> _monsterRespawnPending = new HashSet<int>();

        public void Reset()
        {
            _characterVid = 0;
            _monsterRegenDataByVid.Clear();
            _monsterRespawnPending.Clear();
        }

        public void Initialize(MapManager manager)
        {
            _mapManager = manager;
            _characterVid = 0;
            _tableLoaderManager = TableLoaderManager.Instance;
            _tableMonster = _tableLoaderManager.TableMonster;
            _defaultMonsterRegenTimeSec = AddressableLoaderSettings.Instance.settings.defaultMonsterRegenTimeSec;
        }

        public async Task LoadPlayer(Vector3 playSpawnPosition, StruckTableMap currentMapTableData, DefaultMap mapTileCommon)
        {
            try
            {
                if (!SceneGame.Instance.player)
                {
                    GameObject player = await SceneGame.Instance.CharacterManager.CreatePlayer();
                    SceneGame.Instance.player = player;
                }

                Vector3 spawnPosition = currentMapTableData.PlayerSpawnPosition;
                if (playSpawnPosition != Vector3.zero)
                {
                    spawnPosition = playSpawnPosition;
                }
                SceneGame.Instance.player.SetActive(true);
                Player scriptPlayer = SceneGame.Instance.player.GetComponent<Player>();
                scriptPlayer.MoveTeleport(spawnPosition.x, spawnPosition.y);
                scriptPlayer.SetMapSize(_mapManager.GetMapSize());
                scriptPlayer.ApplyMapBoundaryOverrides(currentMapTableData);
                scriptPlayer.SetEndTilemapYDeathSuppressed(false);
                scriptPlayer.Stop(true);
                SceneGame.Instance.cameraManager?.SetFollowTarget(SceneGame.Instance.player?.transform);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region 몬스터

        public async Task LoadMonsters(MapTileCommon mapTileCommon, StruckTableMap currentMapTableData)
        {
            string key = ConfigAddressableMap.GetKeyJsonRegenMonster(currentMapTableData.FolderName);
            try
            {
                TextAsset textFile = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);

                if (textFile)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        CharacterRegenDataList characterRegenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                        SpawnMonsters(characterRegenDataList.CharacterRegenDatas, mapTileCommon, currentMapTableData);
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"몬스터 regen json 파싱중 오류. file {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// 리젠 데이터 목록을 기준으로 현재 맵에 몬스터를 생성합니다.
        /// </summary>
        /// <param name="monsterList">몬스터 리젠 데이터 목록입니다.</param>
        /// <param name="mapTileCommon">몬스터를 배치할 맵 루트입니다.</param>
        /// <param name="currentMapTableData">현재 맵 테이블 데이터입니다.</param>
        private void SpawnMonsters(
            List<CharacterRegenData> monsterList,
            MapTileCommon mapTileCommon,
            StruckTableMap currentMapTableData)
        {
            if (monsterList == null) return;

            foreach (CharacterRegenData monsterData in monsterList)
            {
                int uid = monsterData.Uid;
                if (uid <= 0) continue;
                var info = _tableMonster.GetDataByUid(uid);
                if (info.Uid <= 0 || info.AnimationUid <= 0) continue;
                SpawnMonster(uid, monsterData, mapTileCommon, currentMapTableData);
            }
        }

        /// <summary>
        /// 단일 몬스터를 생성하거나 풀에서 가져와 현재 맵 정책을 적용합니다.
        /// </summary>
        /// <param name="monsterUid">생성할 몬스터 UID입니다.</param>
        /// <param name="monsterData">몬스터 리젠 데이터입니다.</param>
        /// <param name="mapTileCommon">몬스터를 배치할 맵 루트입니다.</param>
        /// <param name="currentMapTableData">현재 맵 테이블 데이터입니다.</param>
        /// <param name="forcedVid">리스폰처럼 기존 VID를 유지해야 할 때 사용할 VID입니다.</param>
        private void SpawnMonster(
            int monsterUid,
            CharacterRegenData monsterData,
            MapTileCommon mapTileCommon,
            StruckTableMap currentMapTableData,
            int forcedVid = 0)
        {
            GameObject monster = SceneGame.Instance.CharacterManager.RentMonster(monsterUid, monsterData);
            if (!monster) return;
            monster.transform.SetParent(mapTileCommon.gameObject.transform, worldPositionStays: true);

            Monster myMonsterScript = monster.GetComponent<Monster>();
            if (myMonsterScript == null)
                return;

            int spawnVid = forcedVid > 0 ? forcedVid : ++_characterVid;
            if (spawnVid > _characterVid)
                _characterVid = spawnVid;

            myMonsterScript.vid = spawnVid;
            ApplyMapVisibilityPolicy(monster, monsterData);
            ApplyMonsterMapBoundaryOverrides(myMonsterScript, currentMapTableData);
            mapTileCommon.AddMonster(spawnVid, monster);
            _monsterRegenDataByVid[spawnVid] = monsterData;
            _monsterRespawnPending.Remove(spawnVid);

            _ = CharacterSpawnHooks.InvokeAsync(myMonsterScript);

            if (monsterData.patrolData != null)
            {
                var patrolData = monsterData.patrolData;
                GameObject prefabPatrol =
                    AddressableLoaderPrefabCommon.Instance.GetPreLoadGamePrefabByName(ConfigAddressableMap.ObjectPatrol.Key);
                if (prefabPatrol)
                {
                    GameObject warp = Object.Instantiate(prefabPatrol,
                        new Vector3(patrolData.x, patrolData.y, patrolData.z), Quaternion.identity,
                        mapTileCommon.gameObject.transform);

                    ObjectPatrol objectPatrol = warp.GetComponent<ObjectPatrol>();
                    if (objectPatrol)
                    {
                        objectPatrol.patrolData = patrolData;
                        objectPatrol.SetParentMonster(monster);
                        myMonsterScript.SetPatrolObject(objectPatrol.gameObject);
                    }
                    else
                    {
                        GcLogger.LogError($"{nameof(ObjectPatrol)}이 없습니다.");
                    }
                }
                else
                {
                    GcLogger.LogError($"패트롤 프리팹이 없습니다. path: {ConfigAddressableMap.ObjectPatrol.Path}");
                }
            }
        }

        public void MarkMonsterDead(int monsterVid)
        {
            if (monsterVid <= 0)
                return;

            if (_monsterRegenDataByVid.ContainsKey(monsterVid))
                _monsterRespawnPending.Add(monsterVid);
        }

        public void OnMonsterReturnedToPool(int monsterVid, MapTileCommon mapTileCommon)
        {
            if (monsterVid <= 0 || mapTileCommon == null)
                return;

            mapTileCommon.RemoveMonster(monsterVid);
        }

        public void ReturnAllMonstersToPool(MapTileCommon mapTileCommon)
        {
            if (mapTileCommon == null)
                return;

            var monsters = mapTileCommon.GetMonsterEntries();
            foreach (var entry in monsters)
            {
                var monster = entry.Value != null ? entry.Value.GetComponent<Monster>() : null;
                if (monster == null)
                    continue;

                SceneGame.Instance.CharacterManager.ReturnMonsterToPool(monster);
            }

            mapTileCommon.ClearMonsters();
        }

        public IEnumerator RegenMonster(int monsterVid, int currentMapUid, MapTileCommon mapTileCommon)
        {
            if (!_monsterRegenDataByVid.TryGetValue(monsterVid, out CharacterRegenData monsterData) || monsterData == null)
                yield break;

            if (!_monsterRespawnPending.Contains(monsterVid))
                yield break;

            yield return new WaitForSeconds(_defaultMonsterRegenTimeSec);

            if (!_monsterRespawnPending.Contains(monsterVid))
                yield break;

            int uid = monsterData.Uid;
            int mapUid = monsterData.MapUid;
            if (mapUid != currentMapUid) yield break;
            if (uid <= 0) yield break;
            if (mapTileCommon == null) yield break;

            SpawnMonster(uid, monsterData, mapTileCommon, _mapManager?.GetCurrentMapTableData(), monsterVid);
        }
        #endregion

        #region NPC

        public async Task LoadNpcs(MapTileCommon mapTileCommon, StruckTableMap currentMapTableData)
        {
            string key = ConfigAddressableMap.GetKeyJsonRegenNpc(currentMapTableData.FolderName);
            try
            {
                TextAsset textFile = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);

                if (textFile)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                        SpawnNpcs(regenDataList.CharacterRegenDatas, mapTileCommon);
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"npc json 파싱중 오류. file {key}: {ex.Message}");
            }
        }

        private void SpawnNpcs(List<CharacterRegenData> npcList, MapTileCommon mapTileCommon)
        {
            foreach (CharacterRegenData npcData in npcList)
            {
                int uid = npcData.Uid;
                GameObject npc = SceneGame.Instance.CharacterManager.CreateNpc(uid, npcData);
                if (!npc) continue;
                npc.transform.SetParent(mapTileCommon.gameObject.transform);

                Npc myNpcScript = npc.GetComponent<Npc>();
                if (!myNpcScript) continue;
                myNpcScript.vid = _characterVid;
                myNpcScript.uid = npcData.Uid;
                myNpcScript.CharacterRegenData = npcData;
                ApplyMapVisibilityPolicy(npc, npcData);

                mapTileCommon.AddNpc(_characterVid, npc);
                ApplyInitialNpcVisibilityPolicy(npc, npcData);
                _characterVid++;
            }
        }
        
        /// <summary>
        /// NPC 스폰 직후 명시 표시 정책과 기본 보임 값을 우선순위에 따라 적용합니다.
        /// </summary>
        /// <param name="npcObject">생성된 NPC 오브젝트입니다.</param>
        /// <param name="npcData">맵 배치 리젠 데이터입니다.</param>
        private static void ApplyInitialNpcVisibilityPolicy(GameObject npcObject, CharacterRegenData npcData)
        {
            if (npcObject == null || npcData == null)
            {
                return;
            }

            switch (npcData.MapVisibilityPolicy)
            {
                case MapCharacterVisibilityPolicy.KeepVisible:
                    if (!npcObject.activeSelf)
                    {
                        npcObject.GetComponent<Npc>()?.StartFadeIn();
                    }

                    return;

                case MapCharacterVisibilityPolicy.KeepHidden:
                    npcObject.SetActive(false);
                    return;

                case MapCharacterVisibilityPolicy.DefaultCulling:
                default:
                    if (!npcData.DefaultVisible)
                    {
                        npcObject.SetActive(false);
                    }

                    return;
            }
        }

        /// <summary>
        /// 리젠 데이터에 저장된 맵 표시 정책을 캐릭터 런타임 상태에 반영합니다.
        /// </summary>
        /// <param name="characterObject">정책을 적용할 캐릭터 오브젝트입니다.</param>
        /// <param name="regenData">맵 배치 리젠 데이터입니다.</param>
        private static void ApplyMapVisibilityPolicy(GameObject characterObject, CharacterRegenData regenData)
        {
            if (characterObject == null || regenData == null)
            {
                return;
            }

            CharacterBase character = characterObject.GetComponent<CharacterBase>();
            if (character == null)
            {
                return;
            }

            // 맵 컬링 계산은 CharacterBase의 정책을 기준으로 처리하므로 스폰 직후 동기화합니다.
            character.SetMapVisibilityPolicy(regenData.MapVisibilityPolicy);
        }

        /// <summary>
        /// 현재 맵의 Parallax 정책을 몬스터 이동 경계 제한에 반영합니다.
        /// </summary>
        /// <param name="monster">경계 정책을 적용할 몬스터입니다.</param>
        /// <param name="currentMapTableData">현재 맵 테이블 데이터입니다.</param>
        private static void ApplyMonsterMapBoundaryOverrides(Monster monster, StruckTableMap currentMapTableData)
        {
            if (monster == null)
            {
                return;
            }

            // UseParallax 맵에서는 플레이어와 동일하게 몬스터의 좌/우/상/하 경계 제한도 해제합니다.
            monster.ApplyMapBoundaryOverrides(currentMapTableData);
        }

        #endregion

        #region 워프

        public async Task LoadWarps(MapTileCommon mapTileCommon, StruckTableMap currentMapTableData)
        {
            string key = ConfigAddressableMap.GetKeyJsonWarp(currentMapTableData.FolderName);
            try
            {
                TextAsset textFile = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);

                if (textFile)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        WarpDataList warpDataList = JsonConvert.DeserializeObject<WarpDataList>(content);
                        SpawnWarps(warpDataList.warpDataList, mapTileCommon);
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"워프 json 파싱중 오류. file {key}: {ex.Message}");
            }
        }

        private void SpawnWarps(List<WarpData> warpDatas, MapTileCommon mapTileCommon)
        {
            GameObject warpPrefab =
                AddressableLoaderPrefabCommon.Instance.GetPreLoadGamePrefabByName(ConfigAddressableMap.ObjectWarp.Key);
            if (!warpPrefab) return;

            foreach (WarpData warpData in warpDatas)
            {
                GameObject warp = Object.Instantiate(warpPrefab, new Vector3(warpData.x, warpData.y, warpData.z), Quaternion.identity, mapTileCommon.gameObject.transform);

                ObjectWarp objectWarp = warp.GetComponent<ObjectWarp>();
                if (!objectWarp) continue;
                objectWarp.WarpData = warpData;
            }
        }

        #endregion
    }
}
