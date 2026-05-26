using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorCutsceneSaveProcessor : ITableEditorSaveProcessor
    {
        public int Order => 100;

        public bool CanProcess(TableEditorSaveContext context)
        {
            return context != null && context.IsTable(ConfigAddressableTable.Cutscene);
        }

        public void BeforeSave(TableEditorSaveContext context)
        {
        }

        public void AfterSave(TableEditorSaveContext context)
        {
            SettingCutscene.SyncFromTable(new SettingCutsceneOptions
            {
                ShowConfirmDialog = false,
                ShowCompletedDialog = false,
            });
        }
    }

    internal sealed class TableEditorSoundSaveProcessor : ITableEditorSaveProcessor
    {
        private const string HeaderUid = "Uid";
        private const string HeaderType = "Type";
        private const string HeaderSubType = "SubType";
        private const string HeaderFileName = "FileName";

        public int Order => 10;

        public bool CanProcess(TableEditorSaveContext context)
        {
            return context != null && context.IsTable(ConfigAddressableTable.Sound);
        }

        /// <summary>
        /// sound 테이블 저장 전에 FileName 컬럼의 실제 파일 존재 여부를 검증합니다.
        /// 존재하지 않는 파일이 있으면 예외를 발생시켜 저장을 중단합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public void BeforeSave(TableEditorSaveContext context)
        {
            ValidateSoundFileNames(context);
        }

        public void AfterSave(TableEditorSaveContext context)
        {
        }

        /// <summary>
        /// 편집 중인 sound 테이블 행을 순회하며 FileName으로부터 실제 에셋 경로를 해석합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        /// <exception cref="InvalidOperationException">파일이 없는 행이 하나라도 있으면 발생합니다.</exception>
        private static void ValidateSoundFileNames(TableEditorSaveContext context)
        {
            if (context?.Rows == null || context.Rows.Count == 0)
                return;

            List<string> missingEntries = new List<string>();

            for (int i = 0; i < context.Rows.Count; i++)
            {
                TableEditorDocumentRow row = context.Rows[i];
                if (row?.Values == null)
                    continue;

                string fileName = GetTrimmedValue(row, HeaderFileName);
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;

                SoundConstants.Type type = ParseEnumOrDefault<SoundConstants.Type>(GetTrimmedValue(row, HeaderType));
                SoundConstants.SubType subType = ParseEnumOrDefault<SoundConstants.SubType>(GetTrimmedValue(row, HeaderSubType));

                // FileName 입력 포맷이 프로젝트마다 다를 수 있어, 가능한 후보 경로를 순차적으로 검사합니다.
                if (TryResolveExistingSoundAssetPath(fileName, type, subType, out _))
                    continue;

                string uid = GetTrimmedValue(row, HeaderUid);
                string basePath = ConfigAddressablePath.BuildSoundPath(type, subType);
                missingEntries.Add($"- Uid={FormatUid(uid)}, FileName='{fileName}', BasePath='{basePath}'");
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

        /// <summary>
        /// 행 데이터에서 헤더 값을 안전하게 꺼내고 공백을 제거합니다.
        /// </summary>
        private static string GetTrimmedValue(TableEditorDocumentRow row, string headerName)
        {
            if (row == null || row.Values == null || string.IsNullOrWhiteSpace(headerName))
                return string.Empty;

            return row.Values.TryGetValue(headerName, out string value)
                ? (value ?? string.Empty).Trim()
                : string.Empty;
        }

        /// <summary>
        /// 문자열을 Enum으로 안전하게 변환합니다. 변환 실패 시 기본값(None/0)을 사용합니다.
        /// </summary>
        private static TEnum ParseEnumOrDefault<TEnum>(string rawValue) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return default;

            return Enum.TryParse(rawValue.Trim(), true, out TEnum parsed)
                ? parsed
                : default;
        }

        /// <summary>
        /// 경로를 슬래시 형태로 정규화하고 양끝 공백/따옴표를 제거합니다.
        /// </summary>
        private static string NormalizePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return string.Empty;

            string trimmed = rawPath.Trim().Trim('"');
            return ConfigAddressablePath.EnsureForwardSlashes(trimmed);
        }

        /// <summary>
        /// Uid 표시 문자열을 사용자 친화적으로 정규화합니다.
        /// </summary>
        private static string FormatUid(string uid)
        {
            return string.IsNullOrWhiteSpace(uid) ? "(빈 값)" : uid;
        }
    }
}
