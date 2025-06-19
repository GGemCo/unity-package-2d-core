using System;
using System.Collections.Generic;
using System.IO;
using GGemCo.Scripts;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo.Editor
{
    public class WarpExporter
    {
        private List<WarpData> warpDatas;
        private DefaultMap defaultMap;

        public void Initialize(DefaultMap pDefaultMap)
        {
            defaultMap = pDefaultMap;
        }

        public void SetDefaultMap(DefaultMap pDefaultMap)
        {
            defaultMap = pDefaultMap;
        }
        public void AddWarpToMap()
        {
            if (defaultMap == null)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            GameObject warpPrefab = Resources.Load<GameObject>(MapConstants.PathPrefabWarp);
            if (warpPrefab == null)
            {
                Debug.LogError("Warp prefab is null.");
                return;
            }

            GameObject warp = Object.Instantiate(warpPrefab, Vector3.zero, Quaternion.identity, defaultMap.transform);

            var objectWarp = warp.GetComponent<ObjectWarp>();
            if (objectWarp == null)
            {
                Debug.LogError("ObjectWarp script missing.");
                return;
            }

            Debug.Log("Warp added to the map.");
        }

        public void ExportWarpDataToJson(string filePath, string fileName, int mapUid)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            WarpDataList warpDataList = new WarpDataList();

            foreach (Transform child in mapObject.transform)
            {
                if (child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapObjectWarp)))
                {
                    var objectWarp = child.gameObject.GetComponent<ObjectWarp>();
                    if (objectWarp == null) continue;
                    WarpData warpData = new WarpData(
                        mapUid,child.position,
                        objectWarp.toMapUid,
                        objectWarp.toMapPlayerSpawnPosition,
                        child.transform.eulerAngles,
                        child.GetComponent<BoxCollider2D>().size,
                        child.GetComponent<BoxCollider2D>().offset);
                    warpDataList.warpDataList.Add(warpData);
                }
            }

            string json = JsonConvert.SerializeObject(warpDataList);
            string path = Path.Combine(filePath, fileName);
            File.WriteAllText(path, json);
            Debug.Log("Warp data exported to " + path);
        }
        
        public void LoadWarpData(string regenFileName)
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
                        WarpDataList warpDataList = JsonConvert.DeserializeObject<WarpDataList>(content);
                        warpDatas = warpDataList.warpDataList;
                        SpawnWarps();
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"Error reading file {regenFileName}: {ex.Message}");
            }
        }
        private void SpawnWarps()
        {
            if (defaultMap == null)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            GameObject warpPrefab = Resources.Load<GameObject>(MapConstants.PathPrefabWarp);
            if (warpPrefab == null)
            {
                GcLogger.LogError("워프 프리팹이 없습니다. ");
                return;
            }
            foreach (WarpData warpData in warpDatas)
            {
                // int toMapUid = warpData.ToMapUid;
                // if (toMapUid <= 0) continue;
                GameObject warp = Object.Instantiate(warpPrefab, defaultMap.gameObject.transform);
                
                // NPC의 속성을 설정하는 스크립트가 있을 경우 적용
                ObjectWarp objectWarp = warp.GetComponent<ObjectWarp>();
                if (objectWarp != null)
                {
                    // MapManager.cs:164 도 수정
                    objectWarp.WarpData = warpData;
                }
            }

            Debug.Log("워프 spawned successfully.");
        }
    }
}
