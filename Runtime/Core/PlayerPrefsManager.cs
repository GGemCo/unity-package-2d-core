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
        /// 여러 PlayerPrefs 키를 한 번에 삭제합니다.
        /// </summary>
        /// <param name="keys">삭제할 키 목록입니다.</param>
        private static void DeleteKeys(IEnumerable<KeyIndex> keys)
        {
            foreach (KeyIndex key in keys)
            {
                string keyName = Keys.GetValueOrDefault(key);
                if (string.IsNullOrEmpty(keyName))
                {
                    continue;
                }

                PlayerPrefs.DeleteKey(keyName);
            }

            PlayerPrefs.Save();
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
        /// 저장 슬롯 선택값처럼 게임 진행 데이터와 직접 연결된 PlayerPrefs 값을 삭제합니다.
        /// </summary>
        public static void DeleteGameProgressData()
        {
            DeleteKeys(new[]
            {
                KeyIndex.KeyIndexSaveDataSlot,
            });
        }

        /// <summary>
        /// 앱 로컬 데이터 전체 초기화에 해당하는 PlayerPrefs 값을 모두 삭제합니다.
        /// </summary>
        public static void DeleteAllLocalData()
        {
            DeleteKeys(new[]
            {
                KeyIndex.KeyIndexSaveDataSlot,
                KeyIndex.KeyIndexLocalizationLocale,
                KeyIndex.KeySoundVolumeMaster,
                KeyIndex.KeySoundVolumeBGM,
                KeyIndex.KeySoundVolumeSfx,
                KeyIndex.KeyControlKeyBinding,
                KeyIndex.KeyToolPreviewAlwaysShow,
                KeyIndex.KeyToolPreviewHideWhenMoving,
            });
        }

        /// <summary>
        /// 선택한 언어 코드를 저장합니다.
        /// </summary>
        /// <param name="code">저장할 Locale 코드입니다.</param>
        public static void SaveLocalizationLocaleCode(string code)
        {
            PlayerPrefsSave(KeyIndex.KeyIndexLocalizationLocale, code);
        }

        /// <summary>
        /// 사용자가 선택한 언어 코드가 저장되어 있는지 확인합니다.
        /// </summary>
        /// <returns>언어 코드가 저장되어 있으면 <c>true</c>입니다.</returns>
        public static bool HasLocalizationLocaleCode()
        {
            string keyName = Keys.GetValueOrDefault(KeyIndex.KeyIndexLocalizationLocale);
            return !string.IsNullOrEmpty(keyName) && PlayerPrefs.HasKey(keyName);
        }

        /// <summary>
        /// 저장된 언어 코드를 조회합니다.
        /// 저장값이 없을 때 기본 언어를 대신 반환하지 않으므로 최초 실행 여부를 구분할 수 있습니다.
        /// </summary>
        /// <param name="code">저장된 Locale 코드입니다.</param>
        /// <returns>유효한 저장값을 조회했으면 <c>true</c>입니다.</returns>
        public static bool TryLoadLocalizationLocaleCode(out string code)
        {
            code = string.Empty;
            if (!HasLocalizationLocaleCode())
            {
                return false;
            }

            code = PlayerPrefsLoad(KeyIndex.KeyIndexLocalizationLocale);
            return !string.IsNullOrWhiteSpace(code);
        }

        /// <summary>
        /// 저장된 언어 코드를 삭제합니다.
        /// 다음 실행 시 시스템 언어를 다시 선택해야 할 때 사용합니다.
        /// </summary>
        public static void DeleteLocalizationLocaleCode()
        {
            PlayerPrefsDelete(KeyIndex.KeyIndexLocalizationLocale);
        }

        /// <summary>
        /// 저장된 언어 코드를 반환하고, 저장값이 없으면 프로젝트 기본 언어 코드를 반환합니다.
        /// </summary>
        /// <returns>저장된 Locale 코드 또는 프로젝트 기본 Locale 코드입니다.</returns>
        /// <remarks>
        /// 기존 호출부와의 하위 호환성을 위한 API입니다.
        /// 최초 실행 여부를 구분해야 하는 경우 <see cref="TryLoadLocalizationLocaleCode"/>를 사용합니다.
        /// </remarks>
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
