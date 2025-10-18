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
            KeySoundVolumeMaster,
            KeyControlKeyBinding,
            KeyToolPreviewAlwaysShow,
            KeyToolPreviewHideWhenMoving
        }

        public static readonly Dictionary<KeyIndex, string> Keys = new Dictionary<KeyIndex, string>()
        {
            { KeyIndex.None, "" },
            { KeyIndex.KeyIndexSaveDataSlot, "GGEMCO_KEY_SAVE_DATA_SLOT_INDEX" },
            { KeyIndex.KeyIndexLocalizationLocale, "GGEMCO_KEY_INDEX_LOCALIZATION_LOCALE" },
            { KeyIndex.KeySoundVolumeMaster, "GGEMCO_KEY_SOUND_VOLUME_MASTER" },
            { KeyIndex.KeySoundVolumeBGM, "GGEMCO_KEY_SOUND_VOLUME_BGM" },
            { KeyIndex.KeySoundVolumeSfx, "GGEMCO_KEY_SOUND_VOLUME_SFX" },
            { KeyIndex.KeyControlKeyBinding, "GGEMCO_KEY_CONTROL_KEY_BINDING" },
            { KeyIndex.KeyToolPreviewAlwaysShow, "GGEMCO_KEY_TOOL_PREVIEW_ALWAYS_SHOW" },
            { KeyIndex.KeyToolPreviewHideWhenMoving, "GGEMCO_KEY_TOOL_PREVIEW_HIDE_WHEN_MOVING" },
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
        private static bool PlayerPrefsLoadBool(KeyIndex key, bool defaultValue = false)
        {
            string keyName = Keys.GetValueOrDefault(key);
            return bool.Parse(PlayerPrefs.GetString(keyName, defaultValue.ToString()));
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
        /// <param name="code"></param>
        public static void SaveLocalizationLocaleCode(string code)
        {
            PlayerPrefsSave(KeyIndex.KeyIndexLocalizationLocale, code);
        }
        public static string LoadLocalizationLocaleCode()
        {
            var locale = PlayerPrefsLoad(KeyIndex.KeyIndexLocalizationLocale);
            if (string.IsNullOrEmpty(locale))
            {
                locale = LocalizationConstants.GetDefaultCode();
            }

            return locale;
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
            if (!AddressableLoaderSettings.Instance) return 1;
            return PlayerPrefsLoadFloat(KeyIndex.KeySoundVolumeBGM,
                $"{AddressableLoaderSettings.Instance.optionSettings.volumeBGM}");
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
            if (!AddressableLoaderSettings.Instance) return 1;
            return PlayerPrefsLoadFloat(KeyIndex.KeySoundVolumeSfx,
                $"{AddressableLoaderSettings.Instance.optionSettings.volumeSfx}");
        }
        /// <summary>
        /// 메인 볼륨
        /// </summary>
        /// <param name="value"></param>
        public static void SaveSoundVolumeMaster(float value)
        {
            PlayerPrefsSave(KeyIndex.KeySoundVolumeMaster, $"{value}");
        }
        public static float LoadSoundVolumeMaster()
        {
            if (!AddressableLoaderSettings.Instance) return 1;
            return PlayerPrefsLoadFloat(KeyIndex.KeySoundVolumeMaster,
                $"{AddressableLoaderSettings.Instance.optionSettings.volumeMaster}");
        }
        /// <summary>
        /// 컨트로 키 맵핑
        /// </summary>
        /// <param name="json"></param>
        public static void SaveKeyBinding(string json)
        {
            PlayerPrefsSave(KeyIndex.KeyControlKeyBinding, json);
        }
        public static string LoadKeyBinding()
        {
            return PlayerPrefsLoad(KeyIndex.KeyControlKeyBinding);
        }

        /// <summary>
        /// 시뮬레이션 툴 미리 보기 on/off
        /// </summary>
        /// <param name="isOn"></param>
        public static void SaveToolPreviewAlwaysShow(bool isOn)
        {
            PlayerPrefsSave(KeyIndex.KeyToolPreviewAlwaysShow, isOn.ToString());
        }
        public static bool LoadToolPreviewAlwaysShow()
        {
            return PlayerPrefsLoadBool(KeyIndex.KeyToolPreviewAlwaysShow, true);
        }
        /// <summary>
        /// 시뮬레이션 툴 미리보기, 캐릭터 이동중 보기 on/off
        /// </summary>
        /// <param name="isOn"></param>
        public static void SaveToolPreviewHideWhenMoving(bool isOn)
        {
            PlayerPrefsSave(KeyIndex.KeyToolPreviewHideWhenMoving, isOn.ToString());
        }
        public static bool LoadToolPreviewHideWhenMoving()
        {
            return PlayerPrefsLoadBool(KeyIndex.KeyToolPreviewHideWhenMoving, true);
        }
    }
}