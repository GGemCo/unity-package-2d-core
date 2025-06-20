using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 테이블 등록하기
    /// </summary>
    public class SettingMap : DefaultAddressable
    {
        private const string Title = "맵 추가하기";
        private readonly EditorAddressable _editorAddressable;

        public SettingMap(EditorAddressable editorWindow)
        {
            _editorAddressable = editorWindow;
            TargetGroupName = "GGemCo_Map";
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
            Dictionary<int, Dictionary<string, string>> dictionaryMonsters = _editorAddressable.TableMap.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                GcLogger.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }
            
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryMonsters)
            {
                var info = _editorAddressable.TableMap.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;

                string groupName = $"{TargetGroupName}_{info.FolderName}";
                // GGemCo_Tables 그룹 가져오기 또는 생성
                AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);

                if (!group)
                {
                    GcLogger.LogError($"'{TargetGroupName}' 그룹을 설정할 수 없습니다.");
                    return;
                }
                
                // 타일맵 프리팹
                string key = $"{ConfigAddressables.LabelMap}_{info.FolderName}_{MapConstants.FileNameTilemap}";
                string assetPath = $"{ConfigAddressables.PathMap}/{info.FolderName}/{MapConstants.FileNameTilemap}.prefab";
                Add(settings, group, key, assetPath, key);
                
                // // monster 리젠 파일
                key = $"{ConfigAddressables.LabelMap}_{info.FolderName}_{MapConstants.FileNameRegenMonster}";
                assetPath = $"{ConfigAddressables.PathMap}/{info.FolderName}/{MapConstants.FileNameRegenMonster}{MapConstants.FileExt}";
                Add(settings, group, key, assetPath, key);
                
                // // npc 리젠 파일
                key = $"{ConfigAddressables.LabelMap}_{info.FolderName}_{MapConstants.FileNameRegenNpc}";
                assetPath = $"{ConfigAddressables.PathMap}/{info.FolderName}/{MapConstants.FileNameRegenNpc}{MapConstants.FileExt}";
                Add(settings, group, key, assetPath, key);
                
                // // 워프 리젠 파일
                key = $"{ConfigAddressables.LabelMap}_{info.FolderName}_{MapConstants.FileNameWarp}";
                assetPath = $"{ConfigAddressables.PathMap}/{info.FolderName}/{MapConstants.FileNameWarp}{MapConstants.FileExt}";
                Add(settings, group, key, assetPath, key);
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }

        private void Add(AddressableAssetSettings settings, AddressableAssetGroup group, string keyName, string assetPath, string labelName)
        {
            // 대상 파일 가져오기
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (!asset)
            {
                Debug.LogError($"파일을 찾을 수 없습니다: {assetPath}");
                return;
            }

            // 기존 Addressable 항목 확인
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));

            if (entry == null)
            {
                // 신규 Addressable 항목 추가
                entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
                Debug.Log($"Addressable 항목을 추가했습니다: {assetPath}");
            }
            else
            {
                Debug.Log($"이미 Addressable에 등록된 항목입니다: {assetPath}");
            }

            // 키 값 설정
            entry.address = keyName;
            // 라벨 값 설정
            entry.SetLabel(labelName, true, true);

            // GcLogger.Log($"Addressable 키 값 설정: {keyName}");
        }

    }
}