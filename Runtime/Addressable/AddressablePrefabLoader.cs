using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Addressables 의 프리팹 로드 
    /// </summary>
    public class AddressablePrefabLoader : MonoBehaviour
    {
        public static AddressablePrefabLoader Instance { get; private set; }
        private readonly Dictionary<string, GameObject> preLoadGamePrefabs = new Dictionary<string, GameObject>();
        private float prefabLoadProgress;

        private void Awake()
        {
            prefabLoadProgress = 0f;
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
        /// 게임에서 사용할 프리팹을 Addressables에서 로드
        /// </summary>
        public async Task LoadAllPreLoadGamePrefabsAsync()
        {
            try
            {
                preLoadGamePrefabs.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressables.LabelPreLoadGamePrefabs);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressables.LabelPreLoadGamePrefabs} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<GameObject>(address);

                    while (!loadHandle.IsDone)
                    {
                        prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                        await Task.Yield();
                    }

                    GameObject prefab = await loadHandle.Task;
                    if (prefab != null)
                    {
                        preLoadGamePrefabs[address] = prefab;
                        loadedCount++;
                    }
                }

                prefabLoadProgress = 1f; // 100%
                // GcLogger.Log($"총 {loadedCount}/{totalCount}개의 프리팹을 성공적으로 로드했습니다.");
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"프리팹 로딩 중 오류 발생: {ex.Message}");
            }
        }

        public GameObject GetPreLoadGamePrefabByName(string prefabName)
        {
            if (preLoadGamePrefabs.TryGetValue(prefabName, out var prefab))
            {
                return prefab;
            }

            GcLogger.LogError($"Addressables에서 {prefabName} 프리팹을 찾을 수 없습니다.");
            return null;
        }

        public float GetPrefabLoadProgress() => prefabLoadProgress;
    }
}
