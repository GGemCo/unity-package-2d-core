using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
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
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="pTableMonster"></param>
        /// <param name="pTableAnimation"></param>
        /// <param name="pDefaultMap"></param>
        /// <param name="pcharacterManager"></param>
        public void Initialize(TableMonster pTableMonster, TableAnimation pTableAnimation, DefaultMap pDefaultMap, CharacterManager pcharacterManager)
        {
            _tableMonster = pTableMonster;
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
        /// 맵에 몬스터 추가하기
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <param name="usePatrolMonster"></param>
        public void AddMonsterToMap(int monsterUid, bool usePatrolMonster)
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            if (monsterUid <= 0)
            {
                Debug.LogWarning("선택된 몬스터 Uid 가 유효하지 않습니다.");
                return;
            }

            StruckTableMonster monsterData = _tableMonster.GetDataByUid(monsterUid);
            if (monsterData == null || monsterData.Uid <= 0)
            {
                Debug.LogError($"몬스터 데이터를 찾을 수 없습니다. uid:{monsterUid}");
                return;
            }
            var infoAnimation = _tableAnimation.GetDataByUid(monsterData.AnimationUid);
            if (infoAnimation == null) return;
            string monsterPath = ConfigAddressableMap.GetPathCharacter(infoAnimation);
            
            // Addressable 에 등록되어있는지 체크 
            if (!HelperEditorUI.ExistAddressableByPath(ConfigAddressableMap.GetPathCharacter(infoAnimation, true))) return;
            
            GameObject npcPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(monsterPath);

            CharacterRegenData characterRegenData =
                new CharacterRegenData(monsterData.Uid, Vector3.zero, false, _defaultMap.GetChapterNumber(), true,
                    patrolData: new PatrolData(Vector3.zero, Vector3.zero, Vector2.one, Vector2.zero));
            
            GameObject monster = _characterManager.CreateMonster(monsterData.Uid, characterRegenData, npcPrefab);
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

            if (usePatrolMonster)
            {
                var patrol = PatrolEditorFactory.CreateOrLinkPatrol(_defaultMap, monsterScript, characterRegenData.patrolData);
                if (patrol) monsterScript.SetPatrolObject(patrol.gameObject);
            }
            
            Debug.Log($"{monsterData.Name} 몬스터가 맵에 추가되었습니다.");
        }
        /// <summary>
        /// 배치한 몬스터 정보 json 으로 내보내기
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="mapUid"></param>
        /// <param name="struckTableMap"></param>
        public void ExportMonsterDataToJson(string filePath, string fileName, int mapUid, StruckTableMap struckTableMap)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            CharacterRegenDataList saveMonsterList = new CharacterRegenDataList();

            foreach (Transform child in mapObject.transform)
            {
                if (!child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) continue;
                var monster = child.gameObject.GetComponent<Monster>();
                if (!monster) continue;
                // 패트롤이 있는 경우
                PatrolData patrolData = null;
                if (monster.patrolObject)
                {
                    ObjectPatrol patrolObject = monster.patrolObject.GetComponent<ObjectPatrol>();
                    PatrolData existingData = patrolObject != null ? patrolObject.PatrolData : null;
                    BoxCollider2D patrolCollider = monster.patrolObject.GetComponent<BoxCollider2D>();
                    patrolData = new PatrolData(
                        monster.patrolObject.transform.position,
                        monster.patrolObject.transform.eulerAngles,
                        patrolCollider != null ? patrolCollider.size : Vector2.one,
                        patrolCollider != null ? patrolCollider.offset : Vector2.zero,
                        existingData != null ? existingData.EncounterId : 0,
                        existingData != null && existingData.ReleaseEncounterThreatOnExit);
                }

                saveMonsterList.CharacterRegenDatas.Add(new CharacterRegenData(monster.uid, child.position,
                    monster.isFlip, mapUid, true, 0, 0, monster.canMoveX, monster.canMoveY, patrolData));
                
                // map 라벨 붙여주기 
                // AddressableSettings 가져오기
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings)
                {
                    var info = _tableMonster.GetDataByUid(monster.uid);
                    if (info == null) continue;
                    var infoAnimation = _tableAnimation.GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation) + ".prefab";
                    // 기존 Addressable 항목 확인
                    AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                    string labelName = ConfigAddressableMap.GetLabel(struckTableMap.FolderName);
                    entry?.SetLabel(labelName, true, true);
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
                string content = AssetDatabaseLoaderManager.LoadFileJson(regenFileName);
                if (string.IsNullOrEmpty(content)) return;
                CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);
                _monsterList = regenDataList.CharacterRegenDatas;
                SpawnMonster();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file {regenFileName}: {ex.Message}");
            }
        }
        /// <summary>
        /// 몬스터 생성하기
        /// </summary>
        private void SpawnMonster()
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            foreach (CharacterRegenData monsterData in _monsterList)
            {
                int uid = monsterData.Uid;
                var info = _tableMonster.GetDataByUid(uid);
                if (info == null) continue;
                var infoAnimation = _tableAnimation.GetDataByUid(info.AnimationUid);
                if (infoAnimation == null) continue;
                
                string monsterPath = ConfigAddressableMap.GetPathCharacter(infoAnimation);
                GameObject monsterPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(monsterPath);
                GameObject monster = _characterManager.CreateMonster(uid, monsterData, monsterPrefab);
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
                    myMonsterScript.defaultFacingDirection8 = infoAnimation.DefaultFacingDirection8;
                    myMonsterScript.SetFlip(monsterData.IsFlip);
                    myMonsterScript.canMoveX = monsterData.CanMoveX;
                    myMonsterScript.canMoveY = monsterData.CanMoveY;
                    myMonsterScript.InitTagSortingLayer();
                }
                // npc 정보 보여줄 canvas 추가
                TextMeshProUGUI text = CreateInfoCanvas(myMonsterScript);
                text.text = $"Uid: {monsterData.Uid}\nPos: ({monsterData.x}, {monsterData.y})\nScale: {Math.Abs(monster.transform.localScale.x):F2}";

                var patrol = PatrolEditorFactory.CreateOrLinkPatrol(_defaultMap, myMonsterScript, monsterData.patrolData);
                if (patrol)
                {
                    myMonsterScript.SetPatrolObject(patrol.gameObject);
                }
            }

            Debug.Log("monster spawned successfully.");
        }
    }
}
