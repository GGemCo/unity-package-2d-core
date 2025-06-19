using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 세이브 데이터 json 파일 로드
    /// </summary>
    public class SaveDataLoader : MonoBehaviour
    {
        public static SaveDataLoader Instance { get; private set; }
        
        private SaveDataContainer saveDataContainer;
        private float loadProgress;
        
        protected void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        /// <summary>
        /// 바로 해제를 위해 추가
        /// </summary>
        private void OnDestroy()
        {
            saveDataContainer = null;
        }
        /// <summary>
        /// JSON 파일을 읽어오면서 진행률을 업데이트
        /// </summary>
        public IEnumerator LoadData(System.Action<float> onProgressUpdate)
        {
            int slotIndex = PlayerPrefsManager.LoadSaveDataSlotIndex();

            int maxSlotCount = AddressableSettingsLoader.Instance.saveSettings.saveDataMaxSlotCount;
            string saveDirectory = AddressableSettingsLoader.Instance.saveSettings.SaveDataFolderName;
            SaveFileController saveFileController = new SaveFileController(saveDirectory, maxSlotCount);
            string filePath = saveFileController.GetSaveFilePath(slotIndex);

            if (!File.Exists(filePath))
            {
                // GcLogger.LogError($"저장된 데이터가 없습니다. 슬롯 {slotIndex}");
                loadProgress = 1f;
                onProgressUpdate?.Invoke(loadProgress);
                yield break;
            }

            loadProgress = 0.2f; // JSON 읽기 시작
            onProgressUpdate?.Invoke(loadProgress);
            yield return null;

            string json = File.ReadAllText(filePath);
            loadProgress = 0.6f; // JSON 읽기 완료
            onProgressUpdate?.Invoke(loadProgress);
            yield return null;

            if (json != "")
            {
                saveDataContainer = JsonConvert.DeserializeObject<SaveDataContainer>(json);
            }

            loadProgress = 1f; // JSON 파싱 완료
            onProgressUpdate?.Invoke(loadProgress);
            // GcLogger.Log($"데이터가 불러와졌습니다. 슬롯 {slotIndex}");
        }

        public SaveDataContainer GetSaveDataContainer()
        {
            return saveDataContainer;
        }
    }
}