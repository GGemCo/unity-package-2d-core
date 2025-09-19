using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    public class SlotMetaInfo
    {
        public int slotIndex;
        public int level;
        public string saveTime;
        public string filePath;
        public string thumbnailFilePath;
        public bool exists;
    }
    /// <summary>
    /// 슬롯 메타데이터 구조
    /// </summary>
    public class SaveMetaData
    {
        public readonly List<SlotMetaInfo> slots;

        public SaveMetaData(int maxSlots)
        {
            slots = new List<SlotMetaInfo>();
            for (int i = 1; i <= maxSlots; i++)
            {
                slots.Add(new SlotMetaInfo { slotIndex = i, level = 0, saveTime = "", filePath = "", thumbnailFilePath = "", exists = false });
            }
        }
    }
    /// <summary>
    /// 슬롯 및 메타데이터 관리 클래스
    /// </summary>
    public class SlotMetaDatController
    {
        private readonly string _baseDir;
        private readonly string _metaFilePath;
        private readonly object _ioLock = new();

        private SaveMetaData MetaData { get; set; }

        public SlotMetaDatController(string saveDirectory, int maxSlots)
        {
            var baseDir =
                // 절대 경로로 정규화
                Path.IsPathRooted(saveDirectory)
                ? saveDirectory
                : Path.Combine(Application.persistentDataPath, saveDirectory);
            
            _metaFilePath = Path.Combine(baseDir, "SaveMeta.json");

            Directory.CreateDirectory(baseDir);
            
            // 메타파일이 없으면 기본 데이터 생성
            if (!File.Exists(_metaFilePath))
            {
                MetaData = new SaveMetaData(maxSlots);
                SaveMetaToFile();
            }
            else
            {
                MetaData = LoadMetaData() ?? new SaveMetaData(maxSlots);
            }
        }
        /// <summary>
        /// 슬롯의 정보를 업데이트하고 저장
        /// </summary>
        /// <param name="slotIndex">슬롯 index</param>
        /// <param name="thumbnailFilePath">썸네일 파일 경로</param>
        /// <param name="exists">슬롯 정보가 존재하는지</param>
        /// <param name="level">레벨</param>
        /// <param name="filePath">슬롯 데이터 json 파일 경로</param>
        public void UpdateSlot(int slotIndex, string thumbnailFilePath, bool exists, int level, string filePath)
        {
            var slotInfo = MetaData.slots.Find(s => s.slotIndex == slotIndex);
            if (slotInfo == null)
            {
                GcLogger.LogError($"UpdateSlot: invalid slotIndex={slotIndex}");
                return;
            }

            slotInfo.saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            slotInfo.thumbnailFilePath = thumbnailFilePath ?? "";
            slotInfo.filePath = filePath ?? "";
            slotInfo.exists = exists;
            slotInfo.level = level;

            // 수정한 슬롯 기준으로 로그
            // GcLogger.Log($"[UpdateSlot] Slot={slotIndex}, Exists={slotInfo.exists}, File='{slotInfo.filePath}'");

            SaveMetaToFile();
        }

        /// <summary>
        /// 특정 슬롯 삭제 처리
        /// </summary>
        public void DeleteSlot(int slot)
        {
            UpdateSlot(slot, "", false, 0, "");
        }

        /// <summary>
        /// 메타데이터를 파일에 저장
        /// </summary>
        private void SaveMetaToFile()
        {
            lock (_ioLock)
            {
                var json = JsonConvert.SerializeObject(MetaData, Formatting.Indented);
                File.WriteAllText(_metaFilePath, json);
            }
        }

        /// <summary>
        /// 메타데이터를 파일에서 로드
        /// </summary>
        private SaveMetaData LoadMetaData()
        {
            try
            {
                lock (_ioLock)
                {
                    var json = File.ReadAllText(_metaFilePath);
                    return JsonConvert.DeserializeObject<SaveMetaData>(json);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogError($"[SlotMetaDatController] LoadMetaData failed: {e}");
                return null;
            }
        }
        
        // 필요 시 외부에서 최신 디스크 상태를 강제로 다시 읽기
        public void ReloadFromDisk()
        {
            var reloaded = LoadMetaData();
            if (reloaded != null) MetaData = reloaded;
        }
        /// <summary>
        /// 비어 있는 슬롯 index 가져오기 
        /// </summary>
        /// <returns></returns>
        public int GetEmptySlotIndex()
        {
            // 선택: 외부에서 메타파일을 변경했을 수 있으니 재로드
            // ReloadFromDisk();
            
            foreach (var s in MetaData.slots)
            {
                // 필요시 전체 슬롯 상태를 보고 싶다면 아래 라인을 유지
                // GcLogger.Log($"Slot {s.SlotIndex} Exists={s.Exists}");
                if (!s.exists)
                {
                    // GcLogger.Log($"Empty slot found: {s.slotIndex}");
                    return s.slotIndex;
                }
            }
            // GcLogger.Log("No empty slot.");
            return 0;
        }
        /// <summary>
        /// 저장되어있는 메타 데이터 리스트 가져오기
        /// </summary>
        /// <returns></returns>
        
        public List<SlotMetaInfo> GetMetaDataSlots() => MetaData.slots;
        /// <summary>
        /// json 파일 경로 가져오기
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public string GetFilePath(int slotIndex) => MetaData.slots.FirstOrDefault(s => s.slotIndex == slotIndex)?.filePath ?? "";
        /// <summary>
        /// 썸네일 이미지 경로 가져오기
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public string GetThumbnailFilePath(int slotIndex) => MetaData.slots.FirstOrDefault(s => s.slotIndex == slotIndex)?.thumbnailFilePath ?? "";
        /// <summary>
        /// 데이터가 있는 슬롯 개수 가져오기
        /// </summary>
        /// <returns></returns>
        public int GetExistSlotCounts() => MetaData.slots.Count(s => s.exists);
    }
}
