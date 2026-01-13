using GGemCo2DCore;
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
            targetGroupName = ConfigAddressableGroupName.Common;
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
                    EditorUtility.DisplayDialog(Title, "설정 스크립터블 오브젝트 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
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

            // 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            // 설정 scriptable object
            foreach (var addressableAssetInfo in ConfigAddressableSetting.NeedLoadInLoadingScene)
            {
                Add(settings, group, addressableAssetInfo.Key, addressableAssetInfo.Path, addressableAssetInfo.Label);
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] Setting 스크립터블 오브젝트 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] Setting 스크립터블 오브젝트 설정 완료", "OK");
            }
        }
    }
}
