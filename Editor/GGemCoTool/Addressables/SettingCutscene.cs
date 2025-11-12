using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingCutscene : DefaultAddressable
    {
        private const string Title = "연출 추가하기";
        private readonly AddressableEditor _addressableEditor;
        
        public SettingCutscene(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = ConfigAddressableGroupName.Cutscene;
        }
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (_addressableEditor.TableCutscene == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Cutscene} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    Setup();
                }
            }
        }
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        private void Setup()
        {
            bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
            if (!result) return;
            
            Dictionary<int, StruckTableCutscene> dictionary = _addressableEditor.TableCutscene.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            
            ClearGroupEntries(settings, group);
            
            if (group)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, StruckTableCutscene> outerPair in dictionary)
                {
                    var info = outerPair.Value;
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressableKey.Cutscene}_{info.Uid}";
                    string assetPath = $"{ConfigAddressablePath.Narrative.Cutscene}/{info.FileName}.json";
                    string label = ConfigAddressableLabel.Cutscene;
                
                    Add(settings, group, key, assetPath, label);
                }
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
    }
}