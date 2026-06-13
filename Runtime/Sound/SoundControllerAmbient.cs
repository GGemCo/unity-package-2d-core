using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 여러 환경음을 동시에 재생하고 각 AudioClip의 재생 참조 수명을 관리합니다.
    /// </summary>
    public sealed class SoundControllerAmbient
    {
        private sealed class AmbientPlayback
        {
            public AudioSource Source;
            public SoundPlaybackLease Lease;
            public int RequestVersion;
            public bool ShouldPlay;
        }

        private readonly GameObject _owner;
        private readonly AudioMixerGroup _group;
        private readonly AddressableLoaderSound _loader;
        private readonly Dictionary<int, AmbientPlayback> _playbacksByResourceUid =
            new Dictionary<int, AmbientPlayback>();

        private bool _isDestroyed;

        /// <summary>
        /// 환경음 재생에 사용할 소유 오브젝트, 믹서 그룹, 사운드 로더를 설정합니다.
        /// </summary>
        /// <param name="owner">환경음 AudioSource 오브젝트의 부모입니다.</param>
        /// <param name="group">환경음 출력 AudioMixerGroup입니다.</param>
        /// <param name="loader">AudioClip 재생 참조를 관리할 로더입니다.</param>
        public SoundControllerAmbient(
            GameObject owner,
            AudioMixerGroup group,
            AddressableLoaderSound loader)
        {
            _owner = owner;
            _group = group;
            _loader = loader;
        }

        /// <summary>
        /// 해석된 Ambient 사운드를 비동기로 준비하여 재생합니다.
        /// 이미 같은 리소스가 재생 중이면 볼륨과 피치를 갱신합니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 Ambient 정보입니다.</param>
        /// <param name="coroutineHost">기존 호출부 호환을 위한 코루틴 실행자입니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost)
        {
            if (_isDestroyed || !resolved.ShouldPlay || resolved.ResourceUid <= 0)
                return;

            PlayAsync(resolved);
        }

        /// <summary>
        /// 특정 Ambient 리소스를 정지하고 AudioClip 참조와 재생 임대 객체를 해제합니다.
        /// </summary>
        /// <param name="resourceUid">정지할 실제 Ambient 리소스 UID입니다.</param>
        public void Stop(int resourceUid)
        {
            if (!_playbacksByResourceUid.TryGetValue(resourceUid, out AmbientPlayback playback))
                return;

            playback.RequestVersion++;
            playback.ShouldPlay = false;
            ClearPlayback(playback);
        }

        /// <summary>
        /// 현재 관리 중인 모든 Ambient 리소스를 정지하고 참조를 해제합니다.
        /// </summary>
        public void StopAll()
        {
            foreach (KeyValuePair<int, AmbientPlayback> pair in _playbacksByResourceUid)
            {
                AmbientPlayback playback = pair.Value;
                playback.RequestVersion++;
                playback.ShouldPlay = false;
                ClearPlayback(playback);
            }
        }

        /// <summary>
        /// Ambient 클립의 재생 참조를 획득한 뒤 최신 요청일 때만 AudioSource에 연결합니다.
        /// 정지 또는 재요청으로 오래된 비동기 결과가 되면 즉시 참조를 해제합니다.
        /// </summary>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        private async void PlayAsync(ResolvedSound resolved)
        {
            AmbientPlayback playback = GetOrCreatePlayback(resolved.ResourceUid);
            if (playback == null)
                return;

            int requestVersion = ++playback.RequestVersion;
            playback.ShouldPlay = true;

            if (playback.Lease != null &&
                !playback.Lease.IsReleased &&
                playback.Lease.AddressKey.Equals(BuildAddressKey(resolved.FileName), StringComparison.OrdinalIgnoreCase))
            {
                ApplyPlaybackSettings(playback.Source, resolved);
                if (!playback.Source.isPlaying)
                    playback.Source.Play();
                return;
            }

            string key = BuildAddressKey(resolved.FileName);
            SoundPlaybackLease lease = null;
            try
            {
                lease = await _loader.AcquirePlaybackAsync(key);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"Ambient 클립 로드 중 예외가 발생했습니다. resourceUid={resolved.ResourceUid}, error={ex.Message}");
            }

            if (_isDestroyed ||
                lease == null ||
                playback.RequestVersion != requestVersion ||
                !playback.ShouldPlay ||
                playback.Source == null)
            {
                lease?.Dispose();
                return;
            }

            ClearPlayback(playback);
            playback.Lease = lease;
            playback.Source.clip = lease.Clip;
            ApplyPlaybackSettings(playback.Source, resolved);
            playback.Source.Play();
        }

        /// <summary>
        /// 실제 Ambient 리소스 UID에 대응하는 재생 상태를 가져오거나 생성합니다.
        /// </summary>
        /// <param name="resourceUid">실제 Ambient 리소스 UID입니다.</param>
        /// <returns>사용할 Ambient 재생 상태입니다.</returns>
        private AmbientPlayback GetOrCreatePlayback(int resourceUid)
        {
            if (_playbacksByResourceUid.TryGetValue(resourceUid, out AmbientPlayback playback))
            {
                if (playback.Source != null)
                    return playback;

                ClearPlayback(playback);
            }

            if (_owner == null)
                return null;

            GameObject obj = new GameObject($"{resourceUid}_ambient");
            obj.transform.SetParent(_owner.transform);
            AudioSource source = obj.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _group;
            source.loop = true;

            playback = new AmbientPlayback
            {
                Source = source,
            };
            _playbacksByResourceUid[resourceUid] = playback;
            return playback;
        }

        /// <summary>
        /// AudioSource에 환경음 볼륨, 피치, 루프 설정을 적용합니다.
        /// </summary>
        /// <param name="source">설정을 적용할 AudioSource입니다.</param>
        /// <param name="resolved">해석된 사운드 설정입니다.</param>
        private static void ApplyPlaybackSettings(AudioSource source, ResolvedSound resolved)
        {
            if (source == null)
                return;

            source.volume = Mathf.Clamp01(resolved.Volume);
            source.pitch = Mathf.Approximately(resolved.Pitch, 0f) ? 1f : resolved.Pitch;
            source.loop = true;
        }

        /// <summary>
        /// AudioSource를 정지하고 Clip 참조를 제거한 뒤 재생 임대 객체를 해제합니다.
        /// </summary>
        /// <param name="playback">정리할 Ambient 재생 상태입니다.</param>
        private static void ClearPlayback(AmbientPlayback playback)
        {
            if (playback == null)
                return;

            if (playback.Source != null)
            {
                playback.Source.Stop();
                playback.Source.loop = false;
                playback.Source.clip = null;
            }

            playback.Lease?.Dispose();
            playback.Lease = null;
        }

        /// <summary>
        /// 사운드 파일명으로 AudioClip Addressables 키를 생성합니다.
        /// </summary>
        /// <param name="fileName">사운드 리소스 파일명입니다.</param>
        /// <returns>AudioClip Addressables 키입니다.</returns>
        private static string BuildAddressKey(string fileName)
        {
            return $"{ConfigAddressableGroupName.Sound}_{fileName}";
        }

        /// <summary>
        /// 컨트롤러가 보유한 Ambient AudioSource와 재생 참조를 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            if (_isDestroyed)
                return;

            _isDestroyed = true;
            StopAll();
            _playbacksByResourceUid.Clear();
        }
    }
}
