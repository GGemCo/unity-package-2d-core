using System;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingGGemCo
    {
        private const string Title = "설정 ScriptableObject 추가하기";
        private const string SettingsFolder = "Assets/"+ConfigDefine.NameSDK+"/Settings/";

        public void OnGUI()
        {
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button("설정 ScriptableObject 생성하기"))
            {
                CreateSettings();
            }
        }

        public void CreateSettings(EditorSetupContext ctx = null)
        {
            foreach (var kvp in ConfigScriptableObject.SettingsTypes)
            {
                CreateOrSelectSettings(kvp.Key, kvp.Value, ctx);
            }
        }

        private void CreateOrSelectSettings(string fileName, Type type, EditorSetupContext ctx = null)
        {
            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);

            string path = $"{SettingsFolder}{fileName}.asset";
            UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath(path, type);

            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorUtility.FocusProjectWindow();
                HelperLog.Warn($"{fileName} 설정이 이미 존재합니다.", ctx);
            }
            else
            {
                ScriptableObject asset = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(asset, path);
                if (ctx == null)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                Selection.activeObject = asset;
                EditorUtility.FocusProjectWindow();
                
                HelperLog.Info($"{fileName} ScriptableObject 가 생성되었습니다.", ctx);
            }

            // // 특정 설정에 따라 define 심볼 업데이트
            // if (type == typeof(GGemCoSettings))
            // {
            //     var config = existing ?? AssetDatabase.LoadAssetAtPath<GGemCoSettings>(path);
            //     if (config is GGemCoSettings settings)
            //     {
            //         UpdateScriptingDefineSymbols(settings.useSpine2d, ctx);
            //     }
            // }
        }

        private static void UpdateScriptingDefineSymbols(bool enable, EditorSetupContext ctx = null)
        {
#if UNITY_6000_0_OR_NEWER
            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#else
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
            if (enable)
            {
                if (!symbols.Contains(ConfigDefine.DefineSymbolSpine))
                {
                    symbols += $";{ConfigDefine.DefineSymbolSpine}";
                }
            }
            else
            {
                if (symbols.Contains(ConfigDefine.DefineSymbolSpine))
                {
                    symbols = symbols.Replace(ConfigDefine.DefineSymbolSpine, "").Replace(";;", ";").Trim(';');
                }
            }

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
#endif
            
            HelperLog.Info($"Scripting Define Symbols updated: {symbols}", ctx);
        }
    }
}
