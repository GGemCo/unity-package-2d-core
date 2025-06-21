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
    public class SettingTable : DefaultAddressable
    {
        private const string Title = "테이블 추가하기";
        private readonly EditorAddressable _editorAddressable;

        public SettingTable(EditorAddressable editorWindow)
        {
            _editorAddressable = editorWindow;
            TargetGroupName = "GGemCo_Tables";
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
                GcLogger.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, TargetGroupName);

            if (!group)
            {
                GcLogger.LogError($"'{TargetGroupName}' 그룹을 설정할 수 없습니다.");
                return;
            }

            foreach (var addressableAssetInfo in ConfigAddressableTable.All)
            {
                string assetPath = addressableAssetInfo.Path;
                // 대상 파일 가져오기
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (!asset)
                {
                    GcLogger.LogError($"파일을 찾을 수 없습니다: {assetPath}");
                    continue;
                }

                // 기존 Addressable 항목 확인
                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));

                if (entry == null)
                {
                    // 신규 Addressable 항목 추가
                    entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
                    GcLogger.Log($"Addressable 항목을 추가했습니다: {assetPath}");
                }
                else
                {
                    GcLogger.Log($"이미 Addressable에 등록된 항목입니다: {assetPath}");
                }

                // 키 값 설정
                entry.address = addressableAssetInfo.Key;
                // 라벨 값 설정
                entry.SetLabel(ConfigAddressableLabel.Table, true, true);

                // GcLogger.Log($"Addressable 키 값 설정: {keyName}");
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            // 테이블 다시 로드하기
            _editorAddressable.LoadTables();
            
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }

    }
}