using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo.Scripts
{
    public class MapLoadCharacters
    {
        private int characterVid;
        private MapManager mapManager;
        private TableNpc tableNpc;
        private TableMonster tableMonster;
        private TableAnimation tableAnimation;
        private TableLoaderManager tableLoaderManager;
        // 몬스터가 죽었을때 다시 리젠될는 시간
        private float defaultMonsterRegenTimeSec;

        public void Reset()
        {
            characterVid = 1;
        }
        public void Initialize(MapManager manager)
        {
            mapManager = manager;
            characterVid = 0;
            tableLoaderManager = TableLoaderManager.Instance;
            tableNpc = tableLoaderManager.TableNpc;
            tableAnimation = tableLoaderManager.TableAnimation;
            tableMonster = tableLoaderManager.TableMonster;
            defaultMonsterRegenTimeSec = AddressableSettingsLoader.Instance.settings.defaultMonsterRegenTimeSec;
        }
        /// <summary>
        /// 플레이어 스폰
        /// </summary>
        /// <param name="playSpawnPosition"></param>
        /// <param name="currentMapTableData"></param>
        /// <param name="mapTileCommon"></param>
        /// <returns></returns>
        public IEnumerator LoadPlayer(Vector3 playSpawnPosition, StruckTableMap currentMapTableData, DefaultMap mapTileCommon)
        {
            if (SceneGame.Instance.player == null)
            {
                GameObject player = SceneGame.Instance.CharacterManager.CreatePlayer();
                SceneGame.Instance.player = player;
            }
            
            // 플레이어 위치
            Vector3 spawnPosition = currentMapTableData.PlayerSpawnPosition;
            if (playSpawnPosition != Vector3.zero)
            {
                spawnPosition = playSpawnPosition;
            }
            SceneGame.Instance.player?.GetComponent<Player>().MoveTeleport(spawnPosition.x, spawnPosition.y);
            SceneGame.Instance.player?.GetComponent<Player>().SetMapSize(mapTileCommon.GetMapSize());
            SceneGame.Instance.cameraManager?.SetFollowTarget(SceneGame.Instance.player?.transform);

            // yield return new WaitForSeconds(ConfigCommon.CharacterFadeSec/2);
            yield return null;
        }
        /// <summary>
        /// 몬스터 스폰하기
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadMonsters(MapTileCommon mapTileCommon)
        {
            string regenFileName = mapManager.GetFilePath(MapConstants.FileNameRegenMonster);

            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{regenFileName}");
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        CharacterRegenDataList characterRegenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                        SpawnMonsters(characterRegenDataList.CharacterRegenDatas, mapTileCommon);
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"몬스터 regen json 파싱중 오류. file {regenFileName}: {ex.Message}");
                yield break;
            }

            yield return null;
        }
        /// <summary>
        /// 몬스터 스폰하기
        /// </summary>
        /// <param name="monsterList"></param>
        /// <param name="mapTileCommon"></param>
        private void SpawnMonsters(List<CharacterRegenData> monsterList, MapTileCommon mapTileCommon)
        {
            foreach (CharacterRegenData monsterData in monsterList)
            {
                int uid = monsterData.Uid;
                if (uid <= 0) continue;
                var info = tableMonster.GetDataByUid(uid);
                if (info.Uid <= 0 || info.SpineUid <= 0) continue;
                SpawnMonster(uid, monsterData, mapTileCommon);
            }
        }
        /// <summary>
        /// 몬스터 스폰하기
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <param name="monsterData"></param>
        /// <param name="mapTileCommon"></param>
        private void SpawnMonster(int monsterUid, CharacterRegenData monsterData, MapTileCommon mapTileCommon)
        {
            GameObject monster = SceneGame.Instance.CharacterManager.CreateMonster(monsterUid, monsterData);
            if (monster == null) return;
            monster.transform.SetParent(mapTileCommon.gameObject.transform);
            
            Monster myMonsterScript = monster.GetComponent<Monster>();
            myMonsterScript.vid = characterVid;
            myMonsterScript.CreateHpBar();
            mapTileCommon.AddMonster(characterVid, monster);
            characterVid++;
        }
        
        /// <summary>
        /// npc 스폰하기
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadNpcs(MapTileCommon mapTileCommon)
        {
            string regenFileName = mapManager.GetFilePath(MapConstants.FileNameRegenNpc);

            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{regenFileName}");
                if (textFile != null)
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
                GcLogger.LogError($"npc json 파싱중 오류. file {regenFileName}: {ex.Message}");
                yield break;
            }

            yield return null;
        }
        /// <summary>
        /// npc 스폰하기
        /// </summary>
        /// <param name="npcList"></param>
        /// <param name="mapTileCommon"></param>
        private void SpawnNpcs(List<CharacterRegenData> npcList, MapTileCommon mapTileCommon)
        {
            foreach (CharacterRegenData npcData in npcList)
            {
                int uid = npcData.Uid;
                GameObject npc = SceneGame.Instance.CharacterManager.CreateNpc(uid, npcData);
                if (npc == null) continue;
                npc.transform.SetParent(mapTileCommon.gameObject.transform);
            
                // NPC의 이름과 기타 속성 설정
                Npc myNpcScript = npc.GetComponent<Npc>();
                if (myNpcScript != null)
                {
                    // npcExporter.cs:158 도 수정
                    myNpcScript.vid = characterVid;
                    myNpcScript.uid = npcData.Uid;
                    myNpcScript.CharacterRegenData = npcData;
                    
                    mapTileCommon.AddNpc(characterVid, npc);
                    characterVid++;
                }
            }
        }
        /// <summary>
        /// 몬스터 리젠하기 
        /// </summary>
        /// <param name="monsterVid"></param>
        /// <param name="currentMapUid"></param>
        /// <param name="mapTileCommon"></param>
        public IEnumerator RegenMonster(int monsterVid, int currentMapUid, MapTileCommon mapTileCommon)
        {
            CharacterRegenData monsterData = mapTileCommon.GetMonsterDataByVid(monsterVid);
            if (monsterData == null) yield break;

            yield return new WaitForSeconds(defaultMonsterRegenTimeSec);
            int uid = monsterData.Uid;
            int mapUid = monsterData.MapUid;
            if (mapUid != currentMapUid) yield break;
            if (uid <= 0) yield break;
            SpawnMonster(uid, monsterData, mapTileCommon);
        }
        /// <summary>
        /// 워프 스폰하기
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadWarps(MapTileCommon mapTileCommon)
        {
            string rPathWarp = mapManager.GetFilePath(MapConstants.FileNameWarp);

            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{rPathWarp}");
                if (textFile != null)
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
                GcLogger.LogError($"워프 json 파싱중 오류. file {rPathWarp}: {ex.Message}");
                yield break;
            }
            yield return null;
        }
        /// <summary>
        /// 워프 스폰하기
        /// </summary>
        /// <param name="warpDatas"></param>
        /// <param name="mapTileCommon"></param>
        private void SpawnWarps(List<WarpData> warpDatas, MapTileCommon mapTileCommon)
        {
            GameObject warpPrefab = Resources.Load<GameObject>(MapConstants.PathPrefabWarp);
            if (warpPrefab == null)
            {
                GcLogger.LogError("워프 프리팹이 없습니다. path:"+MapConstants.PathPrefabWarp);
                return;
            }
            foreach (WarpData warpData in warpDatas)
            {
                GameObject warp = Object.Instantiate(warpPrefab, new Vector3(warpData.x, warpData.y, warpData.z), Quaternion.identity, mapTileCommon.gameObject.transform);
            
                // 워프의 이름과 기타 속성 설정
                ObjectWarp objectWarp = warp.GetComponent<ObjectWarp>();
                if (objectWarp != null)
                {
                    // warpExporter.cs:128 도 수정
                    objectWarp.WarpData = warpData;
                }
            }
        }
    }
}