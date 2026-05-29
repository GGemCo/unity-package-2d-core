#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 작업자별 개발용 Settings 에셋이 릴리즈 빌드 콘텐츠에 포함될 수 있는 위험 요소를 검사합니다.
    /// </summary>
    public static class DevelopmentSettingsBuildInclusionScanner
    {
        /// <summary>
        /// 작업자별 개발용 Settings가 저장되는 기본 루트 경로입니다.
        /// </summary>
        public const string LocalSettingsRootPath = "Assets/" + ConfigDefine.NameSDK + "Local/Settings";

        private const string LocalAssetsRootPath = "Assets/" + ConfigDefine.NameSDK + "Local";
        private const string DevelopmentAssetSuffix = ".Development.asset";

        /// <summary>
        /// 릴리즈 빌드에 개발용 Settings가 포함될 수 있는 위험 요소를 찾습니다.
        /// </summary>
        /// <returns>빌드 포함 위험 요소 목록입니다.</returns>
        public static List<DevelopmentSettingsBuildInclusionEntry> FindBuildInclusionRisks()
        {
            List<DevelopmentSettingsBuildInclusionEntry> results = new List<DevelopmentSettingsBuildInclusionEntry>();
            HashSet<string> resultKeys = new HashSet<string>();

            CollectAddressableRisks(results, resultKeys);
            CollectAddressableDependencyRisks(results, resultKeys);
            CollectEnabledSceneDependencyRisks(results, resultKeys);
            CollectResourcesRisks(results, resultKeys);

            return results
                .OrderBy(entry => entry.AssetPath)
                .ThenBy(entry => entry.Reason)
                .ToList();
        }

        /// <summary>
        /// 지정한 에셋 경로가 작업자별 개발용 Settings 에셋인지 확인합니다.
        /// </summary>
        /// <param name="assetPath">Unity 프로젝트 상대 에셋 경로입니다.</param>
        /// <returns>개발용 Settings 에셋이면 true입니다.</returns>
        public static bool IsDevelopmentSettingsAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalizedPath = NormalizeAssetPath(assetPath);
            if (normalizedPath.StartsWith(LocalSettingsRootPath + "/", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return normalizedPath.EndsWith(DevelopmentAssetSuffix, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 지정한 에셋 경로가 작업자별 로컬 에셋 루트 아래인지 확인합니다.
        /// </summary>
        /// <param name="assetPath">Unity 프로젝트 상대 에셋 경로입니다.</param>
        /// <returns>로컬 에셋 루트 아래이면 true입니다.</returns>
        public static bool IsLocalAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalizedPath = NormalizeAssetPath(assetPath);
            return normalizedPath.StartsWith(LocalAssetsRootPath + "/", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 빌드 실패 메시지에 사용할 개발용 Settings 위험 요소 요약을 생성합니다.
        /// </summary>
        /// <param name="entries">빌드 포함 위험 요소 목록입니다.</param>
        /// <returns>줄바꿈이 포함된 메시지 문자열입니다.</returns>
        public static string BuildSummaryMessage(IReadOnlyList<DevelopmentSettingsBuildInclusionEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "릴리즈 빌드에 포함될 수 있는 개발용 Settings 위험 요소가 없습니다.";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"개발용 Settings 빌드 포함 위험 요소 {entries.Count}건을 찾았습니다.");

            foreach (DevelopmentSettingsBuildInclusionEntry entry in entries)
            {
                builder.Append("- ")
                    .Append(entry.AssetPath)
                    .Append(" | ")
                    .Append(entry.Reason);

                if (!string.IsNullOrWhiteSpace(entry.AddressableAddress))
                {
                    builder.Append(" | address=").Append(entry.AddressableAddress);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        /// <summary>
        /// Addressables에 등록된 개발용 Settings 에셋을 찾습니다.
        /// </summary>
        /// <param name="results">검색 결과를 추가할 목록입니다.</param>
        /// <param name="resultKeys">중복 추가를 막기 위한 키 집합입니다.</param>
        private static void CollectAddressableRisks(ICollection<DevelopmentSettingsBuildInclusionEntry> results, ISet<string> resultKeys)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
                return;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.guid))
                        continue;

                    string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (!IsDevelopmentSettingsAssetPath(assetPath))
                        continue;

                    AddResult(
                        results,
                        resultKeys,
                        assetPath,
                        "개발용 Settings가 Addressables에 등록되어 릴리즈 콘텐츠에 포함될 수 있습니다.",
                        entry.address);
                }
            }
        }

        /// <summary>
        /// Addressables 엔트리의 의존성으로 연결된 개발용 Settings 에셋을 찾습니다.
        /// </summary>
        /// <param name="results">검색 결과를 추가할 목록입니다.</param>
        /// <param name="resultKeys">중복 추가를 막기 위한 키 집합입니다.</param>
        private static void CollectAddressableDependencyRisks(ICollection<DevelopmentSettingsBuildInclusionEntry> results, ISet<string> resultKeys)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
                return;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.guid))
                        continue;

                    string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrWhiteSpace(assetPath))
                        continue;

                    foreach (string dependencyPath in AssetDatabase.GetDependencies(assetPath, true))
                    {
                        if (!IsDevelopmentSettingsAssetPath(dependencyPath))
                            continue;

                        AddResult(
                            results,
                            resultKeys,
                            dependencyPath,
                            $"Addressables 엔트리의 의존성으로 연결되어 릴리즈 콘텐츠에 포함될 수 있습니다. source={assetPath}",
                            entry.address);
                    }
                }
            }
        }

        /// <summary>
        /// 빌드에 포함된 Scene 의존성으로 연결된 개발용 Settings 에셋을 찾습니다.
        /// </summary>
        /// <param name="results">검색 결과를 추가할 목록입니다.</param>
        /// <param name="resultKeys">중복 추가를 막기 위한 키 집합입니다.</param>
        private static void CollectEnabledSceneDependencyRisks(ICollection<DevelopmentSettingsBuildInclusionEntry> results, ISet<string> resultKeys)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
                    continue;

                foreach (string dependencyPath in AssetDatabase.GetDependencies(scene.path, true))
                {
                    if (!IsDevelopmentSettingsAssetPath(dependencyPath))
                        continue;

                    AddResult(
                        results,
                        resultKeys,
                        dependencyPath,
                        $"빌드에 포함된 Scene의 의존성으로 연결되어 Player 빌드에 포함될 수 있습니다. scene={scene.path}",
                        null);
                }
            }
        }

        /// <summary>
        /// Resources 폴더 아래에 존재하는 개발용 Settings 에셋을 찾습니다.
        /// </summary>
        /// <param name="results">검색 결과를 추가할 목록입니다.</param>
        /// <param name="resultKeys">중복 추가를 막기 위한 키 집합입니다.</param>
        private static void CollectResourcesRisks(ICollection<DevelopmentSettingsBuildInclusionEntry> results, ISet<string> resultKeys)
        {
            foreach (string guid in EnumerateDevelopmentSettingGuids())
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string normalizedPath = NormalizeAssetPath(assetPath);
                if (!normalizedPath.Contains("/Resources/"))
                    continue;

                AddResult(
                    results,
                    resultKeys,
                    assetPath,
                    "개발용 Settings가 Resources 폴더 아래에 있어 릴리즈 빌드에 포함될 수 있습니다.",
                    null);
            }
        }

        /// <summary>
        /// 작업자별 개발용 Settings 후보 GUID를 순회합니다.
        /// </summary>
        /// <returns>개발용 Settings 후보 GUID 목록입니다.</returns>
        private static IEnumerable<string> EnumerateDevelopmentSettingGuids()
        {
            if (AssetDatabase.IsValidFolder(LocalSettingsRootPath))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { LocalSettingsRootPath }))
                {
                    yield return guid;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (IsDevelopmentSettingsAssetPath(assetPath))
                {
                    yield return guid;
                }
            }
        }

        /// <summary>
        /// 결과 목록에 중복 없이 위험 요소를 추가합니다.
        /// </summary>
        /// <param name="results">검색 결과를 추가할 목록입니다.</param>
        /// <param name="resultKeys">중복 추가를 막기 위한 키 집합입니다.</param>
        /// <param name="assetPath">위험 요소가 발견된 에셋 경로입니다.</param>
        /// <param name="reason">위험 사유입니다.</param>
        /// <param name="addressableAddress">Addressables 주소입니다.</param>
        private static void AddResult(
            ICollection<DevelopmentSettingsBuildInclusionEntry> results,
            ISet<string> resultKeys,
            string assetPath,
            string reason,
            string addressableAddress)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            string key = $"{NormalizeAssetPath(assetPath)}|{reason}|{addressableAddress}";
            if (!resultKeys.Add(key))
                return;

            results.Add(new DevelopmentSettingsBuildInclusionEntry(assetPath, reason, addressableAddress));
        }

        /// <summary>
        /// 경로 구분자를 Unity 표준 에셋 경로 형식으로 정리합니다.
        /// </summary>
        /// <param name="assetPath">정리할 경로입니다.</param>
        /// <returns>슬래시 구분자로 정리된 경로입니다.</returns>
        private static string NormalizeAssetPath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : assetPath.Replace('\\', '/');
        }

        /// <summary>
        /// 개발용 Settings 빌드 포함 위험 요소 한 건에 대한 검색 결과입니다.
        /// </summary>
        public readonly struct DevelopmentSettingsBuildInclusionEntry
        {
            public DevelopmentSettingsBuildInclusionEntry(string assetPath, string reason, string addressableAddress)
            {
                AssetPath = assetPath;
                Reason = reason;
                AddressableAddress = addressableAddress;
            }

            public string AssetPath { get; }
            public string Reason { get; }
            public string AddressableAddress { get; }
        }
    }
}
#endif
