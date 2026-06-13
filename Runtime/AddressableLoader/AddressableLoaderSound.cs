using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.Profiling;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables 기반 AudioClip 로드와 참조 수명을 관리합니다.
    /// </summary>
    public class AddressableLoaderSound : MonoBehaviour
    {
        private sealed class SoundClipEntry
        {
            public string AddressKey;
            public AudioClip Clip;
            public AsyncOperationHandle<AudioClip> Handle;
            public TaskCompletionSource<AudioClip> LoadingSource;
            public int ScopeReferenceCount;
            public int PlaybackReferenceCount;
            public bool IsLegacyPinned;
        }

        public static AddressableLoaderSound Instance { get; private set; }

        private readonly Dictionary<string, SoundClipEntry> _entries =
            new Dictionary<string, SoundClipEntry>(StringComparer.OrdinalIgnoreCase);

        private const string WarmupGroupPrefix = "core.sound";
        private const int DefaultPreloadConcurrentRequestCount = 3;
        private const string GlobalUiCommonScopeId = "UICommon";

        private SoundScopeManager _scopeManager;
        private float _prefabLoadProgress;
        private bool _isDestroying;
        private SoundScopeLease _globalUiCommonScopeLease;
        private int _globalUiCommonRequestVersion;

        /// <summary>
        /// 맵, UI 윈도우 등 사용 범위별 사운드 참조를 관리하는 매니저입니다.
        /// </summary>
        public SoundScopeManager ScopeManager => _scopeManager ??= new SoundScopeManager(this);

        /// <summary>
        /// 사운드 로더 싱글톤과 범위 매니저를 초기화합니다.
        /// </summary>
        private void Awake()
        {
            _prefabLoadProgress = 0f;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _scopeManager = new SoundScopeManager(this);
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 모든 사운드 범위와 Addressables 핸들을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            _isDestroying = true;
            ReleaseAll();

            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 범위 참조와 로드된 모든 AudioClip 핸들을 강제로 해제합니다.
        /// </summary>
        private void ReleaseAll()
        {
            _globalUiCommonRequestVersion++;
            _globalUiCommonScopeLease?.Dispose();
            _globalUiCommonScopeLease = null;

            _scopeManager?.Dispose();
            _scopeManager = null;

            List<SoundClipEntry> entries = new List<SoundClipEntry>(_entries.Values);
            _entries.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                SoundClipEntry entry = entries[i];
                TaskCompletionSource<AudioClip> loadingSource = entry.LoadingSource;
                entry.LoadingSource = null;
                loadingSource?.TrySetResult(null);
                ReleaseEntryHandle(entry);
            }
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
        /// 지정한 라벨의 사운드 클립을 모두 실제 객체로 로드해 전역 고정 캐시에 저장합니다.
        /// 기존 호출부 호환을 위해 이 API로 로드한 클립은 로더가 파괴될 때까지 유지됩니다.
        /// </summary>
        /// <param name="key">로드할 사운드 라벨입니다.</param>
        public async Task LoadSoundAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> locationHandle = default;

            try
            {
                locationHandle = Addressables.LoadResourceLocationsAsync(key);
                await locationHandle.Task;
                if (!locationHandle.IsValid() || locationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    GcLogger.LogError($"{key} 레이블을 가진 리소스를 찾을 수 없습니다.");
                    return;
                }

                int totalCount = locationHandle.Result.Count;
                int loadedCount = 0;

                foreach (UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation location in locationHandle.Result)
                {
                    string address = location.PrimaryKey;
                    await LoadAudioClipAsync(address);
                    loadedCount++;
                    _prefabLoadProgress = totalCount > 0 ? (float)loadedCount / totalCount : 1f;
                }

                _prefabLoadProgress = 1f;
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"사운드 로딩 중 오류 발생: {ex.Message}");
            }
            finally
            {
                if (locationHandle.IsValid())
                    Addressables.Release(locationHandle);
            }
        }

        /// <summary>
        /// 사운드 리소스 테이블에서 PreLoad가 활성화된 AudioClip을 전역 고정 캐시에 미리 로드합니다.
        /// </summary>
        /// <param name="tableLoaderManager">사운드 리소스 테이블을 보유한 테이블 로더입니다.</param>
        /// <param name="introOnly">true이면 Intro 씬에서 사용하는 사운드 리소스만 선로드합니다.</param>
        public async Task PreloadMarkedSoundsAsync(TableLoaderManager tableLoaderManager, bool introOnly = false)
        {
            List<string> keys = CollectPreloadSoundKeys(tableLoaderManager, introOnly);
            await PreloadAudioClipsAsync(keys);
        }

        /// <summary>
        /// PreLoad가 활성화된 사운드와 공용 UI 버튼 사운드를 게임 시작 범위에 준비합니다.
        /// 공용 UI 범위는 새 임대를 먼저 획득한 뒤 기존 임대를 교체하여 갱신 중 참조 공백을 방지합니다.
        /// </summary>
        /// <param name="tableLoaderManager">사운드 테이블을 보유한 테이블 로더입니다.</param>
        /// <param name="introOnly">true이면 PreLoad 행은 Intro용만 선별하되 공용 UI 버튼 사운드는 함께 준비합니다.</param>
        public async Task PreloadStartupSoundsAsync(TableLoaderManager tableLoaderManager, bool introOnly = false)
        {
            await PreloadMarkedSoundsAsync(tableLoaderManager, introOnly);
            await RefreshGlobalUiCommonScopeAsync(tableLoaderManager);
            _prefabLoadProgress = 1f;
        }

        /// <summary>
        /// 사운드 설정에 등록된 공용 UI 버튼 사운드를 전역 범위로 획득합니다.
        /// </summary>
        /// <param name="tableLoaderManager">대표 사운드를 실제 AudioClip 키로 해석할 테이블 로더입니다.</param>
        private async Task RefreshGlobalUiCommonScopeAsync(TableLoaderManager tableLoaderManager)
        {
            int requestVersion = ++_globalUiCommonRequestVersion;
            GGemCoSoundSettings soundSettings = AddressableLoaderSettings.Instance?.soundSettings;
            IReadOnlyList<int> soundUids = soundSettings?.GetCommonUiSoundUids() ?? Array.Empty<int>();
            SoundUsageAddressKeyResolver resolver = new SoundUsageAddressKeyResolver(tableLoaderManager);
            IReadOnlyList<string> addressKeys = resolver.ResolveAddressKeys(soundUids);

            SoundScopeLease acquiredLease = null;
            if (addressKeys.Count > 0 && !_isDestroying)
            {
                try
                {
                    acquiredLease = await AcquireScopeAsync(
                        SoundUsageScopeKey.Global(GlobalUiCommonScopeId),
                        addressKeys);
                }
                catch (Exception ex)
                {
                    GcLogger.LogWarning(
                        $"[AddressableLoaderSound] 공용 UI 사운드 범위를 준비하지 못했습니다. error={ex.Message}");
                }
            }

            if (_isDestroying || requestVersion != _globalUiCommonRequestVersion)
            {
                acquiredLease?.Dispose();
                return;
            }

            SoundScopeLease previousLease = _globalUiCommonScopeLease;
            _globalUiCommonScopeLease = acquiredLease;
            previousLease?.Dispose();
        }

        /// <summary>
        /// 지정한 범위에서 사용할 AudioClip 키를 로드하고 범위 임대 객체를 반환합니다.
        /// </summary>
        /// <param name="scopeKey">맵 또는 UI 윈도우 등을 나타내는 범위 키입니다.</param>
        /// <param name="addressKeys">범위에서 사용할 AudioClip Addressables 키 목록입니다.</param>
        /// <returns>범위 해제 시 Dispose할 임대 객체입니다.</returns>
        public Task<SoundScopeLease> AcquireScopeAsync(
            SoundUsageScopeKey scopeKey,
            IEnumerable<string> addressKeys)
        {
            if (_isDestroying)
                return Task.FromResult<SoundScopeLease>(null);

            return ScopeManager.AcquireAsync(scopeKey, addressKeys);
        }

        /// <summary>
        /// 지정한 범위 키로 획득한 모든 사운드 참조를 해제합니다.
        /// </summary>
        /// <param name="scopeKey">해제할 맵 또는 UI 윈도우 범위 키입니다.</param>
        public void ReleaseScope(SoundUsageScopeKey scopeKey)
        {
            _scopeManager?.ReleaseScope(scopeKey);
        }

        /// <summary>
        /// 재생 중인 AudioSource가 사용할 AudioClip과 재생 참조 임대 객체를 획득합니다.
        /// </summary>
        /// <param name="keyName">로드할 AudioClip Addressables 키입니다.</param>
        /// <returns>재생이 끝난 뒤 Dispose해야 하는 임대 객체입니다. 실패 시 null입니다.</returns>
        public async Task<SoundPlaybackLease> AcquirePlaybackAsync(string keyName)
        {
            if (_isDestroying || string.IsNullOrWhiteSpace(keyName))
                return null;

            SoundClipEntry entry = GetOrCreateEntry(keyName);
            entry.PlaybackReferenceCount++;

            AudioClip clip = await EnsureLoadedAsync(entry);
            if (clip != null && !_isDestroying)
                return new SoundPlaybackLease(this, entry.AddressKey, clip);

            ReleasePlaybackReference(entry.AddressKey);
            return null;
        }

        /// <summary>
        /// 사운드 클립을 필요 시점에 비동기로 로드하고 전역 고정 캐시에 저장합니다.
        /// 기존 API 호환을 위해 이 메서드로 로드한 클립은 범위 참조가 없어도 자동 해제하지 않습니다.
        /// 새 맵/UI 범위에서는 <see cref="AcquireScopeAsync"/>를 사용하고,
        /// 실제 재생에서는 <see cref="AcquirePlaybackAsync"/>를 사용하는 것을 권장합니다.
        /// </summary>
        /// <param name="keyName">로드할 사운드 Addressables 키입니다.</param>
        /// <returns>로드된 AudioClip입니다. 실패 시 null을 반환합니다.</returns>
        public async Task<AudioClip> LoadAudioClipAsync(string keyName)
        {
            if (_isDestroying || string.IsNullOrWhiteSpace(keyName))
                return null;

            SoundClipEntry entry = GetOrCreateEntry(keyName);
            entry.IsLegacyPinned = true;

            AudioClip clip = await EnsureLoadedAsync(entry);
            if (clip != null)
                return clip;

            entry.IsLegacyPinned = false;
            TryReleaseEntry(entry);
            return null;
        }

        /// <summary>
        /// 캐시된 사운드 클립 조회를 시도합니다.
        /// </summary>
        /// <param name="keyName">조회할 사운드 Addressables 키입니다.</param>
        /// <param name="audioClip">캐시된 AudioClip입니다.</param>
        /// <returns>캐시에 존재하면 true를 반환합니다.</returns>
        public bool TryGetAudioClip(string keyName, out AudioClip audioClip)
        {
            audioClip = null;
            if (string.IsNullOrWhiteSpace(keyName))
                return false;

            if (!_entries.TryGetValue(keyName, out SoundClipEntry entry) || entry.Clip == null)
                return false;

            audioClip = entry.Clip;
            return true;
        }

        /// <summary>
        /// 캐시된 사운드 클립을 반환합니다.
        /// </summary>
        /// <param name="keyName">조회할 사운드 Addressables 키입니다.</param>
        /// <returns>캐시에 있으면 AudioClip, 없으면 null입니다.</returns>
        public AudioClip GetAudioClip(string keyName)
        {
            if (TryGetAudioClip(keyName, out AudioClip audioClip))
                return audioClip;

            GcLogger.LogWarning($"Addressables에서 {keyName} 사운드 캐시를 찾을 수 없습니다. 필요 시 LoadAudioClipAsync를 사용하세요.");
            return null;
        }

        /// <summary>
        /// 지정한 AudioClip 키의 현재 범위/재생 참조 수를 조회합니다.
        /// 런타임 디버그 도구에서 참조 누수를 확인할 때 사용할 수 있습니다.
        /// </summary>
        /// <param name="keyName">조회할 AudioClip Addressables 키입니다.</param>
        /// <param name="scopeReferenceCount">범위 참조 수입니다.</param>
        /// <param name="playbackReferenceCount">재생 참조 수입니다.</param>
        /// <returns>해당 키의 로드 엔트리가 존재하면 true를 반환합니다.</returns>
        public bool TryGetReferenceCounts(
            string keyName,
            out int scopeReferenceCount,
            out int playbackReferenceCount)
        {
            scopeReferenceCount = 0;
            playbackReferenceCount = 0;

            if (string.IsNullOrWhiteSpace(keyName) ||
                !_entries.TryGetValue(keyName, out SoundClipEntry entry))
            {
                return false;
            }

            scopeReferenceCount = entry.ScopeReferenceCount;
            playbackReferenceCount = entry.PlaybackReferenceCount;
            return true;
        }

        /// <summary>
        /// 현재 로드 엔트리와 활성 범위의 진단 스냅샷을 생성합니다.
        /// 메모리 크기 계산은 비용이 있으므로 에디터 디버그 창의 수동 새로고침처럼 제한된 시점에만 요청해야 합니다.
        /// </summary>
        /// <param name="includeRuntimeMemorySize">AudioClip 네이티브 메모리 추정값을 계산할지 여부입니다.</param>
        /// <returns>현재 참조 카운트, 로드 상태 및 범위 목록의 복사본입니다.</returns>
        public SoundRuntimeDiagnosticsSnapshot CreateDiagnosticsSnapshot(bool includeRuntimeMemorySize = false)
        {
            List<SoundClipDiagnosticsEntry> clips = new List<SoundClipDiagnosticsEntry>(_entries.Count);
            HashSet<int> measuredClipIds = new HashSet<int>();
            long totalRuntimeMemoryBytes = 0L;
            int loadedClipCount = 0;
            int loadingClipCount = 0;
            int legacyPinnedClipCount = 0;
            int totalScopeReferenceCount = 0;
            int totalPlaybackReferenceCount = 0;

            foreach (KeyValuePair<string, SoundClipEntry> pair in _entries)
            {
                SoundClipEntry entry = pair.Value;
                if (entry == null)
                    continue;

                AudioClip clip = entry.Clip;
                long runtimeMemoryBytes = 0L;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (includeRuntimeMemorySize && clip != null && measuredClipIds.Add(clip.GetInstanceID()))
                    runtimeMemoryBytes = Profiler.GetRuntimeMemorySizeLong(clip);
#endif
                if (clip != null)
                    loadedClipCount++;
                if (entry.LoadingSource != null)
                    loadingClipCount++;
                if (entry.IsLegacyPinned)
                    legacyPinnedClipCount++;

                totalScopeReferenceCount += entry.ScopeReferenceCount;
                totalPlaybackReferenceCount += entry.PlaybackReferenceCount;
                totalRuntimeMemoryBytes += runtimeMemoryBytes;

                clips.Add(new SoundClipDiagnosticsEntry
                {
                    AddressKey = entry.AddressKey,
                    ClipName = clip != null ? clip.name : string.Empty,
                    ScopeReferenceCount = entry.ScopeReferenceCount,
                    PlaybackReferenceCount = entry.PlaybackReferenceCount,
                    IsLegacyPinned = entry.IsLegacyPinned,
                    IsLoaded = clip != null,
                    IsLoading = entry.LoadingSource != null,
                    LengthSeconds = clip != null ? clip.length : 0f,
                    RuntimeMemoryBytes = runtimeMemoryBytes,
                });
            }

            clips.Sort((left, right) => string.Compare(left.AddressKey, right.AddressKey, StringComparison.OrdinalIgnoreCase));
            return new SoundRuntimeDiagnosticsSnapshot
            {
                CapturedAtUtc = DateTime.UtcNow,
                LoadedClipCount = loadedClipCount,
                LoadingClipCount = loadingClipCount,
                LegacyPinnedClipCount = legacyPinnedClipCount,
                TotalScopeReferenceCount = totalScopeReferenceCount,
                TotalPlaybackReferenceCount = totalPlaybackReferenceCount,
                TotalRuntimeMemoryBytes = totalRuntimeMemoryBytes,
                Clips = clips,
                Scopes = _scopeManager?.CreateDiagnosticsSnapshot() ?? Array.Empty<SoundScopeDiagnosticsEntry>(),
            };
        }

        /// <summary>
        /// 지정한 Addressables 키 목록에 현재 로드된 AudioClip의 런타임 메모리 추정값을 합산합니다.
        /// 개발 빌드와 에디터에서만 실제 메모리 크기를 계산하며 일반 릴리즈에서는 0을 반환합니다.
        /// </summary>
        /// <param name="addressKeys">메모리 크기를 조회할 AudioClip 키 목록입니다.</param>
        /// <returns>중복 AudioClip을 한 번만 계산한 바이트 합계입니다.</returns>
        public long GetRuntimeMemoryBytes(IEnumerable<string> addressKeys)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (addressKeys == null)
                return 0L;

            long totalBytes = 0L;
            HashSet<string> visitedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<int> measuredClipIds = new HashSet<int>();
            foreach (string rawKey in addressKeys)
            {
                string key = rawKey?.Trim();
                if (string.IsNullOrWhiteSpace(key) || !visitedKeys.Add(key))
                    continue;

                if (!_entries.TryGetValue(key, out SoundClipEntry entry) || entry?.Clip == null)
                    continue;

                AudioClip clip = entry.Clip;
                if (measuredClipIds.Add(clip.GetInstanceID()))
                    totalBytes += Profiler.GetRuntimeMemorySizeLong(clip);
            }

            return totalBytes;
#else
            return 0L;
#endif
        }

        /// <summary>
        /// 사운드 로딩 진행률을 반환합니다.
        /// </summary>
        /// <returns>0~1 범위의 로딩 진행률입니다.</returns>
        public float GetLoadProgress()
        {
            return _prefabLoadProgress;
        }

        /// <summary>
        /// 범위 매니저가 사용하는 단일 AudioClip 범위 참조를 획득합니다.
        /// </summary>
        /// <param name="keyName">로드할 AudioClip Addressables 키입니다.</param>
        /// <returns>로드와 참조 획득에 성공하면 true를 반환합니다.</returns>
        internal async Task<bool> AcquireScopeReferenceAsync(string keyName)
        {
            if (_isDestroying || string.IsNullOrWhiteSpace(keyName))
                return false;

            SoundClipEntry entry = GetOrCreateEntry(keyName);
            entry.ScopeReferenceCount++;

            AudioClip clip = await EnsureLoadedAsync(entry);
            if (clip != null && !_isDestroying)
                return true;

            ReleaseScopeReference(entry.AddressKey);
            return false;
        }

        /// <summary>
        /// 지정한 AudioClip 키의 범위 참조 수를 감소시키고 미사용 리소스를 해제합니다.
        /// </summary>
        /// <param name="keyName">범위 참조를 해제할 Addressables 키입니다.</param>
        internal void ReleaseScopeReference(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName) ||
                !_entries.TryGetValue(keyName, out SoundClipEntry entry))
            {
                return;
            }

            entry.ScopeReferenceCount = Math.Max(0, entry.ScopeReferenceCount - 1);
            TryReleaseEntry(entry);
        }

        /// <summary>
        /// 지정한 AudioClip 키의 재생 참조 수를 감소시키고 미사용 리소스를 해제합니다.
        /// </summary>
        /// <param name="keyName">재생 참조를 해제할 Addressables 키입니다.</param>
        internal void ReleasePlaybackReference(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName) ||
                !_entries.TryGetValue(keyName, out SoundClipEntry entry))
            {
                return;
            }

            entry.PlaybackReferenceCount = Math.Max(0, entry.PlaybackReferenceCount - 1);
            TryReleaseEntry(entry);
        }

        /// <summary>
        /// 선로드 대상 Addressables 키 목록을 제한된 개수만큼 병렬로 실제 AudioClip로 로드합니다.
        /// 무제한 병렬 요청으로 인한 메모리 및 압축 해제 부하를 방지하기 위해 배치 단위로 처리합니다.
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

            int concurrentRequestCount = ResolvePreloadConcurrentRequestCount();
            int completedCount = 0;

            for (int startIndex = 0; startIndex < keys.Count; startIndex += concurrentRequestCount)
            {
                int batchCount = Math.Min(concurrentRequestCount, keys.Count - startIndex);
                List<Task> loadTasks = new List<Task>(batchCount);

                for (int offset = 0; offset < batchCount; offset++)
                {
                    string key = keys[startIndex + offset];
                    loadTasks.Add(PreloadAudioClipSafeAsync(key));
                }

                await Task.WhenAll(loadTasks);
                completedCount += batchCount;
                _prefabLoadProgress = (float)completedCount / keys.Count;
            }

            _prefabLoadProgress = 1f;
        }

        /// <summary>
        /// 단일 AudioClip 선로드 요청을 예외로부터 보호하여 전체 선로드 흐름이 중단되지 않도록 처리합니다.
        /// </summary>
        /// <param name="key">로드할 사운드 Addressables 키입니다.</param>
        private async Task PreloadAudioClipSafeAsync(string key)
        {
            try
            {
                await LoadAudioClipAsync(key);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"[AddressableLoaderSound] 선로드 중 예외가 발생했습니다. key={key}, error={ex.Message}");
            }
        }

        /// <summary>
        /// 사운드 설정에서 선로드 동시 요청 개수를 조회합니다.
        /// 설정이 아직 로드되지 않은 경우에는 안전한 기본값을 사용합니다.
        /// </summary>
        /// <returns>동시에 요청할 최대 AudioClip 개수입니다.</returns>
        private static int ResolvePreloadConcurrentRequestCount()
        {
            GGemCoSoundSettings soundSettings = AddressableLoaderSettings.Instance?.soundSettings;
            return soundSettings != null
                ? soundSettings.GetPreloadConcurrentRequestCount()
                : DefaultPreloadConcurrentRequestCount;
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
        /// 지정한 키의 로드 엔트리를 반환하거나 새로 생성합니다.
        /// </summary>
        /// <param name="keyName">AudioClip Addressables 키입니다.</param>
        /// <returns>키에 대응하는 로드 엔트리입니다.</returns>
        private SoundClipEntry GetOrCreateEntry(string keyName)
        {
            string normalizedKey = keyName.Trim();
            if (_entries.TryGetValue(normalizedKey, out SoundClipEntry entry))
                return entry;

            entry = new SoundClipEntry
            {
                AddressKey = normalizedKey,
            };
            _entries.Add(normalizedKey, entry);
            return entry;
        }

        /// <summary>
        /// 지정한 엔트리의 AudioClip 로드 작업을 중복 없이 실행합니다.
        /// </summary>
        /// <param name="entry">로드할 사운드 엔트리입니다.</param>
        /// <returns>로드된 AudioClip입니다. 실패 시 null입니다.</returns>
        private Task<AudioClip> EnsureLoadedAsync(SoundClipEntry entry)
        {
            if (entry == null || _isDestroying)
                return Task.FromResult<AudioClip>(null);

            if (entry.Clip != null)
                return Task.FromResult(entry.Clip);

            if (entry.LoadingSource != null)
                return entry.LoadingSource.Task;

            TaskCompletionSource<AudioClip> loadingSource = new TaskCompletionSource<AudioClip>();
            entry.LoadingSource = loadingSource;
            _ = LoadEntryAsync(entry, loadingSource);
            return loadingSource.Task;
        }

        /// <summary>
        /// Addressables에서 단일 AudioClip을 로드하고 엔트리에 핸들을 저장합니다.
        /// 실패한 핸들은 즉시 해제하여 Addressables 참조가 남지 않도록 처리합니다.
        /// </summary>
        /// <param name="entry">로드 결과를 저장할 엔트리입니다.</param>
        /// <param name="loadingSource">동일 키 대기자에게 결과를 전달할 완료 소스입니다.</param>
        private async Task LoadEntryAsync(
            SoundClipEntry entry,
            TaskCompletionSource<AudioClip> loadingSource)
        {
            AudioClip result = null;
            AsyncOperationHandle<AudioClip> loadHandle = default;

            try
            {
                loadHandle = Addressables.LoadAssetAsync<AudioClip>(entry.AddressKey);
                entry.Handle = loadHandle;
                AudioClip audioClip = await loadHandle.Task;
                if (!_isDestroying && loadHandle.Status == AsyncOperationStatus.Succeeded && audioClip != null)
                {
                    entry.Clip = audioClip;
                    result = audioClip;
                }
                else
                {
                    GcLogger.LogWarning($"[AddressableLoaderSound] 사운드 클립 로드에 실패했습니다. key={entry.AddressKey}");
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"[AddressableLoaderSound] 사운드 클립 로드 중 예외가 발생했습니다. key={entry.AddressKey}, error={ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(entry.LoadingSource, loadingSource))
                    entry.LoadingSource = null;

                if (entry.Clip == null)
                {
                    ReleaseEntryHandle(entry);
                    RemoveEntryIfSame(entry);
                }
                else
                {
                    TryReleaseEntry(entry);
                }

                loadingSource.TrySetResult(result);
            }
        }

        /// <summary>
        /// 범위, 재생, 전역 고정 참조가 모두 없는 로드 엔트리를 해제합니다.
        /// </summary>
        /// <param name="entry">해제 여부를 검사할 엔트리입니다.</param>
        private void TryReleaseEntry(SoundClipEntry entry)
        {
            if (entry == null || entry.LoadingSource != null || entry.IsLegacyPinned)
                return;

            if (entry.ScopeReferenceCount > 0 || entry.PlaybackReferenceCount > 0)
                return;

            RemoveEntryIfSame(entry);
            ReleaseEntryHandle(entry);
        }

        /// <summary>
        /// 현재 사전에 같은 인스턴스로 등록된 엔트리만 제거합니다.
        /// </summary>
        /// <param name="entry">제거할 엔트리입니다.</param>
        private void RemoveEntryIfSame(SoundClipEntry entry)
        {
            if (entry == null)
                return;

            if (_entries.TryGetValue(entry.AddressKey, out SoundClipEntry current) && ReferenceEquals(current, entry))
                _entries.Remove(entry.AddressKey);
        }

        /// <summary>
        /// 엔트리가 보관한 AudioClip과 Addressables 핸들을 안전하게 해제합니다.
        /// </summary>
        /// <param name="entry">핸들을 해제할 엔트리입니다.</param>
        private static void ReleaseEntryHandle(SoundClipEntry entry)
        {
            if (entry == null)
                return;

            entry.Clip = null;
            if (entry.Handle.IsValid())
                Addressables.Release(entry.Handle);

            entry.Handle = default;
        }
    }
}
