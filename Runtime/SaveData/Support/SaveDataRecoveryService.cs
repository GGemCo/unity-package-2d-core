using System;
using System.IO;
using Newtonsoft.Json;

namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 파일 로드 실패 시 기본 파일, 백업 파일, invalid 격리를 순서대로 처리하는 복구 서비스입니다.
    /// </summary>
    public static class SaveDataRecoveryService
    {
        /// <summary>
        /// 기본 저장 파일을 먼저 불러오고, 실패하면 백업 파일 복구를 시도합니다.
        /// </summary>
        /// <param name="saveFileController">저장 파일 경로와 invalid 격리를 담당하는 컨트롤러입니다.</param>
        /// <param name="slot">로드할 저장 슬롯 번호입니다.</param>
        /// <param name="identity">암호화 AAD에 사용할 논리 저장 식별자입니다.</param>
        /// <returns>로드 또는 복구 처리 결과입니다.</returns>
        public static SaveDataLoadResult LoadWithRecovery(
            SaveFileController saveFileController,
            int slot,
            SaveDataIdentity identity)
        {
            // 슬롯 유효성 검사는 하위 오버로드에서 수행하므로,
            // 여기서는 경로를 사전 계산하지 않고 기본 경로 사용(null)으로 위임합니다.
            return LoadWithRecovery(saveFileController, slot, identity, null, null);
        }

        /// <summary>
        /// 지정한 주 저장/백업 파일 경로를 기준으로 저장 데이터를 로드하고 복구를 시도합니다.
        /// 파생 로더가 Core 기본 파일명 외의 전용 파일명을 사용할 때 이 경로를 전달합니다.
        /// </summary>
        /// <param name="saveFileController">저장 파일 경로와 invalid 격리를 담당하는 컨트롤러입니다.</param>
        /// <param name="slot">로드할 저장 슬롯 번호입니다.</param>
        /// <param name="identity">암호화 AAD에 사용할 논리 저장 식별자입니다.</param>
        /// <param name="primaryPath">우선 로드할 주 저장 파일 경로입니다.</param>
        /// <param name="backupPath">주 저장 파일 실패 시 사용할 백업 파일 경로입니다.</param>
        /// <returns>로드 또는 복구 처리 결과입니다.</returns>
        public static SaveDataLoadResult LoadWithRecovery(
            SaveFileController saveFileController,
            int slot,
            SaveDataIdentity identity,
            string primaryPath,
            string backupPath)
        {
            if (saveFileController == null)
            {
                GcLogger.LogError("[SaveDataRecoveryService] 저장 파일 컨트롤러가 없습니다.");
                return new SaveDataLoadResult(
                    SaveDataLoadStatus.Failed,
                    userMessageKey: SaveDataMessageKeys.CannotLoadSaveData,
                    shouldShowUserMessage: true);
            }

            if (!saveFileController.IsValidSlot(slot))
            {
                GcLogger.LogError($"[SaveDataRecoveryService] 잘못된 슬롯 번호입니다. 슬롯 {slot}");
                return new SaveDataLoadResult(
                    SaveDataLoadStatus.Failed,
                    userMessageKey: SaveDataMessageKeys.CannotLoadSaveData,
                    shouldShowUserMessage: true);
            }

            SaveDataIdentity resolvedIdentity = identity ?? SaveDataIdentity.Core(slot);
            string resolvedPrimaryPath = string.IsNullOrWhiteSpace(primaryPath)
                ? saveFileController.GetSaveFilePath(slot)
                : primaryPath;
            string resolvedBackupPath = string.IsNullOrWhiteSpace(backupPath)
                ? saveFileController.GetBackupFilePath(slot)
                : backupPath;
            bool hadPrimaryFile = File.Exists(resolvedPrimaryPath);
            bool hadBackupFile = File.Exists(resolvedBackupPath);

            if (TryLoadSaveJson(resolvedPrimaryPath, resolvedIdentity, out string primaryJson, out Exception primaryException))
            {
                return new SaveDataLoadResult(SaveDataLoadStatus.LoadedPrimary, primaryJson);
            }

            if (File.Exists(resolvedPrimaryPath))
            {
                string primaryReason = ResolveInvalidReason(primaryException);
                GcLogger.LogWarning($"[SaveDataRecoveryService] 기본 저장 파일 로드 실패. 백업 복구를 시도합니다. 원인: {primaryException?.Message}");
                saveFileController.MoveToInvalid(resolvedPrimaryPath, slot, primaryReason);
            }

            if (TryLoadSaveJson(resolvedBackupPath, resolvedIdentity, out string backupJson, out Exception backupException))
            {
                RestoreBackupToPrimary(resolvedBackupPath, resolvedPrimaryPath);
                GcLogger.LogWarning($"[SaveDataRecoveryService] 백업 저장 파일로 복구했습니다. 슬롯 {slot}");
                return new SaveDataLoadResult(
                    SaveDataLoadStatus.RestoredFromBackup,
                    backupJson,
                    SaveDataMessageKeys.RestoredFromBackup,
                    true);
            }

            if (File.Exists(resolvedBackupPath))
            {
                string backupReason = ResolveInvalidReason(backupException);
                GcLogger.LogWarning($"[SaveDataRecoveryService] 백업 저장 파일 로드도 실패했습니다. 원인: {backupException?.Message}");
                saveFileController.MoveToInvalid(resolvedBackupPath, slot, backupReason);
            }

            if (!hadPrimaryFile && !hadBackupFile)
            {
                return new SaveDataLoadResult(SaveDataLoadStatus.NoSaveFile);
            }

            return new SaveDataLoadResult(
                SaveDataLoadStatus.NewDataRequired,
                userMessageKey: SaveDataMessageKeys.CannotLoadSaveData,
                shouldShowUserMessage: true);
        }

        /// <summary>
        /// 저장 파일을 읽고 복호화와 역직렬화가 가능한 JSON인지 검증합니다.
        /// </summary>
        /// <param name="filePath">로드할 저장 파일 경로입니다.</param>
        /// <param name="identity">암호화 AAD에 사용할 논리 저장 식별자입니다.</param>
        /// <param name="json">검증된 평문 JSON입니다.</param>
        /// <param name="exception">로드 실패 시 발생한 예외입니다.</param>
        /// <returns>저장 파일을 정상적으로 읽었으면 true입니다.</returns>
        private static bool TryLoadSaveJson(
            string filePath,
            SaveDataIdentity identity,
            out string json,
            out Exception exception)
        {
            json = null;
            exception = null;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string loadedJson = SaveDataFileService.ReadAllText(filePath, identity);
                SaveDataContainer container = JsonConvert.DeserializeObject<SaveDataContainer>(loadedJson);
                if (container == null)
                {
                    throw new JsonSerializationException("저장 데이터 컨테이너가 비어 있습니다.");
                }

                json = loadedJson;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary>
        /// 백업 파일을 기본 저장 파일 위치로 복사합니다.
        /// </summary>
        /// <param name="backupPath">백업 저장 파일 경로입니다.</param>
        /// <param name="primaryPath">기본 저장 파일 경로입니다.</param>
        private static void RestoreBackupToPrimary(string backupPath, string primaryPath)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(primaryPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.Copy(backupPath, primaryPath, true);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"[SaveDataRecoveryService] 백업 파일을 기본 저장 파일로 복사하지 못했습니다. 다음 저장 시 기본 파일이 다시 생성됩니다. {ex.Message}");
            }
        }

        /// <summary>
        /// 발생한 예외를 기준으로 invalid 파일명에 사용할 사유를 결정합니다.
        /// </summary>
        /// <param name="exception">로드 실패 원인 예외입니다.</param>
        /// <returns>invalid 파일명에 사용할 사유 문자열입니다.</returns>
        private static string ResolveInvalidReason(Exception exception)
        {
            if (exception == null)
            {
                return "missing";
            }

            if (IsAeadBadTagFailure(exception))
            {
                return "aead_bad_tag";
            }

            if (exception is SaveDataCryptoException)
            {
                return "crypto_failed";
            }

            if (exception is JsonException)
            {
                return "json_failed";
            }

            if (exception is IOException)
            {
                return "io_failed";
            }

            return "invalid";
        }

        /// <summary>
        /// 저장 데이터 복호화 실패 원인이 AEAD 인증 태그 검증 실패인지 확인합니다.
        /// </summary>
        /// <param name="exception">검사할 예외입니다.</param>
        /// <returns>AEAD 인증 태그 검증 실패이면 true입니다.</returns>
        private static bool IsAeadBadTagFailure(Exception exception)
        {
            Exception current = exception;
            while (current != null)
            {
                string text = current.ToString();
                if (text.Contains("AEADBadTagException")
                    || text.Contains("Tag mismatch")
                    || text.Contains("mac check in GCM failed"))
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }
    }
}
