using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GGemCo.Scripts
{
    public class SlotMetaInfo
    {
        public int SlotIndex;
        public int Level;
        public string SaveTime;
        public string FilePath;
        public string ThumbnailFilePath;
        public bool Exists;
    }
    /// <summary>
    /// 슬롯 메타데이터 구조
    /// </summary>
    public class SaveMetaData
    {
        public readonly List<SlotMetaInfo> Slots;

        public SaveMetaData(int maxSlots)
        {
            Slots = new List<SlotMetaInfo>();
            for (int i = 1; i <= maxSlots; i++)
            {
                Slots.Add(new SlotMetaInfo { SlotIndex = i, Level = 0, SaveTime = "", FilePath = "", ThumbnailFilePath = "", Exists = false });
            }
        }
    }
    /// <summary>
    /// 슬롯 및 메타데이터 관리 클래스
    /// </summary>
    public class SlotMetaDatController
    {
        private readonly string metaFilePath;
        private SaveMetaData MetaData { get; set; }

        public SlotMetaDatController(string saveDirectory, int maxSlots)
        {
            metaFilePath = Path.Combine(saveDirectory, "SaveMeta.json");

            // 메타파일이 없으면 기본 데이터 생성
            if (!File.Exists(metaFilePath))
            {
                Directory.CreateDirectory(saveDirectory);
                MetaData = new SaveMetaData(maxSlots);
                SaveMetaToFile();
            }
            else
            {
                MetaData = LoadMetaData();
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
            string saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var slotInfo = MetaData.Slots.Find(s => s.SlotIndex == slotIndex);
            if (slotInfo != null)
            {
                slotInfo.SaveTime = saveTime;
                slotInfo.ThumbnailFilePath = thumbnailFilePath;
                slotInfo.FilePath = filePath;
                slotInfo.Exists = exists;
                slotInfo.Level = level;
                SaveMetaToFile();
            }
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
            string json = JsonConvert.SerializeObject(MetaData);
            File.WriteAllText(metaFilePath, json);
        }

        /// <summary>
        /// 메타데이터를 파일에서 로드
        /// </summary>
        private SaveMetaData LoadMetaData()
        {
            string json = File.ReadAllText(metaFilePath);
            return JsonConvert.DeserializeObject<SaveMetaData>(json);
        }
        /// <summary>
        /// 비어 있는 슬롯 index 가져오기 
        /// </summary>
        /// <returns></returns>
        public int GetEmptySlotIndex()
        {
            return (from slotMetaInfo in MetaData.Slots where slotMetaInfo.Exists == false select slotMetaInfo.SlotIndex).FirstOrDefault();
        }
        /// <summary>
        /// 저장되어있는 메타 데이터 리스트 가져오기
        /// </summary>
        /// <returns></returns>
        public List<SlotMetaInfo> GetMetaDataSlots()
        {
            return MetaData.Slots;
        }
        /// <summary>
        /// json 파일 경로 가져오기
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public string GetFilePath(int slotIndex)
        {
            return (from slotMetaInfo in MetaData.Slots where slotMetaInfo.SlotIndex == slotIndex select slotMetaInfo.FilePath).FirstOrDefault();
        }
        /// <summary>
        /// 썸네일 이미지 경로 가져오기
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public string GetThumbnailFilePath(int slotIndex)
        {
            return (from slotMetaInfo in MetaData.Slots where slotMetaInfo.SlotIndex == slotIndex select slotMetaInfo.ThumbnailFilePath).FirstOrDefault();
        }
        /// <summary>
        /// 데이터가 있는 슬롯 개수 가져오기
        /// </summary>
        /// <returns></returns>
        public int GetExistSlotCounts()
        {
            int count = 0;
            foreach (SlotMetaInfo slotMetaInfo in MetaData.Slots)
            {
                if (slotMetaInfo.Exists)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
