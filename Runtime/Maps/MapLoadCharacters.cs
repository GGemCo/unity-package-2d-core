using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            defaultMonsterRegenTimeSec = AddressableLoaderSettings.Instance.settings.defaultMonsterRegenTimeSec;
        }
        /// <summary>
        /// 플레이어 스폰
        /// </summary>
        /// <param name="playSpawnPosition"></param>
        /// <param name="currentMapTableData"></param>
        /// <param name="mapTileCommon"></param>
        /// <returns></returns>
        public async Task LoadPlayer(Vector3 playSpawnPosition, StruckTableMap currentMapTableData, DefaultMap mapTileCommon)
        {
            try
            {
                if (!SceneGame.Instance.player)
                {
                    GameObject player = await SceneGame.Instance.CharacterManager.CreatePlayer();
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        /// <summary>
        /// 몬스터 스폰하기
        /// </summary>
        /// <returns></returns>
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
                        SpawnMonsters(characterRegenDataList.CharacterRegenDatas, mapTileCommon);
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"몬스터 regen json 파싱중 오류. file {key}: {ex.Message}");
            }
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
            if (!monster) return;
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
                if (!npc) continue;
                npc.transform.SetParent(mapTileCommon.gameObject.transform);
            
                // NPC의 이름과 기타 속성 설정
                Npc myNpcScript = npc.GetComponent<Npc>();
                if (!myNpcScript) continue;
                // npcExporter.cs:158 도 수정
                myNpcScript.vid = characterVid;
                myNpcScript.uid = npcData.Uid;
                myNpcScript.CharacterRegenData = npcData;
                    
                mapTileCommon.AddNpc(characterVid, npc);
                characterVid++;
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
        /// <summary>
        /// 워프 스폰하기
        /// </summary>
        /// <param name="warpDatas"></param>
        /// <param name="mapTileCommon"></param>
        private void SpawnWarps(List<WarpData> warpDatas, MapTileCommon mapTileCommon)
        {
            GameObject warpPrefab =
                AddressableLoaderPrefabCommon.Instance.GetPreLoadGamePrefabByName(ConfigAddressableMap.ObjectWarp.Key);
            if (!warpPrefab) return;
            
            foreach (WarpData warpData in warpDatas)
            {
                GameObject warp = Object.Instantiate(warpPrefab, new Vector3(warpData.x, warpData.y, warpData.z), Quaternion.identity, mapTileCommon.gameObject.transform);
            
                // 워프의 이름과 기타 속성 설정
                ObjectWarp objectWarp = warp.GetComponent<ObjectWarp>();
                if (!objectWarp) continue;
                // warpExporter.cs:128 도 수정
                objectWarp.WarpData = warpData;
            }
        }
    }
}