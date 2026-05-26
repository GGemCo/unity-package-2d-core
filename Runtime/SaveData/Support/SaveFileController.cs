using System;
using System.IO;
using Newtonsoft.Json;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 파일 관리
    /// </summary>
    public class SaveFileController
    {
        private readonly string _saveDirectory;
        private readonly int _maxSaveSlotCount;

        public SaveFileController(string saveDirectory, int maxSaveSlotCount)
        {
            _saveDirectory = saveDirectory;
            _maxSaveSlotCount = maxSaveSlotCount;
            Directory.CreateDirectory(saveDirectory);
        }

        /// <summary>
        /// 저장 데이터를 임시 파일에 먼저 기록한 뒤 검증하고, 기존 정상 파일을 백업한 후 기본 파일로 교체합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <param name="saveData">저장할 데이터 컨테이너입니다.</param>
        public void SaveData(int slot, SaveDataContainer saveData)
        {
            if (!IsValidSlot(slot)) return;

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            WriteSaveJsonWithBackup(slot, json, SaveDataIdentity.Core(slot));
            GcLogger.Log($"데이터가 저장되었습니다. 슬롯 {slot}");
        }

        /// <summary>
        /// 파일에서 읽어오기
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public SaveDataContainer LoadData(int slot)
        {
            if (!IsValidSlot(slot)) return null;

            string filePath = GetSaveFilePath(slot);
            if (!File.Exists(filePath))
            {
                GcLogger.LogError($"저장된 데이터가 없습니다. 슬롯 {slot}");
                return null;
            }

            string json = SaveDataFileService.ReadAllText(filePath, SaveDataIdentity.Core(slot));
            GcLogger.Log($"데이터가 불러와졌습니다. 슬롯 {slot}");
            return JsonConvert.DeserializeObject<SaveDataContainer>(json);
        }

        /// <summary>
        /// 파일 삭제하기
        /// </summary>
        /// <param name="slot"></param>
        public void DeleteData(int slot)
        {
            if (!IsValidSlot(slot)) return;

            DeleteIfExists(GetSaveFilePath(slot));
            DeleteIfExists(GetBackupFilePath(slot));
            DeleteIfExists(GetTempFilePath(slot));
            GcLogger.Log($"데이터 삭제 완료: 슬롯 {slot}");
        }

        /// <summary>
        /// 저장 JSON을 임시 파일에 기록하고 검증한 뒤 기본 저장 파일로 교체합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <param name="json">저장할 평문 JSON입니다.</param>
        /// <param name="identity">암호화 AAD에 사용할 논리 저장 식별자입니다.</param>
        public void WriteSaveJsonWithBackup(int slot, string json, SaveDataIdentity identity)
        {
            if (!IsValidSlot(slot)) return;

            string primaryPath = GetSaveFilePath(slot);
            string backupPath = GetBackupFilePath(slot);
            string tempPath = GetTempFilePath(slot);
            SaveDataIdentity resolvedIdentity = identity ?? SaveDataIdentity.Core(slot);

            DeleteIfExists(tempPath);
            SaveDataFileService.WriteAllText(tempPath, json ?? string.Empty, resolvedIdentity);
            ValidateSaveJsonFile(tempPath, resolvedIdentity);

            if (File.Exists(primaryPath))
            {
                BackupPrimaryFile(slot, primaryPath, backupPath, resolvedIdentity);
            }

            ReplacePrimaryFile(tempPath, primaryPath);
        }

        /// <summary>
        /// 기본 저장 파일을 읽어 검증한 뒤 백업 파일로 다시 저장합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <param name="primaryPath">기본 저장 파일 경로입니다.</param>
        /// <param name="backupPath">백업 저장 파일 경로입니다.</param>
        /// <param name="identity">암호화 AAD에 사용할 논리 저장 식별자입니다.</param>
        private void BackupPrimaryFile(int slot, string primaryPath, string backupPath, SaveDataIdentity identity)
        {
            try
            {
                string previousJson = SaveDataFileService.ReadAllText(primaryPath, identity);
                JsonConvert.DeserializeObject<SaveDataContainer>(previousJson);
                SaveDataFileService.WriteAllText(backupPath, previousJson, identity);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"[SaveFileController] 기존 저장 파일을 백업할 수 없어 invalid 폴더로 이동합니다. {ex.Message}");
                MoveToInvalid(primaryPath, slot, "save_backup_failed");
            }
        }

        /// <summary>
        /// 저장 파일이 복호화와 역직렬화가 가능한지 검증합니다.
        /// </summary>
        /// <param name="filePath">검증할 저장 파일 경로입니다.</param>
        /// <param name="identity">암호화 AAD에 사용할 논리 저장 식별자입니다.</param>
        private static void ValidateSaveJsonFile(string filePath, SaveDataIdentity identity)
        {
            string verifyJson = SaveDataFileService.ReadAllText(filePath, identity);
            JsonConvert.DeserializeObject<SaveDataContainer>(verifyJson);
        }

        /// <summary>
        /// 검증된 임시 저장 파일을 기본 저장 파일로 교체합니다.
        /// </summary>
        /// <param name="tempPath">임시 저장 파일 경로입니다.</param>
        /// <param name="primaryPath">기본 저장 파일 경로입니다.</param>
        private static void ReplacePrimaryFile(string tempPath, string primaryPath)
        {
            string directoryPath = Path.GetDirectoryName(primaryPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            DeleteIfExists(primaryPath);
            File.Move(tempPath, primaryPath);
        }

        /// <summary>
        /// 손상되었거나 검증에 실패한 저장 파일을 invalid 폴더로 이동합니다.
        /// </summary>
        /// <param name="sourceFilePath">이동할 저장 파일 경로입니다.</param>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <param name="reason">invalid 처리 사유입니다.</param>
        /// <returns>이동된 invalid 파일 경로입니다. 이동하지 못하면 null입니다.</returns>
        public string MoveToInvalid(string sourceFilePath, int slot, string reason)
        {
            if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return null;
            }

            try
            {
                string invalidDirectoryPath = GetInvalidDirectoryPath(slot);
                Directory.CreateDirectory(invalidDirectoryPath);

                string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                string extension = Path.GetExtension(sourceFilePath);
                string safeReason = NormalizeInvalidReason(reason);
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                string destinationPath = Path.Combine(invalidDirectoryPath, $"{fileName}.{safeReason}.{timestamp}{extension}");

                File.Move(sourceFilePath, destinationPath);
                return destinationPath;
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"[SaveFileController] invalid 저장 파일 이동 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 저장 슬롯 폴더 경로를 반환합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <returns>저장 슬롯 폴더 경로입니다.</returns>
        public string GetSlotDirectoryPath(int slot)
        {
            var saveDirectory = Path.Combine(_saveDirectory, $"{slot}");
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            return saveDirectory;
        }

        /// <summary>
        /// 기본 저장 파일 경로를 반환합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <param name="fileName">확장자를 제외한 파일 이름입니다.</param>
        /// <returns>저장 파일 경로입니다.</returns>
        public string GetSaveFilePath(int slot, string fileName = "")
        {
            var saveDirectory = GetSlotDirectoryPath(slot);
            var newFileName = SaveDataConstants.DefaultFileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                newFileName = $"{fileName}{SaveDataConstants.SaveDataFileExt}";
            }

            return Path.Combine(saveDirectory, newFileName);
        }

        /// <summary>
        /// 백업 저장 파일 경로를 반환합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <returns>백업 저장 파일 경로입니다.</returns>
        public string GetBackupFilePath(int slot)
        {
            return GetSaveFilePath(slot, SaveDataConstants.BackupFileNameWithoutExtension);
        }

        /// <summary>
        /// 임시 저장 파일 경로를 반환합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <returns>임시 저장 파일 경로입니다.</returns>
        public string GetTempFilePath(int slot)
        {
            return GetSaveFilePath(slot, SaveDataConstants.TempFileNameWithoutExtension);
        }

        /// <summary>
        /// invalid 저장 파일 보관 폴더 경로를 반환합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <returns>invalid 저장 파일 보관 폴더 경로입니다.</returns>
        public string GetInvalidDirectoryPath(int slot)
        {
            return Path.Combine(GetSlotDirectoryPath(slot), SaveDataConstants.InvalidDirectoryName);
        }

        /// <summary>
        /// 저장 슬롯 번호가 유효한지 확인합니다.
        /// </summary>
        /// <param name="slot">저장 슬롯 번호입니다.</param>
        /// <returns>유효한 슬롯이면 true입니다.</returns>
        public bool IsValidSlot(int slot) => slot >= 1 && slot <= _maxSaveSlotCount;

        /// <summary>
        /// 파일이 존재하면 삭제합니다.
        /// </summary>
        /// <param name="filePath">삭제할 파일 경로입니다.</param>
        private static void DeleteIfExists(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// invalid 파일명에 사용할 사유 문자열을 안전하게 정규화합니다.
        /// </summary>
        /// <param name="reason">invalid 처리 사유입니다.</param>
        /// <returns>파일명에 사용할 수 있는 사유 문자열입니다.</returns>
        private static string NormalizeInvalidReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "invalid";
            }

            string normalized = reason.Trim().ToLowerInvariant();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                normalized = normalized.Replace(invalidChar, '_');
            }

            return normalized.Replace(' ', '_');
        }
    }
}
