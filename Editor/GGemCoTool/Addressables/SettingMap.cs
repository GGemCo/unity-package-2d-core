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
    /// 테이블 등록하기
    /// </summary>
    public class SettingMap : DefaultAddressable
    {
        private const string Title = "맵 추가하기";
        private readonly AddressableEditor _addressableEditor;
        private enum Type
        {
            Npc,
            Monster
        }

        public SettingMap(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = ConfigAddressableGroupName.Map;
        }
        public void OnGUI()
        {
            if (!File.Exists($"{ConfigAddressableTable.TableMap.Path}"))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Map} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(Title, "맵 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                    }
                }
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        public void Setup(EditorSetupContext ctx = null)
        {
            Dictionary<int, StruckTableMap> dictionaryMap = TableLoaderManager.LoadMapTable().GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }
            
            // object 셋팅하기
            // 현재는 warp object 처리 중
            AddressableAssetGroup group = GetOrCreateGroup(settings, ConfigAddressableGroupName.Common);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            foreach (var addressableAssetInfo in ConfigAddressableMap.NeedLoadInLoadingScene)
            {
                Add(settings, group, addressableAssetInfo.Key, addressableAssetInfo.Path, addressableAssetInfo.Label);
            }
            
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, StruckTableMap> outerPair in dictionaryMap)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;

                string groupName = $"{targetGroupName}_{info.FolderName}";
                // GGemCo_Tables 그룹 가져오기 또는 생성
                group = GetOrCreateGroup(settings, groupName);

                if (!group)
                {
                    HelperLog.Warn($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                    return;
                }
                
                // 타일맵 프리팹
                string key = ConfigAddressableMap.GetKeyTileMap(info.FolderName);
                string assetPath = ConfigAddressableMap.GetAssetPathTileMap(info.FolderName);
                Add(settings, group, key, assetPath);
                
                // monster 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonRegenMonster(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathRegenMonster(info.FolderName);
                Add(settings, group, key, assetPath);
                
                SetCharacterLabel(assetPath, info, settings, Type.Monster);
                
                // npc 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonRegenNpc(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathRegenNpc(info.FolderName);
                Add(settings, group, key, assetPath);
                
                SetCharacterLabel(assetPath, info, settings, Type.Npc);
                
                // 워프 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonWarp(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathWarp(info.FolderName);
                Add(settings, group, key, assetPath);
                
                // 패트롤 파일
                key = ConfigAddressableMap.GetKeyJsonPatrol(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathPatrol(info.FolderName);
                Add(settings, group, key, assetPath);
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 맵 설정 완료. 사용안하는 맵 Group은 삭제해주세요.", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] 맵 설정 완료\n사용안하는 맵 Group은 삭제해주세요.", "OK");
            }
        }
        /// <summary>
        /// regen_monster, regen_npc 정보로 캐릭터 label 설정하기
        /// </summary>
        private void SetCharacterLabel(string regenFileName, StruckTableMap struckTableMap, AddressableAssetSettings settings, Type type)
        {
            string labelName = ConfigAddressableMap.GetLabel(struckTableMap.FolderName);
            if (string.IsNullOrEmpty(labelName)) return;

            // 기존에 설정된 map 라벨은 삭제
            RemoveCharacterMapLabel(settings, type, labelName);
            
            string content = AssetDatabaseLoaderManager.LoadFileJson(regenFileName);
            if (string.IsNullOrEmpty(content)) return;
            CharacterRegenDataList regenDataList = JsonConvert.DeserializeObject<CharacterRegenDataList>(content);

            foreach (CharacterRegenData characterRegenData in regenDataList.CharacterRegenDatas)
            {
                int uid = characterRegenData.Uid;
                int spineUid = 0;
                if (type == Type.Monster)
                {
                    var info = TableLoaderManager.LoadMonsterTable().GetDataByUid(uid);
                    if (info == null) continue;
                    spineUid = info.AnimationUid;
                }
                else if (type == Type.Npc)
                {
                    var info = TableLoaderManager.LoadNpcTable().GetDataByUid(uid);
                    if (info == null) continue;
                    spineUid = info.AnimationUid;
                }
                if (spineUid <= 0) continue;

                var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(spineUid);
                if (infoAnimation == null) continue;
                string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation) + ".prefab";
                // 기존 Addressable 항목 확인
                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                entry?.SetLabel(labelName, true, true);
            }
        }

        private void RemoveCharacterMapLabel(AddressableAssetSettings settings, Type type, string labelName)
        {
            if (type == Type.Monster)
            {
                Dictionary<int, StruckTableMonster> datas = TableLoaderManager.LoadMonsterTable().GetDatas();
                foreach (KeyValuePair<int, StruckTableMonster> outerPair in datas)
                {
                    var info = outerPair.Value;
                    if (info == null) continue;
                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, true);
                    // 기존 Addressable 항목 확인
                    AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                    entry?.SetLabel(labelName, false, true);
                }
            }
            else if (type == Type.Npc)
            {
                Dictionary<int, StruckTableNpc> datas = TableLoaderManager.LoadNpcTable().GetDatas();
                foreach (KeyValuePair<int, StruckTableNpc> outerPair in datas)
                {
                    var info = outerPair.Value;
                    if (info == null) continue;
                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, true);
                    // 기존 Addressable 항목 확인
                    AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                    entry?.SetLabel(labelName, false, true);
                }
            }
        }
    }
}