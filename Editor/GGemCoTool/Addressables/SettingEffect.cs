using System.Collections.Generic;
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
    public class SettingEffect : DefaultAddressable
    {
        private const string Title = "이펙트 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingEffect(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = $"{ConfigAddressableGroupName.Effect}";
        }
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (_addressableEditor.TableEffect == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Effect} 테이블이 없습니다.", MessageType.Info);
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
            
            Dictionary<int, StruckTableEffect> dictionary = _addressableEditor.TableEffect.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            
            // 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);

            if (group)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, StruckTableEffect> outerPair in dictionary)
                {
                    var info = outerPair.Value;
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressableGroupName.Effect}_{info.PrefabName}";
                    string assetPath = $"{ConfigAddressablePath.BuildEffectPath(info.Category)}/{info.PrefabName}.prefab";
                    string label = ConfigAddressableLabel.Effect;
                
                    Add(settings, group, key, assetPath, label);
                }
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            // 테이블 다시 로드하기
            _addressableEditor.LoadTables();
            
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
    }
}