using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// sound 테이블 저장 전/후 후처리를 담당하는 SaveProcessor입니다.
    /// - 저장 전: FileName 변경/추가 행의 실제 파일 존재 검증
    /// - 저장 후: 변경된 행만 Addressables 증분 동기화, 삭제된 행은 Addressables에서 제거
    /// </summary>
    internal sealed class TableEditorSoundSaveProcessor : TableEditorSaveProcessorBase
    {
        private const string HeaderUid = "Uid";
        private const string HeaderType = "Type";
        private const string HeaderSubType = "SubType";
        private const string HeaderFileName = "FileName";
        private const string HeaderUseIntroScene = "UseIntroScene";

        private readonly List<StruckTableSound> _rowsToValidate = new List<StruckTableSound>();
        private readonly List<StruckTableSound> _rowsToUpsert = new List<StruckTableSound>();
        private readonly List<StruckTableSound> _rowsToRemove = new List<StruckTableSound>();

        /// <summary>
        /// 처리 대상 테이블 키입니다.
        /// </summary>
        protected override string TargetTableKey => ConfigAddressableTable.Sound;

        /// <summary>
        /// sound 검증은 저장 전에 실행되어야 하므로 낮은 우선순위를 사용합니다.
        /// </summary>
        public override int Order => 10;

        /// <summary>
        /// sound 테이블 저장 전에 FileName 변경/추가 행만 실제 파일 존재 여부를 검증합니다.
        /// 존재하지 않는 파일이 있으면 예외를 발생시켜 저장을 중단합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public override void BeforeSave(TableEditorSaveContext context)
        {
            ClearPendingDelta();

            if (!ShouldRunChangedOnlyProcessing(context))
                return;

            BuildPendingDelta(context);
            ValidateSoundFileNames(_rowsToValidate);
        }

        /// <summary>
        /// sound 테이블 저장 완료 후 변경 행만 Addressables에 증분 반영합니다.
        /// 삭제된 행은 기존 Addressables 엔트리를 제거합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public override void AfterSave(TableEditorSaveContext context)
        {
            try
            {
                if (!ShouldRunChangedOnlyProcessing(context))
                    return;

                if (_rowsToUpsert.Count == 0 && _rowsToRemove.Count == 0)
                    return;

                // 테이블 저장 플로우에서 자동 호출되므로 확인/완료 다이얼로그는 비활성화합니다.
                SettingSound.SyncFromTableDelta(_rowsToUpsert, _rowsToRemove, new SettingSoundOptions
                {
                    ShowConfirmDialog = false,
                    ShowCompletedDialog = false,
                    SaveAssets = true,
                });
            }
            finally
            {
                ClearPendingDelta();
            }
        }

        /// <summary>
        /// 현재 저장 요청이 실제 문서 변경을 포함하는지 확인합니다.
        /// 변경이 없으면 파일 검증과 Addressables 동기화를 모두 생략해 불필요한 후처리를 줄입니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        /// <returns>변경 기반 후처리를 수행해야 하면 true를 반환합니다.</returns>
        private static bool ShouldRunChangedOnlyProcessing(TableEditorSaveContext context)
        {
            return context != null && context.HasDocumentChanges;
        }

        /// <summary>
        /// 저장 전/후에 사용할 증분 변경 목록(검증/등록/삭제 대상)을 계산합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        private void BuildPendingDelta(TableEditorSaveContext context)
        {
            Dictionary<int, StruckTableSound> currentRowsByUid = BuildRowsByUid(context?.Rows);
            Dictionary<int, StruckTableSound> previousRowsByUid = LoadPersistedRowsByUid(context);

            foreach (KeyValuePair<int, StruckTableSound> pair in currentRowsByUid)
            {
                int uid = pair.Key;
                StruckTableSound current = pair.Value;

                if (!previousRowsByUid.TryGetValue(uid, out StruckTableSound previous))
                {
                    AddUpsertAndValidationTargetIfNeeded(current);
                    continue;
                }

                if (!IsFileNameChanged(previous, current))
                    continue;

                if (!string.IsNullOrWhiteSpace(previous.FileName))
                    _rowsToRemove.Add(previous);

                AddUpsertAndValidationTargetIfNeeded(current);
            }

            foreach (KeyValuePair<int, StruckTableSound> pair in previousRowsByUid)
            {
                int uid = pair.Key;
                if (currentRowsByUid.ContainsKey(uid))
                    continue;

                StruckTableSound deleted = pair.Value;
                if (deleted == null || string.IsNullOrWhiteSpace(deleted.FileName))
                    continue;

                _rowsToRemove.Add(deleted);
            }
        }

        /// <summary>
        /// 현재 저장 요청에서 계산한 증분 변경 목록을 초기화합니다.
        /// </summary>
        private void ClearPendingDelta()
        {
            _rowsToValidate.Clear();
            _rowsToUpsert.Clear();
            _rowsToRemove.Clear();
        }

        /// <summary>
        /// FileName이 유효한 행을 Addressables 등록/파일 검증 대상으로 추가합니다.
        /// </summary>
        /// <param name="row">추가 후보 사운드 행입니다.</param>
        private void AddUpsertAndValidationTargetIfNeeded(StruckTableSound row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.FileName))
                return;

            _rowsToUpsert.Add(row);
            _rowsToValidate.Add(row);
        }

        /// <summary>
        /// 저장 전 검증 대상 행을 순회하며 FileName으로부터 실제 에셋 경로를 해석합니다.
        /// </summary>
        /// <param name="rowsToValidate">검증 대상 사운드 행 목록입니다.</param>
        /// <exception cref="InvalidOperationException">파일이 없는 행이 하나라도 있으면 발생합니다.</exception>
        private static void ValidateSoundFileNames(IReadOnlyList<StruckTableSound> rowsToValidate)
        {
            if (rowsToValidate == null || rowsToValidate.Count == 0)
                return;

            List<string> missingEntries = new List<string>();

            for (int i = 0; i < rowsToValidate.Count; i++)
            {
                StruckTableSound row = rowsToValidate[i];
                if (row == null || string.IsNullOrWhiteSpace(row.FileName))
                    continue;

                // FileName 입력 포맷이 프로젝트마다 다를 수 있어, 가능한 후보 경로를 순차적으로 검사합니다.
                if (TryResolveExistingSoundAssetPath(row.FileName, row.Type, row.SubType, out _))
                    continue;

                string basePath = ConfigAddressablePath.BuildSoundPath(row.Type, row.SubType);
                missingEntries.Add($"- Uid={row.Uid}, FileName='{row.FileName}', BasePath='{basePath}'");
            }

            if (missingEntries.Count == 0)
                return;

            const int maxPreviewCount = 12;
            int previewCount = Math.Min(maxPreviewCount, missingEntries.Count);
            List<string> preview = missingEntries.GetRange(0, previewCount);

            string message = "sound 테이블 저장을 중단했습니다.\n"
                             + "FileName 컬럼에 입력된 사운드 파일을 찾을 수 없습니다.\n\n"
                             + string.Join("\n", preview);

            if (missingEntries.Count > previewCount)
                message += $"\n... 외 {missingEntries.Count - previewCount}건";

            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// 현재 편집 중인 행 목록을 UID 기준 사운드 딕셔너리로 변환합니다.
        /// UID가 없거나 0 이하인 행은 비교 대상에서 제외합니다.
        /// </summary>
        /// <param name="rows">변환할 TableEditor 행 목록입니다.</param>
        /// <returns>UID 기준 사운드 행 딕셔너리입니다.</returns>
        private static Dictionary<int, StruckTableSound> BuildRowsByUid(IReadOnlyList<TableEditorDocumentRow> rows)
        {
            Dictionary<int, StruckTableSound> result = new Dictionary<int, StruckTableSound>();
            if (rows == null || rows.Count == 0)
                return result;

            for (int i = 0; i < rows.Count; i++)
            {
                if (!TryCreateSoundRow(rows[i], out StruckTableSound row))
                    continue;

                result[row.Uid] = row;
            }

            return result;
        }

        /// <summary>
        /// 저장 직전 디스크 기준 sound 테이블을 읽어 UID 기준 사운드 딕셔너리를 구성합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        /// <returns>디스크 기준 UID 사운드 딕셔너리입니다.</returns>
        private static Dictionary<int, StruckTableSound> LoadPersistedRowsByUid(TableEditorSaveContext context)
        {
            Dictionary<int, StruckTableSound> empty = new Dictionary<int, StruckTableSound>();
            string assetPath = context?.TableDefinition?.AssetPath;
            if (string.IsNullOrWhiteSpace(assetPath))
                return empty;

            try
            {
                TableEditorDocument persisted = TableEditorDocument.Load(assetPath);
                if (persisted == null)
                    return empty;

                List<TableEditorDocumentRow> rows = new List<TableEditorDocumentRow>();
                foreach (TableEditorDocumentRow row in persisted.GetRows())
                    rows.Add(row);

                return BuildRowsByUid(rows);
            }
            catch (FileNotFoundException)
            {
                return empty;
            }
        }

        /// <summary>
        /// 테이블 에디터의 단일 행을 사운드 행 스냅샷으로 변환합니다.
        /// UID가 유효하지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="row">변환 대상 TableEditor 행입니다.</param>
        /// <param name="soundRow">변환된 사운드 행입니다.</param>
        /// <returns>변환에 성공하면 true를 반환합니다.</returns>
        private static bool TryCreateSoundRow(TableEditorDocumentRow row, out StruckTableSound soundRow)
        {
            soundRow = null;
            if (row?.Values == null)
                return false;

            if (!int.TryParse(GetTrimmedValue(row, HeaderUid), out int uid) || uid <= 0)
                return false;

            soundRow = new StruckTableSound
            {
                Uid = uid,
                Type = ParseEnumOrDefault<SoundConstants.Type>(GetTrimmedValue(row, HeaderType)),
                SubType = ParseEnumOrDefault<SoundConstants.SubType>(GetTrimmedValue(row, HeaderSubType)),
                FileName = GetTrimmedValue(row, HeaderFileName),
                UseIntroScene = ParseBooleanOrDefault(GetTrimmedValue(row, HeaderUseIntroScene)),
            };

            return true;
        }

        /// <summary>
        /// FileName 문자열 변경 여부를 경로 정규화 기준으로 비교합니다.
        /// </summary>
        /// <param name="before">저장 전 행입니다.</param>
        /// <param name="after">저장 후 행입니다.</param>
        /// <returns>FileName이 변경되었으면 true를 반환합니다.</returns>
        private static bool IsFileNameChanged(StruckTableSound before, StruckTableSound after)
        {
            string beforeFileName = NormalizePath(before?.FileName);
            string afterFileName = NormalizePath(after?.FileName);
            return !string.Equals(beforeFileName, afterFileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// bool 문자열을 느슨하게 파싱합니다.
        /// true/false 외에도 1/0, y/n 형식을 허용합니다.
        /// </summary>
        /// <param name="rawValue">원본 문자열 값입니다.</param>
        /// <returns>파싱된 bool 값입니다.</returns>
        private static bool ParseBooleanOrDefault(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            string trimmed = rawValue.Trim();
            if (bool.TryParse(trimmed, out bool parsed))
                return parsed;

            if (int.TryParse(trimmed, out int numeric))
                return numeric != 0;

            return string.Equals(trimmed, "y", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(trimmed, "yes", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(trimmed, "on", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(trimmed, "t", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// FileName 값을 다양한 입력 형식(절대 Assets 경로, DataAddressable 상대 경로, 파일명만 입력 등)으로 해석해
        /// 실제 파일 존재 여부를 판단합니다.
        /// </summary>
        /// <param name="fileName">테이블의 FileName 원본 값입니다.</param>
        /// <param name="type">사운드 Type 값입니다.</param>
        /// <param name="subType">사운드 SubType 값입니다.</param>
        /// <param name="resolvedAssetPath">찾은 실제 경로(Assets 기준)입니다.</param>
        /// <returns>파일이 존재하면 true를 반환합니다.</returns>
        private static bool TryResolveExistingSoundAssetPath(
            string fileName,
            SoundConstants.Type type,
            SoundConstants.SubType subType,
            out string resolvedAssetPath)
        {
            resolvedAssetPath = string.Empty;

            string normalizedFileName = NormalizePath(fileName);
            if (string.IsNullOrWhiteSpace(normalizedFileName))
                return false;

            HashSet<string> candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Path.IsPathRooted(normalizedFileName) || normalizedFileName.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(normalizedFileName);
            }
            else
            {
                string baseSoundPath = ConfigAddressablePath.BuildSoundPath(type, subType);
                candidates.Add(ConfigAddressablePath.Combine(baseSoundPath, normalizedFileName));
                candidates.Add(ConfigAddressablePath.Combine(ConfigAddressablePath.Sounds, normalizedFileName));
                candidates.Add(ConfigAddressablePath.Combine(ConfigAddressablePath.Root, normalizedFileName));
            }

            foreach (string candidate in candidates)
            {
                if (!File.Exists(candidate))
                    continue;

                resolvedAssetPath = NormalizePath(candidate);
                return true;
            }

            return false;
        }
    }
}
