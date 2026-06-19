using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 여러 환경음을 동시에 재생하고 맵 전환 시 추가·유지·제거되는 환경음을 개별적으로 Fade In/Out합니다.
    /// </summary>
    public sealed class SoundControllerAmbient
    {
        private sealed class AmbientPlayback
        {
            public AudioSource Source;
            public SoundPlaybackLease Lease;
            public int RequestVersion;
            public bool ShouldPlay;
            public float FadeDuration;
        }

        private readonly GameObject _owner;
        private readonly AudioMixerGroup _group;
        private readonly AddressableLoaderSound _loader;
        private readonly float _defaultFadeDuration;
        private readonly Dictionary<int, AmbientPlayback> _playbacksByResourceUid =
            new Dictionary<int, AmbientPlayback>();
        private readonly HashSet<int> _nextResourceUids = new HashSet<int>();
        private readonly List<int> _removedResourceUids = new List<int>();

        private bool _isDestroyed;

        /// <summary>
        /// 기존 호출부와의 하위 호환성을 유지하며 환경음 컨트롤러를 생성합니다.
        /// 글로벌 기본 페이드 시간은 0.7초를 사용합니다.
        /// </summary>
        public SoundControllerAmbient(
            GameObject owner,
            AudioMixerGroup group,
            AddressableLoaderSound loader)
            : this(owner, group, loader, 0.7f)
        {
        }

        /// <summary>
        /// 환경음 재생 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="owner">환경음 AudioSource를 소유할 GameObject입니다.</param>
        /// <param name="group">환경음 전용 AudioMixerGroup입니다.</param>
        /// <param name="loader">AudioClip 재생 참조를 관리할 로더입니다.</param>
        /// <param name="defaultFadeDuration">리소스에 Override가 없을 때 사용할 글로벌 페이드 시간입니다.</param>
        public SoundControllerAmbient(
            GameObject owner,
            AudioMixerGroup group,
            AddressableLoaderSound loader,
            float defaultFadeDuration)
        {
            _owner = owner;
            _group = group;
            _loader = loader;
            _defaultFadeDuration = Mathf.Max(0f, defaultFadeDuration);
        }

        /// <summary>
        /// 단일 환경음을 현재 목록에 추가하거나 기존 재생 설정을 갱신합니다.
        /// 기존 호출부 호환용 API이며 다른 환경음을 제거하지 않습니다.
        /// </summary>
        /// <param name="resolved">최종 재생할 환경음 정보입니다.</param>
        /// <param name="coroutineHost">페이드 Coroutine을 실행할 객체입니다.</param>
        public void Play(ResolvedSound resolved, MonoBehaviour coroutineHost)
        {
            if (_isDestroyed || coroutineHost == null || !resolved.ShouldPlay || resolved.ResourceUid <= 0)
                return;

            PlayOrUpdateAsync(resolved, coroutineHost, ResolveFadeDuration(resolved));
        }

        /// <summary>
        /// 다음 맵에서 필요한 환경음 목록으로 전환합니다.
        /// 공통 환경음은 재시작하지 않고 유지하며, 제거되는 환경음은 Fade Out하고 추가되는 환경음은 Fade In합니다.
        /// </summary>
        /// <param name="nextSounds">다음 범위에서 재생할 환경음 목록입니다.</param>
        /// <param name="coroutineHost">페이드 Coroutine을 실행할 객체입니다.</param>
        public void TransitionTo(IReadOnlyList<ResolvedSound> nextSounds, MonoBehaviour coroutineHost)
        {
            if (_isDestroyed || coroutineHost == null)
                return;

            _nextResourceUids.Clear();
            if (nextSounds != null)
            {
                for (int i = 0; i < nextSounds.Count; i++)
                {
                    ResolvedSound resolved = nextSounds[i];
                    if (!resolved.ShouldPlay ||
                        resolved.Type != SoundConstants.Type.Ambient ||
                        resolved.ResourceUid <= 0 ||
                        !_nextResourceUids.Add(resolved.ResourceUid))
                    {
                        continue;
                    }

                    PlayOrUpdateAsync(resolved, coroutineHost, ResolveFadeDuration(resolved));
                }
            }

            _removedResourceUids.Clear();
            foreach (KeyValuePair<int, AmbientPlayback> pair in _playbacksByResourceUid)
            {
                AmbientPlayback playback = pair.Value;
                if (playback != null && playback.ShouldPlay && !_nextResourceUids.Contains(pair.Key))
                    _removedResourceUids.Add(pair.Key);
            }

            for (int i = 0; i < _removedResourceUids.Count; i++)
            {
                int resourceUid = _removedResourceUids[i];
                if (_playbacksByResourceUid.TryGetValue(resourceUid, out AmbientPlayback playback))
                    BeginFadeOut(playback, coroutineHost, playback.FadeDuration);
            }
        }

        /// <summary>
        /// 특정 환경음 리소스를 글로벌 기본 시간으로 Fade Out한 뒤 정리합니다.
        /// </summary>
        /// <param name="resourceUid">정지할 실제 환경음 리소스 UID입니다.</param>
        /// <param name="coroutineHost">페이드 Coroutine을 실행할 객체입니다.</param>
        public void Stop(int resourceUid, MonoBehaviour coroutineHost)
        {
            if (!_playbacksByResourceUid.TryGetValue(resourceUid, out AmbientPlayback playback))
                return;

            BeginFadeOut(playback, coroutineHost, playback.FadeDuration);
        }

        /// <summary>
        /// 기존 호출부와의 하위 호환성을 위해 특정 환경음을 즉시 정리합니다.
        /// 자연스러운 전환이 필요한 경우 Coroutine 실행 객체를 받는 Overload를 사용합니다.
        /// </summary>
        public void Stop(int resourceUid)
        {
            if (!_playbacksByResourceUid.TryGetValue(resourceUid, out AmbientPlayback playback))
                return;

            playback.RequestVersion++;
            playback.ShouldPlay = false;
            ClearPlayback(playback);
        }

        /// <summary>
        /// 현재 관리 중인 모든 환경음을 글로벌 기본 시간으로 Fade Out한 뒤 정리합니다.
        /// </summary>
        /// <param name="coroutineHost">페이드 Coroutine을 실행할 객체입니다.</param>
        public void StopAll(MonoBehaviour coroutineHost)
        {
            if (coroutineHost == null)
            {
                StopAllImmediate();
                return;
            }

            foreach (KeyValuePair<int, AmbientPlayback> pair in _playbacksByResourceUid)
                BeginFadeOut(pair.Value, coroutineHost, pair.Value?.FadeDuration ?? _defaultFadeDuration);
        }

        /// <summary>
        /// 기존 호출부와의 하위 호환성을 위해 모든 환경음을 즉시 정리합니다.
        /// 자연스러운 전환이 필요한 경우 Coroutine 실행 객체를 받는 Overload를 사용합니다.
        /// </summary>
        public void StopAll()
        {
            StopAllImmediate();
        }

        /// <summary>
        /// 환경음 AudioClip을 준비하고 최신 요청일 때만 AudioSource에 연결합니다.
        /// </summary>
        /// <param name="resolved">최종 재생 정보입니다.</param>
        /// <param name="coroutineHost">페이드 Coroutine 실행 객체입니다.</param>
        /// <param name="fadeDuration">이번 전환에 적용할 페이드 시간입니다.</param>
        private async void PlayOrUpdateAsync(
            ResolvedSound resolved,
            MonoBehaviour coroutineHost,
            float fadeDuration)
        {
            AmbientPlayback playback = GetOrCreatePlayback(resolved.ResourceUid);
            if (playback == null)
                return;

            bool wasActive = playback.ShouldPlay;
            int requestVersion = ++playback.RequestVersion;
            playback.ShouldPlay = true;
            playback.FadeDuration = Mathf.Max(0f, fadeDuration);
            string addressKey = BuildAddressKey(resolved.FileName);

            if (playback.Lease != null &&
                !playback.Lease.IsReleased &&
                playback.Lease.AddressKey.Equals(addressKey, StringComparison.OrdinalIgnoreCase))
            {
                ApplyPlaybackSettings(playback.Source, resolved, preserveVolume: true);
                if (!playback.Source.isPlaying || !wasActive)
                {
                    float startVolume = playback.Source.isPlaying ? playback.Source.volume : 0f;
                    if (!playback.Source.isPlaying)
                    {
                        playback.Source.volume = 0f;
                        playback.Source.Play();
                    }

                    coroutineHost.StartCoroutine(
                        FadeVolume(
                            playback,
                            requestVersion,
                            startVolume,
                            resolved.Volume,
                            fadeDuration,
                            false));
                }

                return;
            }

            SoundPlaybackLease lease = null;
            try
            {
                lease = await _loader.AcquirePlaybackAsync(addressKey);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning(
                    $"Ambient 클립 로드 중 예외가 발생했습니다. resourceUid={resolved.ResourceUid}, error={ex.Message}");
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
            playback.ShouldPlay = true;
            playback.Source.clip = lease.Clip;
            ApplyPlaybackSettings(playback.Source, resolved, preserveVolume: false);
            playback.Source.volume = 0f;
            playback.Source.Play();
            coroutineHost.StartCoroutine(
                FadeVolume(playback, requestVersion, 0f, resolved.Volume, fadeDuration, false));
        }

        /// <summary>
        /// 환경음 Fade Out을 시작하고 완료 시 AudioClip 및 재생 임대를 정리합니다.
        /// </summary>
        private void BeginFadeOut(
            AmbientPlayback playback,
            MonoBehaviour coroutineHost,
            float fadeDuration)
        {
            if (playback == null)
                return;

            int requestVersion = ++playback.RequestVersion;
            playback.ShouldPlay = false;
            if (playback.Source == null || !playback.Source.isPlaying || coroutineHost == null)
            {
                ClearPlayback(playback);
                return;
            }

            coroutineHost.StartCoroutine(
                FadeVolume(
                    playback,
                    requestVersion,
                    playback.Source.volume,
                    0f,
                    Mathf.Max(0f, fadeDuration),
                    true));
        }

        /// <summary>
        /// 지정한 환경음 AudioSource 볼륨을 시간에 따라 보간합니다.
        /// 요청 버전이 바뀌면 이전 Fade 작업은 즉시 종료합니다.
        /// </summary>
        private static IEnumerator FadeVolume(
            AmbientPlayback playback,
            int requestVersion,
            float fromVolume,
            float toVolume,
            float duration,
            bool clearOnComplete)
        {
            if (playback?.Source == null)
                yield break;

            if (duration <= 0f)
            {
                playback.Source.volume = toVolume;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (playback.RequestVersion != requestVersion || playback.Source == null)
                        yield break;

                    elapsed += Time.deltaTime;
                    playback.Source.volume = Mathf.Lerp(
                        fromVolume,
                        toVolume,
                        Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }

                if (playback.RequestVersion != requestVersion || playback.Source == null)
                    yield break;

                playback.Source.volume = toVolume;
            }

            if (clearOnComplete)
                ClearPlayback(playback);
        }

        /// <summary>
        /// 실제 환경음 리소스 UID에 대응하는 재생 상태를 가져오거나 생성합니다.
        /// </summary>
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
        private static void ApplyPlaybackSettings(
            AudioSource source,
            ResolvedSound resolved,
            bool preserveVolume)
        {
            if (source == null)
                return;

            if (!preserveVolume)
                source.volume = Mathf.Clamp01(resolved.Volume);
            source.pitch = Mathf.Approximately(resolved.Pitch, 0f) ? 1f : resolved.Pitch;
            source.loop = true;
        }

        /// <summary>
        /// 리소스 테이블 Override 또는 글로벌 기본값을 기준으로 최종 환경음 페이드 시간을 계산합니다.
        /// </summary>
        private float ResolveFadeDuration(ResolvedSound resolved)
        {
            return resolved.UseFadeDurationOverride
                ? Mathf.Max(0f, resolved.FadeDuration)
                : _defaultFadeDuration;
        }

        /// <summary>
        /// AudioSource를 정지하고 AudioClip 참조와 재생 임대를 정리합니다.
        /// </summary>
        private static void ClearPlayback(AmbientPlayback playback)
        {
            if (playback == null)
                return;

            if (playback.Source != null)
            {
                playback.Source.Stop();
                playback.Source.loop = false;
                playback.Source.clip = null;
                playback.Source.volume = 0f;
            }

            playback.Lease?.Dispose();
            playback.Lease = null;
        }

        /// <summary>
        /// 모든 환경음을 즉시 정리합니다.
        /// </summary>
        private void StopAllImmediate()
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
        /// 사운드 파일명으로 AudioClip Addressables 키를 생성합니다.
        /// </summary>
        private static string BuildAddressKey(string fileName)
        {
            return $"{ConfigAddressableGroupName.Sound}_{fileName}";
        }

        /// <summary>
        /// 컨트롤러가 보유한 환경음 AudioSource와 재생 참조를 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            if (_isDestroyed)
                return;

            _isDestroyed = true;
            StopAllImmediate();
            _playbacksByResourceUid.Clear();
        }
    }
}
