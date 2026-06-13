using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// AudioClip이 실제 재생 중인 동안 Addressables 참조를 유지하는 임대 객체입니다.
    /// </summary>
    public sealed class SoundPlaybackLease : IDisposable
    {
        private AddressableLoaderSound _owner;
        private bool _isReleased;

        /// <summary>
        /// 참조를 유지하는 Addressables 키입니다.
        /// </summary>
        public string AddressKey { get; }

        /// <summary>
        /// 재생에 사용할 AudioClip입니다.
        /// </summary>
        public AudioClip Clip { get; }

        /// <summary>
        /// 참조가 이미 해제되었는지 여부입니다.
        /// </summary>
        public bool IsReleased => _isReleased;

        /// <summary>
        /// 로더에서 관리하는 재생 참조 임대 객체를 생성합니다.
        /// </summary>
        /// <param name="owner">참조 카운트를 관리하는 사운드 로더입니다.</param>
        /// <param name="addressKey">AudioClip Addressables 키입니다.</param>
        /// <param name="clip">로드된 AudioClip입니다.</param>
        internal SoundPlaybackLease(AddressableLoaderSound owner, string addressKey, AudioClip clip)
        {
            _owner = owner;
            AddressKey = addressKey ?? string.Empty;
            Clip = clip;
        }

        /// <summary>
        /// 재생 참조를 한 번만 해제합니다.
        /// AudioSource에서 Clip 참조를 제거한 뒤 호출해야 합니다.
        /// </summary>
        public void Dispose()
        {
            if (_isReleased)
                return;

            _isReleased = true;
            AddressableLoaderSound owner = _owner;
            _owner = null;
            owner?.ReleasePlaybackReference(AddressKey);
        }
    }
}
