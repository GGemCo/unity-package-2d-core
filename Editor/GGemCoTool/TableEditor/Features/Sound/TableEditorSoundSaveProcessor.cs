using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// sound 테이블 저장 전/후 후처리를 담당하는 SaveProcessor입니다.
    /// - 저장 전: FileName 실제 파일 존재 검증
    /// - 저장 후: Addressables 사운드 그룹 자동 동기화
    /// </summary>
    internal sealed class TableEditorSoundSaveProcessor : TableEditorSaveProcessorBase
    {
        private const string HeaderUid = "Uid";
        private const string HeaderType = "Type";
        private const string HeaderSubType = "SubType";
        private const string HeaderFileName = "FileName";

        /// <summary>
        /// 처리 대상 테이블 키입니다.
        /// </summary>
        protected override string TargetTableKey => ConfigAddressableTable.Sound;

        /// <summary>
        /// sound 검증은 저장 전에 실행되어야 하므로 낮은 우선순위를 사용합니다.
        /// </summary>
        public override int Order => 10;

        /// <summary>
        /// sound 테이블 저장 전에 FileName 컬럼의 실제 파일 존재 여부를 검증합니다.
        /// 존재하지 않는 파일이 있으면 예외를 발생시켜 저장을 중단합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public override void BeforeSave(TableEditorSaveContext context)
        {
            if (!ShouldRunChangedOnlyProcessing(context))
                return;

            ValidateSoundFileNames(context);
        }

        /// <summary>
        /// sound 테이블 저장 완료 후 Addressables 사운드 그룹을 자동으로 동기화합니다.
        /// </summary>
        /// <param name="context">현재 저장 컨텍스트입니다.</param>
        public override void AfterSave(TableEditorSaveContext context)
        {
            if (!ShouldRunChangedOnlyProcessing(context))
                return;

            // 테이블 저장 플로우에서 자동 호출되므로 확인/완료 다이얼로그는 비활성화합니다.
            SettingSound.SyncFromTable(new SettingSoundOptions
            {
                ShowConfirmDialog = false,
                ShowCompletedDialog = false,
                SaveAssets = true,
            });
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
    }
}
