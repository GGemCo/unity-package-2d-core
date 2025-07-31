using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo2DCore
{
    public class SoundManager : MonoBehaviour
    {
        public float bgmFadeDuration = 0.7f;
        
        private AudioSource audioSourceDefaultGameBgm;
        private AudioSource audioSourceBgm2;

        private AudioSource currentBgmAudioSource;
        private AudioSource nextBgmAudioSource;

        private AudioClip[] bgms;

        private readonly Dictionary<int, int> soundPlayCount = new Dictionary<int, int>(); // Uid별 현재 재생 중인 사운드의 개수
        private readonly Dictionary<int, int> maxConcurrentPlays = new Dictionary<int, int>(); // Uid별 최대 동시 재생 개수

        private readonly Dictionary<int, Queue<GameObject>> soundSfxPoolDictionary = new Dictionary<int, Queue<GameObject>>();
        private TableLoaderManager _tableLoaderManager;
        private AddressableLoaderSound _addressableLoaderSound;
        
        protected void Awake()
        {
            _tableLoaderManager = TableLoaderManager.Instance;
            _addressableLoaderSound = AddressableLoaderSound.Instance;
            // AudioSource 컴포넌트를 동적으로 추가
            audioSourceDefaultGameBgm = gameObject.AddComponent<AudioSource>();
            audioSourceBgm2 = gameObject.AddComponent<AudioSource>();
            InitializationBgm();
            InitializeSoundPools();
            InitializeSoundData();
        }
        /// <summary>
        /// 초기 배경음악 재생
        /// </summary>
        private void InitializationBgm() {
            // if (tableLoaderManager == null || UidSoundBgmDefault <= 0) return;
            // var info = tableLoaderManager.TableSound.GetSoundData(UidSoundBgmDefault);
            // if (info.AudioClip == null) return;
            // AudioSourceDefaultGameBgm.clip = info.AudioClip;
            // AudioSourceDefaultGameBgm.playOnAwake = true; // 게임 시작 시 자동 재생 비활성화
            // AudioSourceDefaultGameBgm.loop = true; // 반복 재생
            
            currentBgmAudioSource = audioSourceDefaultGameBgm;
            nextBgmAudioSource = audioSourceBgm2;
            
            // CurrentBgmAudioSource.Play();
            // CurrentBgmAudioSource.volume = SoundSettings.GetBGMVolume();
        }
        /// <summary>
        /// Sfx 사운드 pool 만들기 
        /// </summary>
        private void InitializeSoundPools()
        {
            if (_tableLoaderManager == null) return;
            // soundPlayCount 초기화
            Dictionary<int, Dictionary<string, string>> sounds = _tableLoaderManager.TableSound.GetDatas();

            TableSound tableSound = _tableLoaderManager.TableSound;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in sounds)
            {
                StruckTableSound info = tableSound.GetDataByUid(outerPair.Key);
                if (info.Type != SoundConstants.Type.Sfx) continue;
                Queue<GameObject> pool = new Queue<GameObject>();
                for (int i = 0; i < info.MaxPlayCount; i++)
                {
                    GameObject soundObject = CreateNewAudioSource(info);
                    soundObject .SetActive(false); // 비활성화 상태로 유지
                    pool.Enqueue(soundObject);
                }
                soundSfxPoolDictionary.TryAdd(outerPair.Key, pool);
            }
        }
        /// <summary>
        /// Sfx 사운드 pool 용 GameObject 만들기 
        /// </summary>
        private GameObject CreateNewAudioSource(StruckTableSound info)
        {
            GameObject soundObject = new GameObject($"{info.Uid}");
            soundObject.transform.SetParent(this.transform);
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            string key = $"{ConfigAddressableGroupName.Sound}_{info.FileName}";
            audioSource.clip = _addressableLoaderSound.GetAudioClip(key);
            audioSource.volume = info.Volume;
            return soundObject;
        }
        private void InitializeSoundData()
        {
            if (_tableLoaderManager == null) return;
            // soundPlayCount 초기화
            Dictionary<int, Dictionary<string, string>> sounds = _tableLoaderManager.TableSound.GetDatas();
            
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in sounds)
            {
                Dictionary<string, string> innerDictionary = outerPair.Value;
                soundPlayCount.TryAdd(outerPair.Key, 0);
                maxConcurrentPlays.TryAdd(outerPair.Key, int.Parse(innerDictionary["MaxPlayCount"]));
            }
        }
        /// <summary>
        /// 배경음악 교체하기
        /// </summary>
        /// <param name="newClip"></param>
        public void ChangeBackgroundMusic(AudioClip newClip)
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
            float startVolume = PlayerPrefsManager.LoadSoundVolumeBGM();
            while (startVolume > 0 && currentBgmAudioSource.volume > 0)
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
        private void PlaySfxByUid(int uid)
        {
            if (soundSfxPoolDictionary.ContainsKey(uid))
            {
                GameObject soundObject = GetAvailableAudioSource(uid);
                if (soundObject != null)
                {
                    AudioSource audioSource = soundObject.GetComponent<AudioSource>();
                    soundObject.SetActive(true); // 활성화
                    audioSource.Play();
                    audioSource.volume = PlayerPrefsManager.LoadSoundVolumeSfx();
                    StartCoroutine(DeactivateAfterPlay(soundObject, audioSource.clip.length));
                }
                else
                {
                    GcLogger.LogWarning("No available audio source in the pool for Uid: " + uid);
                }
            }
            else
            {
                GcLogger.LogWarning("Uid not found in the sound pool: " + uid);
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

        public void ChangeSoundVolumeBgm(float value, bool save = true)
        {
            if (currentBgmAudioSource == null) return;
            currentBgmAudioSource.volume = value;
            if (save)
            {
                PlayerPrefsManager.SaveSoundVolumeBGM(value);
            }
        }
        public void ChangeSoundVolumeSfx(float value, bool save = true)
        {
            foreach (var uid in soundSfxPoolDictionary.Keys)
            {
                SetSfxVolume(uid, value);
            }

            if (save)
            {
                PlayerPrefsManager.SaveSoundVolumeSfx(value);
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
                GcLogger.LogWarning("Uid not found in the sound pool: " + uid);
            }
        }

        public void PlayByUid(int uid)
        {
            if (uid <= 0) return;
            var info = _tableLoaderManager.TableSound.GetDataByUid(uid);
            if (info.Uid <= 0) return;

            if (info.Type == SoundConstants.Type.Bgm)
            {
                string key = $"{ConfigAddressables.KeySound}_{info.FileName}";
                ChangeBackgroundMusic(_addressableLoaderSound.GetAudioClip(key));
            }
            else if (info.Type == SoundConstants.Type.Sfx)
            {
                PlaySfxByUid(uid);
            }
        }

        public void StopBgm()
        {
            currentBgmAudioSource.Stop();
        }
    }
}