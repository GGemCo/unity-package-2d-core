using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Core 기본 사용처와 설치된 상위 패키지 확장 사용처를 분석하여 sound_usage_manifest.txt를 생성합니다.
    /// </summary>
    public static class SoundUsageManifestBuilder
    {
        private const string Header =
            "Uid\tName\tScopeType\tScopeUid\tSoundUid\tSourceType\tSourceUid\tSourcePath\tMemo\tEnabled";

        /// <summary>
        /// 전체 맵, UI 윈도우 및 외부 패키지 사운드 사용처를 분석하고 매니페스트 테이블을 생성합니다.
        /// </summary>
        /// <param name="rebuildRuntimeTablePack">
        /// true이면 생성 직후 Core 런타임 테이블 팩도 다시 생성합니다.
        /// </param>
        /// <returns>생성 결과와 진단 메시지입니다.</returns>
        public static SoundUsageManifestBuildResult Build(bool rebuildRuntimeTablePack = true)
        {
            SoundUsageManifestBuildResult result = new SoundUsageManifestBuildResult
            {
                OutputPath = ConfigAddressableTable.TableSoundUsageManifest.Path,
            };

            try
            {
                TableMap tableMap = TableLoaderManager.LoadMapTable();
                TableMonster tableMonster = TableLoaderManager.LoadMonsterTable();
                TableNpc tableNpc = TableLoaderManager.LoadNpcTable();
                TableAnimation tableAnimation = TableLoaderManager.LoadSpineTable();
                TableWindow tableWindow = TableLoaderManager.LoadWindowTable();
                TableSound tableSound = TableLoaderManager.LoadSoundTable();

                if (!ValidateRequiredTables(
                        tableMap,
                        tableMonster,
                        tableNpc,
                        tableAnimation,
                        tableWindow,
                        tableSound,
                        result))
                {
                    return result;
                }

                List<SoundUsageManifestBuildRecord> rawRecords =
                    new List<SoundUsageManifestBuildRecord>();
                SoundUsageManifestBuildContext context = new SoundUsageManifestBuildContext(
                    rawRecords,
                    result,
                    tableMap,
                    tableMonster);

                MapSoundUsageScanner mapScanner = new MapSoundUsageScanner(
                    tableMap,
                    tableMonster,
                    tableNpc,
                    tableAnimation);
                mapScanner.Scan(rawRecords, result, context);

                UiSoundUsageScanner uiScanner = new UiSoundUsageScanner(tableWindow);
                uiScanner.Scan(rawRecords, result);

                ExecuteContributors(context, result);

                List<SoundUsageManifestBuildRecord> records =
                    NormalizeAndValidateRecords(rawRecords, tableSound, result);
                WriteManifest(records, result.OutputPath);
                SoundUsageManifestBuildMetadata metadata =
                    SoundUsageManifestSourceFingerprint.CreateMetadata();
                SoundUsageManifestSourceFingerprint.WriteMetadata(metadata);
                result.AddMessage(
                    $"원본 지문 저장 완료: sources={metadata.SourceCount}, fingerprint={metadata.SourceFingerprint}");

                result.RecordCount = records.Count;
                result.MapScopeCount = records
                    .Where(record => record.ScopeType == SoundUsageManifestScopeType.Map)
                    .Select(record => record.ScopeUid)
                    .Distinct()
                    .Count();
                result.UiWindowScopeCount = records
                    .Where(record => record.ScopeType == SoundUsageManifestScopeType.UiWindow)
                    .Select(record => record.ScopeUid)
                    .Distinct()
                    .Count();

                AssetDatabase.ImportAsset(
                    result.OutputPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                _ = TableLoaderManager.LoadSoundUsageManifestTable(true);

                if (rebuildRuntimeTablePack)
                {
                    result.RuntimeTablePackRebuilt = RuntimeTablePackBuilder.Build(
                        ConfigAddressableTablePack.PackageCore,
                        ConfigAddressableTablePack.Core,
                        ConfigAddressableTable.All);
                }

                AssetDatabase.SaveAssets();
                result.Succeeded = true;
                result.AddMessage(
                    $"매니페스트 생성 완료: records={result.RecordCount}, maps={result.MapScopeCount}, uiWindows={result.UiWindowScopeCount}, contributors={result.ContributorCount}");
            }
            catch (Exception ex)
            {
                result.SetFailure(ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 설치된 상위 패키지의 사운드 사용처 확장기를 검색하여 순서대로 실행합니다.
        /// 개별 확장기의 실패는 전체 Core 기본 분석을 중단하지 않고 경고로 격리합니다.
        /// </summary>
        /// <param name="context">기본 맵/UI 분석 결과와 레코드 추가 API를 제공하는 컨텍스트입니다.</param>
        /// <param name="result">확장기 실행 결과와 경고를 기록할 생성 결과입니다.</param>
        private static void ExecuteContributors(
            SoundUsageManifestBuildContext context,
            SoundUsageManifestBuildResult result)
        {
            List<ISoundUsageManifestContributor> contributors =
                new List<ISoundUsageManifestContributor>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ISoundUsageManifestContributor>())
            {
                if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                    continue;

                try
                {
                    if (Activator.CreateInstance(type) is ISoundUsageManifestContributor contributor)
                        contributors.Add(contributor);
                }
                catch (Exception ex)
                {
                    result?.AddWarning(
                        $"사운드 매니페스트 확장기를 생성하지 못했습니다. type={type.FullName}, error={ex.Message}");
                }
            }

            contributors = contributors
                .OrderBy(contributor => contributor.Order)
                .ThenBy(contributor => contributor.GetType().FullName, StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < contributors.Count; i++)
            {
                ISoundUsageManifestContributor contributor = contributors[i];
                string displayName = string.IsNullOrWhiteSpace(contributor.DisplayName)
                    ? contributor.GetType().FullName
                    : contributor.DisplayName;

                try
                {
                    contributor.Collect(context);
                    result.ContributorCount++;
                    result.AddMessage($"외부 사운드 분석기 완료: {displayName}");
                }
                catch (Exception ex)
                {
                    result.AddWarning(
                        $"외부 사운드 분석기 실행 중 오류가 발생했습니다. contributor={displayName}, error={ex}");
                }
            }
        }

        /// <summary>
        /// 자동 분석에 필요한 필수 테이블이 모두 로드되었는지 확인합니다.
        /// </summary>
        private static bool ValidateRequiredTables(
            TableMap tableMap,
            TableMonster tableMonster,
            TableNpc tableNpc,
            TableAnimation tableAnimation,
            TableWindow tableWindow,
            TableSound tableSound,
            SoundUsageManifestBuildResult result)
        {
            List<string> missing = new List<string>();
            if (tableMap == null) missing.Add(ConfigAddressableTable.Map);
            if (tableMonster == null) missing.Add(ConfigAddressableTable.Monster);
            if (tableNpc == null) missing.Add(ConfigAddressableTable.Npc);
            if (tableAnimation == null) missing.Add(ConfigAddressableTable.Animation);
            if (tableWindow == null) missing.Add(ConfigAddressableTable.Window);
            if (tableSound == null) missing.Add(ConfigAddressableTable.Sound);

            if (missing.Count == 0)
                return true;

            result?.SetFailure($"필수 테이블을 로드하지 못했습니다: {string.Join(", ", missing)}");
            return false;
        }

        /// <summary>
        /// 잘못된 UID를 제외하고 같은 사용처 레코드를 제거한 뒤 안정적인 순서로 정렬합니다.
        /// </summary>
        private static List<SoundUsageManifestBuildRecord> NormalizeAndValidateRecords(
            List<SoundUsageManifestBuildRecord> rawRecords,
            TableSound tableSound,
            SoundUsageManifestBuildResult result)
        {
            Dictionary<string, SoundUsageManifestBuildRecord> unique =
                new Dictionary<string, SoundUsageManifestBuildRecord>(StringComparer.Ordinal);

            if (rawRecords != null)
            {
                for (int i = 0; i < rawRecords.Count; i++)
                {
                    SoundUsageManifestBuildRecord record = rawRecords[i];
                    if (record == null ||
                        record.ScopeType == SoundUsageManifestScopeType.None ||
                        record.ScopeUid <= 0 ||
                        record.SoundUid <= 0)
                    {
                        continue;
                    }

                    if (!tableSound.TryGetDataByUid(record.SoundUid, out StruckTableSound sound) || sound == null)
                    {
                        result?.AddWarning(
                            $"분석된 sound UID가 sound 테이블에 없어 제외합니다. scope={record.ScopeType}:{record.ScopeUid}, soundUid={record.SoundUid}, source={record.SourcePath}");
                        continue;
                    }

                    record.SourcePath = SanitizeCell(record.SourcePath);
                    record.Memo = SanitizeCell(record.Memo);
                    unique.TryAdd(record.BuildDeduplicationKey(), record);
                }
            }

            return unique.Values
                .OrderBy(record => record.ScopeType)
                .ThenBy(record => record.ScopeUid)
                .ThenBy(record => record.SoundUid)
                .ThenBy(record => record.SourceType)
                .ThenBy(record => record.SourceUid)
                .ThenBy(record => record.SourcePath, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// 정규화된 레코드를 탭 구분 테이블 형식으로 UTF-8 BOM 없이 저장합니다.
        /// </summary>
        /// <param name="records">저장할 정규화 레코드 목록입니다.</param>
        /// <param name="outputPath">Unity 프로젝트 기준 출력 에셋 경로입니다.</param>
        private static void WriteManifest(
            IReadOnlyList<SoundUsageManifestBuildRecord> records,
            string outputPath)
        {
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Header);

            for (int i = 0; i < records.Count; i++)
            {
                int uid = i + 1;
                SoundUsageManifestBuildRecord record = records[i];
                string name = BuildRowName(record);

                builder.Append(uid).Append('\t')
                    .Append(SanitizeCell(name)).Append('\t')
                    .Append(record.ScopeType).Append('\t')
                    .Append(record.ScopeUid).Append('\t')
                    .Append(record.SoundUid).Append('\t')
                    .Append(record.SourceType).Append('\t')
                    .Append(record.SourceUid).Append('\t')
                    .Append(SanitizeCell(record.SourcePath)).Append('\t')
                    .Append(SanitizeCell(record.Memo)).Append('\t')
                    .Append('Y')
                    .Append('\n');
            }

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// 테이블 에디터와 디버그 로그에서 사용처를 식별할 수 있는 행 이름을 생성합니다.
        /// </summary>
        /// <param name="record">이름을 만들 매니페스트 레코드입니다.</param>
        /// <returns>범위와 원본을 포함한 행 이름입니다.</returns>
        private static string BuildRowName(SoundUsageManifestBuildRecord record)
        {
            return $"{record.ScopeType}_{record.ScopeUid}_{record.SourceType}_{record.SourceUid}_Sound_{record.SoundUid}";
        }

        /// <summary>
        /// 탭 및 줄바꿈이 테이블 열 구조를 깨뜨리지 않도록 문자열을 보정합니다.
        /// </summary>
        /// <param name="value">테이블 셀에 기록할 문자열입니다.</param>
        /// <returns>탭이 공백으로 바뀌고 줄바꿈이 이스케이프된 문자열입니다.</returns>
        private static string SanitizeCell(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\r", string.Empty)
                    .Replace("\n", @"\n")
                    .Replace("\t", " ");
        }
    }
}
