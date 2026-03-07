using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 저장 구조
    /// </summary>
    public class SaveDataIcon
    {
        public int SlotIndex { get; private set; }
        public int Uid { get; private set; }
        public int Count { get; private set; }
        /// <summary>
        /// 아이템 인스턴스 ID.
        /// - 0: 정의(ItemUid) 기반(기존 방식)
        /// - >0: 인스턴스 기반(랜덤 옵션 등)
        /// </summary>
        public long InstanceId { get; private set; }
        /// <summary>
        /// 저장된 컨텐츠 종류(퀵슬롯 전용).
        /// - 0: 미사용/호환(기존)
        /// - 1: Skill
        /// - 2: Item
        /// </summary>
        public int IconType { get; private set; }
        public int Level { get; private set; }
        public bool IsLearned { get; private set; }

        public SaveDataIcon(int slotIndex, int uid, int count = 0, int level = 0, bool isLearned = false, long instanceId = 0, int iconType = 0)
        {
            SlotIndex = slotIndex;
            Uid = uid;
            Count = count;
            InstanceId = instanceId;
            IconType = iconType;
            Level = level;
            IsLearned = isLearned;
        }

        public void SetLevel(int level)
        {
            Level = level;
        }
        public void SetUid(int uid)
        {
            Uid = uid;
        }

        public void SetInstanceId(long instanceId)
        {
            InstanceId = instanceId;
        }
    }

    /// <summary>
    /// 세이브 데이터 공용
    /// </summary>
    public abstract class DefaultData
    {
        private int _maxSlotCount;

        protected int MaxSlotCount
        {
            get
            {
                if (_maxSlotCount <= 0)
                    _maxSlotCount = GetMaxSlotCount();
                return _maxSlotCount;
            }
        }

        protected abstract int GetMaxSlotCount();

        protected virtual void SaveDatas()
        {
            SceneGame.Instance.saveDataManager.StartSaveData();
        }
        protected static void PlayerPrefsSave(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
        protected int PlayerPrefsLoadInt(string key, string defaultValue = "0")
        {
            return int.Parse(PlayerPrefs.GetString(key, defaultValue));
        }
        protected float PlayerPrefsLoadFloat(string key, string defaultValue = "0")
        {
            return float.Parse(PlayerPrefs.GetString(key, defaultValue));
        }
        protected long PlayerPrefsLoadLong(string key, string defaultValue = "0")
        {
            return long.Parse(PlayerPrefs.GetString(key, defaultValue));
        }
        protected string PlayerPrefsLoad(string key)
        {
            return PlayerPrefs.GetString(key);
        }
    }
}