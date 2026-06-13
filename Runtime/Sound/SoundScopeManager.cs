using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵, UI 윈도우 등 사용 범위별 AudioClip 참조를 관리합니다.
    /// </summary>
    public sealed class SoundScopeManager : IDisposable
    {
        private sealed class ActiveScope
        {
            public SoundUsageScopeKey ScopeKey;
            public string[] LoadedKeys;
            public SoundScopeLease Lease;
            public double LoadDurationMilliseconds;
            public float AcquiredRealtimeSeconds;
            public int FailedKeyCount;
        }

        private readonly AddressableLoaderSound _loader;
        private readonly Dictionary<long, ActiveScope> _activeScopes = new Dictionary<long, ActiveScope>();
        private long _nextLeaseId;
        private bool _isDisposed;

        /// <summary>
        /// 활성화된 범위 임대 개수입니다.
        /// </summary>
        public int ActiveScopeCount => _activeScopes.Count;

        /// <summary>
        /// 지정한 사운드 로더를 사용하는 범위 매니저를 생성합니다.
        /// </summary>
        /// <param name="loader">실제 AudioClip 참조 카운트를 관리하는 로더입니다.</param>
        internal SoundScopeManager(AddressableLoaderSound loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// 지정한 범위에서 사용할 AudioClip 키를 로드하고 참조를 유지합니다.
        /// 동일한 범위 키를 여러 번 획득하면 각각 독립적인 임대 객체로 관리됩니다.
        /// </summary>
        /// <param name="scopeKey">맵 또는 UI 윈도우 등을 나타내는 범위 키입니다.</param>
        /// <param name="addressKeys">범위에서 사용할 AudioClip Addressables 키 목록입니다.</param>
        /// <returns>로드 성공 및 실패 키를 포함한 범위 임대 객체입니다.</returns>
        public async Task<SoundScopeLease> AcquireAsync(
            SoundUsageScopeKey scopeKey,
            IEnumerable<string> addressKeys)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SoundScopeManager));

            if (!scopeKey.IsValid)
                throw new ArgumentException("사운드 범위 키가 비어 있습니다.", nameof(scopeKey));

            Stopwatch stopwatch = Stopwatch.StartNew();
            string[] normalizedKeys = NormalizeKeys(addressKeys);
            List<string> loadedKeys = new List<string>(normalizedKeys.Length);
            List<string> failedKeys = new List<string>();

            try
            {
                for (int i = 0; i < normalizedKeys.Length; i++)
                {
                    string key = normalizedKeys[i];
                    bool acquired = await _loader.AcquireScopeReferenceAsync(key);
                    if (acquired)
                        loadedKeys.Add(key);
                    else
                        failedKeys.Add(key);
                }
            }
            catch
            {
                ReleaseKeys(loadedKeys);
                throw;
            }

            if (_isDisposed)
            {
                ReleaseKeys(loadedKeys);
                throw new ObjectDisposedException(nameof(SoundScopeManager));
            }

            stopwatch.Stop();
            long leaseId = ++_nextLeaseId;
            string[] loadedKeyArray = loadedKeys.ToArray();
            string[] failedKeyArray = failedKeys.ToArray();
            SoundScopeLease lease = new SoundScopeLease(
                this,
                leaseId,
                scopeKey,
                loadedKeyArray,
                failedKeyArray);

            _activeScopes.Add(leaseId, new ActiveScope
            {
                ScopeKey = scopeKey,
                LoadedKeys = loadedKeyArray,
                Lease = lease,
                LoadDurationMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                AcquiredRealtimeSeconds = Time.realtimeSinceStartup,
                FailedKeyCount = failedKeyArray.Length,
            });

            return lease;
        }

        /// <summary>
        /// 지정한 범위 키로 획득한 모든 임대 객체를 해제합니다.
        /// </summary>
        /// <param name="scopeKey">해제할 사운드 사용 범위 키입니다.</param>
        public void ReleaseScope(SoundUsageScopeKey scopeKey)
        {
            if (!scopeKey.IsValid || _activeScopes.Count == 0)
                return;

            List<long> leaseIds = new List<long>();
            foreach (KeyValuePair<long, ActiveScope> pair in _activeScopes)
            {
                if (pair.Value.ScopeKey == scopeKey)
                    leaseIds.Add(pair.Key);
            }

            for (int i = 0; i < leaseIds.Count; i++)
                Release(leaseIds[i]);
        }

        /// <summary>
        /// 지정한 임대 식별자가 유지하던 범위 참조를 해제합니다.
        /// </summary>
        /// <param name="leaseId">해제할 내부 임대 식별자입니다.</param>
        internal void Release(long leaseId)
        {
            if (!_activeScopes.TryGetValue(leaseId, out ActiveScope activeScope))
                return;

            _activeScopes.Remove(leaseId);
            ReleaseKeys(activeScope.LoadedKeys);
            activeScope.Lease?.MarkReleasedByOwner();
        }


        /// <summary>
        /// 현재 활성 범위 임대 목록을 진단용 스냅샷으로 복사합니다.
        /// 반환된 목록은 원본 수명과 독립적이므로 에디터 디버그 창에서 안전하게 사용할 수 있습니다.
        /// </summary>
        /// <returns>활성 범위별 로드 결과와 소요 시간 목록입니다.</returns>
        internal IReadOnlyList<SoundScopeDiagnosticsEntry> CreateDiagnosticsSnapshot()
        {
            if (_activeScopes.Count == 0)
                return Array.Empty<SoundScopeDiagnosticsEntry>();

            List<SoundScopeDiagnosticsEntry> result = new List<SoundScopeDiagnosticsEntry>(_activeScopes.Count);
            foreach (KeyValuePair<long, ActiveScope> pair in _activeScopes)
            {
                ActiveScope scope = pair.Value;
                if (scope == null)
                    continue;

                result.Add(new SoundScopeDiagnosticsEntry
                {
                    ScopeKey = scope.ScopeKey.ToString(),
                    LoadedKeyCount = scope.LoadedKeys?.Length ?? 0,
                    FailedKeyCount = scope.FailedKeyCount,
                    LoadDurationMilliseconds = scope.LoadDurationMilliseconds,
                    AcquiredRealtimeSeconds = scope.AcquiredRealtimeSeconds,
                });
            }

            result.Sort((left, right) => string.Compare(left.ScopeKey, right.ScopeKey, StringComparison.Ordinal));
            return result;
        }

        /// <summary>
        /// 현재 활성화된 모든 사운드 범위를 해제합니다.
        /// </summary>
        public void ReleaseAll()
        {
            if (_activeScopes.Count == 0)
                return;

            List<long> leaseIds = new List<long>(_activeScopes.Keys);
            for (int i = 0; i < leaseIds.Count; i++)
                Release(leaseIds[i]);
        }

        /// <summary>
        /// 모든 범위를 해제하고 매니저를 더 이상 사용할 수 없도록 정리합니다.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            ReleaseAll();
        }

        /// <summary>
        /// 범위에서 해제할 Addressables 키의 참조 카운트를 감소시킵니다.
        /// </summary>
        /// <param name="keys">해제할 키 목록입니다.</param>
        private void ReleaseKeys(IReadOnlyList<string> keys)
        {
            if (keys == null)
                return;

            for (int i = 0; i < keys.Count; i++)
                _loader.ReleaseScopeReference(keys[i]);
        }

        /// <summary>
        /// 비어 있는 키와 중복 키를 제거하여 안정적인 범위 키 목록을 생성합니다.
        /// </summary>
        /// <param name="addressKeys">정리할 Addressables 키 열거입니다.</param>
        /// <returns>대소문자를 구분하지 않고 중복이 제거된 키 배열입니다.</returns>
        private static string[] NormalizeKeys(IEnumerable<string> addressKeys)
        {
            if (addressKeys == null)
                return Array.Empty<string>();

            List<string> result = new List<string>();
            HashSet<string> registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string addressKey in addressKeys)
            {
                string key = addressKey?.Trim();
                if (string.IsNullOrWhiteSpace(key) || !registered.Add(key))
                    continue;

                result.Add(key);
            }

            return result.ToArray();
        }
    }
}
