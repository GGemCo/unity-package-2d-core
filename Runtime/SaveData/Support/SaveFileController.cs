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
        /// 파일에 저장하기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="saveData"></param>
        public void SaveData(int slot, SaveDataContainer saveData)
        {
            if (!IsValidSlot(slot)) return;

            string filePath = GetSaveFilePath(slot);
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(filePath, json);
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

            string json = File.ReadAllText(filePath);
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

            string filePath = GetSaveFilePath(slot);
            if (File.Exists(filePath)) File.Delete(filePath);
            GcLogger.Log($"데이터 삭제 완료: 슬롯 {slot}");
        }

        private bool IsValidSlot(int slot) => slot >= 1 && slot <= _maxSaveSlotCount;

        public string GetSaveFilePath(int slot, string fileName = "")
        {
            var saveDirectory = Path.Combine(_saveDirectory, $"{slot}");
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            
            var newFileName = $"{SaveDataConstants.DefaultFileName}";
            if (!string.IsNullOrEmpty(fileName))
            {
                newFileName = $"{fileName}{SaveDataConstants.SaveDataFileExt}";
            }
            return Path.Combine(saveDirectory, newFileName);
        }
    }
}