#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 릴리즈 빌드 시작 전에 서비스용 Settings의 디버그 옵션과 개발용 Settings의 빌드 포함 위험을 검사합니다.
    /// Development Build 가 아닌 경우에만 검사를 수행합니다.
    /// </summary>
    public sealed class ReleaseDebugOptionBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        /// <summary>
        /// 빌드 시작 전에 릴리즈 빌드 안전 조건을 검사합니다.
        /// </summary>
        /// <param name="report">Unity 빌드 리포트입니다.</param>
        /// <exception cref="BuildFailedException">릴리즈 빌드 안전 조건을 만족하지 못할 때 발생합니다.</exception>
        public void OnPreprocessBuild(BuildReport report)
        {
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            if (isDevelopmentBuild)
            {
                return;
            }

            if (TryValidateReleaseBuild(report.summary.platformGroup, out string message))
            {
                return;
            }

            throw new BuildFailedException(message);
        }

        /// <summary>
        /// 서비스용 Settings와 개발용 Settings 포함 위험을 검사하여 릴리즈 빌드 가능 여부를 반환합니다.
        /// </summary>
        /// <param name="message">검증 성공 또는 실패 안내 메시지입니다.</param>
        /// <returns>릴리즈 빌드 안전 조건을 만족하면 true입니다.</returns>
        public static bool TryValidateReleaseBuild(out string message)
        {
            return TryValidateReleaseBuild(EditorUserBuildSettings.selectedBuildTargetGroup, out message);
        }

        /// <summary>
        /// 지정한 빌드 타겟 그룹 기준으로 서비스용 Settings, 개발용 Settings 포함 위험, 금지 Scripting Define Symbol을 검사합니다.
        /// </summary>
        /// <param name="buildTargetGroup">검사할 빌드 타겟 그룹입니다.</param>
        /// <param name="message">검증 성공 또는 실패 안내 메시지입니다.</param>
        /// <returns>릴리즈 빌드 안전 조건을 만족하면 true입니다.</returns>
        public static bool TryValidateReleaseBuild(BuildTargetGroup buildTargetGroup, out string message)
        {
            List<DebugOptionAssetScanner.DebugOptionEntry> enabledEntries = DebugOptionAssetScanner.FindEnabledDebugOptions(DebugOptionScanScope.ReleaseBuildCandidates);
            List<DevelopmentSettingsBuildInclusionScanner.DevelopmentSettingsBuildInclusionEntry> developmentRisks = DevelopmentSettingsBuildInclusionScanner.FindBuildInclusionRisks();
            List<BuildProfileScriptingDefineUtility.ScriptingDefineRiskEntry> scriptingDefineRisks = BuildProfileScriptingDefineUtility.FindReleaseBlockingSymbols(buildTargetGroup);
            string versionCodeError = string.Empty;
            bool isVersionCodeValid =
                buildTargetGroup != BuildTargetGroup.Android ||
                BuildProfileVersionCodeUtility.TryValidateAndroidBundleVersionCode(
                    out versionCodeError);
            if (enabledEntries.Count == 0 &&
                developmentRisks.Count == 0 &&
                scriptingDefineRisks.Count == 0 &&
                isVersionCodeValid)
            {
                message = "Release Build 안전 검증을 통과했습니다.";
                return true;
            }

            message = BuildFailureMessage(
                enabledEntries,
                developmentRisks,
                scriptingDefineRisks,
                versionCodeError);
            return false;
        }

        /// <summary>
        /// 릴리즈 빌드 검증 실패 메시지를 생성합니다.
        /// </summary>
        /// <param name="enabledEntries">서비스용 빌드 후보에서 발견된 활성 디버그 옵션 목록입니다.</param>
        /// <param name="developmentRisks">개발용 Settings 빌드 포함 위험 요소 목록입니다.</param>
        /// <param name="scriptingDefineRisks">릴리즈 빌드를 차단해야 하는 Scripting Define Symbol 목록입니다.</param>
        /// <param name="versionCodeError">Android 버전 코드 검증 실패 원인입니다.</param>
        /// <returns>빌드 실패 안내 메시지입니다.</returns>
        private static string BuildFailureMessage(
            IReadOnlyList<DebugOptionAssetScanner.DebugOptionEntry> enabledEntries,
            IReadOnlyList<DevelopmentSettingsBuildInclusionScanner.DevelopmentSettingsBuildInclusionEntry> developmentRisks,
            IReadOnlyList<BuildProfileScriptingDefineUtility.ScriptingDefineRiskEntry> scriptingDefineRisks,
            string versionCodeError)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[GGemCo] Release Build 안전 검증에 실패했습니다.");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(versionCodeError))
            {
                builder.AppendLine("Android Player Version과 Bundle Version Code 설정이 릴리즈 정책에 맞지 않습니다.");
                builder.AppendLine(versionCodeError);
                builder.AppendLine();
            }

            if (enabledEntries != null && enabledEntries.Count > 0)
            {
                builder.AppendLine("서비스용 또는 릴리즈 빌드 후보 Settings에 활성화된 디버그 옵션이 포함되어 있습니다.");
                builder.AppendLine("아래 항목을 false 로 변경하거나, 메뉴에서 릴리즈 후보 디버그 옵션 비활성화를 실행해주세요.");
                builder.AppendLine();
                builder.AppendLine(DebugOptionAssetScanner.BuildSummaryMessage(enabledEntries));
            }

            if (developmentRisks != null && developmentRisks.Count > 0)
            {
                builder.AppendLine("작업자별 Development Settings가 릴리즈 빌드 콘텐츠에 포함될 수 있는 상태입니다.");
                builder.AppendLine("Addressables 등록을 제거하거나 Resources 폴더 밖으로 이동해주세요.");
                builder.AppendLine();
                builder.AppendLine(DevelopmentSettingsBuildInclusionScanner.BuildSummaryMessage(developmentRisks));
            }

            if (scriptingDefineRisks != null && scriptingDefineRisks.Count > 0)
            {
                builder.AppendLine("릴리즈 빌드에서 금지된 Scripting Define Symbol이 포함되어 있습니다.");
                builder.AppendLine($"{GGemCo2DCore.GGemCoScriptingDefineSymbols.EnableCheatTools} 심볼은 Development/Release Simulation 반복 테스트 중에는 유지할 수 있지만, 실제 Release 빌드 전에는 제거해야 합니다.");
                builder.AppendLine();
                builder.AppendLine(BuildProfileScriptingDefineUtility.BuildSummaryMessage(scriptingDefineRisks));
            }

            return builder.ToString();
        }
    }
}
#endif
