using System.Collections.Generic;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 테이블 등록하기
    /// </summary>
    public class SettingMap : DefaultAddressable
    {
        private const string Title = "맵 추가하기";
        private readonly AddressableEditor _addressableEditor;
        private readonly TableMonster _tableMonster;
        private readonly TableNpc _tableNpc;
        private readonly TableAnimation _tableAnimation;
        private enum Type
        {
            Npc,
            Monster
        }

        public SettingMap(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            TargetGroupName = ConfigAddressableGroupName.Map;
            
            _tableMonster = _addressableEditor.TableMonster;
            _tableNpc = _addressableEditor.TableNpc;
            _tableAnimation = _addressableEditor.TableAnimation;
        }
        public void OnGUI()
        {
            Common.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                Setup();
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        private void Setup()
        {
            Dictionary<int, Dictionary<string, string>> dictionaryMap = _addressableEditor.TableMap.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }
            
            // object 셋팅하기
            // 현재는 warp object 처리 중
            AddressableAssetGroup group = GetOrCreateGroup(settings, ConfigAddressableGroupName.Common);

            if (group)
            {
                foreach (var addressableAssetInfo in ConfigAddressableMap.NeedLoadInLoadingScene)
                {
                    Add(settings, group, addressableAssetInfo.Key, addressableAssetInfo.Path, addressableAssetInfo.Label);
                }
            }
            
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryMap)
            {
                var info = _addressableEditor.TableMap.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;

                string groupName = $"{TargetGroupName}_{info.FolderName}";
                // GGemCo_Tables 그룹 가져오기 또는 생성
                group = GetOrCreateGroup(settings, groupName);

                if (!group)
                {
                    Debug.LogError($"'{TargetGroupName}' 그룹을 설정할 수 없습니다.");
                    return;
                }
                
                // 타일맵 프리팹
                string key = ConfigAddressableMap.GetKeyTileMap(info.FolderName);
                string assetPath = ConfigAddressableMap.GetAssetPathTileMap(info.FolderName);
                Add(settings, group, key, assetPath, key);
                
                // monster 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonRegenMonster(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathRegenMonster(info.FolderName);
                Add(settings, group, key, assetPath, key);

                SetCharacterLabel(assetPath, info, settings, Type.Monster);
                
                // npc 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonRegenNpc(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathRegenNpc(info.FolderName);
                Add(settings, group, key, assetPath, key);
                
                SetCharacterLabel(assetPath, info, settings, Type.Npc);
                
                // 워프 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonWarp(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathWarp(info.FolderName);
                Add(settings, group, key, assetPath, key);
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
        /// <summary>
        /// regen_monster, regen_npc 정보로 캐릭터 label 설정하기
        /// </summary>
        private void SetCharacterLabel(string regenFileName, StruckTableMap struckTableMap, AddressableAssetSettings settings, Type type)
        {
            if (_tableMonster == null || _tableNpc == null || _tableAnimation == null) return;
            string labelName = ConfigAddressableMap.GetLabel(struckTableMap.FolderName);
            if (string.IsNullOrEmpty(labelName)) return;
            
            string content = AssetDatabaseLoaderManager.LoadFileJson(regenFileName);
            if (string.IsNullOrEmpty(content)) return;
            CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);

            foreach (CharacterRegenData characterRegenData in regenDataList.CharacterRegenDatas)
            {
                int uid = characterRegenData.Uid;
                int spineUid = 0;
                if (type == Type.Monster)
                {
                    var info = _tableMonster.GetDataByUid(uid);
                    if (info == null) continue;
                    spineUid = info.SpineUid;
                }
                else if (type == Type.Npc)
                {
                    var info = _tableNpc.GetDataByUid(uid);
                    if (info == null) continue;
                    spineUid = info.SpineUid;
                }
                if (spineUid <= 0) continue;

                var infoAnimation = _tableAnimation.GetDataByUid(spineUid);
                if (infoAnimation == null) continue;
                string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation.PrefabPath) + ".prefab";
                // 기존 Addressable 항목 확인
                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                entry?.SetLabel(labelName, true, true);
            }
        }
    }
}