using System;
using System.Collections.Generic;
using System.IO;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GGemCo.Editor
{
    public class SettingGGemCo
    {
        private readonly string title = "설정 ScriptableObject 추가하기";
        private const string SettingsFolder = "Assets/GGemCo/Settings/";

        private readonly Dictionary<string, Type> settingsTypes = new()
        {
            { "GGemCoSettings", typeof(GGemCoSettings) },
            { "GGemCoMapSettings", typeof(GGemCoMapSettings) },
            { "GGemCoPlayerSettings", typeof(GGemCoPlayerSettings) },
            { "GGemCoSaveSettings", typeof(GGemCoSaveSettings) }
        };

        public void OnGUI()
        {
            Common.OnGUITitle(title);

            if (GUILayout.Button("설정 ScriptableObject 생성하기"))
            {
                CreateSettings();
            }
            // foreach (var kvp in settingsTypes)
            // {
            //     if (GUILayout.Button($"{kvp.Key} 생성"))
            //     {
            //         CreateOrSelectSettings(kvp.Key, kvp.Value);
            //     }
            // }
        }

        private void CreateSettings()
        {
            foreach (var kvp in settingsTypes)
            {
                CreateOrSelectSettings(kvp.Key, kvp.Value);
            }
        }

        private void CreateOrSelectSettings(string fileName, Type type)
        {
            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);

            string path = $"{SettingsFolder}{fileName}.asset";
            UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath(path, type);

            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorUtility.FocusProjectWindow();
                Debug.Log($"{fileName} 설정이 이미 존재합니다.");
            }
            else
            {
                ScriptableObject asset = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = asset;
                EditorUtility.FocusProjectWindow();
                Debug.Log($"{fileName} ScriptableObject 가 생성되었습니다.");
            }

            // 특정 설정에 따라 define 심볼 업데이트
            if (type == typeof(GGemCoSettings))
            {
                var config = existing ?? AssetDatabase.LoadAssetAtPath<GGemCoSettings>(path);
                if (config is GGemCoSettings settings)
                {
                    UpdateScriptingDefineSymbols(settings.useSpine2d);
                }
            }
        }

        private static void UpdateScriptingDefineSymbols(bool enable)
        {
#if UNITY_6000_0_OR_NEWER
            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#else
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
            if (enable)
            {
                if (!symbols.Contains(ConfigDefine.SpineDefineSymbol))
                {
                    symbols += $";{ConfigDefine.SpineDefineSymbol}";
                }
            }
            else
            {
                if (symbols.Contains(ConfigDefine.SpineDefineSymbol))
                {
                    symbols = symbols.Replace(ConfigDefine.SpineDefineSymbol, "").Replace(";;", ";").Trim(';');
                }
            }

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
#endif
            Debug.Log($"Scripting Define Symbols updated: {symbols}");
        }
    }
}
