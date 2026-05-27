using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
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
        // 환경음 Audio Mixer. 비어 있으면 BGM 믹서 그룹을 사용합니다.
        public AudioMixerGroup ambientMixerGroup;

        // BGM 페이드 시간
        [SerializeField] private float bgmFadeDuration = 0.7f;

        private SoundControllerBgm _soundControllerBgm;
        private SoundControllerSfx _soundControllerSfx;
        private SoundControllerAmbient _soundControllerAmbient;
        private SoundResolver _soundResolver;

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
            _soundControllerAmbient = new SoundControllerAmbient(gameObject, ambientMixerGroup != null ? ambientMixerGroup : bgmMixerGroup, _addressableLoaderSound);
            _soundResolver = new SoundResolver(_tableLoaderManager);
            
            ClickSoundEventDispatcher.OnClickDispatched += OnButtonClicked;
        }

        private void OnDestroy()
        {
            _soundControllerBgm?.OnDestroy();
            _soundControllerSfx?.OnDestroy();
            _soundControllerAmbient?.OnDestroy();
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
        /// <param name="uid">외부 시스템이 사용하는 대표 sound UID입니다.</param>
        public void PlayByUid(int uid)
        {
            if (!_tableLoaderManager || !_addressableLoaderSound || _soundResolver == null) return;
            if (!_soundResolver.TryResolve(uid, out ResolvedSound resolved)) return;
            if (!resolved.ShouldPlay) return;

            if (resolved.Type == SoundConstants.Type.Bgm)
                StartCoroutine(PlayBgmRoutine(resolved));
            else if (resolved.Type == SoundConstants.Type.Ambient)
                _soundControllerAmbient?.Play(resolved, this);
            else if (resolved.Type == SoundConstants.Type.Sfx)
                _soundControllerSfx?.Play(resolved, this);
        }

        /// <summary>
        /// BGM 클립을 필요 시점에 비동기로 로드한 뒤 재생합니다.
        /// </summary>
        /// <param name="resolved">해석된 BGM 재생 정보입니다.</param>
        /// <returns>Unity 코루틴 실행자에 전달할 열거자입니다.</returns>
        private IEnumerator PlayBgmRoutine(ResolvedSound resolved)
        {
            if (!resolved.ShouldPlay || _addressableLoaderSound == null || string.IsNullOrWhiteSpace(resolved.FileName))
                yield break;

            string key = $"{ConfigAddressableKey.Sound}_{resolved.FileName}";
            Task<AudioClip> task = _addressableLoaderSound.LoadAudioClipAsync(key);
            while (!task.IsCompleted)
                yield return null;

            if (task.IsCanceled || task.IsFaulted || task.Result == null)
            {
                GcLogger.LogWarning($"BGM 클립을 로드하지 못했습니다. key={key}");
                yield break;
            }

            // BGM은 사용자 BGM 볼륨과 별개로, 테이블별 Volume/VolumeScale 값을 배율로 적용합니다.
            _soundControllerBgm?.Play(task.Result, this, resolved.Volume);
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBgm() => _soundControllerBgm?.Stop();

        /// <summary>
        /// Ambient 전체 정지
        /// </summary>
        public void StopAmbient() => _soundControllerAmbient?.StopAll();

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
            HashSet<int> registeredUids = new HashSet<int>();

            if (_tableLoaderManager.TableSoundSfx != null)
            {
                foreach (KeyValuePair<int, StruckTableSoundSfx> pair in _tableLoaderManager.TableSoundSfx.GetDatas())
                {
                    StruckTableSoundSfx info = pair.Value;
                    if (info is not { UseIntroScene: true }) continue;
                    AddIntroSfxUid(introSfxUids, registeredUids, info.Uid);
                }
            }

            if (_tableLoaderManager.TableSound != null)
            {
                foreach (KeyValuePair<int, StruckTableSound> pair in _tableLoaderManager.TableSound.GetDatas())
                {
                    StruckTableSound sound = pair.Value;
                    if (sound is not { UseIntroScene: true, Type: SoundConstants.Type.Sfx }) continue;
                    AddIntroSfxResourceUidsForSound(sound, introSfxUids, registeredUids);
                }
            }

            _soundControllerSfx?.InitializeSelective(_tableLoaderManager.TableSoundSfx, introSfxUids, _tableLoaderManager.TableSound);
        }

        /// <summary>
        /// 인트로 선로딩 대상 대표 sound 행에서 실제 SFX 리소스 UID 후보를 수집합니다.
        /// Direct 연결, Variant 후보, 레거시 FileName 행을 모두 고려해 부분 마이그레이션 중에도 누락을 줄입니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="target">수집 결과 목록입니다.</param>
        /// <param name="registeredUids">중복 등록 방지용 UID 집합입니다.</param>
        private void AddIntroSfxResourceUidsForSound(StruckTableSound sound, List<int> target, HashSet<int> registeredUids)
        {
            if (sound == null || target == null || registeredUids == null)
                return;

            StruckTableSoundSfx directResource = _tableLoaderManager.TableSoundSfx?.GetFirstBySoundUid(sound.Uid);
            if (directResource != null)
                AddIntroSfxUid(target, registeredUids, directResource.Uid);

            IReadOnlyList<StruckTableSoundVariant> variants = _tableLoaderManager.TableSoundVariant?.GetVariants(sound.Uid);
            if (variants != null)
            {
                for (int i = 0; i < variants.Count; i++)
                {
                    StruckTableSoundVariant variant = variants[i];
                    if (variant == null || !variant.Enabled || variant.CandidateResourceUid <= 0)
                        continue;

                    if (_tableLoaderManager.TableSoundSfx?.GetDataByUid(variant.CandidateResourceUid) == null)
                        continue;

                    AddIntroSfxUid(target, registeredUids, variant.CandidateResourceUid);
                }
            }

            if (sound.HasLegacyResource() && directResource == null)
                AddIntroSfxUid(target, registeredUids, sound.Uid);
        }

        /// <summary>
        /// 인트로 선로딩 대상 실제 SFX 리소스 UID를 중복 없이 추가합니다.
        /// </summary>
        /// <param name="target">수집 결과 목록입니다.</param>
        /// <param name="registeredUids">중복 등록 방지용 UID 집합입니다.</param>
        /// <param name="resourceUid">추가할 실제 SFX 리소스 UID입니다.</param>
        private static void AddIntroSfxUid(List<int> target, HashSet<int> registeredUids, int resourceUid)
        {
            if (target == null || registeredUids == null || resourceUid <= 0)
                return;

            if (registeredUids.Add(resourceUid))
                target.Add(resourceUid);
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
            _soundControllerSfx?.Initialize(_tableLoaderManager?.TableSoundSfx, _tableLoaderManager?.TableSound);
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
