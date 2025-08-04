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

        public void Play(AudioClip clip, MonoBehaviour coroutineHost)
        {
            coroutineHost.StartCoroutine(FadeAndSwitch(clip));
        }

        private IEnumerator FadeAndSwitch(AudioClip newClip)
        {
            float savedVolume = PlayerPrefsManager.LoadSoundVolumeBGM();
            float dbTarget = Mathf.Log10(Mathf.Max(savedVolume, 0.0001f)) * 20f;

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
