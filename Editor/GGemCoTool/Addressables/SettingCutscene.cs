using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class SettingCutsceneOptions
    {
        public bool ShowConfirmDialog = true;
        public bool ShowCompletedDialog = true;
        public EditorSetupContext Context;
    }

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
            if (!File.Exists($"{ConfigAddressableTable.TableCutscene.Path}"))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Cutscene} 테이블이 없습니다.", MessageType.Info);
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
                        EditorUtility.DisplayDialog(Title, "연출 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                    }
                }
            }
        }
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        public void Setup(EditorSetupContext ctx = null)
        {
            SyncFromTable(new SettingCutsceneOptions
            {
                Context = ctx,
                ShowConfirmDialog = ctx == null,
                ShowCompletedDialog = ctx == null,
            });
        }

        public static void SyncFromTable(SettingCutsceneOptions options = null)
        {
            options ??= new SettingCutsceneOptions();
            EditorSetupContext ctx = options.Context;

            if (options.ShowConfirmDialog)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result)
                    return;
            }

            Dictionary<int, StruckTableCutscene> dictionary = TableLoaderManager.LoadCutsceneTable(true).GetDatas();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = new SettingCutscene(null).CreateAddressableSettings();
            }

            SettingCutscene helper = new SettingCutscene(null);
            AddressableAssetGroup group = helper.GetOrCreateGroup(settings, helper.targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{helper.targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            ClearGroupEntries(settings, group);

            foreach (KeyValuePair<int, StruckTableCutscene> outerPair in dictionary)
            {
                StruckTableCutscene info = outerPair.Value;
                if (info.Uid <= 0)
                    continue;

                string key = $"{ConfigAddressableKey.Cutscene}_{info.Uid}";
                string assetPath = $"{ConfigAddressablePath.Narrative.Cutscene}/{info.FileName}.json";
                string label = info.PreLoad ? ConfigAddressableLabel.Cutscene : string.Empty;

                helper.Add(settings, group, key, assetPath, label);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 연출 설정 완료", ctx);
            }
            else if (options.ShowCompletedDialog)
            {
                EditorUtility.DisplayDialog(Title, "[Addressable] 연출 설정 완료", "OK");
            }
        }
    }
}