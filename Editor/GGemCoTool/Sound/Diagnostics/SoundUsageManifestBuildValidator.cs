using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Player 빌드 전에 사운드 매니페스트와 Addressables 연결을 검사하여 잘못된 콘텐츠 빌드를 차단합니다.
    /// </summary>
    public sealed class SoundUsageManifestBuildValidator : IPreprocessBuildWithReport
    {
        /// <inheritdoc />
        public int callbackOrder => 900;

        /// <summary>
        /// 빌드 시작 전에 사운드 매니페스트 전체 검증을 실행합니다.
        /// 오류가 있으면 원인을 포함한 BuildFailedException으로 빌드를 중단합니다.
        /// </summary>
        /// <param name="report">Unity Player 빌드 정보입니다.</param>
        /// <exception cref="BuildFailedException">사운드 콘텐츠 검증 오류가 하나 이상 있을 때 발생합니다.</exception>
        public void OnPreprocessBuild(BuildReport report)
        {
            SoundUsageManifestValidationResult result = SoundUsageManifestValidator.Validate(checkStaleness: true);
            if (result.IsValid)
                return;

            string details = string.Join(
                "\n",
                result.Messages
                    .Where(message => message.Severity == SoundUsageValidationSeverity.Error)
                    .Select(message => $"- {message.Message}"));
            throw new BuildFailedException(
                $"사운드 사용 매니페스트 검증에 실패하여 빌드를 중단합니다. errors={result.ErrorCount}\n{details}");
        }
    }
}
