using System.Collections.Generic;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    [CustomEditor(typeof(GGemCoSettings))]
    public class SettingGGemCoInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GGemCoSettings settings = (GGemCoSettings)target;

            bool oldUseSpine = settings.useSpine2d;
            InputSystemType oldInputSystemType = settings.inputSystemType;
            bool oldUseInGameTime = settings.useInGameTime;

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (!changed)
            {
                return;
            }

            if (oldUseSpine != settings.useSpine2d)
            {
                UpdateScriptingDefineSymbols(settings.useSpine2d);
            }

            if (oldInputSystemType != settings.inputSystemType)
            {
                SyncInputDefineSymbols(settings.inputSystemType);
            }

            if (oldUseInGameTime != settings.useInGameTime)
            {
                SyncDefineSymbolUseInGameTime(settings.useInGameTime);
            }
        }

        public void UpdateScriptingDefineSymbols(bool enable)
        {
            List<string> symbols = GetCurrentDefineSymbols();

            if (enable)
            {
                AddSymbol(symbols, ConfigDefine.DefineSymbolSpine);
            }
            else
            {
                RemoveSymbol(symbols, ConfigDefine.DefineSymbolSpine);
            }

            ApplyDefineSymbols(symbols, "Scripting Define Symbols updated");
        }

        public void SyncInputDefineSymbols(InputSystemType inputType)
        {
            List<string> symbols = GetCurrentDefineSymbols();

            RemoveSymbol(symbols, ConfigDefine.DefineSymbolInputSystemOld);
            RemoveSymbol(symbols, ConfigDefine.DefineSymbolInputSystemNew);

            switch (inputType)
            {
                case InputSystemType.OldInputManager:
                    AddSymbol(symbols, ConfigDefine.DefineSymbolInputSystemOld);
                    break;
                case InputSystemType.NewInputSystem:
                    AddSymbol(symbols, ConfigDefine.DefineSymbolInputSystemNew);
                    break;
                case InputSystemType.Both:
                    AddSymbol(symbols, ConfigDefine.DefineSymbolInputSystemOld);
                    AddSymbol(symbols, ConfigDefine.DefineSymbolInputSystemNew);
                    break;
            }

            ApplyDefineSymbols(symbols, "[GGemCoSettingsEditor] Define Symbols 설정 완료");
        }

        private void SyncDefineSymbolUseInGameTime(bool enable)
        {
            List<string> symbols = GetCurrentDefineSymbols();

            if (enable)
            {
                AddSymbol(symbols, ConfigDefine.DefineSymbolUseInGameTime);
            }
            else
            {
                RemoveSymbol(symbols, ConfigDefine.DefineSymbolUseInGameTime);
            }

            ApplyDefineSymbols(symbols, "Scripting Define Symbols updated");
        }

        private static List<string> GetCurrentDefineSymbols()
        {
#if UNITY_6000_0_OR_NEWER
            string symbols = PlayerSettings.GetScriptingDefineSymbols(GetActiveNamedBuildTarget());
#else
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(GetActiveBuildTargetGroup());
#endif
            return SplitDefineSymbols(symbols);
        }

        private static void ApplyDefineSymbols(List<string> symbols, string logPrefix)
        {
            string symbolText = string.Join(";", symbols);

#if UNITY_6000_0_OR_NEWER
            NamedBuildTarget buildTarget = GetActiveNamedBuildTarget();
            PlayerSettings.SetScriptingDefineSymbols(buildTarget, symbolText);
            Debug.Log($"{logPrefix} ({buildTarget.TargetName}): {symbolText}");
#else
            BuildTargetGroup buildTargetGroup = GetActiveBuildTargetGroup();
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbolText);
            Debug.Log($"{logPrefix} ({buildTargetGroup}): {symbolText}");
#endif
        }

        private static List<string> SplitDefineSymbols(string symbols)
        {
            return symbols.Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();
        }

        private static void AddSymbol(List<string> symbols, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return;
            }

            if (!symbols.Contains(symbol))
            {
                symbols.Add(symbol);
            }
        }

        private static void RemoveSymbol(List<string> symbols, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return;
            }

            symbols.RemoveAll(s => s == symbol);
        }

#if UNITY_6000_0_OR_NEWER
        private static NamedBuildTarget GetActiveNamedBuildTarget()
        {
            BuildTargetGroup group = GetActiveBuildTargetGroup();
            return NamedBuildTarget.FromBuildTargetGroup(group);
        }
#endif

        private static BuildTargetGroup GetActiveBuildTargetGroup()
        {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);

            if (buildTargetGroup == BuildTargetGroup.Unknown)
            {
                return BuildTargetGroup.Standalone;
            }

            return buildTargetGroup;
        }
    }
}
