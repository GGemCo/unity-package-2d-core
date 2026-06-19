using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// BGM 교체, 페이드, AudioClip 재생 참조 수명을 관리합니다.
    /// </summary>
    public class SoundControllerBgm
    {
        private readonly AudioMixer _mixer;
        private readonly string _volumeParam;
        private readonly float _fadeDuration;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _current;
        private AudioSource _next;
        private SoundPlaybackLease _currentLease;
        private SoundPlaybackLease _pendingLease;
        private int _playVersion;
        private float _currentFadeDuration;
        private bool _hasCurrentFadeDuration;

        /// <summary>
        /// BGM 재생에 사용할 AudioSource와 믹서 설정을 초기화합니다.
        /// </summary>
        /// <param name="owner">AudioSource를 추가할 소유 GameObject입니다.</param>
        /// <param name="mixer">BGM 볼륨을 제어할 AudioMixer입니다.</param>
        /// <param name="group">BGM 출력 AudioMixerGroup입니다.</param>
        /// <param name="volumeParam">BGM 볼륨 Exposed Parameter 이름입니다.</param>
        /// <param name="fadeDuration">BGM 교체 페이드 시간입니다.</param>
        public SoundControllerBgm(
            GameObject owner,
            AudioMixer mixer,
            AudioMixerGroup group,
            string volumeParam,
            float fadeDuration)
        {
            _mixer = mixer;
            _volumeParam = volumeParam;
            _fadeDuration = Mathf.Max(0f, fadeDuration);

            _sourceA = owner.AddComponent<AudioSource>();
            _sourceB = owner.AddComponent<AudioSource>();

            _sourceA.outputAudioMixerGroup = group;
            _sourceB.outputAudioMixerGroup = group;

            _current = _sourceA;
            _next = _sourceB;
        }

        /// <summary>
        /// 외부에서 직접 전달한 BGM 클립을 재생합니다.
        /// 이 경로는 Addressables 재생 참조 임대 객체를 보유하지 않습니다.
        /// </summary>
        /// <param name="clip">재생할 BGM 클립입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        public void Play(AudioClip clip, MonoBehaviour coroutineHost)
        {
            Play(clip, coroutineHost, 1f);
        }

        /// <summary>
        /// 외부에서 직접 전달한 BGM 클립을 지정한 볼륨 배율로 재생합니다.
        /// </summary>
        /// <param name="clip">재생할 BGM 클립입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        /// <param name="clipVolume">클립별 볼륨 배율입니다.</param>
        public void Play(AudioClip clip, MonoBehaviour coroutineHost, float clipVolume)
        {
            BeginPlay(clip, null, coroutineHost, clipVolume, _fadeDuration);
        }

        /// <summary>
        /// 외부에서 직접 전달한 BGM 클립을 지정한 볼륨과 페이드 시간으로 재생합니다.
        /// </summary>
        /// <param name="clip">재생할 BGM 클립입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        /// <param name="clipVolume">클립별 볼륨 배율입니다.</param>
        /// <param name="fadeDuration">이번 교체 요청에 적용할 페이드 시간입니다.</param>
        public void Play(
            AudioClip clip,
            MonoBehaviour coroutineHost,
            float clipVolume,
            float fadeDuration)
        {
            BeginPlay(clip, null, coroutineHost, clipVolume, fadeDuration);
        }

        /// <summary>
        /// Addressables 재생 참조 임대 객체가 유지하는 BGM 클립을 재생합니다.
        /// 교체되거나 정지될 때 AudioSource의 Clip을 먼저 제거한 뒤 임대 객체를 해제합니다.
        /// </summary>
        /// <param name="playbackLease">재생 중 Addressables 참조를 유지할 임대 객체입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        /// <param name="clipVolume">클립별 볼륨 배율입니다.</param>
        public void Play(
            SoundPlaybackLease playbackLease,
            MonoBehaviour coroutineHost,
            float clipVolume)
        {
            Play(playbackLease, coroutineHost, clipVolume, _fadeDuration);
        }

        /// <summary>
        /// Addressables 재생 참조 임대 객체가 유지하는 BGM을 지정한 페이드 시간으로 재생합니다.
        /// </summary>
        /// <param name="playbackLease">재생 중 Addressables 참조를 유지할 임대 객체입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        /// <param name="clipVolume">클립별 볼륨 배율입니다.</param>
        /// <param name="fadeDuration">이번 교체 요청에 적용할 페이드 시간입니다.</param>
        public void Play(
            SoundPlaybackLease playbackLease,
            MonoBehaviour coroutineHost,
            float clipVolume,
            float fadeDuration)
        {
            if (playbackLease == null || playbackLease.Clip == null)
            {
                playbackLease?.Dispose();
                return;
            }

            BeginPlay(
                playbackLease.Clip,
                playbackLease,
                coroutineHost,
                clipVolume,
                fadeDuration);
        }

        /// <summary>
        /// 새로운 BGM 교체 요청을 등록하고 이전 대기 중인 요청을 취소합니다.
        /// </summary>
        /// <param name="clip">재생할 AudioClip입니다.</param>
        /// <param name="playbackLease">클립의 Addressables 재생 참조 임대 객체입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴 실행자입니다.</param>
        /// <param name="clipVolume">클립별 볼륨 배율입니다.</param>
        /// <param name="fadeDuration">이번 교체 요청에 적용할 페이드 시간입니다.</param>
        private void BeginPlay(
            AudioClip clip,
            SoundPlaybackLease playbackLease,
            MonoBehaviour coroutineHost,
            float clipVolume,
            float fadeDuration)
        {
            if (clip == null || coroutineHost == null)
            {
                playbackLease?.Dispose();
                return;
            }

            _playVersion++;
            ReleasePendingLease();
            _pendingLease = playbackLease;
            coroutineHost.StartCoroutine(FadeAndSwitch(
                clip,
                Mathf.Clamp01(clipVolume),
                playbackLease,
                _playVersion,
                Mathf.Max(0f, fadeDuration)));
        }

        /// <summary>
        /// 현재 BGM을 페이드아웃한 뒤 새 BGM으로 교체하고 페이드인합니다.
        /// 최신 요청이 아닌 코루틴은 보유한 대기 임대 객체를 해제하고 종료합니다.
        /// </summary>
        /// <param name="newClip">교체할 BGM 클립입니다.</param>
        /// <param name="clipVolume">클립별 볼륨 배율입니다.</param>
        /// <param name="playbackLease">새 클립의 재생 참조 임대 객체입니다.</param>
        /// <param name="version">요청 순서를 식별하는 버전입니다.</param>
        /// <param name="fadeDuration">이번 교체 요청에 적용할 페이드 시간입니다.</param>
        /// <returns>페이드 실행 열거자입니다.</returns>
        private IEnumerator FadeAndSwitch(
            AudioClip newClip,
            float clipVolume,
            SoundPlaybackLease playbackLease,
            int version,
            float fadeDuration)
        {
            float savedVolume = PlayerPrefsManager.LoadSoundVolumeBGM();
            float dbTarget = Mathf.Log10(Mathf.Max(savedVolume, 0.0001f)) * 20f;

            _mixer.GetFloat(_volumeParam, out float currentDb);
            float currentVolume = Mathf.Pow(10f, currentDb / 20f);

            float t = 0f;
            while (t < fadeDuration)
            {
                if (version != _playVersion)
                {
                    ReleasePendingLeaseIfSame(playbackLease);
                    yield break;
                }

                t += Time.deltaTime;
                float v = Mathf.Lerp(currentVolume, 0f, fadeDuration > 0f ? t / fadeDuration : 1f);
                _mixer.SetFloat(_volumeParam, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
                yield return null;
            }

            if (version != _playVersion)
            {
                ReleasePendingLeaseIfSame(playbackLease);
                yield break;
            }

            ClearCurrentSourceAndLease();
            (_current, _next) = (_next, _current);

            _current.clip = newClip;
            _current.loop = true;
            _current.volume = clipVolume;
            _currentLease = playbackLease;
            _currentFadeDuration = fadeDuration;
            _hasCurrentFadeDuration = true;
            if (ReferenceEquals(_pendingLease, playbackLease))
                _pendingLease = null;
            _current.Play();

            t = 0f;
            while (t < fadeDuration)
            {
                if (version != _playVersion)
                    yield break;

                t += Time.deltaTime;
                float v = Mathf.Lerp(0f, savedVolume, fadeDuration > 0f ? t / fadeDuration : 1f);
                _mixer.SetFloat(_volumeParam, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
                yield return null;
            }

            if (version == _playVersion)
                _mixer.SetFloat(_volumeParam, dbTarget);
        }

        /// <summary>
        /// 현재 BGM 재생을 정지하고 AudioSource와 Addressables 참조를 정리합니다.
        /// </summary>
        public void Stop()
        {
            _playVersion++;
            ReleasePendingLease();
            ClearCurrentSourceAndLease();
            ClearAudioSource(_next);
        }

        /// <summary>
        /// 현재 BGM을 지정한 시간 동안 Fade Out한 뒤 AudioSource와 재생 임대를 정리합니다.
        /// </summary>
        /// <param name="coroutineHost">Fade Out Coroutine을 실행할 객체입니다.</param>
        /// <param name="fadeDuration">Fade Out 지속 시간입니다.</param>
        public void Stop(MonoBehaviour coroutineHost, float fadeDuration)
        {
            _playVersion++;
            int version = _playVersion;
            ReleasePendingLease();

            if (coroutineHost == null || _current == null || !_current.isPlaying || fadeDuration <= 0f)
            {
                ClearCurrentSourceAndLease();
                ClearAudioSource(_next);
                return;
            }

            coroutineHost.StartCoroutine(FadeOutAndStop(version, Mathf.Max(0f, fadeDuration)));
        }

        /// <summary>
        /// 현재 BGM에 적용된 페이드 시간을 사용하여 Fade Out한 뒤 정리합니다.
        /// 아직 BGM이 재생되지 않았다면 생성 시 전달된 글로벌 기본 시간을 사용합니다.
        /// </summary>
        /// <param name="coroutineHost">Fade Out Coroutine을 실행할 객체입니다.</param>
        public void Stop(MonoBehaviour coroutineHost)
        {
            float fadeDuration = _hasCurrentFadeDuration
                ? _currentFadeDuration
                : _fadeDuration;
            Stop(coroutineHost, fadeDuration);
        }

        /// <summary>
        /// BGM Mixer 볼륨을 0까지 낮춘 뒤 현재 재생 참조를 정리합니다.
        /// 새로운 재생 요청이 들어오면 이전 Fade Out은 정리를 수행하지 않고 종료합니다.
        /// </summary>
        private IEnumerator FadeOutAndStop(int version, float fadeDuration)
        {
            _mixer.GetFloat(_volumeParam, out float currentDb);
            float startVolume = Mathf.Pow(10f, currentDb / 20f);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (version != _playVersion)
                    yield break;

                elapsed += Time.deltaTime;
                float volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeDuration));
                _mixer.SetFloat(_volumeParam, Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
                yield return null;
            }

            if (version != _playVersion)
                yield break;

            ClearCurrentSourceAndLease();
            ClearAudioSource(_next);
        }

        /// <summary>
        /// BGM 믹서 볼륨을 변경합니다.
        /// </summary>
        /// <param name="volume">0~1 범위의 볼륨입니다.</param>
        /// <param name="save">PlayerPrefs에 저장할지 여부입니다.</param>
        public void SetVolume(float volume, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            _mixer.SetFloat(_volumeParam, db);
            if (save)
                PlayerPrefsManager.SaveSoundVolumeBGM(volume);
        }

        /// <summary>
        /// 컨트롤러가 보유한 AudioSource와 재생 참조를 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            Stop();
        }

        /// <summary>
        /// 현재 AudioSource의 Clip 참조를 제거하고 재생 임대 객체를 해제합니다.
        /// </summary>
        private void ClearCurrentSourceAndLease()
        {
            ClearAudioSource(_current);
            _currentLease?.Dispose();
            _currentLease = null;
            _currentFadeDuration = 0f;
            _hasCurrentFadeDuration = false;
        }

        /// <summary>
        /// 대기 중인 BGM 재생 임대 객체를 해제합니다.
        /// </summary>
        private void ReleasePendingLease()
        {
            _pendingLease?.Dispose();
            _pendingLease = null;
        }

        /// <summary>
        /// 전달된 임대 객체가 현재 대기 요청과 같을 때만 해제합니다.
        /// </summary>
        /// <param name="playbackLease">검사할 재생 임대 객체입니다.</param>
        private void ReleasePendingLeaseIfSame(SoundPlaybackLease playbackLease)
        {
            if (!ReferenceEquals(_pendingLease, playbackLease))
                return;

            ReleasePendingLease();
        }

        /// <summary>
        /// AudioSource를 정지하고 AudioClip 참조를 제거합니다.
        /// </summary>
        /// <param name="source">정리할 AudioSource입니다.</param>
        private static void ClearAudioSource(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.loop = false;
            source.clip = null;
        }
    }
}
