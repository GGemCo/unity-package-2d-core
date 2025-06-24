using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    public class GameLoaderManager : MonoBehaviour
    {
        private enum Type
        {
            None,
            Table,
            GamePrefab,
            SaveData,
            GamePrefabEffect,
            Item
        }

        public TextMeshProUGUI textLoadingPercent; // 진행률 표시
        private TableLoaderManager _tableLoader;
        private SaveDataLoader _saveDataLoader;
        private AddressableLoaderPrefabCommon _addressableLoaderPrefabCommon;
        private AddressableLoaderPrefabEffect _addressableLoaderPrefabEffect;
        private AddressableLoaderItem _addressableLoaderItem;

        private float _loadProgressTable;
        private float _loadProgressPrefabCommon;
        private float _loadProgressPrefabEffect;
        private float _loadProgressItem;
        private float _loadProgressSaveData;
        private float _progressTotal;
        private float _progressBase;

        private void Awake()
        {
            _loadProgressTable = 0f;
            _loadProgressPrefabCommon = 0f;
            _loadProgressPrefabEffect = 0f;
            _loadProgressItem = 0f;
            _loadProgressSaveData = 0f;
            _progressTotal = 0f;
            // 3 가지 경우를 로드 하고 있다
            _progressBase = 100f / 5f;

            if (textLoadingPercent)
            {
                textLoadingPercent.text = "0%";
            }
            
            GameObject gameObjectTableLoaderManager = new GameObject("TableLoaderManager");
            _tableLoader = gameObjectTableLoaderManager.AddComponent<TableLoaderManager>();
            
            GameObject gameObjectAddressablePrefabLoader = new GameObject("AddressableLoaderPrefabCommon");
            _addressableLoaderPrefabCommon = gameObjectAddressablePrefabLoader.AddComponent<AddressableLoaderPrefabCommon>();
            
            GameObject gameObjectAddressableLoaderPrefabEffect = new GameObject("AddressableLoaderPrefabEffect");
            _addressableLoaderPrefabEffect = gameObjectAddressableLoaderPrefabEffect.AddComponent<AddressableLoaderPrefabEffect>();
            
            GameObject gameObjectAddressableLoaderItem = new GameObject("AddressableLoaderItem");
            _addressableLoaderItem = gameObjectAddressableLoaderItem.AddComponent<AddressableLoaderItem>();
            
            GameObject gameObjectSaveDataLoader = new GameObject("SaveDataLoader");
            _saveDataLoader = gameObjectSaveDataLoader.AddComponent<SaveDataLoader>();
        }

        private void Start()
        {
            StartCoroutine(LoadGameData());
        }

        private IEnumerator LoadGameData()
        {
            yield return LoadTableData();
            yield return LoadAddressablePrefabCommon();
            yield return LoadAddressablePrefabEffect();
            yield return LoadAddressableItem();
            yield return LoadSaveData();
            UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNameGame);
        }

        /// <summary>
        /// 테이블 데이터를 로드하고 진행률을 업데이트합니다.
        /// </summary>
        private IEnumerator LoadTableData()
        {
            int i = 0;
            int fileCount = ConfigAddressableTable.All.Count;
            foreach (AddressableAssetInfo addressableAssetInfo in ConfigAddressableTable.All)
            {
                yield return _tableLoader.LoadDataFile(addressableAssetInfo);
                _loadProgressTable = (float)(i + 1) / fileCount * _progressBase;
                UpdateLoadingProgress(Type.Table);
                i++;
            }
        }
        /// <summary>
        /// Addressable 리소스를 로드하고 진행률을 업데이트합니다.
        /// </summary>
        private IEnumerator LoadAddressablePrefabCommon()
        {
            Task prefabLoadTask = _addressableLoaderPrefabCommon.LoadAllPreLoadGamePrefabsAsync();

            while (!prefabLoadTask.IsCompleted)
            {
                _loadProgressPrefabCommon = _addressableLoaderPrefabCommon.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(Type.GamePrefab);
                yield return null;
            }
        }

        private IEnumerator LoadAddressablePrefabEffect()
        {
            Task prefabLoadTask = _addressableLoaderPrefabEffect.LoadPrefabsAsync();

            while (!prefabLoadTask.IsCompleted)
            {
                _loadProgressPrefabEffect = _addressableLoaderPrefabEffect.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(Type.GamePrefabEffect);
                yield return null;
            }
        }
        private IEnumerator LoadAddressableItem()
        {
            Task prefabLoadTask = _addressableLoaderItem.LoadPrefabsAsync();

            while (!prefabLoadTask.IsCompleted)
            {
                _loadProgressItem = _addressableLoaderItem.GetPrefabLoadProgress() * _progressBase;
                UpdateLoadingProgress(Type.Item);
                yield return null;
            }
        }

        /// <summary>
        /// 세이브 데이터를 로드하고 진행률을 업데이트합니다.
        /// </summary>
        private IEnumerator LoadSaveData()
        {
            yield return _saveDataLoader.LoadData(progress =>
            {
                _loadProgressSaveData = progress * _progressBase; // 전체 로드의 33.3% 비중
                UpdateLoadingProgress(Type.SaveData);
            });
        }
        /// <summary>
        /// 진행률을 계산하고 UI 업데이트
        /// </summary>
        private void UpdateLoadingProgress(Type type)
        {
            _progressTotal = _loadProgressTable + _loadProgressPrefabCommon + _loadProgressPrefabEffect + _loadProgressItem + _loadProgressSaveData;
            string subTitle = "Tables";
            if (type == Type.GamePrefab)
            {
                subTitle = "Resources";
            }
            else if (type == Type.SaveData)
            {
                subTitle = "Save Data";
            }
            else if (type == Type.GamePrefabEffect)
            {
                subTitle = "Effect Resources";
            }
            else if (type == Type.Item)
            {
                subTitle = "Item Data";
            }
            if (textLoadingPercent != null)
            {
                textLoadingPercent.text = $"{subTitle} Loading... {Mathf.Floor(_progressTotal)}%";
            }
        }
    }
}
