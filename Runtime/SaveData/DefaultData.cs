using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이콘 저장 구조
    /// </summary>
    public class SaveDataIcon
    {
        public int SlotIndex { get; private set; }
        public int Uid { get; private set; }
        public int Count { get; private set; }
        public int Level { get; private set; }
        public bool IsLearned { get; private set; }

        public SaveDataIcon(int slotIndex, int uid, int count = 0, int level = 0, bool isLearned = false)
        {
            SlotIndex = slotIndex;
            Uid = uid;
            Count = count;
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
    }

    /// <summary>
    /// 세이브 데이터 공용
    /// </summary>
    public abstract class DefaultData
    {
        private int maxSlotCount = 0;

        protected int MaxSlotCount
        {
            get
            {
                if (maxSlotCount <= 0)
                    maxSlotCount = GetMaxSlotCount();
                return maxSlotCount;
            }
        }
        protected abstract int GetMaxSlotCount();

        protected void SaveDatas()
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