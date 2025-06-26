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

            // 기존 use_spine 값 저장
            bool oldUseSpine = settings.useSpine2d;
            InputSystemType oldInputSystemType = settings.inputSystemType;

            // 기본 Inspector 표시
            DrawDefaultInspector();

            // 값이 변경되었을 경우 define 업데이트
            if (oldUseSpine != settings.useSpine2d)
            {
                UpdateScriptingDefineSymbols(settings.useSpine2d);
            }
            
            // 값이 변경되었을 경우 define 업데이트
            if (oldInputSystemType != settings.inputSystemType)
            {
                SyncInputDefineSymbols(settings.inputSystemType);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateScriptingDefineSymbols(bool enable)
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
            Debug.Log($"Scripting Define Symbols updated: {symbols}");
        }
        private void SyncInputDefineSymbols(GGemCo2DCore.InputSystemType inputType)
        {
#if UNITY_6000_0_OR_NEWER
            string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
#else
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif

            // 기존 Define 제거
            symbols = symbols.Replace(ConfigDefine.DefineSymbolInputSystemOld, "")
                .Replace(ConfigDefine.DefineSymbolInputSystemNew, "")
                .Replace($"{ConfigDefine.DefineSymbolInputSystemOld};{ConfigDefine.DefineSymbolInputSystemNew}", "");

            switch (inputType)
            {
                case GGemCo2DCore.InputSystemType.OldInputManager:
                    symbols += $";{ConfigDefine.DefineSymbolInputSystemOld}";
                    break;
                case GGemCo2DCore.InputSystemType.NewInputSystem:
                    symbols += $";{ConfigDefine.DefineSymbolInputSystemNew}";
                    break;
                case GGemCo2DCore.InputSystemType.Both:
                    symbols += $";{ConfigDefine.DefineSymbolInputSystemOld};{ConfigDefine.DefineSymbolInputSystemNew}";
                    break;
            }

            // 앞뒤 세미콜론 정리
            symbols = string.Join(";", symbols.Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct());

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
#endif
            Debug.Log($"[GGemCoSettingsEditor] Define Symbols 설정 완료: {symbols}");
        }
    }
}