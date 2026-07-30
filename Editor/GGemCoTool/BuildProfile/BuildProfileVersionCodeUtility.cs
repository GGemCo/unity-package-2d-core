using System.Globalization;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Android Bundle Version Code 동기화 결과를 전달하는 불변 값입니다.
    /// </summary>
    internal readonly struct AndroidBundleVersionCodeSyncResult
    {
        /// <summary>
        /// 동기화 기준으로 사용한 Player Version 문자열입니다.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// 동기화 전 Android Bundle Version Code입니다.
        /// </summary>
        public int PreviousVersionCode { get; }

        /// <summary>
        /// Player Version에서 계산한 Android Bundle Version Code입니다.
        /// </summary>
        public int CalculatedVersionCode { get; }

        /// <summary>
        /// 이번 동기화에서 ProjectSettings 값이 실제로 변경되었는지 여부입니다.
        /// </summary>
        public bool Changed { get; }

        /// <summary>
        /// Android Bundle Version Code 동기화 결과를 생성합니다.
        /// </summary>
        /// <param name="version">계산 기준 Player Version입니다.</param>
        /// <param name="previousVersionCode">동기화 전 코드입니다.</param>
        /// <param name="calculatedVersionCode">계산된 코드입니다.</param>
        /// <param name="changed">실제 변경 여부입니다.</param>
        public AndroidBundleVersionCodeSyncResult(
            string version,
            int previousVersionCode,
            int calculatedVersionCode,
            bool changed)
        {
            Version = version;
            PreviousVersionCode = previousVersionCode;
            CalculatedVersionCode = calculatedVersionCode;
            Changed = changed;
        }
    }

    /// <summary>
    /// Player Version을 Android Bundle Version Code로 변환하고 동기화하는 정책을 제공합니다.
    /// </summary>
    internal static class BuildProfileVersionCodeUtility
    {
        private const int VersionSegmentMax = 99;
        private const int MajorMultiplier = 10000;
        private const int MinorMultiplier = 100;
        private const int SplitApkVersionCodeLimit = 100000;

        /// <summary>
        /// 현재 Player Version을 기준으로 Android Bundle Version Code를 계산합니다.
        /// 계산식은 major × 10,000 + minor × 100 + patch이며, 각 버전 항목은 0~99 범위여야 합니다.
        /// </summary>
        /// <param name="version">major.minor.patch 형식의 Player Version입니다.</param>
        /// <param name="versionCode">계산된 Android Bundle Version Code입니다.</param>
        /// <param name="errorMessage">계산 실패 원인입니다.</param>
        /// <returns>유효한 버전 코드를 계산했으면 <see langword="true"/>입니다.</returns>
        public static bool TryCalculateAndroidBundleVersionCode(
            string version,
            out int versionCode,
            out string errorMessage)
        {
            versionCode = 0;
            errorMessage = string.Empty;

            string normalizedVersion = version?.Trim();
            if (string.IsNullOrEmpty(normalizedVersion))
            {
                errorMessage = "Player Version이 비어 있습니다. major.minor.patch 형식으로 입력해주세요.";
                return false;
            }

            string[] segments = normalizedVersion.Split('.');
            if (segments.Length != 3)
            {
                errorMessage =
                    $"Player Version '{version}'은 major.minor.patch 형식이어야 합니다. 예: 0.24.0";
                return false;
            }

            if (!TryParseVersionSegment(segments[0], "major", out int major, out errorMessage) ||
                !TryParseVersionSegment(segments[1], "minor", out int minor, out errorMessage) ||
                !TryParseVersionSegment(segments[2], "patch", out int patch, out errorMessage))
            {
                return false;
            }

            long calculatedCode =
                (long)major * MajorMultiplier +
                (long)minor * MinorMultiplier +
                patch;
            if (calculatedCode <= 0 || calculatedCode > int.MaxValue)
            {
                errorMessage =
                    $"Player Version '{version}'에서 계산한 Bundle Version Code가 유효한 양의 int 범위를 벗어났습니다.";
                return false;
            }

            if (PlayerSettings.Android.buildApkPerCpuArchitecture &&
                calculatedCode >= SplitApkVersionCodeLimit)
            {
                errorMessage =
                    $"Split APKs by target architecture 사용 시 Bundle Version Code는 {SplitApkVersionCodeLimit} 미만이어야 합니다. " +
                    $"Player Version:{version}, 계산값:{calculatedCode}";
                return false;
            }

            versionCode = (int)calculatedCode;
            return true;
        }

        /// <summary>
        /// 현재 Player Version에서 계산한 값으로 Android Bundle Version Code를 동기화합니다.
        /// 이미 등록된 코드보다 낮은 값은 스토어 업데이트 순서를 역전시킬 수 있으므로 적용하지 않습니다.
        /// </summary>
        /// <param name="result">동기화 전후 값과 변경 여부입니다.</param>
        /// <param name="errorMessage">동기화 실패 원인입니다.</param>
        /// <returns>동기화 가능한 상태이면 <see langword="true"/>입니다.</returns>
        public static bool TrySynchronizeAndroidBundleVersionCode(
            out AndroidBundleVersionCodeSyncResult result,
            out string errorMessage)
        {
            result = default;
            string version = PlayerSettings.bundleVersion;
            if (!TryCalculateAndroidBundleVersionCode(
                    version,
                    out int calculatedVersionCode,
                    out errorMessage))
            {
                return false;
            }

            int previousVersionCode = PlayerSettings.Android.bundleVersionCode;
            if (calculatedVersionCode < previousVersionCode)
            {
                errorMessage =
                    "Player Version에서 계산한 Bundle Version Code가 현재 값보다 작습니다. " +
                    $"Player Version:{version}, 현재값:{previousVersionCode}, 계산값:{calculatedVersionCode}. " +
                    "이미 배포한 코드보다 큰 값이 계산되도록 Player Version을 올려주세요.";
                return false;
            }

            bool changed = calculatedVersionCode != previousVersionCode;
            if (changed)
            {
                PlayerSettings.Android.bundleVersionCode = calculatedVersionCode;
            }

            result = new AndroidBundleVersionCodeSyncResult(
                version,
                previousVersionCode,
                calculatedVersionCode,
                changed);
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 현재 Player Version과 Android Bundle Version Code가 계산 정책에 맞게 동기화되어 있는지 검증합니다.
        /// </summary>
        /// <param name="errorMessage">검증 실패 원인입니다.</param>
        /// <returns>현재 값이 계산 결과와 일치하면 <see langword="true"/>입니다.</returns>
        public static bool TryValidateAndroidBundleVersionCode(out string errorMessage)
        {
            string version = PlayerSettings.bundleVersion;
            if (!TryCalculateAndroidBundleVersionCode(
                    version,
                    out int calculatedVersionCode,
                    out errorMessage))
            {
                return false;
            }

            int currentVersionCode = PlayerSettings.Android.bundleVersionCode;
            if (currentVersionCode == calculatedVersionCode)
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage =
                "Player Version과 Android Bundle Version Code가 동기화되어 있지 않습니다. " +
                $"Player Version:{version}, 현재값:{currentVersionCode}, 계산값:{calculatedVersionCode}. " +
                "Build 프로파일 창에서 'Release 빌드 준비 실행'을 먼저 실행해주세요.";
            return false;
        }

        /// <summary>
        /// 동기화 결과를 사용자 안내 및 로그에 사용할 한 줄 메시지로 변환합니다.
        /// </summary>
        /// <param name="result">동기화 결과입니다.</param>
        /// <returns>버전 코드 변경 결과 메시지입니다.</returns>
        public static string BuildSynchronizationMessage(AndroidBundleVersionCodeSyncResult result)
        {
            return result.Changed
                ? $"Android Bundle Version Code를 Player Version {result.Version} 기준으로 " +
                  $"{result.PreviousVersionCode} → {result.CalculatedVersionCode}(으)로 변경했습니다."
                : $"Android Bundle Version Code가 Player Version {result.Version}의 계산값 " +
                  $"{result.CalculatedVersionCode}과 이미 일치합니다.";
        }

        /// <summary>
        /// 버전 문자열의 단일 숫자 항목을 0~99 범위로 파싱합니다.
        /// </summary>
        /// <param name="segment">파싱할 숫자 문자열입니다.</param>
        /// <param name="segmentName">오류 메시지에 표시할 항목 이름입니다.</param>
        /// <param name="value">파싱된 숫자입니다.</param>
        /// <param name="errorMessage">파싱 실패 원인입니다.</param>
        /// <returns>유효한 숫자 항목이면 <see langword="true"/>입니다.</returns>
        private static bool TryParseVersionSegment(
            string segment,
            string segmentName,
            out int value,
            out string errorMessage)
        {
            if (!int.TryParse(
                    segment,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out value) ||
                value < 0 ||
                value > VersionSegmentMax)
            {
                errorMessage =
                    $"Player Version의 {segmentName} 값은 0~{VersionSegmentMax} 범위의 정수여야 합니다. 입력값:'{segment}'";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
