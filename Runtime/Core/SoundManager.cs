using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class SoundManager : MonoBehaviour
    {
        public enum Type
        {
            None,
            Bgm,
            Sfx
        }
        public enum SubType
        {
            None,
            Player,
            Monster,
            UI,
            Skill
        }

        protected AudioSource AudioSourceDefaultGameBgm;
        protected AudioSource AudioSourceBgm2;
        public float bgmFadeDuration = 0.7f;

        private AudioSource currentBgmAudioSource;
        private AudioSource nextBgmAudioSource;

        private AudioClip[] bgms;

        protected readonly Dictionary<int, int> SoundPlayCount = new Dictionary<int, int>(); // Uid별 현재 재생 중인 사운드의 개수
        protected readonly Dictionary<int, int> MaxConcurrentPlays = new Dictionary<int, int>(); // Uid별 최대 동시 재생 개수

        private readonly Dictionary<int, Queue<GameObject>> soundSfxPoolDictionary = new Dictionary<int, Queue<GameObject>>();
        
        protected void Awake()
        {
            // AudioSource 컴포넌트를 동적으로 추가
            AudioSourceDefaultGameBgm = gameObject.AddComponent<AudioSource>();
            AudioSourceBgm2 = gameObject.AddComponent<AudioSource>();
        }
        /// <summary>
        /// 배경음악 교체하기
        /// </summary>
        /// <param name="newClip"></param>
        protected void ChangeBackgroundMusic(AudioClip newClip)
        {
            if (newClip == null)
            {
                GcLogger.LogError("오디오 클립이 없습니다.");
                return;
            }
            StartCoroutine(BgmFadeOutAndIn(newClip));
        }
        /// <summary>
        /// 배경음악 교체시 fade in out
        /// </summary>
        /// <param name="newClip"></param>
        /// <returns></returns>
        private IEnumerator BgmFadeOutAndIn(AudioClip newClip)
        {
            // Fade out current audio
            float startVolume = 1;
            while (currentBgmAudioSource.volume > 0)
            {
                currentBgmAudioSource.volume -= startVolume * Time.deltaTime / bgmFadeDuration;
                yield return null;
            }

            currentBgmAudioSource.Stop();
            currentBgmAudioSource.volume = startVolume;

            // Swap audio sources
            (currentBgmAudioSource, nextBgmAudioSource) = (nextBgmAudioSource, currentBgmAudioSource);

            // Set new clip and fade in
            currentBgmAudioSource.clip = newClip;
            currentBgmAudioSource.Play();
            currentBgmAudioSource.loop = true;

            currentBgmAudioSource.volume = 0;
            while (currentBgmAudioSource.volume < startVolume)
            {
                currentBgmAudioSource.volume += startVolume * Time.deltaTime / bgmFadeDuration;
                yield return null;
            }

            currentBgmAudioSource.volume = startVolume;
        }
        /// <summary>
        /// 효과음 재생하기 
        /// </summary>
        /// <param name="uid"></param>
        public void PlaySfxByUid(int uid)
        {
            if (soundSfxPoolDictionary.ContainsKey(uid))
            {
                GameObject soundObject = GetAvailableAudioSource(uid);
                if (soundObject != null)
                {
                    AudioSource audioSource = soundObject.GetComponent<AudioSource>();
                    soundObject.SetActive(true); // 활성화
                    audioSource.Play();
                    audioSource.volume = 1;
                    StartCoroutine(DeactivateAfterPlay(soundObject, audioSource.clip.length));
                }
                else
                {
                    GcLogger.LogWarning("사용 가능한 audio pool 이 없습니다. Uid: " + uid);
                }
            }
            else
            {
                GcLogger.LogWarning("Sfx pool 에서 찾을 수 없는 audio uid 입니다. Uid: " + uid);
            }
        }
        /// <summary>
        /// 재생이 끝난 sfx GameObject 를 비활성화 시켜준다 
        /// </summary>
        /// <param name="soundObject"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator DeactivateAfterPlay(GameObject soundObject, float delay)
        {
            yield return new WaitForSeconds(delay);
            soundObject.SetActive(false); // 사운드 재생 후 비활성화
            soundSfxPoolDictionary[int.Parse(soundObject.name)].Enqueue(soundObject); // 다시 풀에 추가
        }
        /// <summary>
        /// soundPoolDictionary 에서 재생 가능한 오디오 가져오기 
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        private GameObject GetAvailableAudioSource(int uid)
        {
            Queue<GameObject> pool = soundSfxPoolDictionary[uid];
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            return null; // 풀에 재생 가능한 오디오 소스가 없는 경우
        }
        /// <summary>
        /// 모든 사운드 on / off
        /// </summary>
        /// <param name="set"></param>
        public void MuteAllSound(bool set)
        {
            AudioListener.pause = set;
        }
        public void ChangeSoundVolumeBgm(float value)
        {
            if (currentBgmAudioSource == null) return;
            currentBgmAudioSource.volume = value;
        }
        public void ChangeSoundVolumeSfx(float value)
        {
            foreach (var uid in soundSfxPoolDictionary.Keys)
            {
                SetSfxVolume(uid, value);
            }
        }
        /// <summary>
        /// sfx 볼륨 조절하기
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="volume"></param>
        private void SetSfxVolume(int uid, float volume)
        {
            if (soundSfxPoolDictionary.TryGetValue(uid, out var value))
            {
                foreach (var audioSource in value.Select(soundObject => soundObject.GetComponent<AudioSource>()).Where(audioSource => audioSource != null))
                {
                    audioSource.volume = volume;
                }
            }
            else
            {
                GcLogger.LogWarning("Sfx pool 에서 찾을 수 없는 audio uid 입니다. Uid: " + uid);
            }
        }
    }
}
