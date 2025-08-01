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

        private BgmSoundController _bgm;
        private SfxSoundController _sfx;

        private TableLoaderManager _tableLoaderManager;
        private AddressableLoaderSound _addressableLoaderSound;

        private void Awake()
        {
            if (TableLoaderManager.Instance)
            {
                _tableLoaderManager = TableLoaderManager.Instance;
            }
            if (AddressableLoaderSound.Instance)
            {
                _addressableLoaderSound = AddressableLoaderSound.Instance;
            }

            _bgm = new BgmSoundController(gameObject, mainAudioMixer, bgmMixerGroup, SoundConstants.NameExposedParameterBGM, bgmFadeDuration);
            _sfx = new SfxSoundController(transform, mainAudioMixer, sfxMixerGroup, SoundConstants.NameExposedParameterSfx, _addressableLoaderSound);
            _sfx?.Initialize(_tableLoaderManager?.TableSound);
        }

        /// <summary>
        /// 저장된 볼륨 적용
        /// </summary>
        private void Start()
        {
            SetMasterVolume(PlayerPrefsManager.LoadSoundVolumeMaster());
            _bgm?.SetVolume(PlayerPrefsManager.LoadSoundVolumeBGM());
            _sfx?.SetVolume(PlayerPrefsManager.LoadSoundVolumeSfx());
        }

        /// <summary>
        /// 사운드 UID 기반으로 재생
        /// </summary>
        /// <param name="uid"></param>
        public void PlayByUid(int uid)
        {
            if (!_tableLoaderManager) return;
            var info = _tableLoaderManager.TableSound.GetDataByUid(uid);
            if (info.Type == SoundConstants.Type.Bgm)
                _bgm.Play(_addressableLoaderSound.GetAudioClip($"{ConfigAddressables.KeySound}_{info.FileName}"), this);
            else if (info.Type == SoundConstants.Type.Sfx)
                _sfx.Play(uid, this);
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBgm() => _bgm?.Stop();

        /// <summary>
        /// BGM 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void SetBgmVolume(float value, bool save = true) => _bgm?.SetVolume(value, save);
        
        /// <summary>
        /// SFX 볼륨 변경
        /// </summary>
        /// <param name="value"></param>
        /// <param name="save"></param>
        public void SetSfxVolume(float value, bool save = true) => _sfx?.SetVolume(value, save);

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
            _bgm?.Play(clip, this);
        }
    }
}
