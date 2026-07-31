using System;
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
        // 환경음 Audio Mixer. 비어 있으면 BGM 믹서 그룹을 사용합니다.
        public AudioMixerGroup ambientMixerGroup;

        private SoundControllerBgm _soundControllerBgm;
        private SoundControllerSfx _soundControllerSfx;
        private SoundControllerAmbient _soundControllerAmbient;
        private SoundPlaybackDebugReporter _playbackDebugReporter;
        private SoundResolver _soundResolver;

        private TableLoaderManager _tableLoaderManager;
        private AddressableLoaderSound _addressableLoaderSound;
        private int _bgmRequestVersion;
        private float _defaultBgmFadeDuration;
        private float _defaultAmbientFadeDuration;

        /// <summary>
        /// 사운드 컨트롤러와 테이블 기반 해석기를 초기화합니다.
        /// </summary>
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

            GGemCoSoundSettings soundSettings = AddressableLoaderSettings.Instance?.soundSettings;
            _defaultBgmFadeDuration = soundSettings != null
                ? soundSettings.GetDefaultBgmFadeDurationSeconds()
                : 0.7f;
            _defaultAmbientFadeDuration = soundSettings != null
                ? soundSettings.GetDefaultAmbientFadeDurationSeconds()
                : 0.7f;
            _playbackDebugReporter = new SoundPlaybackDebugReporter(soundSettings);

            _soundControllerBgm = new SoundControllerBgm(
                gameObject,
                mainAudioMixer,
                bgmMixerGroup,
                SoundConstants.NameExposedParameterBGM,
                _defaultBgmFadeDuration,
                _playbackDebugReporter);
            _soundControllerSfx = new SoundControllerSfx(
                transform,
                mainAudioMixer,
                sfxMixerGroup,
                SoundConstants.NameExposedParameterSfx,
                _addressableLoaderSound,
                _playbackDebugReporter);
            _soundControllerAmbient = new SoundControllerAmbient(
                gameObject,
                ambientMixerGroup,
                _addressableLoaderSound,
                _defaultAmbientFadeDuration,
                _playbackDebugReporter);
            if (ambientMixerGroup == null)
            {
                GcLogger.LogError(
                    "[SoundManager] Ambient 전용 AudioMixerGroup이 설정되지 않았습니다. 환경음 Fade 전환을 위해 전용 그룹을 연결해주세요.");
            }
            _soundResolver = new SoundResolver(_tableLoaderManager);

            ClickSoundEventDispatcher.OnClickDispatched += OnButtonClicked;
        }

        /// <summary>
        /// 모든 재생 컨트롤러와 이벤트 구독을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            _bgmRequestVersion++;
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
            PlayByUidInternal(uid, null, 0f, SoundPlaybackStopPolicy.Auto, 0f);
        }

        /// <summary>
        /// 사운드 UID 기반으로 재생하며, 요청 단위 재생 옵션을 일부 덮어씁니다.
        /// </summary>
        /// <param name="uid">외부 시스템이 사용하는 대표 sound UID입니다.</param>
        /// <param name="loopOverride">null이면 테이블의 Loop 값을 사용하고, 값이 있으면 해당 루프 여부를 사용합니다.</param>
        /// <param name="durationSeconds">루프 SFX를 자동 정리할 요청 지속 시간입니다. 0 이하이면 클립 길이를 사용합니다.</param>
        public void PlayByUid(int uid, bool? loopOverride, float durationSeconds = 0f)
        {
            PlayByUidInternal(uid, loopOverride, durationSeconds, SoundPlaybackStopPolicy.Auto, 0f);
        }

        /// <summary>
        /// 대표 sound UID가 SFX로 해석되는 경우에만 재생합니다.
        /// 잘못된 설정으로 BGM이나 환경음이 피격·UI 같은 SFX 경로에서 재생되는 것을 방지합니다.
        /// </summary>
        /// <param name="uid">재생할 sound 테이블의 대표 UID입니다.</param>
        /// <returns>재생 정지 핸들입니다. SFX로 재생하지 못하면 null을 반환합니다.</returns>
        public SoundPlaybackHandle PlaySfxByUid(int uid)
        {
            return PlayByUidInternal(
                uid,
                null,
                0f,
                SoundPlaybackStopPolicy.Auto,
                0f,
                sfxOnly: true);
        }

        /// <summary>
        /// SFX sound UID를 루프 재생하고 요청 단위 재생 속도 배율을 적용합니다.
        /// BGM/Ambient로 해석되는 UID는 이 메서드에서 재생하지 않으며, 반환된 핸들을 통해 호출자가 직접 정지해야 합니다.
        /// </summary>
        /// <param name="uid">재생할 sound 테이블의 대표 UID입니다.</param>
        /// <param name="pitchMultiplier">요청 단위 재생 속도 배율입니다. 0 이하이면 1로 보정합니다.</param>
        /// <returns>재생 정지 핸들입니다. SFX로 재생하지 못하면 null을 반환합니다.</returns>
        public SoundPlaybackHandle PlayLoopingSfxByUidWithPitchMultiplier(int uid, float pitchMultiplier)
        {
            return PlayByUidInternal(
                uid,
                true,
                0f,
                SoundPlaybackStopPolicy.ByHandle,
                0f,
                pitchMultiplier,
                sfxOnly: true);
        }

        /// <summary>
        /// 공용 사운드 재생 요청을 기반으로 사운드를 재생합니다.
        /// </summary>
        /// <param name="request">사운드 UID, 루프, 지속 시간 옵션을 담은 요청입니다.</param>
        /// <returns>정지 가능한 재생이면 핸들을 반환하고, 아니면 null을 반환합니다.</returns>
        public SoundPlaybackHandle Play(SoundPlayRequest request)
        {
            if (request == null || !request.IsValid)
                return null;

            return PlayByUidInternal(
                request.soundUid,
                request.ResolveLoopOverride(),
                request.ResolveDuration(),
                request.stopPolicy,
                0f);
        }

        /// <summary>
        /// 사운드 UID 기반 재생을 처리하고, 정지 가능한 재생이면 핸들을 반환합니다.
        /// </summary>
        /// <param name="uid">외부 시스템이 사용하는 대표 sound UID입니다.</param>
        /// <param name="loopOverride">null이면 테이블의 Loop 값을 사용하고, 값이 있으면 해당 루프 여부를 사용합니다.</param>
        /// <param name="durationSeconds">루프 SFX를 자동 정리할 요청 지속 시간입니다. 0 이하이면 클립 길이를 사용합니다.</param>
        /// <param name="stopPolicy">사운드 정지 정책입니다.</param>
        /// <param name="bgmFadeDurationOverride">BGM일 때 적용할 요청 단위 페이드 시간입니다.</param>
        /// <returns>SFX처럼 정지 가능한 재생이면 핸들, 아니면 null입니다.</returns>
        private SoundPlaybackHandle PlayByUidInternal(
            int uid,
            bool? loopOverride,
            float durationSeconds,
            SoundPlaybackStopPolicy stopPolicy,
            float bgmFadeDurationOverride,
            float sfxPitchMultiplier = 1f,
            bool sfxOnly = false)
        {
            if (!_tableLoaderManager || !_addressableLoaderSound || _soundResolver == null) return null;
            if (!TryResolveSound(uid, out ResolvedSound resolved)) return null;
            if (loopOverride.HasValue)
                resolved = resolved.WithLoop(loopOverride.Value);
            if (!resolved.ShouldPlay) return null;
            if (sfxOnly && resolved.Type != SoundConstants.Type.Sfx) return null;

            if (resolved.Type == SoundConstants.Type.Bgm)
            {
                int requestVersion = ++_bgmRequestVersion;
                PlayBgmAsync(
                    resolved,
                    requestVersion,
                    bgmFadeDurationOverride,
                    bgmFadeDurationOverride > 0f);
            }
            else if (resolved.Type == SoundConstants.Type.Ambient)
            {
                _soundControllerAmbient?.Play(resolved, this);
            }
            else if (resolved.Type == SoundConstants.Type.Sfx)
            {
                return _soundControllerSfx?.PlayWithHandle(
                    resolved,
                    this,
                    durationSeconds,
                    stopPolicy,
                    sfxPitchMultiplier);
            }

            return null;
        }

        /// <summary>
        /// 대표 sound UID를 실제 재생 대상 정보로 해석합니다.
        /// 커스텀 테스트 툴과 디버그 코드가 실제 게임 재생 경로와 같은 해석 결과를 확인할 때 사용합니다.
        /// </summary>
        /// <param name="uid">외부 시스템이 사용하는 대표 sound UID입니다.</param>
        /// <param name="resolved">해석된 최종 사운드 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다. 무음 후보도 성공 결과로 반환될 수 있습니다.</returns>
        public bool TryResolveSound(int uid, out ResolvedSound resolved)
        {
            resolved = default;
            if (!_tableLoaderManager || _soundResolver == null)
                return false;

            return _soundResolver.TryResolve(uid, out resolved);
        }

        /// <summary>
        /// BGM 클립의 재생 참조를 비동기로 획득한 뒤 최신 SoundManager가 유효할 때 재생합니다.
        /// SoundManager가 파괴된 뒤 로드가 완료되면 AudioSource에 연결하지 않고 참조를 즉시 해제합니다.
        /// </summary>
        /// <param name="resolved">해석된 BGM 재생 정보입니다.</param>
        /// <param name="requestVersion">최신 BGM 요청을 식별하는 버전입니다.</param>
        /// <param name="fadeDurationOverride">0보다 크면 리소스 FadeDuration보다 우선할 페이드 시간입니다.</param>
        /// <param name="useFadeDurationOverride">0초를 포함해 요청 값을 명시적으로 사용할지 여부입니다</param>
        private async void PlayBgmAsync(
            ResolvedSound resolved,
            int requestVersion,
            float fadeDurationOverride,
            bool useFadeDurationOverride)
        {
            if (!resolved.ShouldPlay || _addressableLoaderSound == null || string.IsNullOrWhiteSpace(resolved.FileName))
                return;

            string key = $"{ConfigAddressableKey.Sound}_{resolved.FileName}";
            SoundPlaybackLease lease = null;
            try
            {
                lease = await _addressableLoaderSound.AcquirePlaybackAsync(key);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning($"BGM 클립 로드 중 예외가 발생했습니다. key={key}, error={ex.Message}");
            }

            if (lease == null)
            {
                GcLogger.LogWarning($"BGM 클립을 로드하지 못했습니다. key={key}");
                return;
            }

            if (this == null || _soundControllerBgm == null || requestVersion != _bgmRequestVersion)
            {
                lease.Dispose();
                return;
            }

            // BGM은 사용자 BGM 볼륨과 별개로, 테이블별 Volume/VolumeScale 값을 배율로 적용합니다.
            float fadeDuration = useFadeDurationOverride
                ? fadeDurationOverride
                : resolved.UseFadeDurationOverride
                    ? resolved.FadeDuration
                    : _defaultBgmFadeDuration;
            _soundControllerBgm.PlayResolved(lease, this, resolved.Volume, fadeDuration, resolved);
        }

        /// <summary>
        /// 대표 sound UID를 BGM으로 재생하고 선택적으로 페이드 시간을 덮어씁니다.
        /// BGM 타입이 아닌 UID는 재생하지 않습니다.
        /// </summary>
        /// <param name="uid">재생할 대표 sound UID입니다.</param>
        /// <param name="fadeDurationOverride">0보다 크면 실제 BGM 리소스의 FadeDuration 대신 사용할 시간입니다.</param>
        public void PlayBgmByUid(int uid, float fadeDurationOverride = 0f)
        {
            PlayBgmByUid(uid, fadeDurationOverride, fadeDurationOverride > 0f);
        }

        /// <summary>
        /// 대표 sound UID를 BGM으로 재생하고 요청 단위 페이드 Override 사용 여부를 명시합니다.
        /// </summary>
        /// <param name="uid">재생할 대표 sound UID입니다.</param>
        /// <param name="fadeDurationOverride">요청 단위 페이드 시간입니다.</param>
        /// <param name="useFadeDurationOverride">0초를 포함해 요청 값을 명시적으로 사용할지 여부입니다.</param>
        public void PlayBgmByUid(
            int uid,
            float fadeDurationOverride,
            bool useFadeDurationOverride)
        {
            if (!TryResolveSound(uid, out ResolvedSound resolved) ||
                !resolved.ShouldPlay ||
                resolved.Type != SoundConstants.Type.Bgm)
            {
                GcLogger.LogWarning($"[SoundManager] BGM으로 해석할 수 없는 sound UID입니다. uid={uid}");
                return;
            }

            int requestVersion = ++_bgmRequestVersion;
            PlayBgmAsync(
                resolved,
                requestVersion,
                Mathf.Max(0f, fadeDurationOverride),
                useFadeDurationOverride);
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBgm()
        {
            _bgmRequestVersion++;
            _soundControllerBgm?.Stop(this);
        }

        /// <summary>
        /// 모든 환경음을 글로벌 기본 시간으로 Fade Out한 뒤 정지합니다.
        /// </summary>
        public void StopAmbient() => _soundControllerAmbient?.StopAll(this);

        /// <summary>
        /// 특정 Ambient 실제 리소스 UID의 재생을 정지합니다.
        /// 사운드 테스트 툴에서 개별 환경음 루프를 멈출 때 사용합니다.
        /// </summary>
        /// <param name="resourceUid">정지할 sound_ambient 실제 리소스 UID입니다.</param>
        public void StopAmbientByResourceUid(int resourceUid) => _soundControllerAmbient?.Stop(resourceUid, this);

        /// <summary>
        /// 다음 맵에서 필요한 환경음 목록으로 전환합니다.
        /// 공통 리소스는 유지하고 제거·추가되는 리소스만 Fade Out/In합니다.
        /// </summary>
        /// <param name="requests">대표 sound UID와 선택적 요청 단위 페이드 Override 목록입니다.</param>
        public void TransitionAmbient(IReadOnlyList<SoundFadeRequest> requests)
        {
            List<ResolvedSound> resolvedSounds = new List<ResolvedSound>(requests?.Count ?? 0);
            HashSet<int> registeredResourceUids = new HashSet<int>();
            if (requests != null)
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    SoundFadeRequest request = requests[i];
                    if (request.SoundUid <= 0 ||
                        !TryResolveSound(request.SoundUid, out ResolvedSound resolved) ||
                        !resolved.ShouldPlay ||
                        resolved.Type != SoundConstants.Type.Ambient ||
                        !registeredResourceUids.Add(resolved.ResourceUid))
                    {
                        continue;
                    }

                    float fadeDuration = request.UseFadeDurationOverride
                        ? request.FadeDurationOverride
                        : resolved.UseFadeDurationOverride
                            ? resolved.FadeDuration
                            : _defaultAmbientFadeDuration;
                    resolvedSounds.Add(resolved.WithFadeDuration(fadeDuration));
                }
            }

            _soundControllerAmbient?.TransitionTo(resolvedSounds, this);
        }

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
            _bgmRequestVersion++;
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
                    AddSfxResourceUidsForSound(sound, introSfxUids, registeredUids);
                }
            }

            GGemCoSoundSettings soundSettings = AddressableLoaderSettings.Instance?.soundSettings;
            IReadOnlyList<int> commonUiSoundUids = soundSettings?.GetCommonUiSoundUids() ?? Array.Empty<int>();
            for (int i = 0; i < commonUiSoundUids.Count; i++)
            {
                StruckTableSound sound = _tableLoaderManager.GetSoundData(commonUiSoundUids[i], false);
                if (sound is { Type: SoundConstants.Type.Sfx })
                    AddSfxResourceUidsForSound(sound, introSfxUids, registeredUids);
            }

            _soundControllerSfx?.InitializeSelective(_tableLoaderManager.TableSoundSfx, introSfxUids);
        }

        /// <summary>
        /// 대표 sound 행에서 실제 SFX 리소스 UID 후보를 수집합니다.
        /// Direct 연결, 활성 Variant 후보 및 폴백 리소스를 모두 포함합니다.
        /// </summary>
        /// <param name="sound">대표 sound 행입니다.</param>
        /// <param name="target">수집 결과 목록입니다.</param>
        /// <param name="registeredUids">중복 등록 방지용 UID 집합입니다.</param>
        private void AddSfxResourceUidsForSound(StruckTableSound sound, List<int> target, HashSet<int> registeredUids)
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

            if (sound.FallbackResourceUid > 0 &&
                _tableLoaderManager.TableSoundSfx?.GetDataByUid(sound.FallbackResourceUid) != null)
            {
                AddIntroSfxUid(target, registeredUids, sound.FallbackResourceUid);
            }
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
            _soundControllerSfx?.Initialize(_tableLoaderManager?.TableSoundSfx);
        }

        /// <summary>
        /// 게임 씬에서 사용하는 효과음 pool을 현재 sound_sfx 테이블 기준으로 다시 구성합니다.
        /// 커스텀 테스트 툴에서 SFX 리소스 Row를 수정한 뒤 MaxPlayCount/FileName/Volume 변경을 즉시 반영할 때 사용합니다.
        /// </summary>
        public void ReinitializeSoundSfxPool()
        {
            _soundControllerSfx?.Reinitialize(_tableLoaderManager?.TableSoundSfx);
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
