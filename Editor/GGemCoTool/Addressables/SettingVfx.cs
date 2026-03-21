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
    public class SettingVfx : DefaultAddressable
    {
        private const string Title = "Vfx 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingVfx(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = $"{ConfigAddressableGroupName.Vfx}";
        }
        public void OnGUI()
        {
            bool hasEffect = File.Exists(ConfigAddressableTable.TableVfxEffect.Path);
            bool hasParticle = File.Exists(ConfigAddressableTable.TableVfxParticle.Path);

            if (!hasEffect && !hasParticle)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.VfxEffect}, {ConfigAddressableTable.VfxParticle} 테이블이 없습니다.", MessageType.Info);
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
                        EditorUtility.DisplayDialog(Title, "VFX Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                    }
                }
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        public void Setup(EditorSetupContext ctx = null)
        {
            if (ctx == null)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result) return;
            }
            
            Dictionary<int, VfxRuntimeData> dictionary = TableLoaderManager.LoadVfxRuntimeData() ?? new Dictionary<int, VfxRuntimeData>();
            
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
            
            // 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);

            if (group)
            {
                var registeredPrefabPaths = new HashSet<string>();

                foreach (KeyValuePair<int, VfxRuntimeData> outerPair in dictionary)
                {
                    var info = outerPair.Value;
                    if (info == null || info.Uid <= 0 || string.IsNullOrWhiteSpace(info.PrefabPath))
                        continue;

                    if (!registeredPrefabPaths.Add(info.PrefabPath))
                        continue;

                    string key = $"{ConfigAddressableGroupName.Vfx}_{info.PrefabPath}";
                    string assetPath = $"{ConfigAddressablePath.Vfx.RootVfx}/{info.PrefabPath}.prefab";
                    string label = ConfigAddressableLabel.Vfx;

                    Add(settings, group, key, assetPath, label);
                }
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] VFX 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] VFX 설정 완료", "OK");
            }
        }
    }
}