using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 아이콘 SpriteAtlas 로드/조회.
    /// </summary>
    /// <remarks>
    /// - Addressables 레이블로 Atlas들을 로드한 뒤,
    /// - <see cref="GetImageIconByName"/>에서 SpriteAtlas를 순회하여 Sprite를 반환한다.
    /// - 한번 찾은 Sprite는 캐싱하여 반복 검색 비용을 줄인다.
    /// </remarks>
    public class AddressableLoaderAffect : MonoBehaviour
    {
        public static AddressableLoaderAffect Instance { get; private set; }

        private readonly List<SpriteAtlas> _atlases = new();
        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.Ordinal);
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new();

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
        /// 모든 로드된 리소스를 해제한다.
        /// </summary>
        private void ReleaseAll()
        {
            AddressableLoaderController.ReleaseByHandles(_activeHandles);
        }

        public async Task LoadPrefabsAsync()
        {
            try
            {
                _atlases.Clear();
                _spriteCache.Clear();

                // 아이콘 Atlas 로케이션 조회
                var locationHandle = Addressables.LoadResourceLocationsAsync(ConfigAddressableLabel.ImageAffectIcon);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{ConfigAddressableLabel.ImageAffectIcon} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    var loadHandle = Addressables.LoadAssetAsync<SpriteAtlas>(address);

                    while (!loadHandle.IsDone)
                    {
                        _prefabLoadProgress = (loadedCount + loadHandle.PercentComplete) / Mathf.Max(1, totalCount);
                        await Task.Yield();
                    }

                    _activeHandles.Add(loadHandle);

                    var atlas = await loadHandle.Task;
                    if (atlas == null) continue;

                    _atlases.Add(atlas);
                    loadedCount++;
                }

                _activeHandles.Add(locationHandle);

                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"[AffectIcon] 로딩 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 아이콘 키(스프라이트 이름)로 Sprite를 가져온다.
        /// </summary>
        public Sprite GetImageIconByName(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;

            if (_spriteCache.TryGetValue(iconKey, out var cached) && cached != null)
                return cached;

            for (int i = 0; i < _atlases.Count; i++)
            {
                var atlas = _atlases[i];
                if (atlas == null) continue;

                var sprite = atlas.GetSprite(iconKey);
                if (sprite != null)
                {
                    _spriteCache[iconKey] = sprite;
                    return sprite;
                }
            }

            // 1회만 로그를 남기기 위해 null도 캐싱
            _spriteCache[iconKey] = null;
            GcLogger.LogError($"Addressables에서 {iconKey} 아이콘 이미지를 찾을 수 없습니다.");
            return null;
        }

        public float GetPrefabLoadProgress() => _prefabLoadProgress;
    }
}
