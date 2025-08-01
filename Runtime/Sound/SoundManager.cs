using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GGemCo2DCore
{
    /// <summary>
    /// 사운드 매니저
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header(ConfigCommon.TitleHeaderRequired)]
        public AudioMixer mainAudioMixer; // 메인 Audio Mixer
        public AudioMixerGroup bgmMixerGroup; // BGM 그룹
        public AudioMixerGroup sfxMixerGroup; // SFX 그룹

        public float bgmFadeDuration = 0.7f; // BGM 페이드 시간

        private AudioSource audioSourceDefaultGameBgm;
        private AudioSource audioSourceBgm2;

        private AudioSource currentBgmAudioSource;
        private AudioSource nextBgmAudioSource;

        private readonly Dictionary<int, int> soundPlayCount = new(); // 현재 재생 중인 사운드 개수
        private readonly Dictionary<int, int> maxConcurrentPlays = new(); // 최대 동시 재생 개수
        private readonly Dictionary<int, Queue<GameObject>> soundSfxPoolDictionary = new(); // SFX 풀

        private TableLoaderManager _tableLoaderManager;
        private AddressableLoaderSound _addressableLoaderSound;

        // 볼륨을 dB로 변환 (0.0001 이상만 허용)
        private const float CalculateDecibelMinVolume = 0.0001f;
        private const float CalculateDecibelParam1 = 20f;

        // 초기화
        private void Awake()
        {
            _tableLoaderManager = TableLoaderManager.Instance;
            _addressableLoaderSound = AddressableLoaderSound.Instance;

            // AudioSource 생성 및 Mixer Group 설정
            audioSourceDefaultGameBgm = gameObject.AddComponent<AudioSource>();
            audioSourceDefaultGameBgm.outputAudioMixerGroup = bgmMixerGroup;

            audioSourceBgm2 = gameObject.AddComponent<AudioSource>();
            audioSourceBgm2.outputAudioMixerGroup = bgmMixerGroup;

            InitializationBgm();
            InitializeSoundPools();
            InitializeSoundData();
        }

        private void Start()
        {
            // audioMixer 가 초기화 되고 호출되도록 Start 에서 호출
            ApplySavedVolumes();
        }
        /// <summary>
        /// 저장된 볼륨 적용
        /// </summary>
        private void ApplySavedVolumes()
        {
            ChangeSoundVolumeBgm(PlayerPrefsManager.LoadSoundVolumeBGM(), false);
            ChangeSoundVolumeSfx(PlayerPrefsManager.LoadSoundVolumeSfx(), false);
            ChangeSoundVolumeMaster(PlayerPrefsManager.LoadSoundVolumeMaster(), false);
        }

        /// <summary>
        /// BGM 초기 설정
        /// </summary>
        private void InitializationBgm()
        {
            currentBgmAudioSource = audioSourceDefaultGameBgm;
            nextBgmAudioSource = audioSourceBgm2;
        }

        /// <summary>
        /// SFX 풀 초기화
        /// </summary>
        private void InitializeSoundPools()
        {
            if (_tableLoaderManager == null) return;

            var sounds = _tableLoaderManager.TableSound.GetDatas();
            TableSound tableSound = _tableLoaderManager.TableSound;

            foreach (var pair in sounds)
            {
                var info = tableSound.GetDataByUid(pair.Key);
                if (info.Type != SoundConstants.Type.Sfx) continue;

                Queue<GameObject> pool = new();
                for (int i = 0; i < info.MaxPlayCount; i++)
                {
                    GameObject soundObject = CreateNewAudioSource(info);
                    soundObject.SetActive(false);
                    pool.Enqueue(soundObject);
                }
                soundSfxPoolDictionary.TryAdd(pair.Key, pool);
            }
        }

        /// <summary>
        /// AudioSource 오브젝트 생성
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private GameObject CreateNewAudioSource(StruckTableSound info)
        {
            GameObject soundObject = new GameObject($"{info.Uid}");
            soundObject.transform.SetParent(transform);

            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
            audioSource.volume = info.Volume;

            string key = $"{ConfigAddressableGroupName.Sound}_{info.FileName}";
            audioSource.clip = _addressableLoaderSound.GetAudioClip(key);
            return soundObject;
        }

        /// <summary>
        /// 사운드 데이터 초기화
        /// </summary>
        private void InitializeSoundData()
        {
            if (_tableLoaderManager == null) return;
            var sounds = _tableLoaderManager.TableSound.GetDatas();

            foreach (var pair in sounds)
            {
                var inner = pair.Value;
                soundPlayCount.TryAdd(pair.Key, 0);
                maxConcurrentPlays.TryAdd(pair.Key, int.Parse(inner["MaxPlayCount"]));
            }
        }

        /// <summary>
        /// BGM 교체
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
        /// BGM 페이드 아웃 후 페이드 인
        /// </summary>
        /// <param name="newClip"></param>
        /// <returns></returns>
        private IEnumerator BgmFadeOutAndIn(AudioClip newClip)
        {
            float savedVolume = PlayerPrefsManager.LoadSoundVolumeBGM();
            float dbTarget = Mathf.Log10(Mathf.Max(savedVolume, CalculateDecibelMinVolume)) * CalculateDecibelParam1;

            mainAudioMixer.GetFloat(SoundConstants.NameExposedParameterBGM, out float currentDb);
            float currentVolume = Mathf.Pow(10f, currentDb / CalculateDecibelParam1);

            float t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(currentVolume, 0f, t / bgmFadeDuration);
                mainAudioMixer.SetFloat(SoundConstants.NameExposedParameterBGM, Mathf.Log10(Mathf.Max(v, CalculateDecibelMinVolume)) * CalculateDecibelParam1);
                yield return null;
            }

            currentBgmAudioSource.Stop();
            (currentBgmAudioSource, nextBgmAudioSource) = (nextBgmAudioSource, currentBgmAudioSource);

            currentBgmAudioSource.clip = newClip;
            currentBgmAudioSource.loop = true;
            currentBgmAudioSource.Play();

            t = 0f;
            while (t < bgmFadeDuration)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(0f, savedVolume, t / bgmFadeDuration);
                mainAudioMixer.SetFloat(SoundConstants.NameExposedParameterBGM, Mathf.Log10(Mathf.Max(v, CalculateDecibelMinVolume)) * CalculateDecibelParam1);
                yield return null;
            }

            mainAudioMixer.SetFloat(SoundConstants.NameExposedParameterBGM, dbTarget);
        }

        /// <summary>
        /// SFX 재생
        /// </summary>
        /// <param name="uid"></param>
        private void PlaySfxByUid(int uid)
        {
            if (!soundSfxPoolDictionary.ContainsKey(uid)) return;

            // 최대 동시 재생 수 초과 시 무시
            if (soundPlayCount.TryGetValue(uid, out var currentCount) &&
                maxConcurrentPlays.TryGetValue(uid, out var maxCount) &&
                currentCount >= maxCount)
            {
                // 제한 초과: 무시 또는 로그
                GcLogger.LogWarning($"SFX UID:{uid} 최대 동시 재생 {maxCount}개 초과");
                return;
            }

            GameObject soundObject = GetAvailableAudioSource(uid);
            if (soundObject == null)
            {
                GcLogger.LogWarning($"No available audio source in the pool for Uid: {uid}");
                return;
            }

            // 재생 카운트 증가
            soundPlayCount[uid]++;

            AudioSource audioSource = soundObject.GetComponent<AudioSource>();
            soundObject.SetActive(true);
            audioSource.Play();

            StartCoroutine(DeactivateAfterPlay(soundObject, audioSource.clip.length, uid));
        }

        /// <summary>
        /// SFX 재생 후 비활성화
        /// </summary>
        /// <param name="soundObject"></param>
        /// <param name="delay"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        private IEnumerator DeactivateAfterPlay(GameObject soundObject, float delay, int uid)
        {
            yield return new WaitForSeconds(delay);
            soundObject.SetActive(false);
            soundSfxPoolDictionary[uid].Enqueue(soundObject);

            // 재생 종료 → 카운트 감소
            if (soundPlayCount.ContainsKey(uid))
                soundPlayCount[uid] = Mathf.Max(0, soundPlayCount[uid] - 1);
        }
        
        /// <summary>
        /// 사용 가능한 AudioSource 반환
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        private GameObject GetAvailableAudioSource(int uid)
        {
            Queue<GameObject> pool = soundSfxPoolDictionary[uid];
            return pool.Count > 0 ? pool.Dequeue() : null;
        }

        /// <summary>
        /// 전체 사운드 일시정지
        /// </summary>
        /// <param name="set"></param>
        public void MuteAllSound(bool set)
        {
            AudioListener.pause = set;
        }

        public void ChangeSoundVolumeMaster(float value, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(value, CalculateDecibelMinVolume)) * CalculateDecibelParam1;
            mainAudioMixer.SetFloat(SoundConstants.NameExposedParameterMaster, db);

            if (save)
                PlayerPrefsManager.SaveSoundVolumeMaster(value);
        }
        /// <summary>
        /// BGM 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void ChangeSoundVolumeBgm(float value, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(value, CalculateDecibelMinVolume)) * CalculateDecibelParam1;
            mainAudioMixer.SetFloat(SoundConstants.NameExposedParameterBGM, db);

            if (save)
                PlayerPrefsManager.SaveSoundVolumeBGM(value);
        }

        /// <summary>
        /// SFX 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void ChangeSoundVolumeSfx(float value, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(value, CalculateDecibelMinVolume)) * CalculateDecibelParam1;
            mainAudioMixer.SetFloat(SoundConstants.NameExposedParameterSfx, db);

            if (save)
                PlayerPrefsManager.SaveSoundVolumeSfx(value);
        }

        /// <summary>
        /// 사운드 UID 기반으로 재생
        /// </summary>
        /// <param name="uid"></param>
        public void PlayByUid(int uid)
        {
            if (uid <= 0) return;
            var info = _tableLoaderManager.TableSound.GetDataByUid(uid);
            if (info.Uid <= 0) return;

            string key = $"{ConfigAddressables.KeySound}_{info.FileName}";

            if (info.Type == SoundConstants.Type.Bgm)
                ChangeBackgroundMusic(_addressableLoaderSound.GetAudioClip(key));
            else if (info.Type == SoundConstants.Type.Sfx)
                PlaySfxByUid(uid);
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBgm()
        {
            currentBgmAudioSource.Stop();
        }
    }
}
