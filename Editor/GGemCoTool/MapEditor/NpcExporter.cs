using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치툴 > Npc 배치, 내보내기
    /// </summary>
    public class NpcExporter : DefaultExporter
    {
        private List<CharacterRegenData> _npcList;
        private TableNpc _tableNpc;
        private TableAnimation _tableAnimation;
        private DefaultMap _defaultMap;
        private CharacterManager _characterManager;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="pTableNpc"></param>
        /// <param name="pTableAnimation"></param>
        /// <param name="pDefaultMap"></param>
        /// <param name="pcharacterManager"></param>
        public void Initialize(TableNpc pTableNpc, TableAnimation pTableAnimation, DefaultMap pDefaultMap, CharacterManager pcharacterManager)
        {
            _tableNpc = pTableNpc;
            _tableAnimation = pTableAnimation;
            _defaultMap = pDefaultMap;
            _characterManager = pcharacterManager;
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
        /// 맵에 npc 추가하기
        /// </summary>
        /// <param name="selectedNpcIndex"></param>
        public void AddNpcToMap(int selectedNpcIndex)
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            var npcDictionary = _tableNpc.GetDatas();
            int index = 0;
            StruckTableNpc npcData = new StruckTableNpc();

            foreach (var outerPair in npcDictionary)
            {
                if (index == selectedNpcIndex)
                {
                    npcData = _tableNpc.GetDataByUid(outerPair.Key);
                    break;
                }
                index++;
            }
            var infoAnimation = _tableAnimation.GetDataByUid(npcData.SpineUid);
            if (infoAnimation == null) return;
            
            string npcPath = ConfigAddressableMap.GetPathCharacter(infoAnimation.PrefabPath);
            GameObject npcPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(npcPath);
            CharacterRegenData characterRegenData =
                new CharacterRegenData(npcData.Uid, Vector3.zero, false, _defaultMap.GetChapterNumber(), true);
            GameObject npc = _characterManager.CreateNpc(npcData.Uid, characterRegenData, npcPrefab);
            if (!npc)
            {
                Debug.LogError("NPC 데이터가 없습니다.");
                return;
            }
            npc.transform.SetParent(_defaultMap.gameObject.transform);
            
            var npcScript = npc.GetComponent<Npc>();
            if (npcScript)
            {
                npcScript.uid = npcData.Uid;
                npcScript.SetScale(npcData.Scale);
                npcScript.InitTagSortingLayer();
            }
            
            // npc 정보 보여줄 canvas 추가
            TextMeshProUGUI text = CreateInfoCanvas(npcScript);
            text.text = $"Uid: {npcData.Uid}\nPos: (0, 0)\nScale: {Math.Abs(npc.transform.localScale.x):F2}";

            Debug.Log($"{npcData.Name} NPC가 맵에 추가되었습니다.");
        }
        /// <summary>
        /// 배치한 정보 json 으로 내보내기
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="mapUid"></param>
        public void ExportNpcDataToJson(string filePath, string fileName, int mapUid)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            CharacterRegenDataList saveNpcList = new CharacterRegenDataList();

            foreach (Transform child in mapObject.transform)
            {
                if (!child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Npc))) continue;
                var npc = child.gameObject.GetComponent<Npc>();
                if (!npc) continue;
                saveNpcList.CharacterRegenDatas.Add(new CharacterRegenData(npc.uid, child.position, npc.isFlip,
                    mapUid, true));
                
                // addressables 에 등록됬는지 확인
                
                // addressables label 등록하기
            }

            string json = JsonConvert.SerializeObject(saveNpcList);
            string path = Path.Combine(filePath, fileName);
            File.WriteAllText(path, json);
            Debug.Log("NPC data exported to " + path);
        }
        /// <summary>
        /// json 에서 npc 정보 불러오기
        /// </summary>
        /// <param name="regenFileName"></param>
        public void LoadNpcData(string regenFileName)
        {
            // JSON 파일을 읽기
            try
            {
                string content = AssetDatabaseLoaderManager.LoadFileJson(regenFileName);
                if (string.IsNullOrEmpty(content)) return;
                CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                _npcList = regenDataList.CharacterRegenDatas;
                SpawnNpc();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file {regenFileName}: {ex.Message}");
            }
        }
        /// <summary>
        /// npc 생성하기
        /// </summary>
        private void SpawnNpc()
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            foreach (CharacterRegenData npcData in _npcList)
            {
                int uid = npcData.Uid;
                var info = _tableNpc.GetDataByUid(uid);
                if (info == null) continue;
                var infoAnimation = _tableAnimation.GetDataByUid(info.SpineUid);
                if (infoAnimation == null) continue;
                
                string npcPath = ConfigAddressableMap.GetPathCharacter(infoAnimation.PrefabPath);
                GameObject npcPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(npcPath);
                GameObject npc = _characterManager.CreateNpc(uid, npcData, npcPrefab);
                if (!npc) continue;
                npc.transform.SetParent(_defaultMap.gameObject.transform);
                
                // NPC의 속성을 설정하는 스크립트가 있을 경우 적용
                Npc myNpcScript = npc.GetComponent<Npc>();
                if (myNpcScript)
                {
                    // MapManager.cs:138 도 수정
                    myNpcScript.uid = npcData.Uid;
                    myNpcScript.CharacterRegenData = npcData;
                    // SetScale 다음에 처리해야 함
                    myNpcScript.isFlip = npcData.IsFlip;
                    myNpcScript.SetFlip(npcData.IsFlip);
                    myNpcScript.InitTagSortingLayer();
                }
                
                // npc 정보 보여줄 canvas 추가
                TextMeshProUGUI text = CreateInfoCanvas(myNpcScript);
                text.text = $"Uid: {npcData.Uid}\nPos: ({npcData.x}, {npcData.y})\nScale: {Math.Abs(npc.transform.localScale.x):F2}";
            }

            Debug.Log("NPCs spawned successfully.");
        }
    }
}
