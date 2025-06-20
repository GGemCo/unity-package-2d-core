using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GGemCo.Scripts;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Editor
{
    /// <summary>
    /// 맵 배치툴 > 몬스터 배치, 내보내기
    /// </summary>
    public class MonsterExporter : DefaultExporter
    {
        private List<CharacterRegenData> _monsterList;
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;
        private DefaultMap _defaultMap;
        private CharacterManager _characterManager;
        private MapExporter _mapExporter;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="mapExporter"></param>
        public void Initialize(MapExporter mapExporter)
        {
            _mapExporter = mapExporter;
            _tableMonster = _mapExporter.TableMonster;
            _tableAnimation = _mapExporter.TableAnimation;
            _defaultMap = _mapExporter.defaultMap;
            _characterManager = _mapExporter.CharacterManager;
        }
        /// <summary>
        /// 배치할 맵 셋팅
        /// </summary>
        /// <param name="pDefaultMap"></param>
        public void SetDefaultMap(DefaultMap pDefaultMap)
        {
            _defaultMap = pDefaultMap;
        }
        /// <summary>
        /// 맵에 몬스터 추가하기
        /// </summary>
        /// <param name="selectedMonsterIndex"></param>
        public void AddMonsterToMap(int selectedMonsterIndex)
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            var monsterDictionary = _tableMonster.GetDatas();
            int index = 0;
            StruckTableMonster monsterData = new StruckTableMonster();

            foreach (var outerPair in monsterDictionary)
            {
                if (index == selectedMonsterIndex)
                {
                    monsterData = _tableMonster.GetDataByUid(outerPair.Key);
                    break;
                }
                index++;
            }
            CharacterRegenData characterRegenData =
                new CharacterRegenData(monsterData.Uid, Vector3.zero, false, _defaultMap.GetChapterNumber(), true);

            var infoAnimation = _tableAnimation.GetDataByUid(monsterData.SpineUid);
            GameObject prefabMonster = null;
            if (infoAnimation != null && infoAnimation.Uid > 0)
            {
                prefabMonster = _mapExporter.AddressableMonsterLoader.GetMonster(infoAnimation.Uid);
            }

            GameObject monster = _characterManager.CreateMonster2(monsterData.Uid, characterRegenData, prefabMonster);
            if (!monster)
            {
                Debug.LogError("몬스터 데이터가 없습니다.");
                return;
            }
            monster.transform.SetParent(_defaultMap.gameObject.transform);

            var monsterScript = monster.GetComponent<Monster>();
            if (monsterScript)
            {
                monsterScript.uid = monsterData.Uid;
                monsterScript.SetScale(monsterData.Scale);
                monsterScript.InitTagSortingLayer();
            }
            
            // npc 정보 보여줄 canvas 추가
            TextMeshProUGUI text = CreateInfoCanvas(monsterScript);
            text.text = $"Uid: {monsterData.Uid}\nPos: (0, 0)\nScale: {Math.Abs(monster.transform.localScale.x):F2}";

            Debug.Log($"{monsterData.Name} 몬스터가 맵에 추가되었습니다.");
        }
        /// <summary>
        /// 배치한 몬스터 정보 json 으로 내보내기
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="mapUid"></param>
        public void ExportMonsterDataToJson(string filePath, string fileName, int mapUid)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            CharacterRegenDataList saveMonsterList = new CharacterRegenDataList();

            foreach (Transform child in mapObject.transform)
            {
                if (!child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) continue;
                var monster = child.gameObject.GetComponent<Monster>();
                if (!monster) continue;
                saveMonsterList.CharacterRegenDatas.Add(new CharacterRegenData(monster.uid, child.position, monster.isFlip, mapUid, true));
            }

            string json = JsonConvert.SerializeObject(saveMonsterList);
            string path = Path.Combine(filePath, fileName);
            File.WriteAllText(path, json);
            Debug.Log("몬스터 data exported to " + path);
        }
        /// <summary>
        /// json 에 저장된 몬스터 정보 불러오기
        /// </summary>
        /// <param name="regenFileName"></param>
        public async Task LoadMonsterData(string regenFileName)
        {
            // JSON 파일을 읽기
            try
            {
                var handle = Addressables.LoadAssetAsync<TextAsset>(regenFileName);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded || !handle.Result)
                {
                    Debug.LogError("monster regen file 로드 실패. addressKey: " + regenFileName);
                    return;
                }
                
                TextAsset textAsset = handle.Result;
                string content = textAsset.text;
                if (string.IsNullOrEmpty(content)) return;
                CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                _monsterList = regenDataList.CharacterRegenDatas;
                
                await SpawnMonster();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file {regenFileName}: {ex.Message}");
            }
        }
        /// <summary>
        /// 몬스터 생성하기
        /// </summary>
        private async Task SpawnMonster()
        {
            // JSON 파일을 읽기
            try
            {
                if (!_defaultMap)
                {
                    Debug.LogError("_defaultMap 이 없습니다.");
                    return;
                }

                foreach (CharacterRegenData monsterData in _monsterList)
                {
                    int uid = monsterData.Uid;
                    var infoMonster = _tableMonster.GetDataByUid(uid);
                    if (infoMonster == null) continue;
                    
                    var infoAnimation = _tableAnimation.GetDataByUid(infoMonster.SpineUid);
                    GameObject prefabMonster = null;
                    if (infoAnimation != null && infoAnimation.Uid > 0)
                    {
                        prefabMonster = _mapExporter.AddressableMonsterLoader.GetMonster(infoAnimation.Uid);
                    }
                    GameObject monster = _characterManager.CreateMonster2(uid, monsterData, prefabMonster);
                    
                    if (!monster) continue;
                    monster.transform.SetParent(_defaultMap.gameObject.transform);
                
                    // 몬스터의 속성을 설정하는 스크립트가 있을 경우 적용
                    Monster myMonsterScript = monster.GetComponent<Monster>();
                    if (myMonsterScript)
                    {
                        // MapManager.cs:138 도 수정
                        myMonsterScript.uid = monsterData.Uid;
                        myMonsterScript.CharacterRegenData = monsterData;
                        // SetScale 다음에 처리해야 함
                        myMonsterScript.SetFlip(monsterData.IsFlip);
                        myMonsterScript.InitTagSortingLayer();
                    }
                    // npc 정보 보여줄 canvas 추가
                    TextMeshProUGUI text = CreateInfoCanvas(myMonsterScript);
                    text.text = $"Uid: {monsterData.Uid}\nPos: ({monsterData.x}, {monsterData.y})\nScale: {Math.Abs(monster.transform.localScale.x):F2}";
                }

                Debug.Log("monster spawned successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"몬스터 불러오기 오류: {ex.Message}");
            }
        }
    }
}
