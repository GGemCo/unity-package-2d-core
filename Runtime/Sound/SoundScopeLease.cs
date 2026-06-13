using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 특정 맵이나 UI 윈도우 범위에서 사용하는 사운드 참조 묶음입니다.
    /// </summary>
    public sealed class SoundScopeLease : IDisposable
    {
        private SoundScopeManager _owner;
        private readonly long _leaseId;
        private bool _isReleased;

        /// <summary>
        /// 이 임대 객체가 나타내는 사운드 사용 범위입니다.
        /// </summary>
        public SoundUsageScopeKey ScopeKey { get; }

        /// <summary>
        /// 정상적으로 로드되어 범위 참조가 유지되는 Addressables 키 목록입니다.
        /// </summary>
        public IReadOnlyList<string> LoadedKeys { get; }

        /// <summary>
        /// 로드에 실패하여 범위에 포함하지 못한 Addressables 키 목록입니다.
        /// </summary>
        public IReadOnlyList<string> FailedKeys { get; }

        /// <summary>
        /// 범위 참조가 이미 해제되었는지 여부입니다.
        /// </summary>
        public bool IsReleased => _isReleased;

        /// <summary>
        /// 범위 임대 객체를 생성합니다.
        /// </summary>
        /// <param name="owner">범위 참조를 관리하는 매니저입니다.</param>
        /// <param name="leaseId">매니저 내부 임대 식별자입니다.</param>
        /// <param name="scopeKey">사운드 사용 범위 키입니다.</param>
        /// <param name="loadedKeys">정상적으로 참조한 키 목록입니다.</param>
        /// <param name="failedKeys">로드에 실패한 키 목록입니다.</param>
        internal SoundScopeLease(
            SoundScopeManager owner,
            long leaseId,
            SoundUsageScopeKey scopeKey,
            IReadOnlyList<string> loadedKeys,
            IReadOnlyList<string> failedKeys)
        {
            _owner = owner;
            _leaseId = leaseId;
            ScopeKey = scopeKey;
            LoadedKeys = loadedKeys ?? Array.Empty<string>();
            FailedKeys = failedKeys ?? Array.Empty<string>();
        }

        /// <summary>
        /// 이 임대 객체가 유지하던 모든 범위 참조를 한 번만 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (_isReleased)
                return;

            _isReleased = true;
            SoundScopeManager owner = _owner;
            _owner = null;
            owner?.Release(_leaseId);
        }

        /// <summary>
        /// 매니저가 범위를 일괄 해제했음을 임대 객체에 반영합니다.
        /// </summary>
        internal void MarkReleasedByOwner()
        {
            _isReleased = true;
            _owner = null;
        }
    }
}
