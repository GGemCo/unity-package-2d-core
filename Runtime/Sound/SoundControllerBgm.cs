using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// BGM 컨트롤러
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

        public SoundControllerBgm(GameObject owner, AudioMixer mixer, AudioMixerGroup group, string volumeParam, float fadeDuration)
        {
            _mixer = mixer;
            _volumeParam = volumeParam;
            _fadeDuration = fadeDuration;

            _sourceA = owner.AddComponent<AudioSource>();
            _sourceB = owner.AddComponent<AudioSource>();

            _sourceA.outputAudioMixerGroup = group;
            _sourceB.outputAudioMixerGroup = group;

            _current = _sourceA;
            _next = _sourceB;
        }

        /// <summary>
        /// BGM 클립을 재생합니다.
        /// 테이블 볼륨을 사용하지 않는 일반 경로(기존 호출)는 기본 볼륨 1.0으로 재생됩니다.
        /// </summary>
        /// <param name="clip">재생할 BGM 클립입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        public void Play(AudioClip clip, MonoBehaviour coroutineHost)
        {
            Play(clip, coroutineHost, 1f);
        }

        /// <summary>
        /// BGM 클립을 재생합니다.
        /// 사용자 BGM 볼륨(PlayerPrefs)과 별개로, 클립별 볼륨 배율을 함께 적용합니다.
        /// </summary>
        /// <param name="clip">재생할 BGM 클립입니다.</param>
        /// <param name="coroutineHost">페이드 코루틴을 실행할 MonoBehaviour입니다.</param>
        /// <param name="clipVolume">클립별 볼륨(0~1)입니다.</param>
        public void Play(AudioClip clip, MonoBehaviour coroutineHost, float clipVolume)
        {
            coroutineHost.StartCoroutine(FadeAndSwitch(clip, clipVolume));
        }

        /// <summary>
        /// 현재 BGM을 페이드아웃한 뒤 새 BGM으로 교체하고 페이드인합니다.
        /// 믹서 BGM 볼륨은 사용자 설정값을 유지하고, AudioSource 볼륨에는 클립별 배율을 적용합니다.
        /// </summary>
        /// <param name="newClip">교체할 BGM 클립입니다.</param>
        /// <param name="clipVolume">클립별 볼륨(0~1)입니다.</param>
        private IEnumerator FadeAndSwitch(AudioClip newClip, float clipVolume)
        {
            float savedVolume = PlayerPrefsManager.LoadSoundVolumeBGM();
            float dbTarget = Mathf.Log10(Mathf.Max(savedVolume, 0.0001f)) * 20f;
            float normalizedClipVolume = Mathf.Clamp01(clipVolume);

            _mixer.GetFloat(_volumeParam, out float currentDb);
            float currentVolume = Mathf.Pow(10f, currentDb / 20f);

            float t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(currentVolume, 0f, t / _fadeDuration);
                _mixer.SetFloat(_volumeParam, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
                yield return null;
            }

            _current.Stop();
            (_current, _next) = (_next, _current);

            _current.clip = newClip;
            _current.loop = true;
            _current.volume = normalizedClipVolume;
            _current.Play();

            t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(0f, savedVolume, t / _fadeDuration);
                _mixer.SetFloat(_volumeParam, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
                yield return null;
            }

            _mixer.SetFloat(_volumeParam, dbTarget);
        }

        public void Stop()
        {
            _current?.Stop();
        }

        public void SetVolume(float volume, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            _mixer.SetFloat(_volumeParam, db);
            if (save)
            {
                PlayerPrefsManager.SaveSoundVolumeBGM(volume);
            }
        }

        public void OnDestroy()
        {
        }
    }
}
