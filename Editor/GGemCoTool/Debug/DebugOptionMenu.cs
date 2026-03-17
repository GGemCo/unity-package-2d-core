#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 디버그 옵션 상태를 확인하고 일괄 정리하기 위한 메뉴 명령입니다.
    /// </summary>
    public static class DebugOptionMenu
    {
        [MenuItem(ConfigEditor.NameToolListEnabledDebugOptions, false, (int)ConfigEditor.ToolOrdering.ListEnabledDebugOptions)]
        public static void ListEnabledDebugOptions()
        {
            List<DebugOptionAssetScanner.DebugOptionEntry> entries = DebugOptionAssetScanner.FindEnabledDebugOptions();
            string message = DebugOptionAssetScanner.BuildSummaryMessage(entries);

            if (entries.Count == 0)
            {
                Debug.Log($"[GGemCo] {message}");
                EditorUtility.DisplayDialog("GGemCo Debug Options", message, "확인");
                return;
            }

            Debug.Log($"[GGemCo] {message}");
            EditorUtility.DisplayDialog("GGemCo Debug Options", message, "확인");
        }

        [MenuItem(ConfigEditor.NameToolDisableAllDebugOptions, false, (int)ConfigEditor.ToolOrdering.DisableAllDebugOptions)]
        public static void DisableAllDebugOptions()
        {
            if (!EditorUtility.DisplayDialog(
                    "GGemCo Debug Options",
                    "프로젝트 내 ScriptableObject 디버그 옵션을 모두 false 로 변경합니다. 계속하시겠습니까?",
                    "실행",
                    "취소"))
            {
                return;
            }

            int touchedFieldCount = DebugOptionAssetScanner.DisableAllDebugOptions();
            string message = $"디버그 옵션 일괄 비활성화를 완료했습니다.\n대상 필드 수: {touchedFieldCount}";

            Debug.Log($"[GGemCo] {message}");
            EditorUtility.DisplayDialog("GGemCo Debug Options", message, "확인");
        }
    }
}
#endif
