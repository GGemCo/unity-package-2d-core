using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public static class PlayerPrefsManager
    {
        public enum KeyIndex
        {
            None,
            KeyIndexSaveDataSlot,
            KeyIndexLocalizationLocale,
            KeySoundVolumeBGM,
            KeySoundVolumeSfx,
        }

        public static readonly Dictionary<KeyIndex, string> Keys = new Dictionary<KeyIndex, string>()
        {
            { KeyIndex.None, "" },
            { KeyIndex.KeyIndexSaveDataSlot, "GGEMCO_KEY_SAVE_DATA_SLOT_INDEX" },
            { KeyIndex.KeyIndexLocalizationLocale, "GGEMCO_KEY_INDEX_LOCALIZATION_LOCALE" },
            { KeyIndex.KeySoundVolumeBGM, "GGEMCO_KEY_INDEX_SOUND_VOLUME_BGM" },
            { KeyIndex.KeySoundVolumeSfx, "GGEMCO_KEY_INDEX_SOUND_VOLUME_SFX" },
        };

        private static void PlayerPrefsDelete(KeyIndex key)
        {
            string keyName = Keys.GetValueOrDefault(key);
            PlayerPrefs.DeleteKey(keyName);
            PlayerPrefs.Save();
        }
        private static void PlayerPrefsSave(KeyIndex key, string value)
        {
            string keyName = Keys.GetValueOrDefault(key);
            PlayerPrefs.SetString(keyName, value);
            PlayerPrefs.Save();
        }
        private static int PlayerPrefsLoadInt(KeyIndex key, string defaultValue = "0")
        {
            string keyName = Keys.GetValueOrDefault(key);
            return int.Parse(PlayerPrefs.GetString(keyName, defaultValue));
        }
        private static float PlayerPrefsLoadFloat(KeyIndex key, string defaultValue = "0")
        {
            string keyName = Keys.GetValueOrDefault(key);
            return float.Parse(PlayerPrefs.GetString(keyName, defaultValue));
        }
        private static long PlayerPrefsLoadLong(KeyIndex key, string defaultValue = "0")
        {
            string keyName = Keys.GetValueOrDefault(key);
            return long.Parse(PlayerPrefs.GetString(keyName, defaultValue));
        }
        private static string PlayerPrefsLoad(KeyIndex key)
        {
            string keyName = Keys.GetValueOrDefault(key);
            return PlayerPrefs.GetString(keyName);
        }
        /// <summary>
        /// 게임 세이브 데이터 
        /// </summary>
        /// <param name="gameLoadSlotIndex"></param>
        public static void SaveSaveDataSlotIndex(int gameLoadSlotIndex)
        {
            PlayerPrefsSave(KeyIndex.KeyIndexSaveDataSlot, gameLoadSlotIndex.ToString());
        }
        public static int LoadSaveDataSlotIndex()
        {
            return PlayerPrefsLoadInt(KeyIndex.KeyIndexSaveDataSlot);
        }
        public static void DeleteSaveDataSlotIndex()
        {
            PlayerPrefsDelete(KeyIndex.KeyIndexSaveDataSlot);
        }
        /// <summary>
        /// 언어 선택 
        /// </summary>
        /// <param name="localeIndex"></param>
        public static void SaveIndexLocalizationLocale(int localeIndex)
        {
            PlayerPrefsSave(KeyIndex.KeyIndexLocalizationLocale, localeIndex.ToString());
        }
        public static int LoadIndexLocalizationLocale()
        {
            return PlayerPrefsLoadInt(KeyIndex.KeyIndexLocalizationLocale, ((int)LocalizationConstants.DefaultLanguageIndex).ToString());
        }
        /// <summary>
        /// BGM 볼륨  
        /// </summary>
        /// <param name="value"></param>
        public static void SaveSoundVolumeBGM(float value)
        {
            PlayerPrefsSave(KeyIndex.KeySoundVolumeBGM, $"{value}");
        }
        public static float LoadSoundVolumeBGM()
        {
            return PlayerPrefsLoadFloat(KeyIndex.KeySoundVolumeBGM, "0.5");
        }
        /// <summary>
        /// SFX 볼륨  
        /// </summary>
        /// <param name="value"></param>
        public static void SaveSoundVolumeSfx(float value)
        {
            PlayerPrefsSave(KeyIndex.KeySoundVolumeSfx, $"{value}");
        }
        public static float LoadSoundVolumeSfx()
        {
            return PlayerPrefsLoadFloat(KeyIndex.KeySoundVolumeSfx, "0.5");
        }
    }
}