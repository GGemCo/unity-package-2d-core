using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 json 파일 로드
    /// </summary>
    public class SaveDataLoaderBase : MonoBehaviour
    {
        private int _maxSlotCount;
        private string _saveDirectory;
        protected SaveFileController saveFileController;
        
        private float _loadProgress;
        protected virtual void Awake()
        {
            if (!AddressableLoaderSettings.Instance) return;
            _maxSlotCount = AddressableLoaderSettings.Instance.saveSettings.saveDataMaxSlotCount;
            _saveDirectory = AddressableLoaderSettings.Instance.saveSettings.SaveDataFolderName;
            saveFileController = new SaveFileController(_saveDirectory, _maxSlotCount);
        }

        protected virtual string GetSaveFilePath(int slotIndex)
        {
            return saveFileController.GetSaveFilePath(slotIndex);
        }

        /// <summary>
        /// 현재 로더가 읽는 저장 데이터의 논리 식별자를 반환합니다.
        /// </summary>
        /// <param name="slotIndex">로드할 저장 슬롯 번호입니다.</param>
        /// <returns>암호화 AAD 구성에 사용할 논리 저장 식별자입니다.</returns>
        protected virtual SaveDataIdentity GetSaveDataIdentity(int slotIndex)
        {
            return SaveDataIdentity.Core(slotIndex);
        }

        /// <summary>
        /// JSON 파일을 읽어오면서 진행률을 업데이트
        /// </summary>
        public IEnumerator LoadData(Action<float> onProgressUpdate)
        {
            int slotIndex = PlayerPrefsManager.LoadSaveDataSlotIndex();

            string filePath = GetSaveFilePath(slotIndex);

            if (!File.Exists(filePath))
            {
                // GcLogger.LogError($"저장된 데이터가 없습니다. 슬롯 {slotIndex}");
                _loadProgress = 1f;
                onProgressUpdate?.Invoke(_loadProgress);
                yield break;
            }

            _loadProgress = 0.2f; // JSON 읽기 시작
            onProgressUpdate?.Invoke(_loadProgress);
            yield return null;

            string json = SaveDataFileService.ReadAllText(filePath, GetSaveDataIdentity(slotIndex));
            _loadProgress = 0.6f; // JSON 읽기 완료
            onProgressUpdate?.Invoke(_loadProgress);
            yield return null;

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    OnLoaded(json);
                }
                catch (Exception ex)
                {
                    GcLogger.LogError($"[SaveDataLoader] Failed to deserialize {ex}");
                }
            }

            _loadProgress = 1f; // JSON 파싱 완료
            onProgressUpdate?.Invoke(_loadProgress);
            // GcLogger.Log($"데이터가 불러와졌습니다. 슬롯 {slotIndex}");
        }

        protected virtual void OnLoaded(string json) 
        {
        }
    }
}
