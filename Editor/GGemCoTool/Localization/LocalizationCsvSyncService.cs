#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 모든 StringTableCollection 을 하나의 CSV 로 내보내고,
    /// 다시 읽어 Merge 방식으로 반영하는 에디터 전용 서비스입니다.
    /// </summary>
    internal static class LocalizationCsvSyncService
    {
        /// <summary>
        /// CSV 기본 파일명을 정의합니다.
        /// </summary>
        public const string DefaultFileName = "Localization_AllCollections.csv";

        private const string ColumnCollection = "Collection";
        private const string ColumnKey = "Key";
        private const string ColumnId = "Id";
        private const string LocalePrefix = "Locale_";
        private const string SmartSuffix = "_IsSmart";

        /// <summary>
        /// 모든 StringTableCollection 을 조회하여 단일 CSV 파일로 저장합니다.
        /// </summary>
        /// <param name="filePath">저장할 CSV 절대 경로입니다.</param>
        /// <param name="includeSmartColumns">Smart String 플래그 컬럼을 포함할지 여부입니다.</param>
        /// <returns>작업 결과 정보입니다.</returns>
        public static LocalizationCsvSyncResult ExportAll(string filePath, bool includeSmartColumns)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("CSV 저장 경로가 비어 있습니다.", nameof(filePath));
            }

            var result = new LocalizationCsvSyncResult();
            var collections = LocalizationEditorSettings.GetStringTableCollections()
                .OrderBy(x => x.TableCollectionName, StringComparer.Ordinal)
                .ToList();
            var locales = LocalizationEditorSettings.GetLocales()
                .OrderBy(x => x.Identifier.Code, StringComparer.Ordinal)
                .ToList();

            var headers = BuildHeaders(locales, includeSmartColumns);
            var csvRows = new List<IReadOnlyList<string>>(collections.Count * 10 + 1)
            {
                headers
            };

            foreach (var collection in collections)
            {
                var localeTables = GetLocaleTableMap(collection, locales);
                var sharedEntries = collection.SharedData?.Entries;
                if (sharedEntries == null)
                {
                    result.Log($"[건너뜀] SharedData 가 없는 컬렉션: {collection.TableCollectionName}");
                    continue;
                }

                foreach (var sharedEntry in sharedEntries.OrderBy(x => x.Id))
                {
                    var row = new List<string>(headers.Count)
                    {
                        collection.TableCollectionName,
                        sharedEntry.Key ?? string.Empty,
                        sharedEntry.Id.ToString()
                    };

                    foreach (var locale in locales)
                    {
                        localeTables.TryGetValue(locale.Identifier.Code, out var table);
                        var tableEntry = table != null ? table.GetEntry(sharedEntry.Id) : null;
                        row.Add(tableEntry != null ? tableEntry.Value ?? string.Empty : string.Empty);

                        if (includeSmartColumns)
                        {
                            row.Add(tableEntry != null && tableEntry.IsSmart ? "1" : "0");
                        }
                    }

                    csvRows.Add(row);
                    result.ExportedRowCount++;
                }

                result.ExportedCollectionCount++;
            }

            EnsureParentDirectory(filePath);
            File.WriteAllText(filePath, LocalizationCsvUtility.Write(csvRows), new UTF8Encoding(true));
            AssetDatabase.Refresh();

            result.Log($"CSV 내보내기 완료: {filePath}");
            result.Log($"컬렉션 {result.ExportedCollectionCount}개, 행 {result.ExportedRowCount}개");
            return result;
        }

        /// <summary>
        /// CSV 파일을 읽어 현재 프로젝트의 StringTableCollection 에 병합합니다.
        /// </summary>
        /// <param name="filePath">읽을 CSV 절대 경로입니다.</param>
        /// <param name="options">병합 옵션입니다.</param>
        /// <returns>작업 결과 정보입니다.</returns>
        public static LocalizationCsvSyncResult ImportAndMerge(string filePath, LocalizationCsvSyncOptions options)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("CSV 불러오기 경로가 비어 있습니다.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV 파일을 찾을 수 없습니다.", filePath);
            }

            options ??= LocalizationCsvSyncOptions.CreateDefault();

            var result = new LocalizationCsvSyncResult();
            var document = LocalizationCsvUtility.Parse(File.ReadAllText(filePath));
            var localeColumns = ResolveLocaleColumns(document.Headers, result);
            ValidateRequiredColumns(document.Headers);

            var undoContext = new LocalizationUndoContext();
            var collections = LocalizationEditorSettings.GetStringTableCollections()
                .ToDictionary(x => x.TableCollectionName, StringComparer.Ordinal);
            var localeCache = LocalizationEditorSettings.GetLocales()
                .ToDictionary(x => x.Identifier.Code, StringComparer.Ordinal);
            var sharedEntrySnapshots = BuildSharedEntrySnapshots(collections.Values, result);
            var csvIdentityTracker = new LocalizationCsvIdentityTracker();

            for (int rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
            {
                var rawRow = document.Rows[rowIndex];
                var rowNumber = rowIndex + 2;
                var row = LocalizationCsvRow.From(rawRow, document.HeaderIndexMap, localeColumns);
                result.ImportedRowCount++;

                if (string.IsNullOrWhiteSpace(row.CollectionName))
                {
                    result.SkippedRowCount++;
                    result.Log($"[건너뜀][{rowNumber}] Collection 값이 비어 있습니다.");
                    continue;
                }

                if (!collections.TryGetValue(row.CollectionName, out var collection) || collection == null)
                {
                    result.SkippedRowCount++;
                    result.Log($"[건너뜀][{rowNumber}] 컬렉션을 찾을 수 없습니다: {row.CollectionName}");
                    continue;
                }

                if (!csvIdentityTracker.TryRegister(row, rowNumber, result))
                {
                    result.SkippedRowCount++;
                    continue;
                }

                if (!sharedEntrySnapshots.TryGetValue(
                        row.CollectionName,
                        out LocalizationSharedEntrySnapshot sharedEntrySnapshot))
                {
                    result.SkippedRowCount++;
                    result.WarningCount++;
                    result.Log($"[건너뜀][{rowNumber}] SharedData 조회 스냅샷이 없습니다: {row.CollectionName}");
                    continue;
                }

                var resolve = ResolveSharedEntry(
                    collection,
                    sharedEntrySnapshot,
                    row,
                    options,
                    result,
                    rowNumber,
                    undoContext);
                if (resolve == null)
                {
                    result.SkippedRowCount++;
                    continue;
                }

                ApplyLocaleValues(collection, resolve, row, localeColumns, localeCache, options, result, rowNumber, undoContext);
            }

            if (!options.DryRun)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            result.Log(options.DryRun ? "미리보기 완료" : "CSV 병합 적용 완료");
            result.Log($"행 {result.ImportedRowCount}개, 신규 키 {result.CreatedEntryCount}개, 값 변경 {result.UpdatedValueCount}개, Smart 변경 {result.UpdatedSmartFlagCount}개, 경고 {result.WarningCount}개, 건너뜀 {result.SkippedRowCount}개");
            return result;
        }

        /// <summary>
        /// 각 String Table Collection의 실제 <see cref="SharedTableData.Entries"/>를 기준으로
        /// ID/Key 조회 스냅샷을 생성합니다.
        /// Unity Localization 내부 조회 캐시가 오래된 상태여도 직렬화 목록을 기준으로 병합할 수 있습니다.
        /// </summary>
        /// <param name="collections">현재 프로젝트의 String Table Collection 목록입니다.</param>
        /// <param name="result">중복 Shared Entry 진단을 기록할 작업 결과입니다.</param>
        /// <returns>컬렉션 이름과 Shared Entry 조회 스냅샷의 매핑입니다.</returns>
        private static Dictionary<string, LocalizationSharedEntrySnapshot> BuildSharedEntrySnapshots(
            IEnumerable<StringTableCollection> collections,
            LocalizationCsvSyncResult result)
        {
            var snapshots = new Dictionary<string, LocalizationSharedEntrySnapshot>(StringComparer.Ordinal);
            foreach (StringTableCollection collection in collections)
            {
                if (collection == null || string.IsNullOrWhiteSpace(collection.TableCollectionName))
                {
                    continue;
                }

                snapshots[collection.TableCollectionName] =
                    LocalizationSharedEntrySnapshot.Create(collection, result);
            }

            return snapshots;
        }

        /// <summary>
        /// CSV 헤더 목록을 생성합니다.
        /// </summary>
        /// <param name="locales">프로젝트에 등록된 Locale 목록입니다.</param>
        /// <param name="includeSmartColumns">Smart 컬럼 포함 여부입니다.</param>
        /// <returns>CSV 헤더 목록입니다.</returns>
        private static List<string> BuildHeaders(IReadOnlyList<Locale> locales, bool includeSmartColumns)
        {
            var headers = new List<string>
            {
                ColumnCollection,
                ColumnKey,
                ColumnId
            };

            foreach (var locale in locales)
            {
                var code = locale.Identifier.Code;
                headers.Add(GetLocaleColumnName(code));
                if (includeSmartColumns)
                {
                    headers.Add(GetLocaleSmartColumnName(code));
                }
            }

            return headers;
        }

        /// <summary>
        /// 컬렉션의 각 Locale 테이블을 코드 기준 사전으로 구성합니다.
        /// </summary>
        /// <param name="collection">대상 컬렉션입니다.</param>
        /// <param name="locales">조회할 Locale 목록입니다.</param>
        /// <returns>Locale 코드와 테이블의 매핑입니다.</returns>
        private static Dictionary<string, StringTable> GetLocaleTableMap(StringTableCollection collection, IEnumerable<Locale> locales)
        {
            var map = new Dictionary<string, StringTable>(StringComparer.Ordinal);
            foreach (var locale in locales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                map[locale.Identifier.Code] = table;
            }

            return map;
        }

        /// <summary>
        /// CSV 에 포함된 Locale 관련 컬럼을 해석합니다.
        /// </summary>
        /// <param name="headers">헤더 목록입니다.</param>
        /// <param name="result">작업 결과 로그 대상입니다.</param>
        /// <returns>Locale 컬럼 정보 목록입니다.</returns>
        private static List<LocaleColumnInfo> ResolveLocaleColumns(IReadOnlyList<string> headers, LocalizationCsvSyncResult result)
        {
            var list = new List<LocaleColumnInfo>();
            var map = new Dictionary<string, LocaleColumnInfo>(StringComparer.Ordinal);

            for (int index = 0; index < headers.Count; index++)
            {
                var header = headers[index] ?? string.Empty;
                if (!header.StartsWith(LocalePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (header.EndsWith(SmartSuffix, StringComparison.Ordinal))
                {
                    var localeCode = header.Substring(LocalePrefix.Length, header.Length - LocalePrefix.Length - SmartSuffix.Length);
                    if (!map.TryGetValue(localeCode, out var info))
                    {
                        info = new LocaleColumnInfo(localeCode);
                        map.Add(localeCode, info);
                        list.Add(info);
                    }

                    info.SmartColumnIndex = index;
                    continue;
                }

                var code = header.Substring(LocalePrefix.Length);
                if (!map.TryGetValue(code, out var localeInfo))
                {
                    localeInfo = new LocaleColumnInfo(code);
                    map.Add(code, localeInfo);
                    list.Add(localeInfo);
                }

                localeInfo.ValueColumnIndex = index;
            }

            if (list.Count == 0)
            {
                result.Log("[경고] Locale 컬럼이 없습니다. Collection/Key/Id 만 읽히며 실제 번역 값은 반영되지 않습니다.");
                result.WarningCount++;
            }

            return list.OrderBy(x => x.LocaleCode, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// 필수 헤더가 존재하는지 검증합니다.
        /// </summary>
        /// <param name="headers">CSV 헤더 목록입니다.</param>
        private static void ValidateRequiredColumns(IReadOnlyList<string> headers)
        {
            if (!headers.Contains(ColumnCollection))
            {
                throw new InvalidDataException($"필수 헤더가 없습니다: {ColumnCollection}");
            }

            if (!headers.Contains(ColumnKey))
            {
                throw new InvalidDataException($"필수 헤더가 없습니다: {ColumnKey}");
            }

            if (!headers.Contains(ColumnId))
            {
                throw new InvalidDataException($"필수 헤더가 없습니다: {ColumnId}");
            }
        }

        /// <summary>
        /// CSV 행을 현재 컬렉션의 SharedTableEntry 와 연결하거나, 필요 시 신규 생성합니다.
        /// </summary>
        /// <param name="collection">대상 컬렉션입니다.</param>
        /// <param name="row">CSV 행 데이터입니다.</param>
        /// <param name="options">병합 옵션입니다.</param>
        /// <param name="result">작업 결과입니다.</param>
        /// <param name="rowNumber">CSV 표시용 행 번호입니다.</param>
        /// <param name="undoContext">Undo 기록 컨텍스트입니다.</param>
        /// <returns>매칭된 SharedTableEntry 입니다.</returns>
        private static ResolvedSharedEntry ResolveSharedEntry(
            StringTableCollection collection,
            LocalizationSharedEntrySnapshot sharedEntrySnapshot,
            LocalizationCsvRow row,
            LocalizationCsvSyncOptions options,
            LocalizationCsvSyncResult result,
            int rowNumber,
            LocalizationUndoContext undoContext)
        {
            var sharedData = collection.SharedData;
            if (sharedData == null)
            {
                result.WarningCount++;
                result.Log($"[경고][{rowNumber}] SharedData 가 없습니다: {collection.TableCollectionName}");
                return null;
            }

            ResolvedSharedEntry resolvedEntry = null;
            if (row.Id > 0)
            {
                resolvedEntry = sharedEntrySnapshot.GetById(row.Id);
            }

            if (resolvedEntry == null && !string.IsNullOrWhiteSpace(row.Key))
            {
                resolvedEntry = sharedEntrySnapshot.GetByKey(row.Key);
            }

            if (resolvedEntry == null)
            {
                if (!options.CreateMissingEntries)
                {
                    result.WarningCount++;
                    result.Log($"[건너뜀][{rowNumber}] 항목이 없고 신규 생성이 비활성화되어 있습니다: {collection.TableCollectionName} / {row.Key}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(row.Key))
                {
                    result.WarningCount++;
                    result.Log($"[건너뜀][{rowNumber}] 신규 생성이 필요하지만 Key 값이 비어 있습니다.");
                    return null;
                }

                SharedTableData.SharedTableEntry sharedEntry = null;
                if (!options.DryRun)
                {
                    undoContext.Record(sharedData, "Merge Localization CSV - SharedData");
                    sharedEntry = row.Id > 0 ? sharedData.AddKey(row.Key, row.Id) : sharedData.AddKey(row.Key);
                    if (sharedEntry == null)
                    {
                        // Unity 내부 캐시 대신 실제 Entries 목록을 다시 스캔하여 생성 결과를 확인합니다.
                        sharedEntry = sharedData.Entries.FirstOrDefault(
                            entry => entry != null &&
                                     string.Equals(entry.Key, row.Key, StringComparison.Ordinal));
                    }
                    EditorUtility.SetDirty(sharedData);
                }

                result.CreatedEntryCount++;
                result.Log($"[신규][{rowNumber}] {collection.TableCollectionName} / {row.Key}");
                resolvedEntry = ResolvedSharedEntry.Create(sharedEntry, row.Id, row.Key);
                sharedEntrySnapshot.Register(resolvedEntry);
                return resolvedEntry;
            }

            if (!string.IsNullOrWhiteSpace(row.Key) &&
                !string.Equals(resolvedEntry.Key, row.Key, StringComparison.Ordinal))
            {
                if (!options.AllowKeyRename)
                {
                    result.WarningCount++;
                    result.Log($"[경고][{rowNumber}] Id 로 매칭되었지만 Key 가 다릅니다. 기존 Key 유지: {collection.TableCollectionName} / 기존={resolvedEntry.Key} / CSV={row.Key}");
                    return resolvedEntry;
                }

                ResolvedSharedEntry duplicateKeyEntry = sharedEntrySnapshot.GetByKey(row.Key);
                if (duplicateKeyEntry != null && duplicateKeyEntry.Id != resolvedEntry.Id)
                {
                    result.WarningCount++;
                    result.Log($"[경고][{rowNumber}] 변경 대상 Key 가 이미 존재하여 이름을 바꾸지 못했습니다: {collection.TableCollectionName} / {row.Key}");
                    return resolvedEntry;
                }

                if (!options.DryRun)
                {
                    undoContext.Record(sharedData, "Merge Localization CSV - Rename Key");
                    sharedData.RenameKey(resolvedEntry.Id, row.Key);
                    EditorUtility.SetDirty(sharedData);
                }

                sharedEntrySnapshot.Rename(resolvedEntry, row.Key);
                result.RenamedKeyCount++;
                result.Log($"[이름변경][{rowNumber}] {collection.TableCollectionName} / {resolvedEntry.Id} / {row.Key}");
            }

            return resolvedEntry;
        }

        /// <summary>
        /// 한 행의 Locale 값과 Smart 플래그를 각 StringTable 에 반영합니다.
        /// </summary>
        /// <param name="collection">대상 컬렉션입니다.</param>
        /// <param name="sharedEntry">대상 SharedTableEntry 입니다.</param>
        /// <param name="row">CSV 행 데이터입니다.</param>
        /// <param name="localeColumns">CSV Locale 컬럼 정보입니다.</param>
        /// <param name="localeCache">프로젝트 Locale 캐시입니다.</param>
        /// <param name="options">병합 옵션입니다.</param>
        /// <param name="result">작업 결과입니다.</param>
        /// <param name="rowNumber">CSV 표시용 행 번호입니다.</param>
        /// <param name="undoContext">Undo 기록 컨텍스트입니다.</param>
        private static void ApplyLocaleValues(
            StringTableCollection collection,
            ResolvedSharedEntry sharedEntry,
            LocalizationCsvRow row,
            IReadOnlyList<LocaleColumnInfo> localeColumns,
            IReadOnlyDictionary<string, Locale> localeCache,
            LocalizationCsvSyncOptions options,
            LocalizationCsvSyncResult result,
            int rowNumber,
            LocalizationUndoContext undoContext)
        {
            foreach (var localeColumn in localeColumns)
            {
                if (!localeCache.TryGetValue(localeColumn.LocaleCode, out var locale) || locale == null)
                {
                    result.WarningCount++;
                    result.Log($"[경고][{rowNumber}] 프로젝트에 없는 Locale 입니다: {localeColumn.LocaleCode}");
                    continue;
                }

                if (!row.LocaleValues.TryGetValue(localeColumn.LocaleCode, out var localeValue))
                {
                    continue;
                }

                var hasValueColumn = localeColumn.ValueColumnIndex >= 0;
                var hasSmartColumn = localeColumn.SmartColumnIndex >= 0;
                var shouldTouchValue = hasValueColumn && (options.OverwriteWithEmptyValue || !string.IsNullOrEmpty(localeValue.Value));
                var shouldTouchSmart = hasSmartColumn && localeValue.IsSmart.HasValue;
                if (!shouldTouchValue && !shouldTouchSmart)
                {
                    continue;
                }

                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    if (!options.CreateMissingLocaleTables)
                    {
                        result.WarningCount++;
                        result.Log($"[건너뜀][{rowNumber}] Locale 테이블이 없고 자동 생성이 비활성화되어 있습니다: {collection.TableCollectionName} / {locale.Identifier.Code}");
                        continue;
                    }

                    if (!options.DryRun)
                    {
                        table = HelperLocalization.EnsureLocaleTable(collection, locale);
                    }
                }

                var currentEntry = table != null ? table.GetEntry(sharedEntry.Id) : null;
                var originalValue = currentEntry != null ? currentEntry.Value ?? string.Empty : string.Empty;
                var originalSmart = currentEntry != null && currentEntry.IsSmart;

                if (options.DryRun)
                {
                    if (shouldTouchValue && !string.Equals(originalValue, localeValue.Value ?? string.Empty, StringComparison.Ordinal))
                    {
                        result.UpdatedValueCount++;
                        result.Log($"[값변경][{rowNumber}] {collection.TableCollectionName} / {sharedEntry.Key} / {locale.Identifier.Code}");
                    }

                    if (shouldTouchSmart && originalSmart != localeValue.IsSmart.Value)
                    {
                        result.UpdatedSmartFlagCount++;
                        result.Log($"[Smart변경][{rowNumber}] {collection.TableCollectionName} / {sharedEntry.Key} / {locale.Identifier.Code} / {originalSmart} -> {localeValue.IsSmart.Value}");
                    }

                    continue;
                }

                if (table == null)
                {
                    result.WarningCount++;
                    result.Log($"[경고][{rowNumber}] Locale 테이블 생성에 실패했습니다: {collection.TableCollectionName} / {locale.Identifier.Code}");
                    continue;
                }

                var touched = false;
                StringTableEntry targetEntry = currentEntry;
                if (shouldTouchValue)
                {
                    if (!string.Equals(originalValue, localeValue.Value ?? string.Empty, StringComparison.Ordinal) || currentEntry == null)
                    {
                        undoContext.Record(table, "Merge Localization CSV - Update Value");
                        targetEntry = table.AddEntry(sharedEntry.Id, localeValue.Value ?? string.Empty);
                        EditorUtility.SetDirty(table);
                        result.UpdatedValueCount++;
                        result.Log($"[값변경][{rowNumber}] {collection.TableCollectionName} / {sharedEntry.Key} / {locale.Identifier.Code}");
                        touched = true;
                    }
                }
                else if (currentEntry != null)
                {
                    targetEntry = currentEntry;
                }
                else if (shouldTouchSmart)
                {
                    undoContext.Record(table, "Merge Localization CSV - Create Entry For Smart");
                    targetEntry = table.AddEntry(sharedEntry.Id, string.Empty);
                    EditorUtility.SetDirty(table);
                    touched = true;
                }

                if (targetEntry != null && shouldTouchSmart && targetEntry.IsSmart != localeValue.IsSmart.Value)
                {
                    if (!touched)
                    {
                        undoContext.Record(table, "Merge Localization CSV - Update Smart");
                    }

                    targetEntry.IsSmart = localeValue.IsSmart.Value;
                    EditorUtility.SetDirty(table);
                    result.UpdatedSmartFlagCount++;
                    result.Log($"[Smart변경][{rowNumber}] {collection.TableCollectionName} / {sharedEntry.Key} / {locale.Identifier.Code} / {originalSmart} -> {localeValue.IsSmart.Value}");
                }
            }
        }

        /// <summary>
        /// Locale 값 컬럼명을 생성합니다.
        /// </summary>
        /// <param name="localeCode">Locale 코드입니다.</param>
        /// <returns>CSV 컬럼명입니다.</returns>
        private static string GetLocaleColumnName(string localeCode)
        {
            return $"{LocalePrefix}{localeCode}";
        }

        /// <summary>
        /// Locale Smart 플래그 컬럼명을 생성합니다.
        /// </summary>
        /// <param name="localeCode">Locale 코드입니다.</param>
        /// <returns>CSV Smart 컬럼명입니다.</returns>
        private static string GetLocaleSmartColumnName(string localeCode)
        {
            return $"{LocalePrefix}{localeCode}{SmartSuffix}";
        }

        /// <summary>
        /// 파일 저장 전 상위 디렉터리를 보장합니다.
        /// </summary>
        /// <param name="filePath">대상 파일 경로입니다.</param>
        private static void EnsureParentDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// Localization CSV 병합 옵션을 정의합니다.
    /// </summary>
    internal sealed class LocalizationCsvSyncOptions
    {
        /// <summary>
        /// 빈 문자열 셀도 실제 값으로 반영할지 여부입니다.
        /// </summary>
        public bool OverwriteWithEmptyValue { get; set; }

        /// <summary>
        /// CSV 에 없는 항목이 발견되면 신규 Key 를 생성할지 여부입니다.
        /// </summary>
        public bool CreateMissingEntries { get; set; }

        /// <summary>
        /// 대상 Locale 테이블이 없을 때 자동 생성할지 여부입니다.
        /// </summary>
        public bool CreateMissingLocaleTables { get; set; }

        /// <summary>
        /// CSV 의 Key 가 다를 경우 Shared Key 이름 변경을 허용할지 여부입니다.
        /// </summary>
        public bool AllowKeyRename { get; set; }

        /// <summary>
        /// 실제 자산을 수정하지 않고 결과만 계산할지 여부입니다.
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// 기본 옵션을 생성합니다.
        /// </summary>
        /// <returns>기본 병합 옵션입니다.</returns>
        public static LocalizationCsvSyncOptions CreateDefault()
        {
            return new LocalizationCsvSyncOptions
            {
                OverwriteWithEmptyValue = false,
                CreateMissingEntries = true,
                CreateMissingLocaleTables = true,
                AllowKeyRename = false,
                DryRun = false
            };
        }
    }

    /// <summary>
    /// Localization CSV 작업 결과를 누적합니다.
    /// </summary>
    internal sealed class LocalizationCsvSyncResult
    {
        private readonly StringBuilder _logBuilder = new StringBuilder(1024);

        /// <summary>CSV 내보내기 시 처리된 컬렉션 수입니다.</summary>
        public int ExportedCollectionCount { get; set; }

        /// <summary>CSV 내보내기 시 처리된 행 수입니다.</summary>
        public int ExportedRowCount { get; set; }

        /// <summary>CSV 가져오기 시 읽은 행 수입니다.</summary>
        public int ImportedRowCount { get; set; }

        /// <summary>신규 생성된 Shared Key 수입니다.</summary>
        public int CreatedEntryCount { get; set; }

        /// <summary>값이 변경된 Locale 항목 수입니다.</summary>
        public int UpdatedValueCount { get; set; }

        /// <summary>Smart 플래그가 변경된 항목 수입니다.</summary>
        public int UpdatedSmartFlagCount { get; set; }

        /// <summary>Key 이름이 변경된 수입니다.</summary>
        public int RenamedKeyCount { get; set; }

        /// <summary>건너뛴 행 수입니다.</summary>
        public int SkippedRowCount { get; set; }

        /// <summary>경고 수입니다.</summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// 결과 로그를 누적합니다.
        /// </summary>
        /// <param name="message">추가할 로그 메시지입니다.</param>
        public void Log(string message)
        {
            _logBuilder.AppendLine(message);
        }

        /// <summary>
        /// 누적된 결과 로그를 문자열로 반환합니다.
        /// </summary>
        /// <returns>결과 로그 문자열입니다.</returns>
        public string GetLogText()
        {
            return _logBuilder.ToString();
        }
    }

    /// <summary>
    /// 한 String Table Collection의 Shared Entry를 ID와 Key로 조회하는 작업 단위 스냅샷입니다.
    /// Unity Localization의 내부 Dictionary 캐시 대신 실제 <see cref="SharedTableData.Entries"/>를 기준으로 구성합니다.
    /// </summary>
    internal sealed class LocalizationSharedEntrySnapshot
    {
        private readonly Dictionary<long, ResolvedSharedEntry> _entriesById =
            new Dictionary<long, ResolvedSharedEntry>();
        private readonly Dictionary<string, ResolvedSharedEntry> _entriesByKey =
            new Dictionary<string, ResolvedSharedEntry>(StringComparer.Ordinal);

        /// <summary>
        /// 컬렉션의 실제 Shared Entry 목록을 순회하여 조회 스냅샷을 생성하고 중복 데이터를 진단합니다.
        /// </summary>
        /// <param name="collection">조회 스냅샷을 생성할 String Table Collection입니다.</param>
        /// <param name="result">중복 진단을 기록할 작업 결과입니다.</param>
        /// <returns>생성된 Shared Entry 조회 스냅샷입니다.</returns>
        public static LocalizationSharedEntrySnapshot Create(
            StringTableCollection collection,
            LocalizationCsvSyncResult result)
        {
            var snapshot = new LocalizationSharedEntrySnapshot();
            SharedTableData sharedData = collection?.SharedData;
            if (sharedData?.Entries == null)
            {
                return snapshot;
            }

            for (int index = 0; index < sharedData.Entries.Count; index++)
            {
                SharedTableData.SharedTableEntry sharedEntry = sharedData.Entries[index];
                if (sharedEntry == null)
                {
                    result.WarningCount++;
                    result.Log($"[오류] SharedData에 비어 있는 항목이 있습니다: {collection.TableCollectionName} / index={index}");
                    continue;
                }

                ResolvedSharedEntry resolvedEntry =
                    ResolvedSharedEntry.Create(sharedEntry, sharedEntry.Id, sharedEntry.Key);
                snapshot.RegisterInitial(collection.TableCollectionName, resolvedEntry, result);
            }

            return snapshot;
        }

        /// <summary>
        /// ID로 Shared Entry를 조회합니다.
        /// </summary>
        /// <param name="id">조회할 Shared Entry ID입니다.</param>
        /// <returns>일치하는 항목이며, 없으면 <see langword="null"/>입니다.</returns>
        public ResolvedSharedEntry GetById(long id)
        {
            return id > 0 && _entriesById.TryGetValue(id, out ResolvedSharedEntry entry)
                ? entry
                : null;
        }

        /// <summary>
        /// Key로 Shared Entry를 조회합니다.
        /// </summary>
        /// <param name="key">조회할 Shared Entry Key입니다.</param>
        /// <returns>일치하는 항목이며, 없으면 <see langword="null"/>입니다.</returns>
        public ResolvedSharedEntry GetByKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                   _entriesByKey.TryGetValue(key, out ResolvedSharedEntry entry)
                ? entry
                : null;
        }

        /// <summary>
        /// 병합 중 신규 생성되거나 미리보기에서 가상 생성된 항목을 스냅샷에 등록합니다.
        /// </summary>
        /// <param name="entry">등록할 해석된 Shared Entry입니다.</param>
        public void Register(ResolvedSharedEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.Id > 0)
            {
                _entriesById[entry.Id] = entry;
            }

            if (!string.IsNullOrWhiteSpace(entry.Key))
            {
                _entriesByKey[entry.Key] = entry;
            }
        }

        /// <summary>
        /// 병합 과정에서 변경된 Key를 스냅샷에 즉시 반영합니다.
        /// 실제 Merge와 미리보기가 동일한 후속 행 매칭 결과를 갖도록 합니다.
        /// </summary>
        /// <param name="entry">이름이 변경된 Shared Entry입니다.</param>
        /// <param name="newKey">CSV에서 요청한 새 Key입니다.</param>
        public void Rename(ResolvedSharedEntry entry, string newKey)
        {
            if (entry == null || string.IsNullOrWhiteSpace(newKey))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(entry.Key))
            {
                _entriesByKey.Remove(entry.Key);
            }

            entry.SetKey(newKey);
            _entriesByKey[newKey] = entry;
        }

        /// <summary>
        /// 초기 Shared Data 항목을 등록하면서 중복 ID와 중복 Key를 명확하게 진단합니다.
        /// 중복 시 첫 번째 항목을 유지하여 이후 결과가 순회 순서와 무관하게 바뀌지 않도록 합니다.
        /// </summary>
        /// <param name="collectionName">진단에 표시할 컬렉션 이름입니다.</param>
        /// <param name="entry">등록할 Shared Entry입니다.</param>
        /// <param name="result">중복 진단을 기록할 작업 결과입니다.</param>
        private void RegisterInitial(
            string collectionName,
            ResolvedSharedEntry entry,
            LocalizationCsvSyncResult result)
        {
            if (entry.Id > 0)
            {
                if (_entriesById.TryGetValue(entry.Id, out ResolvedSharedEntry duplicateIdEntry))
                {
                    result.WarningCount++;
                    result.Log($"[오류] SharedData에 중복 ID가 있습니다: {collectionName} / ID={entry.Id} / 첫 Key={duplicateIdEntry.Key} / 중복 Key={entry.Key}");
                }
                else
                {
                    _entriesById.Add(entry.Id, entry);
                }
            }

            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                return;
            }

            if (_entriesByKey.TryGetValue(entry.Key, out ResolvedSharedEntry duplicateKeyEntry))
            {
                result.WarningCount++;
                result.Log($"[오류] SharedData에 중복 Key가 있습니다: {collectionName} / Key={entry.Key} / 첫 ID={duplicateKeyEntry.Id} / 중복 ID={entry.Id}");
                return;
            }

            _entriesByKey.Add(entry.Key, entry);
        }
    }

    /// <summary>
    /// CSV 문서 안에서 Collection+ID 및 Collection+Key 중복 행을 추적합니다.
    /// </summary>
    internal sealed class LocalizationCsvIdentityTracker
    {
        private const char IdentitySeparator = '\u001F';
        private readonly Dictionary<string, int> _firstRowById =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _firstRowByKey =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// CSV 행의 ID와 Key 조합을 등록하고 앞선 행과의 중복 여부를 검사합니다.
        /// 중복 행은 동일 엔트리를 두 번 변경하지 않도록 병합 대상에서 제외합니다.
        /// </summary>
        /// <param name="row">검사할 CSV 행입니다.</param>
        /// <param name="rowNumber">로그에 표시할 현재 CSV 행 번호입니다.</param>
        /// <param name="result">중복 진단을 기록할 작업 결과입니다.</param>
        /// <returns>처음 등장한 행이면 <see langword="true"/>, 중복이면 <see langword="false"/>입니다.</returns>
        public bool TryRegister(
            LocalizationCsvRow row,
            int rowNumber,
            LocalizationCsvSyncResult result)
        {
            bool isDuplicate = false;
            string idIdentity = string.Empty;
            string keyIdentity = string.Empty;
            if (row.Id > 0)
            {
                idIdentity = BuildIdentity(row.CollectionName, row.Id.ToString());
                if (_firstRowById.TryGetValue(idIdentity, out int firstIdRow))
                {
                    result.WarningCount++;
                    result.Log($"[건너뜀][{rowNumber}] CSV에 중복 Collection+ID가 있습니다: {row.CollectionName} / ID={row.Id} / 첫 행={firstIdRow}");
                    isDuplicate = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.Key))
            {
                keyIdentity = BuildIdentity(row.CollectionName, row.Key);
                if (_firstRowByKey.TryGetValue(keyIdentity, out int firstKeyRow))
                {
                    result.WarningCount++;
                    result.Log($"[건너뜀][{rowNumber}] CSV에 중복 Collection+Key가 있습니다: {row.CollectionName} / Key={row.Key} / 첫 행={firstKeyRow}");
                    isDuplicate = true;
                }
            }

            if (isDuplicate)
            {
                return false;
            }

            // 중복으로 건너뛴 행은 최초 행으로 등록하지 않아 후속 진단의 기준이 되지 않도록 합니다.
            if (!string.IsNullOrEmpty(idIdentity))
            {
                _firstRowById.Add(idIdentity, rowNumber);
            }

            if (!string.IsNullOrEmpty(keyIdentity))
            {
                _firstRowByKey.Add(keyIdentity, rowNumber);
            }

            return true;
        }

        /// <summary>
        /// 컬렉션 이름과 ID 또는 Key를 충돌 없는 내부 식별 문자열로 결합합니다.
        /// </summary>
        /// <param name="collectionName">CSV Collection 값입니다.</param>
        /// <param name="entryIdentity">CSV ID 또는 Key 값입니다.</param>
        /// <returns>중복 검사용 내부 식별 문자열입니다.</returns>
        private static string BuildIdentity(string collectionName, string entryIdentity)
        {
            return string.Concat(collectionName, IdentitySeparator, entryIdentity);
        }
    }

    /// <summary>
    /// 실제 SharedTableEntry 와, 미리보기에서 사용할 Key/Id 정보를 함께 보관합니다.
    /// </summary>
    internal sealed class ResolvedSharedEntry
    {
        /// <summary>
        /// 실제 SharedTableEntry 인스턴스입니다. 미리보기에서는 null 일 수 있습니다.
        /// </summary>
        public SharedTableData.SharedTableEntry SharedEntry { get; private set; }

        /// <summary>
        /// 병합에 사용할 Id 값입니다.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// 병합에 사용할 Key 값입니다.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// SharedTableEntry 또는 CSV 행 값을 기반으로 결과 객체를 생성합니다.
        /// </summary>
        /// <param name="sharedEntry">실제 SharedTableEntry 입니다.</param>
        /// <param name="fallbackId">미리보기 시 사용할 대체 Id 입니다.</param>
        /// <param name="fallbackKey">미리보기 시 사용할 대체 Key 입니다.</param>
        /// <returns>해석된 엔트리 정보입니다.</returns>
        public static ResolvedSharedEntry Create(SharedTableData.SharedTableEntry sharedEntry, long fallbackId, string fallbackKey)
        {
            return new ResolvedSharedEntry
            {
                SharedEntry = sharedEntry,
                Id = sharedEntry != null ? sharedEntry.Id : fallbackId,
                Key = sharedEntry != null ? sharedEntry.Key : fallbackKey
            };
        }

        /// <summary>
        /// 미리보기 또는 실제 이름 변경 결과를 해석된 항목에 반영합니다.
        /// </summary>
        /// <param name="key">새 Shared Entry Key입니다.</param>
        public void SetKey(string key)
        {
            Key = key ?? string.Empty;
        }
    }

    /// <summary>
    /// CSV 한 행을 Localization 병합용 구조로 변환한 모델입니다.
    /// </summary>
    internal sealed class LocalizationCsvRow
    {
        /// <summary>
        /// 컬렉션 이름입니다.
        /// </summary>
        public string CollectionName { get; private set; }

        /// <summary>
        /// Key 이름입니다.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Shared Key Id 입니다.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Locale 값 맵입니다.
        /// </summary>
        public Dictionary<string, LocalizationCsvLocaleValue> LocaleValues { get; } = new Dictionary<string, LocalizationCsvLocaleValue>(StringComparer.Ordinal);

        /// <summary>
        /// 원시 CSV 행을 LocalizationCsvRow 로 변환합니다.
        /// </summary>
        /// <param name="columns">원시 컬럼 값 목록입니다.</param>
        /// <param name="headerIndexMap">헤더 이름과 인덱스 매핑입니다.</param>
        /// <param name="localeColumns">Locale 컬럼 정보 목록입니다.</param>
        /// <returns>변환된 행 모델입니다.</returns>
        public static LocalizationCsvRow From(IReadOnlyList<string> columns, IReadOnlyDictionary<string, int> headerIndexMap, IReadOnlyList<LocaleColumnInfo> localeColumns)
        {
            var row = new LocalizationCsvRow
            {
                CollectionName = GetCell(columns, headerIndexMap, "Collection"),
                Key = GetCell(columns, headerIndexMap, "Key")
            };

            var idRaw = GetCell(columns, headerIndexMap, "Id");
            if (long.TryParse(idRaw, out var parsedId))
            {
                row.Id = parsedId;
            }

            foreach (var localeColumn in localeColumns)
            {
                var localeValue = new LocalizationCsvLocaleValue();
                if (localeColumn.ValueColumnIndex >= 0)
                {
                    localeValue.Value = GetCell(columns, localeColumn.ValueColumnIndex);
                }

                if (localeColumn.SmartColumnIndex >= 0)
                {
                    var smartRaw = GetCell(columns, localeColumn.SmartColumnIndex);
                    if (!string.IsNullOrWhiteSpace(smartRaw))
                    {
                        localeValue.IsSmart = ParseSmartFlag(smartRaw);
                    }
                }

                row.LocaleValues[localeColumn.LocaleCode] = localeValue;
            }

            return row;
        }

        /// <summary>
        /// 헤더 이름으로 셀 값을 읽습니다.
        /// </summary>
        /// <param name="columns">행 컬럼 값입니다.</param>
        /// <param name="headerIndexMap">헤더 이름과 인덱스 매핑입니다.</param>
        /// <param name="headerName">읽을 헤더 이름입니다.</param>
        /// <returns>셀 문자열입니다.</returns>
        private static string GetCell(IReadOnlyList<string> columns, IReadOnlyDictionary<string, int> headerIndexMap, string headerName)
        {
            return headerIndexMap.TryGetValue(headerName, out var index) ? GetCell(columns, index) : string.Empty;
        }

        /// <summary>
        /// 인덱스로 셀 값을 읽습니다.
        /// </summary>
        /// <param name="columns">행 컬럼 값입니다.</param>
        /// <param name="index">컬럼 인덱스입니다.</param>
        /// <returns>셀 문자열입니다.</returns>
        private static string GetCell(IReadOnlyList<string> columns, int index)
        {
            if (index < 0 || index >= columns.Count)
            {
                return string.Empty;
            }

            return columns[index] ?? string.Empty;
        }

        /// <summary>
        /// CSV Smart 플래그 문자열을 bool 로 변환합니다.
        /// </summary>
        /// <param name="raw">원본 문자열입니다.</param>
        /// <returns>해석된 Smart 여부입니다.</returns>
        private static bool ParseSmartFlag(string raw)
        {
            if (string.Equals(raw, "1", StringComparison.Ordinal) ||
                string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// CSV 의 Locale 셀 값과 Smart 플래그를 보관합니다.
    /// </summary>
    internal sealed class LocalizationCsvLocaleValue
    {
        /// <summary>
        /// 번역 문자열 값입니다.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Smart String 여부입니다. CSV 에 컬럼이 없으면 null 입니다.
        /// </summary>
        public bool? IsSmart { get; set; }
    }

    /// <summary>
    /// CSV 헤더에서 해석한 Locale 컬럼 위치 정보입니다.
    /// </summary>
    internal sealed class LocaleColumnInfo
    {
        /// <summary>
        /// Locale 코드를 초기화합니다.
        /// </summary>
        /// <param name="localeCode">Locale 코드입니다.</param>
        public LocaleColumnInfo(string localeCode)
        {
            LocaleCode = localeCode;
            ValueColumnIndex = -1;
            SmartColumnIndex = -1;
        }

        /// <summary>
        /// Locale 코드입니다.
        /// </summary>
        public string LocaleCode { get; }

        /// <summary>
        /// 문자열 값 컬럼 인덱스입니다.
        /// </summary>
        public int ValueColumnIndex { get; set; }

        /// <summary>
        /// Smart 플래그 컬럼 인덱스입니다.
        /// </summary>
        public int SmartColumnIndex { get; set; }
    }

    /// <summary>
    /// CSV 문서 전체를 보관하는 파싱 결과입니다.
    /// </summary>
    internal sealed class LocalizationCsvDocument
    {
        /// <summary>
        /// CSV 헤더 목록입니다.
        /// </summary>
        public List<string> Headers { get; } = new List<string>();

        /// <summary>
        /// 헤더 이름과 컬럼 인덱스 매핑입니다.
        /// </summary>
        public Dictionary<string, int> HeaderIndexMap { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// 데이터 행 목록입니다.
        /// </summary>
        public List<List<string>> Rows { get; } = new List<List<string>>();
    }

    /// <summary>
    /// CSV 파싱 및 직렬화 유틸리티입니다.
    /// </summary>
    internal static class LocalizationCsvUtility
    {
        /// <summary>
        /// CSV 텍스트를 문서 모델로 파싱합니다.
        /// </summary>
        /// <param name="text">원본 CSV 텍스트입니다.</param>
        /// <returns>파싱 결과 문서입니다.</returns>
        public static LocalizationCsvDocument Parse(string text)
        {
            var rows = ParseRows(text ?? string.Empty);
            if (rows.Count == 0)
            {
                throw new InvalidDataException("CSV 파일이 비어 있습니다.");
            }

            var document = new LocalizationCsvDocument();
            document.Headers.AddRange(rows[0]);
            for (int i = 0; i < document.Headers.Count; i++)
            {
                var header = document.Headers[i] ?? string.Empty;
                if (!document.HeaderIndexMap.ContainsKey(header))
                {
                    document.HeaderIndexMap.Add(header, i);
                }
            }

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                while (row.Count < document.Headers.Count)
                {
                    row.Add(string.Empty);
                }

                document.Rows.Add(row);
            }

            return document;
        }

        /// <summary>
        /// 행/열 구조를 CSV 텍스트로 직렬화합니다.
        /// </summary>
        /// <param name="rows">기록할 행 목록입니다.</param>
        /// <returns>CSV 문자열입니다.</returns>
        public static string Write(IEnumerable<IReadOnlyList<string>> rows)
        {
            var builder = new StringBuilder(1024);
            var firstRow = true;
            foreach (var row in rows)
            {
                if (!firstRow)
                {
                    builder.AppendLine();
                }

                firstRow = false;
                for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    if (columnIndex > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(Escape(row[columnIndex] ?? string.Empty));
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// CSV 문자열을 행 단위로 파싱합니다.
        /// </summary>
        /// <param name="text">원본 CSV 텍스트입니다.</param>
        /// <returns>행 목록입니다.</returns>
        private static List<List<string>> ParseRows(string text)
        {
            var rows = new List<List<string>>();
            var currentRow = new List<string>();
            var currentCell = new StringBuilder();
            var inQuotes = false;

            for (int index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        var nextIsQuote = index + 1 < text.Length && text[index + 1] == '"';
                        if (nextIsQuote)
                        {
                            currentCell.Append('"');
                            index++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentCell.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        break;

                    case ',':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        break;

                    case '\r':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        if (index + 1 < text.Length && text[index + 1] == '\n')
                        {
                            index++;
                        }
                        break;

                    case '\n':
                        currentRow.Add(currentCell.ToString());
                        currentCell.Length = 0;
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        break;

                    default:
                        currentCell.Append(ch);
                        break;
                }
            }

            currentRow.Add(currentCell.ToString());
            if (currentRow.Count > 1 || !string.IsNullOrEmpty(currentRow[0]) || rows.Count == 0)
            {
                rows.Add(currentRow);
            }

            return rows;
        }

        /// <summary>
        /// CSV 규칙에 맞게 셀 문자열을 이스케이프합니다.
        /// </summary>
        /// <param name="value">원본 문자열입니다.</param>
        /// <returns>이스케이프된 문자열입니다.</returns>
        private static string Escape(string value)
        {
            var requiresQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!requiresQuote)
            {
                return value;
            }

            return string.Concat("\"", value.Replace("\"", "\"\""), "\"");
        }
    }

    /// <summary>
    /// 동일 오브젝트에 대한 Undo.RecordObject 호출을 1회로 제한합니다.
    /// </summary>
    internal sealed class LocalizationUndoContext
    {
        private readonly HashSet<int> _recordedInstanceIds = new HashSet<int>();

        /// <summary>
        /// 아직 기록되지 않은 오브젝트만 Undo 에 등록합니다.
        /// </summary>
        /// <param name="target">Undo 대상으로 기록할 오브젝트입니다.</param>
        /// <param name="undoName">Undo 이름입니다.</param>
        public void Record(UnityEngine.Object target, string undoName)
        {
            if (target == null)
            {
                return;
            }

            var instanceId = target.GetInstanceID();
            if (_recordedInstanceIds.Contains(instanceId))
            {
                return;
            }

            Undo.RecordObject(target, undoName);
            _recordedInstanceIds.Add(instanceId);
        }
    }
}
#endif
