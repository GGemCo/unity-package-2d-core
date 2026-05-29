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
            ListEnabledDebugOptions(DebugOptionScanScope.AllProjectAssets, "GGemCo Debug Options");
        }

        [MenuItem(ConfigEditor.NameToolDisableAllDebugOptions, false, (int)ConfigEditor.ToolOrdering.DisableAllDebugOptions)]
        public static void DisableAllDebugOptions()
        {
            DisableDebugOptions(DebugOptionScanScope.AllProjectAssets, "프로젝트 내 ScriptableObject 디버그 옵션을 모두 false 로 변경합니다. 개발용 Settings도 포함됩니다. 계속하시겠습니까?");
        }

        [MenuItem(ConfigEditor.NameToolListReleaseBuildDebugOptions, false, (int)ConfigEditor.ToolOrdering.ListReleaseBuildDebugOptions)]
        public static void ListReleaseBuildDebugOptions()
        {
            ListEnabledDebugOptions(DebugOptionScanScope.ReleaseBuildCandidates, "GGemCo Release Debug Options");
        }

        [MenuItem(ConfigEditor.NameToolDisableReleaseBuildDebugOptions, false, (int)ConfigEditor.ToolOrdering.DisableReleaseBuildDebugOptions)]
        public static void DisableReleaseBuildDebugOptions()
        {
            DisableDebugOptions(DebugOptionScanScope.ReleaseBuildCandidates, "릴리즈 빌드 후보 ScriptableObject 디버그 옵션만 false 로 변경합니다. 개발용 Settings는 제외됩니다. 계속하시겠습니까?");
        }

        [MenuItem(ConfigEditor.NameToolValidateDevelopmentSettingsBuildInclusion, false, (int)ConfigEditor.ToolOrdering.ValidateDevelopmentSettingsBuildInclusion)]
        public static void ValidateDevelopmentSettingsBuildInclusion()
        {
            List<DevelopmentSettingsBuildInclusionScanner.DevelopmentSettingsBuildInclusionEntry> entries = DevelopmentSettingsBuildInclusionScanner.FindBuildInclusionRisks();
            string message = DevelopmentSettingsBuildInclusionScanner.BuildSummaryMessage(entries);

            if (entries.Count == 0)
            {
                Debug.Log($"[GGemCo] {message}");
                EditorUtility.DisplayDialog("GGemCo Development Settings", message, "확인");
                return;
            }

            Debug.LogError($"[GGemCo] {message}");
            EditorUtility.DisplayDialog("GGemCo Development Settings", message, "확인");
        }

        /// <summary>
        /// 지정한 검색 범위에서 활성화된 디버그 옵션을 조회하고 결과를 표시합니다.
        /// </summary>
        /// <param name="scanScope">디버그 옵션 검색 범위입니다.</param>
        /// <param name="dialogTitle">결과 팝업 제목입니다.</param>
        private static void ListEnabledDebugOptions(DebugOptionScanScope scanScope, string dialogTitle)
        {
            List<DebugOptionAssetScanner.DebugOptionEntry> entries = DebugOptionAssetScanner.FindEnabledDebugOptions(scanScope);
            string message = DebugOptionAssetScanner.BuildSummaryMessage(entries);

            if (entries.Count == 0)
            {
                Debug.Log($"[GGemCo] {message}");
                EditorUtility.DisplayDialog(dialogTitle, message, "확인");
                return;
            }

            Debug.Log($"[GGemCo] {message}");
            EditorUtility.DisplayDialog(dialogTitle, message, "확인");
        }

        /// <summary>
        /// 지정한 검색 범위의 디버그 옵션을 false로 일괄 변경합니다.
        /// </summary>
        /// <param name="scanScope">디버그 옵션 검색 범위입니다.</param>
        /// <param name="confirmMessage">실행 전 확인 메시지입니다.</param>
        private static void DisableDebugOptions(DebugOptionScanScope scanScope, string confirmMessage)
        {
            if (!EditorUtility.DisplayDialog(
                    "GGemCo Debug Options",
                    confirmMessage,
                    "실행",
                    "취소"))
            {
                return;
            }

            int touchedFieldCount = DebugOptionAssetScanner.DisableAllDebugOptions(scanScope);
            string message = $"디버그 옵션 일괄 비활성화를 완료했습니다.\n대상 필드 수: {touchedFieldCount}";

            Debug.Log($"[GGemCo] {message}");
            EditorUtility.DisplayDialog("GGemCo Debug Options", message, "확인");
        }
    }
}
#endif
