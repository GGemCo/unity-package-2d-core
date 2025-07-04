﻿using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 설정 ScriptableObject 등록하기
    /// </summary>
    public class SettingScriptableObject : DefaultAddressable
    {
        private const string Title = "설정 ScriptableObject 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingScriptableObject(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            TargetGroupName = ConfigAddressableGroupName.Common;
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
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, TargetGroupName);

            if (!group)
            {
                Debug.LogError($"'{TargetGroupName}' 그룹을 설정할 수 없습니다.");
                return;
            }

            // 설정 scriptable object
            foreach (var addressableAssetInfo in ConfigAddressableSetting.NeedLoadInLoadingScene)
            {
                Add(settings, group, addressableAssetInfo);
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }

        private void Add(AddressableAssetSettings settings, AddressableAssetGroup group, AddressableAssetInfo addressableAssetInfo)
        {
            string assetPath = addressableAssetInfo.Path;
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
            entry.address = addressableAssetInfo.Key;
            // 라벨 값 설정
            if (!string.IsNullOrEmpty(addressableAssetInfo.Label))
            {
                entry.SetLabel(addressableAssetInfo.Label, true, true);
            }

            // Debug.Log($"Addressable 키 값 설정: {keyName}");
        }
    }
}
