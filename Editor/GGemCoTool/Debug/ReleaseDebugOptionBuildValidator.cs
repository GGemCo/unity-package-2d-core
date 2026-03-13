#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 릴리즈 빌드 시작 전에 활성화된 디버그 옵션이 남아 있는지 검사합니다.
    /// Development Build 가 아닌 경우에만 검사를 수행합니다.
    /// </summary>
    public sealed class ReleaseDebugOptionBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            if (isDevelopmentBuild)
            {
                return;
            }

            List<DebugOptionAssetScanner.DebugOptionEntry> enabledEntries = DebugOptionAssetScanner.FindEnabledDebugOptions();
            if (enabledEntries.Count == 0)
            {
                return;
            }

            throw new BuildFailedException(
                "[GGemCo] Release Build 에 활성화된 디버그 옵션이 포함되어 있습니다.\n" +
                "빌드를 중단합니다. 아래 항목을 false 로 변경하거나, 메뉴에서 일괄 비활성화를 실행해주세요.\n\n" +
                DebugOptionAssetScanner.BuildSummaryMessage(enabledEntries));
        }
    }
}
#endif
