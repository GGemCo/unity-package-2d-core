using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class GameLoaderManager : MonoBehaviour
    {
        private enum Type
        {
            None,
            Table,
            GamePrefab,
            SaveData
        }

        public TextMeshProUGUI textLoadingPercent; // 진행률 표시
        private TableLoaderManager tableLoader;
        private AddressablePrefabLoader prefabLoader;
        private SaveDataLoader saveDataLoader;

        private float tableLoadProgress;
        private float prefabLoadProgress;
        private float saveDataLoadProgress;
        private float totalProgress;
        private float baseProgress;

        private void Awake()
        {
            tableLoadProgress = 0f;
            prefabLoadProgress = 0f;
            saveDataLoadProgress = 0f;
            totalProgress = 0f;
            // 3 가지 경우를 로드 하고 있다
            baseProgress = 100f / 4f;

            if (textLoadingPercent != null)
            {
                textLoadingPercent.text = "0%";
            }
            
            GameObject gameObjectTableLoaderManager = new GameObject("TableLoaderManager");
            tableLoader = gameObjectTableLoaderManager.AddComponent<TableLoaderManager>();
            
            GameObject gameObjectAddressablePrefabLoader = new GameObject("AddressablePrefabLoader");
            prefabLoader = gameObjectAddressablePrefabLoader.AddComponent<AddressablePrefabLoader>();
            
            GameObject gameObjectSaveDataLoader = new GameObject("SaveDataLoader");
            saveDataLoader = gameObjectSaveDataLoader.AddComponent<SaveDataLoader>();
        }

        private void Start()
        {
            StartCoroutine(LoadGameData());
        }

        private IEnumerator LoadGameData()
        {
            yield return LoadTableData();
            yield return LoadAddressablePrefabs();
            yield return LoadSaveData();
            UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNameGame);
        }

        /// <summary>
        /// 테이블 데이터를 로드하고 진행률을 업데이트합니다.
        /// </summary>
        private IEnumerator LoadTableData()
        {
            string[] dataFiles = tableLoader.GetDataFiles();
            int fileCount = dataFiles.Length;

            for (int i = 0; i < fileCount; i++)
            {
                tableLoader.LoadDataFile(dataFiles[i]);
                tableLoadProgress = (float)(i + 1) / fileCount * baseProgress;
                UpdateLoadingProgress(Type.Table);
                yield return null;
            }
        }
        /// <summary>
        /// Addressable 리소스를 로드하고 진행률을 업데이트합니다.
        /// </summary>
        private IEnumerator LoadAddressablePrefabs()
        {
            Task prefabLoadTask = prefabLoader.LoadAllPreLoadGamePrefabsAsync();

            while (!prefabLoadTask.IsCompleted)
            {
                prefabLoadProgress = prefabLoader.GetPrefabLoadProgress() * baseProgress;
                UpdateLoadingProgress(Type.GamePrefab);
                yield return null;
            }
        }
        /// <summary>
        /// 세이브 데이터를 로드하고 진행률을 업데이트합니다.
        /// </summary>
        private IEnumerator LoadSaveData()
        {
            yield return saveDataLoader.LoadData(progress =>
            {
                saveDataLoadProgress = progress * baseProgress; // 전체 로드의 33.3% 비중
                UpdateLoadingProgress(Type.SaveData);
            });
        }
        /// <summary>
        /// 진행률을 계산하고 UI 업데이트
        /// </summary>
        private void UpdateLoadingProgress(Type type)
        {
            totalProgress = tableLoadProgress + prefabLoadProgress + saveDataLoadProgress;
            string subTitle = "테이블";
            if (type == Type.GamePrefab)
            {
                subTitle = "리소스";
            }
            else if (type == Type.SaveData)
            {
                subTitle = "세이브 데이터";
            }
            if (textLoadingPercent != null)
            {
                textLoadingPercent.text = $"{subTitle} 로드 중... {Mathf.Floor(totalProgress)}%";
            }
        }
    }
}
