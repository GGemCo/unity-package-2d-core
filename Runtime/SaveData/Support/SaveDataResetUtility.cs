using System;
using System.IO;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 및 로컬 설정 초기화를 담당하는 유틸리티입니다.
    /// </summary>
    public static class SaveDataResetUtility
    {
        /// <summary>
        /// 저장 설정을 기준으로 로컬 데이터를 초기화합니다.
        /// </summary>
        /// <param name="scope">초기화 범위입니다.</param>
        /// <returns>초기화 성공 여부입니다.</returns>
        public static bool ResetPersistentStorage(SaveDataResetScope scope)
        {
            if (!TryGetSaveDirectories(out string saveDirectory, out string thumbnailDirectory))
            {
                return false;
            }

            try
            {
                ClearRuntimeCaches();
                DeleteStorageDirectory(saveDirectory);
                DeleteStorageDirectory(thumbnailDirectory);

                Directory.CreateDirectory(saveDirectory);
                Directory.CreateDirectory(thumbnailDirectory);

                switch (scope)
                {
                    case SaveDataResetScope.AllLocalData:
                        PlayerPrefsManager.DeleteAllLocalData();
                        break;

                    case SaveDataResetScope.GameProgressOnly:
                    default:
                        PlayerPrefsManager.DeleteGameProgressData();
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"[SaveDataResetUtility] 로컬 데이터 초기화 중 오류가 발생했습니다. {ex}");
                return false;
            }
        }

        /// <summary>
        /// 저장 데이터 경로와 썸네일 경로를 가져옵니다.
        /// </summary>
        /// <param name="saveDirectory">저장 데이터 디렉터리입니다.</param>
        /// <param name="thumbnailDirectory">썸네일 디렉터리입니다.</param>
        /// <returns>경로 조회 성공 여부입니다.</returns>
        private static bool TryGetSaveDirectories(out string saveDirectory, out string thumbnailDirectory)
        {
            saveDirectory = string.Empty;
            thumbnailDirectory = string.Empty;

            if (!AddressableLoaderSettings.Instance || AddressableLoaderSettings.Instance.saveSettings == null)
            {
                GcLogger.LogError("[SaveDataResetUtility] saveSettings 를 찾을 수 없어 로컬 데이터를 초기화할 수 없습니다.");
                return false;
            }

            GGemCoSaveSettings saveSettings = AddressableLoaderSettings.Instance.saveSettings;
            saveDirectory = saveSettings.SaveDataFolderName;
            thumbnailDirectory = saveSettings.SaveDataThumnailFolderName;
            return true;
        }

        /// <summary>
        /// 삭제 전에 메모리에 남아 있는 저장 관련 캐시를 정리합니다.
        /// </summary>
        private static void ClearRuntimeCaches()
        {
            SaveDataLoader.Instance?.ClearLoadedData();
            SaveRegistry.ClearPendingRestore();
        }

        /// <summary>
        /// 지정한 디렉터리를 안전하게 삭제합니다.
        /// </summary>
        /// <param name="directoryPath">삭제할 디렉터리 경로입니다.</param>
        private static void DeleteStorageDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            Directory.Delete(directoryPath, true);
        }
    }
}