using System;
using System.Collections.Generic;
using System.IO;
using GGemCo.Scripts;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 맵 배치툴 > 몬스터 배치, 내보내기
    /// </summary>
    public class MonsterExporter : DefaultExporter
    {
        private List<CharacterRegenData> monsterList;
        private TableMonster tableMonster;
        private TableAnimation tableAnimation;
        private DefaultMap defaultMap;
        private CharacterManager characterManager;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="pTableMonster"></param>
        /// <param name="pTableAnimation"></param>
        /// <param name="pDefaultMap"></param>
        public void Initialize(TableMonster pTableMonster, TableAnimation pTableAnimation, DefaultMap pDefaultMap, CharacterManager pcharacterManager)
        {
            tableMonster = pTableMonster;
            tableAnimation = pTableAnimation;
            defaultMap = pDefaultMap;
            characterManager = pcharacterManager;
        }
        /// <summary>
        /// 배치할 맵 셋팅
        /// </summary>
        /// <param name="pDefaultMap"></param>
        public void SetDefaultMap(DefaultMap pDefaultMap)
        {
            defaultMap = pDefaultMap;
        }
        /// <summary>
        /// 맵에 몬스터 추가하기
        /// </summary>
        /// <param name="selectedMonsterIndex"></param>
        public void AddMonsterToMap(int selectedMonsterIndex)
        {
            if (defaultMap == null)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            var monsterDictionary = tableMonster.GetDatas();
            int index = 0;
            StruckTableMonster monsterData = new StruckTableMonster();

            foreach (var outerPair in monsterDictionary)
            {
                if (index == selectedMonsterIndex)
                {
                    monsterData = tableMonster.GetDataByUid(outerPair.Key);
                    break;
                }
                index++;
            }
            CharacterRegenData characterRegenData =
                new CharacterRegenData(monsterData.Uid, Vector3.zero, false, defaultMap.GetChapterNumber(), true);
            GameObject monster = characterManager.CreateMonster(monsterData.Uid, characterRegenData);
            if (monster == null)
            {
                Debug.LogError("몬스터 데이터가 없습니다.");
                return;
            }
            monster.transform.SetParent(defaultMap.gameObject.transform);

            var monsterScript = monster.GetComponent<Monster>();
            if (monsterScript != null)
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
                if (child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster)))
                {
                    var monster = child.gameObject.GetComponent<Monster>();
                    if (monster == null) continue;
                    saveMonsterList.CharacterRegenDatas.Add(new CharacterRegenData(monster.uid, child.position, monster.isFlip, mapUid, true));
                }
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
        public void LoadMonsterData(string regenFileName)
        {
            // JSON 파일을 읽기
            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{regenFileName}");
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                        monsterList = regenDataList.CharacterRegenDatas;
                        SpawnMonster();
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"Error reading file {regenFileName}: {ex.Message}");
            }
        }
        /// <summary>
        /// 몬스터 생성하기
        /// </summary>
        private void SpawnMonster()
        {
            if (defaultMap == null)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            foreach (CharacterRegenData monsterData in monsterList)
            {
                int uid = monsterData.Uid;
                GameObject monster = characterManager.CreateMonster(uid, monsterData);
                if (monster == null) continue;
                monster.transform.SetParent(defaultMap.gameObject.transform);
                
                // 몬스터의 속성을 설정하는 스크립트가 있을 경우 적용
                Monster myMonsterScript = monster.GetComponent<Monster>();
                if (myMonsterScript != null)
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
    }
}
