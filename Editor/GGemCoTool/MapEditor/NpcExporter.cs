using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
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
        /// 선택한 NPC를 현재 맵에 추가하고 배치 표시 정책을 초기화합니다.
        /// </summary>
        /// <param name="npcUid">추가할 NPC UID입니다.</param>
        /// <param name="defaultVisible">런타임 스폰 직후 기본 보임 여부입니다.</param>
        /// <param name="mapVisibilityPolicy">카메라 컬링보다 우선 적용할 맵 표시 정책입니다.</param>
        public void AddNpcToMap(
            int npcUid,
            bool defaultVisible,
            MapCharacterVisibilityPolicy mapVisibilityPolicy)
        {
            if (!_defaultMap)
            {
                Debug.LogError("_defaultMap 이 없습니다.");
                return;
            }

            if (npcUid <= 0)
            {
                Debug.LogWarning("선택된 NPC Uid 가 유효하지 않습니다.");
                return;
            }

            StruckTableNpc npcData = _tableNpc.GetDataByUid(npcUid);
            if (npcData == null || npcData.Uid <= 0)
            {
                Debug.LogError($"NPC 데이터를 찾을 수 없습니다. uid:{npcUid}");
                return;
            }
            var infoAnimation = _tableAnimation.GetDataByUid(npcData.AnimationUid);
            if (infoAnimation == null) return;
            
            string npcPath = ConfigAddressableMap.GetPathCharacter(infoAnimation);
            
            // Addressable 에 등록되어있는지 체크 
            if (!HelperEditorUI.ExistAddressableByPath(ConfigAddressableMap.GetPathCharacter(infoAnimation, true))) return;

            GameObject npcPrefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(npcPath);
            
            int mapUid = _defaultMap.GetChapterNumber();
            CharacterRegenData characterRegenData = new CharacterRegenData(
                npcData.Uid,
                Vector3.zero,
                false,
                mapUid,
                defaultVisible,
                mapVisibilityPolicy: mapVisibilityPolicy);
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
                NpcPlacementEditorUtility.ApplyPlacementPolicy(
                    npcScript,
                    mapUid,
                    defaultVisible,
                    isFlip: false,
                    mapVisibilityPolicy: mapVisibilityPolicy);
            }
            
            // npc 정보 보여줄 canvas 추가
            CreateInfoCanvas(npcScript);
            NpcPlacementEditorUtility.UpdateInfoText(npcScript);

            Debug.Log($"{npcData.Name} NPC가 맵에 추가되었습니다.");
        }
        /// <summary>
        /// 배치한 정보 json 으로 내보내기
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        /// <param name="mapUid"></param>
        /// <param name="struckTableMap"></param>
        public void ExportNpcDataToJson(string filePath, string fileName, int mapUid, StruckTableMap struckTableMap)
        {
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            CharacterRegenDataList saveNpcList = new CharacterRegenDataList();

            foreach (Transform child in mapObject.transform)
            {
                if (!child.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Npc))) continue;
                var npc = child.gameObject.GetComponent<Npc>();
                if (!npc) continue;
                
                bool defaultVisible = ResolveDefaultVisibleFromNpc(npc, mapUid);
                bool isFlip = ResolveFlipFromNpc(npc, mapUid);
                MapCharacterVisibilityPolicy mapVisibilityPolicy =
                    ResolveMapVisibilityPolicyFromNpc(npc, mapUid);
                saveNpcList.CharacterRegenDatas.Add(new CharacterRegenData(
                    npc.uid,
                    child.position,
                    isFlip,
                    mapUid,
                    defaultVisible,
                    mapVisibilityPolicy: mapVisibilityPolicy));
                
                // map 라벨 붙여주기 
                // AddressableSettings 가져오기
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings)
                {
                    var info = _tableNpc.GetDataByUid(npc.uid);
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

            string json = JsonConvert.SerializeObject(saveNpcList);
            string path = Path.Combine(filePath, fileName);
            File.WriteAllText(path, json);
            Debug.Log("NPC data exported to " + path);
        }
        
        /// <summary>
        /// 배치된 NPC 컴포넌트에서 기본 보임 정책을 조회합니다.
        /// 리젠 데이터가 비어 있으면 현재 상태를 기준으로 보정합니다.
        /// </summary>
        /// <param name="npc">정책을 조회할 NPC 컴포넌트</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID</param>
        /// <returns>기본 보임 여부</returns>
        private static bool ResolveDefaultVisibleFromNpc(Npc npc, int fallbackMapUid)
        {
            if (npc == null)
            {
                return true;
            }

            return NpcPlacementEditorUtility.GetDefaultVisible(npc, fallbackMapUid);
        }

        /// <summary>
        /// 배치된 NPC 컴포넌트에서 Flip 정책 값을 조회합니다.
        /// 리젠 데이터가 비어 있으면 현재 상태를 기준으로 보정합니다.
        /// </summary>
        /// <param name="npc">정책을 조회할 NPC 컴포넌트</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID</param>
        /// <returns>Flip 여부</returns>
        private static bool ResolveFlipFromNpc(Npc npc, int fallbackMapUid)
        {
            if (npc == null)
            {
                return false;
            }

            return NpcPlacementEditorUtility.GetFlip(npc, fallbackMapUid);
        }

        /// <summary>
        /// 배치된 NPC 컴포넌트에서 맵 표시 정책을 조회합니다.
        /// 리젠 데이터가 비어 있으면 현재 런타임 상태를 기준으로 보정합니다.
        /// </summary>
        /// <param name="npc">정책을 조회할 NPC 컴포넌트입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <returns>현재 맵 표시 정책입니다.</returns>
        private static MapCharacterVisibilityPolicy ResolveMapVisibilityPolicyFromNpc(
            Npc npc,
            int fallbackMapUid)
        {
            if (npc == null)
            {
                return MapCharacterVisibilityPolicy.DefaultCulling;
            }

            return NpcPlacementEditorUtility.GetMapVisibilityPolicy(npc, fallbackMapUid);
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
                var infoAnimation = _tableAnimation.GetDataByUid(info.AnimationUid);
                if (infoAnimation == null) continue;
                
                string npcPath = ConfigAddressableMap.GetPathCharacter(infoAnimation);
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
                    myNpcScript.InitTagSortingLayer();
                    NpcPlacementEditorUtility.ApplyPlacementPolicy(
                        myNpcScript,
                        _defaultMap.GetChapterNumber(),
                        npcData.DefaultVisible,
                        npcData.IsFlip,
                        npcData.MapVisibilityPolicy);
                }
                
                // npc 정보 보여줄 canvas 추가
                CreateInfoCanvas(myNpcScript);
                NpcPlacementEditorUtility.UpdateInfoText(myNpcScript);
            }

            Debug.Log("NPCs spawned successfully.");
        }
    }
}
