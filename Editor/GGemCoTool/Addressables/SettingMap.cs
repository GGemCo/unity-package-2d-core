using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
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
                
                AddressableCharacterLabelUtility.ApplyMapLabelFromRegen(
                    settings,
                    mapFolderName: info.FolderName,
                    regenJsonAssetPath: assetPath,
                    type: AddressableCharacterType.Monster,
                    clearExistingLabel: true
                );

                // 웨이브 스폰 파일은 맵별 선택 파일이므로, 파일이 있을 때만 Addressables에 등록합니다.
                key = ConfigAddressableMap.GetKeyJsonWaveSpawn(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathWaveSpawn(info.FolderName);
                if (File.Exists(assetPath))
                {
                    Add(settings, group, key, assetPath);
                    AddressableCharacterLabelUtility.ApplyMapLabelFromWaveSpawn(
                        settings,
                        mapFolderName: info.FolderName,
                        waveSpawnJsonAssetPath: assetPath,
                        clearExistingLabel: false
                    );
                }
                
                // npc 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonRegenNpc(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathRegenNpc(info.FolderName);
                Add(settings, group, key, assetPath);
                
                AddressableCharacterLabelUtility.ApplyMapLabelFromRegen(
                    settings,
                    mapFolderName: info.FolderName,
                    regenJsonAssetPath: assetPath,
                    type: AddressableCharacterType.Npc,
                    clearExistingLabel: true
                );
                
                // 워프 리젠 파일
                key = ConfigAddressableMap.GetKeyJsonWarp(info.FolderName);
                assetPath = ConfigAddressableMap.GetAssetPathWarp(info.FolderName);
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
    }
}