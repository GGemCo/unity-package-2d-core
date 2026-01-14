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
    public class SettingTable : DefaultAddressable
    {
        private const string Title = "테이블 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingTable(AddressableEditor addressableEditorWindow = null)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = ConfigAddressableGroupName.Table;
        }
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
            {
                try
                {
                    Setup();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog(Title, "데이터 테이블 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                }
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        public void Setup(EditorSetupContext ctx = null)
        {
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);

            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            foreach (var addressableAssetInfo in ConfigAddressableTable.All)
            {
                Add(settings, group, addressableAssetInfo.Key, addressableAssetInfo.Path, ConfigAddressableLabel.Table);
                // Debug.Log($"Addressable 키 값 설정: {keyName}");
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);

            if (ctx != null)
            {
                HelperLog.Info($"Addressable 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");    
            }
        }
    }
}