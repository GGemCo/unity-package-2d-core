using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCoreEditor
{
    public class PatrolExporter
    {
        private List<PatrolData> _patrolDatas;
        private DefaultMap _defaultMap;

        public void Initialize(DefaultMap pDefaultMap)
        {
            _defaultMap = pDefaultMap;
        }

        public void SetDefaultMap(DefaultMap pDefaultMap)
        {
            _defaultMap = pDefaultMap;
        }
        public void AddPatrolToMap()
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }
            
            GameObject patrolPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(ConfigAddressableMap.ObjectPatrol.Path);
            if (!patrolPrefab)
            {
                Debug.LogError("Patrol prefab is null.");
                return;
            }

            GameObject patrol = Object.Instantiate(patrolPrefab, Vector3.zero, Quaternion.identity, _defaultMap.transform);

            var objectPatrol = patrol.GetComponent<ObjectPatrol>();
            if (!objectPatrol)
            {
                Debug.LogError("ObjectPatrol script missing.");
                return;
            }

            // MapManager.cs:164 도 수정
            objectPatrol.PatrolData = new PatrolData(_defaultMap.GetChapterNumber(), Vector3.zero, 0, Vector2.zero, Vector2.one, Vector3.zero);
            objectPatrol.InitializeByMapEditor();
            Debug.Log("Patrol added to the map.");
        }

        public void ExportPatrolDataToJson(string filePath, string fileName, int mapUid)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            PatrolDataList patrolDataList = new PatrolDataList();

            foreach (Transform child in mapObject.transform)
            {
                if (!child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapObjectPatrol))) continue;
                var objectPatrol = child.gameObject.GetComponent<ObjectPatrol>();
                if (!objectPatrol) continue;
                PatrolData patrolData = new PatrolData(
                    mapUid,
                    child.position,
                    objectPatrol.monsterUid,
                    child.transform.eulerAngles,
                    child.GetComponent<BoxCollider2D>().size,
                    child.GetComponent<BoxCollider2D>().offset
                    );
                patrolDataList.patrolDataList.Add(patrolData);
            }

            string json = JsonConvert.SerializeObject(patrolDataList);
            string path = Path.Combine(filePath, fileName);
            File.WriteAllText(path, json);
            Debug.Log("Patrol data exported to " + path);
        }
        
        public void LoadJsonData(string regenFileName)
        {
            // JSON 파일을 읽기
            try
            {
                string content = AssetDatabaseLoaderManager.LoadFileJson(regenFileName);
                if (string.IsNullOrEmpty(content)) return;
                PatrolDataList patrolDataList = JsonConvert.DeserializeObject<PatrolDataList>(content);
                _patrolDatas = patrolDataList.patrolDataList;
                SpawnPatrols();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file {regenFileName}: {ex.Message}");
            }
        }
        private void SpawnPatrols()
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            GameObject patrolPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(ConfigAddressableMap.ObjectPatrol.Path);
            if (!patrolPrefab)
            {
                Debug.LogError("워프 프리팹이 없습니다. ");
                return;
            }
            foreach (PatrolData patrolData in _patrolDatas)
            {
                // int toMapUid = patrolData.ToMapUid;
                // if (toMapUid <= 0) continue;
                GameObject patrol = Object.Instantiate(patrolPrefab, _defaultMap.gameObject.transform);
                
                // NPC의 속성을 설정하는 스크립트가 있을 경우 적용
                ObjectPatrol objectPatrol = patrol.GetComponent<ObjectPatrol>();
                if (objectPatrol)
                {
                    // MapManager.cs:164 도 수정
                    objectPatrol.PatrolData = patrolData;
                    objectPatrol.InitializeByMapEditor();
                }
            }

            Debug.Log("패트롤 영역 생성 완료.");
        }
    }
}
