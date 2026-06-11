using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// 사운드 로드
    /// </summary>
    public class AddressableLoaderSound : MonoBehaviour
    {
        public static AddressableLoaderSound Instance { get; private set; }
        private readonly Dictionary<string, AudioClip> _dicSound = new Dictionary<string, AudioClip>();
        private readonly HashSet<AsyncOperationHandle> _activeHandles = new HashSet<AsyncOperationHandle>();
        private readonly HashSet<string> _loadingClipKeys = new HashSet<string>();
        private const string WarmupGroupPrefix = "core.sound";
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
            _dicSound.Clear();
            _loadingClipKeys.Clear();
        }

        /// <summary>
        /// 사운드 라벨의 Addressables 종속성만 미리 다운로드합니다.
        /// </summary>
        /// <param name="key">다운로드할 사운드 라벨 또는 키입니다.</param>
        public async Task PrepareDependenciesAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _prefabLoadProgress = 1f;
                return;
            }

            _prefabLoadProgress = 0f;

            string groupId = $"{WarmupGroupPrefix}.{key}";
            AddressableDependencyWarmupService warmupService = AddressableDependencyWarmupService.GetOrCreate();
            Task<bool> task = warmupService.WarmupManyAsync(new List<string> { key }, groupId);

            while (!task.IsCompleted)
            {
                _prefabLoadProgress = warmupService.GetGroupProgress(groupId);
                await Task.Yield();
            }

            _prefabLoadProgress = 1f;
        }

        /// <summary>
        /// 지정한 라벨의 사운드 클립을 모두 실제 객체로 로드해 캐시에 저장합니다.
        /// </summary>
        /// <param name="key">로드할 사운드 라벨입니다.</param>
        public async Task LoadSoundAsync(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return;
                
                var locationHandle = Addressables.LoadResourceLocationsAsync(key);
                await locationHandle.Task;

                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{key} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (var location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    AudioClip prefab = await LoadAudioClipAsync(address);
                    if (!prefab)
                    {
                        loadedCount++;
                        continue;
                    }

                    loadedCount++;
                    _prefabLoadProgress = totalCount > 0 ? (float)loadedCount / totalCount : 1f;
                }
                _activeHandles.Add(locationHandle);

                _prefabLoadProgress = 1f; // 100%
                // GcLogger.Log($"총 {loadedCount}/{totalCount}개의 프리팹을 성공적으로 로드했습니다.");
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"사운드 로딩 중 오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 사운드 리소스 테이블에서 PreLoad가 활성화된 AudioClip을 실제 객체로 미리 로드합니다.
        /// </summary>
        /// <param name="tableLoaderManager">사운드 리소스 테이블을 보유한 테이블 로더입니다.</param>
        /// <param name="introOnly">true이면 Intro 씬에서 사용하는 사운드 리소스만 선로드합니다.</param>
        public async Task PreloadMarkedSoundsAsync(TableLoaderManager tableLoaderManager, bool introOnly = false)
        {
            List<string> keys = CollectPreloadSoundKeys(tableLoaderManager, introOnly);
            await PreloadAudioClipsAsync(keys);
        }

        /// <summary>
        /// 선로드 대상 Addressables 키 목록을 순서대로 실제 AudioClip로 로드합니다.
        /// </summary>
        /// <param name="keys">로드할 사운드 Addressables 키 목록입니다.</param>
        private async Task PreloadAudioClipsAsync(IReadOnlyList<string> keys)
        {
            _prefabLoadProgress = 0f;
            if (keys == null || keys.Count == 0)
            {
                _prefabLoadProgress = 1f;
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                try
                {
                    await LoadAudioClipAsync(key);
                }
                catch (Exception ex)
                {
                    GcLogger.LogWarning($"[AddressableLoaderSound] 선로드 중 예외가 발생했습니다. key={key}, error={ex.Message}");
                }

                _prefabLoadProgress = (float)(i + 1) / keys.Count;
            }

            _prefabLoadProgress = 1f;
        }

        /// <summary>
        /// sound_bgm/sound_ambient/sound_sfx 테이블에서 PreLoad가 활성화된 Addressables 키를 수집합니다.
        /// </summary>
        /// <param name="tableLoaderManager">사운드 리소스 테이블을 보유한 테이블 로더입니다.</param>
        /// <param name="introOnly">true이면 UseIntroScene도 활성화된 행만 수집합니다.</param>
        /// <returns>중복이 제거된 선로드 대상 Addressables 키 목록입니다.</returns>
        private static List<string> CollectPreloadSoundKeys(TableLoaderManager tableLoaderManager, bool introOnly)
        {
            List<string> result = new List<string>();
            HashSet<string> registeredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (tableLoaderManager == null)
                return result;

            AppendPreloadSoundKeys(result, registeredKeys, tableLoaderManager.TableSoundBgm?.GetDatas(), introOnly);
            AppendPreloadSoundKeys(result, registeredKeys, tableLoaderManager.TableSoundAmbient?.GetDatas(), introOnly);
            AppendPreloadSoundKeys(result, registeredKeys, tableLoaderManager.TableSoundSfx?.GetDatas(), introOnly);
            return result;
        }

        /// <summary>
        /// 지정한 사운드 리소스 테이블에서 선로드 대상 키를 결과 목록에 추가합니다.
        /// </summary>
        /// <typeparam name="TResource">사운드 리소스 행 타입입니다.</typeparam>
        /// <param name="target">수집 결과 목록입니다.</param>
        /// <param name="registeredKeys">중복 추가를 방지하기 위한 키 집합입니다.</param>
        /// <param name="rows">검사할 사운드 리소스 행 사전입니다.</param>
        /// <param name="introOnly">true이면 UseIntroScene도 활성화된 행만 추가합니다.</param>
        private static void AppendPreloadSoundKeys<TResource>(
            List<string> target,
            HashSet<string> registeredKeys,
            IReadOnlyDictionary<int, TResource> rows,
            bool introOnly)
            where TResource : StruckTableSoundResource
        {
            if (target == null || registeredKeys == null || rows == null)
                return;

            foreach (KeyValuePair<int, TResource> pair in rows)
            {
                TResource resource = pair.Value;
                if (resource == null || !resource.PreLoad)
                    continue;

                if (introOnly && !resource.UseIntroScene)
                    continue;

                string key = resource.BuildAddressKey();
                if (string.IsNullOrWhiteSpace(key) || !registeredKeys.Add(key))
                    continue;

                target.Add(key);
            }
        }

        /// <summary>
        /// 사운드 클립을 필요 시점에 비동기로 로드하고 캐시에 저장합니다.
        /// </summary>
        /// <param name="keyName">로드할 사운드 Addressables 키입니다.</param>
        /// <returns>로드된 AudioClip입니다. 실패 시 null을 반환합니다.</returns>
        public async Task<AudioClip> LoadAudioClipAsync(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                return null;

            if (_dicSound.TryGetValue(keyName, out AudioClip cached))
                return cached;

            if (_loadingClipKeys.Contains(keyName))
            {
                while (_loadingClipKeys.Contains(keyName))
                    await Task.Yield();

                return _dicSound.TryGetValue(keyName, out cached) ? cached : null;
            }

            _loadingClipKeys.Add(keyName);
            AsyncOperationHandle<AudioClip> loadHandle = Addressables.LoadAssetAsync<AudioClip>(keyName);
            _activeHandles.Add(loadHandle);

            try
            {
                AudioClip audioClip = await loadHandle.Task;
                if (loadHandle.Status == AsyncOperationStatus.Succeeded && audioClip != null)
                {
                    _dicSound[keyName] = audioClip;
                    return audioClip;
                }

                GcLogger.LogWarning($"[AddressableLoaderSound] 사운드 클립 로드에 실패했습니다. key={keyName}");
                return null;
            }
            finally
            {
                _loadingClipKeys.Remove(keyName);
            }
        }

        /// <summary>
        /// 캐시된 사운드 클립 조회를 시도합니다.
        /// </summary>
        /// <param name="keyName">조회할 사운드 Addressables 키입니다.</param>
        /// <param name="audioClip">캐시된 AudioClip입니다.</param>
        /// <returns>캐시에 존재하면 true를 반환합니다.</returns>
        public bool TryGetAudioClip(string keyName, out AudioClip audioClip)
        {
            return _dicSound.TryGetValue(keyName, out audioClip);
        }

        /// <summary>
        /// 캐시된 사운드 클립을 반환합니다.
        /// </summary>
        /// <param name="keyName">조회할 사운드 Addressables 키입니다.</param>
        /// <returns>캐시에 있으면 AudioClip, 없으면 null입니다.</returns>
        public AudioClip GetAudioClip(string keyName)
        {
            if (_dicSound.TryGetValue(keyName, out var audioClip))
            {
                return audioClip;
            }

            GcLogger.LogWarning($"Addressables에서 {keyName} 사운드 캐시를 찾을 수 없습니다. 필요 시 LoadAudioClipAsync를 사용하세요.");
            return null;
        }
        public float GetLoadProgress() => _prefabLoadProgress;
    }
}
