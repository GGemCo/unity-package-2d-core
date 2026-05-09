using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 썸네일 이미지를 Addressables에서 로드하고 캐싱합니다.
    /// </summary>
    public class AddressableLoaderCharacterThumbnail : MonoBehaviour
    {
        public static AddressableLoaderCharacterThumbnail Instance { get; private set; }

        private readonly Dictionary<string, Sprite> _dicThumbnail = new Dictionary<string, Sprite>();
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private float _prefabLoadProgress;

        private void Awake()
        {
            _prefabLoadProgress = 0f;
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }

        /// <summary>
        /// 모든 로드된 리소스를 해제합니다.
        /// </summary>
        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
        }

        /// <summary>
        /// 캐릭터 썸네일 전체를 선로드합니다.
        /// </summary>
        public async Task LoadAsync()
        {
            try
            {
                _dicThumbnail.Clear();
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.CharacterThumbnail);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.CharacterThumbnail} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = Mathf.Max(1, locationHandle.Result.Count);
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<Sprite>(address);

                    while (!loadHandle.IsDone)
                    {
                        _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / totalCount;
                        await Task.Yield();
                    }
                    _activeHandles.Add(loadHandle);

                    Sprite prefab = await loadHandle.Task;
                    if (!prefab) continue;
                    _dicThumbnail[address] = prefab;
                    loadedCount++;
                }

                Addressables.Release(locationHandle);
                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"프리팹 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 키에 해당하는 캐릭터 썸네일을 반환합니다.
        /// 선로드되지 않은 경우에는 최초 1회 지연 로드를 수행합니다.
        /// </summary>
        /// <param name="key">Addressables 키입니다.</param>
        /// <returns>조회된 스프라이트입니다. 없으면 null입니다.</returns>
        public Sprite GetCharacterThumbnailByName(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (_dicThumbnail.TryGetValue(key, out var sprite) && sprite != null)
            {
                return sprite;
            }

            return LoadThumbnailByKeySync(key);
        }

        /// <summary>
        /// 시작 로딩에서 제외된 썸네일을 최초 접근 시 동기적으로 로드합니다.
        /// </summary>
        /// <param name="key">Addressables 키입니다.</param>
        /// <returns>로드된 스프라이트입니다. 실패 시 null입니다.</returns>
        private Sprite LoadThumbnailByKeySync(string key)
        {
            AsyncOperationHandle<Sprite> loadHandle = Addressables.LoadAssetAsync<Sprite>(key);
            _activeHandles.Add(loadHandle);
            Sprite sprite = loadHandle.WaitForCompletion();

            if (loadHandle.Status == AsyncOperationStatus.Succeeded && sprite != null)
            {
                _dicThumbnail[key] = sprite;
                return sprite;
            }

            GcLogger.LogError($"Addressables에서 {key} 캐릭터 썸네일을 찾을 수 없습니다.");
            return null;
        }

        public float GetLoadProgress() => _prefabLoadProgress;
    }
}
