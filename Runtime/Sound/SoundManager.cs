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
        // 메인 Audio Mixer
        public AudioMixer mainAudioMixer;
        // 메인 Audio Mixer
        public AudioMixerGroup bgmMixerGroup;
        // 메인 Audio Mixer
        public AudioMixerGroup sfxMixerGroup;

        // BGM 페이드 시간
        [SerializeField] private float bgmFadeDuration = 0.7f;

        private SoundControllerBgm _soundControllerBgm;
        private SoundControllerSfx _soundControllerSfx;

        private TableLoaderManager _tableLoaderManager;
        private AddressableLoaderSound _addressableLoaderSound;

        private void Awake()
        {
            if (mainAudioMixer == null)
            {
                enabled = false;
                return;
            }
            if (TableLoaderManager.Instance)
            {
                _tableLoaderManager = TableLoaderManager.Instance;
            }
            if (AddressableLoaderSound.Instance)
            {
                _addressableLoaderSound = AddressableLoaderSound.Instance;
            }

            _soundControllerBgm = new SoundControllerBgm(gameObject, mainAudioMixer, bgmMixerGroup, SoundConstants.NameExposedParameterBGM, bgmFadeDuration);
            _soundControllerSfx = new SoundControllerSfx(transform, mainAudioMixer, sfxMixerGroup, SoundConstants.NameExposedParameterSfx, _addressableLoaderSound);
            
            ClickSoundEventDispatcher.OnClickDispatched += OnButtonClicked;
        }

        private void OnDestroy()
        {
            _soundControllerBgm?.OnDestroy();
            _soundControllerSfx?.OnDestroy();
            // FadeAndSwitch 중지 처리
            StopAllCoroutines();
            ClickSoundEventDispatcher.OnClickDispatched -= OnButtonClicked;
        }
        /// <summary>
        /// 저장된 볼륨 적용
        /// </summary>
        private void Start()
        {
            SetMasterVolume(PlayerPrefsManager.LoadSoundVolumeMaster());
            _soundControllerBgm?.SetVolume(PlayerPrefsManager.LoadSoundVolumeBGM());
            _soundControllerSfx?.SetVolume(PlayerPrefsManager.LoadSoundVolumeSfx());
        }
        /// <summary>
        /// 사운드 UID 기반으로 재생
        /// </summary>
        /// <param name="uid"></param>
        public void PlayByUid(int uid)
        {
            if (!_tableLoaderManager) return;
            var info = _tableLoaderManager.GetSoundData(uid);
            if (info.Type == SoundConstants.Type.Bgm)
                _soundControllerBgm.Play(_addressableLoaderSound.GetAudioClip($"{ConfigAddressableKey.Sound}_{info.FileName}"), this);
            else if (info.Type == SoundConstants.Type.Sfx)
                _soundControllerSfx.Play(uid, this);
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBgm() => _soundControllerBgm?.Stop();

        /// <summary>
        /// BGM 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void SetBgmVolume(float value, bool save = true) => _soundControllerBgm?.SetVolume(value, save);
        
        /// <summary>
        /// SFX 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void SetSfxVolume(float value, bool save = true) => _soundControllerSfx?.SetVolume(value, save);

        /// <summary>
        /// 메인 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void SetMasterVolume(float value, bool save = true)
        {
            float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            mainAudioMixer?.SetFloat(SoundConstants.NameExposedParameterMaster, db);
            if (save)
            {
                PlayerPrefsManager.SaveSoundVolumeMaster(value);
            }
        }
        /// <summary>
        /// BGM 교체
        /// </summary>
        /// <param name="clip"></param>
        public void ChangeBackgroundMusic(AudioClip clip)
        {
            _soundControllerBgm?.Play(clip, this);
        }

        private void OnButtonClicked(IClickSoundEventTrigger source)
        {
            if (source is not ClickSoundEventBroadcaster broadcaster) return;
            // 1. 고유 ID 기반 사운드 재생 시도
            int uid = broadcaster.GetSoundId();
            if (uid > 0)
            {
                PlayByUid(uid);
            }
            // 2. 고유 ID가 없거나 찾지 못했을 경우 Enum 기준 조회
            else if (AddressableLoaderSettings.Instance != null) 
            {
                SoundConstants.UIButtonType buttonType = broadcaster.GetSoundType();
                uid = AddressableLoaderSettings.Instance.soundSettings.GetSoundButtonClickUid(buttonType);
                if (uid <= 0)
                {
                    // 디폴트 버튼 사운드 다시 찾기
                    uid = AddressableLoaderSettings.Instance.soundSettings.GetDefaultButtonClick();
                }
                PlayByUid(uid);
            }
        }
        /// <summary>
        /// 인트로 씬에서 필요한 리소스 pool 만들기
        /// </summary>
        public void InitializeSoundSfxPoolForIntro()
        {
            if (!_tableLoaderManager) return;
            List<int> introSfxUids = new List<int>();
            var datas = _tableLoaderManager.TableSound.GetDatas();
            foreach (var data in datas)
            {
                int uid = data.Key;
                var info = _tableLoaderManager.GetSoundData(uid);
                if (info is not { UseIntroScene: true }) continue;
                introSfxUids.Add(info.Uid);
            }
            _soundControllerSfx?.InitializeSelective(_tableLoaderManager.TableSound, introSfxUids);
        }
        /// <summary>
        /// Intro 씬 BGM 재생하기
        /// </summary>
        public void PlayBgmIntro()
        {
            if (!_tableLoaderManager) return;
            int uid = _tableLoaderManager.TableSound.GetBgmIntro();
            if (uid <= 0) return;
            PlayByUid(uid);
        }
        /// <summary>
        /// 게임 씬에서 사용하는 효과음 pool 초기화
        /// </summary>
        public void InitializeSoundSfxPool()
        {
            _soundControllerSfx?.Initialize(_tableLoaderManager?.TableSound);
        }
        /// <summary>
        /// 효과음 음소거 
        /// </summary>
        private void SetMuteSfx(bool set)
        {
            _soundControllerSfx?.Mute(set);
        }
    }
}
