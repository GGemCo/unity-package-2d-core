using UnityEngine;

namespace GGemCo.Scripts
{
    public static class PlayerPrefsManager
    {
        public const string KeySaveDataSlotIndex = "GGEMCO_KEY_SAVE_DATA_SLOT_INDEX";

        private static void PlayerPrefsSave(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }
        private static int PlayerPrefsLoadInt(string key, string defaultValue = "0")
        {
            return int.Parse(PlayerPrefs.GetString(key, defaultValue));
        }
        private static float PlayerPrefsLoadFloat(string key, string defaultValue = "0")
        {
            return float.Parse(PlayerPrefs.GetString(key, defaultValue));
        }
        private static long PlayerPrefsLoadLong(string key, string defaultValue = "0")
        {
            return long.Parse(PlayerPrefs.GetString(key, defaultValue));
        }
        private static string PlayerPrefsLoad(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        
        public static void SaveSaveDataSlotIndex(int gameLoadSlotIndex)
        {
            PlayerPrefsSave(KeySaveDataSlotIndex, gameLoadSlotIndex.ToString());
        }
        public static int LoadSaveDataSlotIndex()
        {
            return PlayerPrefsLoadInt(KeySaveDataSlotIndex);
        }
    }
}