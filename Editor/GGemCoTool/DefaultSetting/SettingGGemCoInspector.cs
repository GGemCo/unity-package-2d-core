using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GGemCo.Editor
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

            // 기본 Inspector 표시
            DrawDefaultInspector();

            // 값이 변경되었을 경우 define 업데이트
            if (oldUseSpine != settings.useSpine2d)
            {
                UpdateScriptingDefineSymbols(settings.useSpine2d);
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