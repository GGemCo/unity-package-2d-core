using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxBehaviourBase : MonoBehaviour
    {
        private CharacterBase _character;
        protected CharacterBase TargetCharacter;
        private float _duration;
        protected Vector3 Direction;
        private float _originalScaleX;
        private Vector3 _defaultLocalScale;
        private Quaternion _defaultLocalRotation;
        private bool _hasDefaultTransform;
        private CharacterBase _followCharacter;
        private VfxConstants.FollowMode _followMode;
        private VfxConstants.FollowAnchorMode _followAnchorMode;
        private bool _hasFollowSpawnOffset;
        private Vector3 _followSpawnOffset;
        private Vector3 _positionOffset;
        private float _positionY;
        private ConfigCommon.PositionYType _positionYType;
        protected Coroutine CoroutineTickTimeDamage;
        private VfxRuntimeData _runtimeData;
        private VfxSpawnPolicy _spawnPolicy;

        private bool _releaseOnAnimationComplete;
        private int _poolKey;
        private Action<int, GameObject> _releaseAction;
        private Coroutine _fadeCoroutine;
        private VfxFadeController _fadeController;
        private bool _isReleasing;
        private bool _useTimelineFade;
        private float _lifetimeElapsed;
        private bool _timelineDurationElapsedHandled;

        private float _defaultVfxFadeInSec;
        private float _defaultVfxFadeOutSec;
        private float _vfxFadeInSec;
        private float _vfxFadeOutSec;
        private Easing.EaseType _vfxFadeInEase;
        private Easing.EaseType _vfxFadeOutEase;
        private bool _disableFadeIn;
        
        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnVfxDestroy;

        protected VfxRuntimeData RuntimeData => _runtimeData;
        protected VfxEffectRuntimeData EffectRuntimeData => _runtimeData as VfxEffectRuntimeData;
        protected VfxParticleRuntimeData ParticleRuntimeData => _runtimeData as VfxParticleRuntimeData;
        protected VfxSpawnPolicy SpawnPolicy => _spawnPolicy;
        protected virtual bool UseTimelineFadeOutAlpha => true;

        /// <summary>
        /// 현재 VFX 런타임 UID입니다.
        /// </summary>
        internal int RuntimeUid => _runtimeData?.Uid ?? 0;

        /// <summary>
        /// 현재 생성 요청에 적용된 VFX 수명주기 정책입니다.
        /// </summary>
        internal VfxConstants.LifecycleType RuntimeLifecycleType =>
            _spawnPolicy?.LifecycleType ?? VfxConstants.LifecycleType.AutoRelease;

        protected virtual void Awake()
        {
            CaptureDefaultTransformIfNeeded();

            _defaultVfxFadeInSec = AddressableLoaderSettings.Instance.settings.vfxFadeInSec;
            _defaultVfxFadeOutSec = AddressableLoaderSettings.Instance.settings.vfxFadeOutSec;
            _vfxFadeInEase = AddressableLoaderSettings.Instance.settings.vfxFadeInEase;
            _vfxFadeOutEase = AddressableLoaderSettings.Instance.settings.vfxFadeOutEase;
            ApplyFadeDurationPolicy();
        }

        /// <summary>
        /// VFX 런타임 데이터와 풀 반환 정보를 초기화합니다.
        /// </summary>
        /// <param name="runtimeData">VFX 테이블에서 해석한 런타임 데이터입니다.</param>
        /// <param name="spawnPolicy">이번 생성 요청에 적용할 생성 정책입니다.</param>
        /// <param name="releaseAction">수명 종료 시 풀에 반환하기 위한 콜백입니다.</param>
        /// <param name="poolKeyOverride">동일 VfxUid를 다른 Behaviour 정책으로 분리해 풀링할 때 사용하는 키입니다.</param>
        public virtual void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, Action<int, GameObject> releaseAction = null, int poolKeyOverride = 0)
        {
            _runtimeData = runtimeData;
            _spawnPolicy = spawnPolicy ?? runtimeData?.DefaultSpawnPolicy?.Clone() ?? new VfxSpawnPolicy();
            _poolKey = poolKeyOverride != 0 ? poolKeyOverride : runtimeData?.Uid ?? 0;
            _releaseAction = releaseAction;
            _character = null;
            TargetCharacter = null;
            _followCharacter = null;
            _followMode = _spawnPolicy.FollowMode;
            _followAnchorMode = _spawnPolicy.FollowAnchorMode;
            _hasFollowSpawnOffset = false;
            _followSpawnOffset = Vector3.zero;
            _positionOffset = Vector3.zero;
            _positionY = 0f;
            _positionYType = ConfigCommon.PositionYType.None;
            _duration = 0f;
            _disableFadeIn = false;
            ApplyFadeDurationPolicy();
            _releaseOnAnimationComplete = false;
            _isReleasing = false;
            _lifetimeElapsed = 0f;
            _timelineDurationElapsedHandled = false;
            _useTimelineFade = ShouldUseTimelineFade();
            RestoreDefaultTransform();
            EnsureFadeController();
            RestoreVisibleState();
        }

        protected virtual void OnEnable()
        {
            if (_runtimeData == null)
                return;

            _releaseOnAnimationComplete = false;
            _isReleasing = false;
            _lifetimeElapsed = 0f;
            _timelineDurationElapsedHandled = false;
            _useTimelineFade = ShouldUseTimelineFade();
            StopManagedCoroutines();
            PrepareSpawnFadeState();
            PlayOnSpawn();

            if (!_useTimelineFade)
                StartFadeInIfNeeded();
        }

        protected virtual void OnDisable()
        {
            StopManagedCoroutines();
            RestoreVisibleState();
            _releaseOnAnimationComplete = false;
            _isReleasing = false;
            _lifetimeElapsed = 0f;
            _timelineDurationElapsedHandled = false;
            _useTimelineFade = false;
        }

        protected virtual void PlayOnSpawn()
        {
            StartLifecycleTimerIfNeeded();
        }

        protected IEnumerator RemoveEffectDuration(float duration)
        {
            if (duration <= 0f)
            {
                PlayEndAnimation();
                yield break;
            }

            if (UseUnscaledTime())
                yield return new WaitForSecondsRealtime(duration);
            else
                yield return new WaitForSeconds(duration);

            if (_fadeController != null)
                _fadeController.SetAlpha(0f);

            ReleaseImmediateInternal();
        }

        protected void StartLifecycleTimerIfNeeded()
        {
            if (_spawnPolicy == null || _useTimelineFade)
                return;

            if ((_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.Duration && _duration > 0f) ||
                (_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.AutoRelease && _duration > 0f))
            {
                StartCoroutine(RemoveEffectDuration(_duration));
            }
        }

        protected float GetPlaybackDuration()
        {
            if (_spawnPolicy == null)
                return _duration;

            return _spawnPolicy.LifecycleType == VfxConstants.LifecycleType.ManualRelease
                ? -1f
                : _duration;
        }

        protected bool ShouldAutoReleaseOnNaturalComplete()
        {
            if (_spawnPolicy == null)
                return true;

            return _spawnPolicy.LifecycleType == VfxConstants.LifecycleType.AutoRelease
                || (_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.Duration && _duration <= 0f);
        }

        protected bool UseUnscaledTime()
        {
            return _runtimeData != null && _runtimeData.UseUnscaledTime;
        }

        public void SetDuration(float duration)
        {
            _duration = duration;
            _useTimelineFade = ShouldUseTimelineFade();
        }

        /// <summary>
        /// 이번 생성 요청에서 Fade-in을 생략할지 설정합니다.
        /// - 풀에서 재사용되는 인스턴스는 <see cref="Initialize"/>에서 기본 정책으로 복구된 뒤 요청별 값을 다시 적용합니다.
        /// </summary>
        /// <param name="disable">true이면 생성 alpha를 0으로 낮추지 않고 원본 alpha를 유지합니다.</param>
        public void SetFadeInDisabled(bool disable)
        {
            _disableFadeIn = disable;
            ApplyFadeDurationPolicy();

            // 비활성 상태에서 다음 OnEnable을 기다리는 풀 인스턴스도 원본 alpha로 확실하게 복구합니다.
            if (_disableFadeIn)
                RestoreVisibleState();
        }

        public virtual void SetForceOneShot(bool forceOneShot)
        {
        }

        public virtual void DestroyForce()
        {
            if (this == null)
                return;

            var go = gameObject;
            if (go == null)
                return;

            if (!go.activeInHierarchy || !isActiveAndEnabled)
            {
                ReleaseImmediateInternal();
                return;
            }

            BeginReleaseSequence();
        }

        /// <summary>
        /// VFX 인스턴스의 기준 스케일을 외부 생성 요청 값으로 덮어씁니다.
        /// </summary>
        /// <param name="scale">적용할 균일 스케일 값입니다.</param>
        public void SetScale(float scale)
        {
            if (scale <= 0f)
                return;

            transform.localScale = new Vector3(scale, scale, transform.localScale.z);
            _originalScaleX = transform.localScale.x;
        }

        private void SetDirection(float dirX)
        {
            transform.localScale = new Vector3(_originalScaleX * dirX, transform.localScale.y, transform.localScale.z);
        }

        public void SetFlip(bool shouldFlip)
        {
            float dirX = shouldFlip ? -1f : 1f;
            SetDirection(dirX);
            OnSetFlip(dirX);
        }

        protected virtual void OnSetFlip(float dirX)
        {
        }

        /// <summary>
        /// 풀에서 처음 생성된 Transform 값을 기준값으로 저장합니다.
        /// </summary>
        private void CaptureDefaultTransformIfNeeded()
        {
            if (_hasDefaultTransform)
                return;

            _defaultLocalScale = transform.localScale;
            _defaultLocalRotation = transform.localRotation;
            _originalScaleX = _defaultLocalScale.x;
            _hasDefaultTransform = true;
        }

        /// <summary>
        /// 풀에서 재사용되는 VFX에 남아 있을 수 있는 이전 Flip/Rotation 상태를 기본값으로 복구합니다.
        /// </summary>
        private void RestoreDefaultTransform()
        {
            CaptureDefaultTransformIfNeeded();
            transform.localScale = _defaultLocalScale;
            transform.localRotation = _defaultLocalRotation;
            _originalScaleX = transform.localScale.x;
        }

        public virtual void OnEndAnimationComplete()
        {
            // VfxLifecycleDiagnostics.Log(
            //     gameObject,
            //     "EndComplete",
            //     $"releaseOnAnimationComplete={_releaseOnAnimationComplete}, " +
            //     $"lifecycle={RuntimeLifecycleType}, " +
            //     $"shouldAutoRelease={ShouldAutoReleaseOnNaturalComplete()}");

            if (!_releaseOnAnimationComplete && !ShouldAutoReleaseOnNaturalComplete())
                return;

            _releaseOnAnimationComplete = false;
            BeginReleaseSequence();
        }

        protected void ReleaseNow()
        {
            _releaseOnAnimationComplete = false;
            BeginReleaseSequence();
        }

        protected void BeginReleaseOnAnimationComplete()
        {
            _releaseOnAnimationComplete = true;
        }

        protected void BeginReleaseSequence()
        {
            if (_isReleasing)
                return;

            _isReleasing = true;
            StopManagedCoroutines();

            if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            {
                ReleaseImmediateInternal();
                return;
            }

            float fadeOutDuration = _vfxFadeOutSec;
            if (fadeOutDuration <= 0f || _fadeController == null)
            {
                ReleaseImmediateInternal();
                return;
            }

            _fadeCoroutine = StartCoroutine(
                FadeAndReleaseRoutine(GetCurrentAlpha(), 0f, fadeOutDuration, _vfxFadeOutEase));
        }

        private IEnumerator FadeAndReleaseRoutine(float startAlpha, float endAlpha, float duration, Easing.EaseType easeType)
        {
            if (_fadeController == null)
            {
                ReleaseImmediateInternal();
                yield break;
            }

            if (duration <= 0f)
            {
                _fadeController.SetAlpha(endAlpha);
                ReleaseImmediateInternal();
                yield break;
            }

            float elapsed = 0f;
            _fadeController.SetAlpha(startAlpha);

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Clamp01(Easing.Apply(t, easeType));
                float alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                _fadeController.SetAlpha(alpha);
                yield return null;
            }

            _fadeController.SetAlpha(endAlpha);
            ReleaseImmediateInternal();
        }

        private void ReleaseImmediateInternal()
        {
            _isReleasing = true;
            StopManagedCoroutines();
            var callback = OnVfxDestroy;
            OnVfxDestroy = null;

            // VfxLifecycleDiagnostics.Log(
            //     gameObject,
            //     "ReleaseImmediate",
            //     $"poolKey={_poolKey}, releaseActionNull={_releaseAction == null}, " +
            //     $"hasDestroySubscriber={callback != null}");

            callback?.Invoke();
            ReleaseOrDestroy();
        }

        /// <summary>
        /// 풀 반환 키가 있으면 풀로 돌려보내고, 없으면 GameObject를 제거합니다.
        /// </summary>
        private void ReleaseOrDestroy()
        {
            if (_releaseAction != null && _poolKey != 0)
            {
                _releaseAction.Invoke(_poolKey, gameObject);
                return;
            }

            Destroy(gameObject);
        }

        public virtual bool TryPlayEndAnimation(DelegateEffectDestroy onEffectDestroy = null)
        {
            if (onEffectDestroy != null)
                OnVfxDestroy += onEffectDestroy;

            return false;
        }

        public virtual void PlayEndAnimation()
        {
            BeginReleaseOnAnimationComplete();
            BeginReleaseSequence();
        }

        public virtual void SetColor(string color)
        {
        }

        /// <summary>
        /// VFX 렌더러의 Sorting Layer를 외부 생성 요청 기준으로 덮어씁니다.
        /// </summary>
        /// <param name="sortingLayer">적용할 Sorting Layer 키입니다.</param>
        public virtual void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
        }

        /// <summary>
        /// VFX 렌더러의 Sorting Order를 외부 생성 요청 기준으로 덮어씁니다.
        /// </summary>
        /// <param name="sortingOrder">적용할 Sorting Order 값입니다.</param>
        public virtual void SetSortingOrder(int sortingOrder)
        {
        }

        /// <summary>
        /// 지정된 방향을 기준으로 VFX의 좌우 반전과 회전 보정을 적용합니다.
        /// </summary>
        /// <param name="direction">VFX가 바라볼 주 방향입니다.</param>
        /// <param name="sourceDirection">주 방향의 X축이 불명확할 때 사용할 보조 방향입니다.</param>
        /// <param name="applyDefaultDirectionFlip">true이면 DefaultDirection 기준 좌우 반전을 적용합니다.</param>
        /// <param name="applyRotation">true이면 NeedRotation 기준 회전 보정을 적용합니다.</param>
        public virtual void ApplyDirectionVisual(
            Vector2 direction,
            Vector2 sourceDirection,
            bool applyDefaultDirectionFlip = true,
            bool applyRotation = true)
        {
        }

        public virtual void SetRotation(Vector2 directionByTarget, Vector2 sourceDirection)
        {
            ApplyDirectionVisual(directionByTarget, sourceDirection, false, true);
        }

        /// <summary>
        /// VFX가 따라갈 캐릭터와 Follow 위치 기준 정책을 설정합니다.
        /// </summary>
        /// <param name="character">따라갈 캐릭터입니다.</param>
        /// <param name="followMode">위치/Flip 갱신 방식입니다.</param>
        /// <param name="followAnchorMode">Follow 위치를 계산할 기준점 정책입니다.</param>
        public void SetFollowCharacter(
            CharacterBase character,
            VfxConstants.FollowMode followMode = VfxConstants.FollowMode.Position,
            VfxConstants.FollowAnchorMode followAnchorMode = VfxConstants.FollowAnchorMode.FollowTargetOrigin)
        {
            _followCharacter = character;
            _followMode = followMode;
            _followAnchorMode = followAnchorMode;
            _hasFollowSpawnOffset = false;
            _followSpawnOffset = Vector3.zero;
        }

        /// <summary>
        /// VFX의 추가 위치 오프셋(World 기준)을 설정합니다.
        /// </summary>
        /// <param name="offset">적용할 위치 오프셋입니다.</param>
        public void SetPositionOffset(Vector3 offset) => _positionOffset = offset;
        public void SetPositionY(float y) => _positionY = y;
        public void SetPositionYType(ConfigCommon.PositionYType type) => _positionYType = type;

        /// <summary>
        /// 기준 월드 위치에 VFX Y 오프셋 정책을 반영한 최종 위치를 계산합니다.
        /// </summary>
        /// <param name="basePosition">오프셋을 적용하기 전 기준 월드 위치입니다.</param>
        /// <param name="heightOwner">캐릭터 높이 보정 기준입니다. null이면 Follow 대상 또는 생성 캐릭터를 사용합니다.</param>
        /// <returns>생성 요청의 Y 오프셋과 높이 보정이 적용된 최종 월드 위치입니다.</returns>
        public Vector3 ResolveSpawnPosition(Vector3 basePosition, CharacterBase heightOwner = null)
        {
            Vector3 result = basePosition + _positionOffset;

            if (Mathf.Abs(_positionY) > Mathf.Epsilon)
                result += new Vector3(0f, _positionY, 0f);

            if (_positionYType == ConfigCommon.PositionYType.CharacterHeight)
            {
                CharacterBase resolvedHeightOwner = heightOwner != null
                    ? heightOwner
                    : (_followCharacter != null ? _followCharacter : _character);

                if (resolvedHeightOwner != null)
                    result += new Vector3(0f, resolvedHeightOwner.GetHeightByScale(), 0f);
            }

            return result;
        }

        /// <summary>
        /// VFX가 활성화되기 전에 기준 위치와 생성 옵션을 즉시 반영합니다.
        /// </summary>
        /// <param name="basePosition">오프셋을 적용하기 전 기준 월드 위치입니다.</param>
        /// <param name="heightOwner">캐릭터 높이 보정 기준입니다. null이면 Follow 대상 또는 생성 캐릭터를 사용합니다.</param>
        /// <remarks>
        /// 풀에서 꺼낸 VFX는 SetActive(true) 직후 OnEnable과 첫 렌더링이 발생할 수 있으므로,
        /// 생성 프레임에 위치가 튀지 않도록 활성화 전에 최종 Transform을 확정합니다.
        /// </remarks>
        public void ApplySpawnPositionImmediate(Vector3 basePosition, CharacterBase heightOwner = null)
        {
            Vector3 spawnPosition = ResolveSpawnPosition(basePosition, heightOwner);
            transform.position = spawnPosition;
            CaptureFollowSpawnOffsetIfNeeded(spawnPosition);

            if (_followCharacter != null && _followMode == VfxConstants.FollowMode.PositionAndFlip)
                SetFlip(_followCharacter.IsFlipped());
        }

        /// <summary>
        /// 현재 Follow 대상 위치를 즉시 반영합니다.
        /// </summary>
        /// <remarks>
        /// 매 프레임 Follow 처리와 생성 직전 위치 보정이 같은 계산식을 사용하도록 보장합니다.
        /// </remarks>
        public void RefreshFollowPositionImmediate()
        {
            if (_followCharacter == null || _followMode == VfxConstants.FollowMode.None)
                return;

            if (_followAnchorMode == VfxConstants.FollowAnchorMode.SpawnPosition && _hasFollowSpawnOffset)
            {
                transform.position = _followCharacter.transform.position + _followSpawnOffset;

                if (_followMode == VfxConstants.FollowMode.PositionAndFlip)
                    SetFlip(_followCharacter.IsFlipped());

                return;
            }

            ApplySpawnPositionImmediate(_followCharacter.transform.position, _followCharacter);
        }

        /// <summary>
        /// 최초 스폰 위치 기준 Follow 정책에서 사용할 월드 오프셋을 저장합니다.
        /// </summary>
        /// <param name="spawnPosition">모든 위치 보정이 적용된 최종 스폰 월드 위치입니다.</param>
        /// <remarks>
        /// <see cref="VfxConstants.FollowAnchorMode.SpawnPosition"/>은 HitArea 랜덤 위치처럼
        /// 스폰 시점에만 계산되는 위치를 Follow 중에도 유지하기 위해 사용합니다.
        /// </remarks>
        private void CaptureFollowSpawnOffsetIfNeeded(Vector3 spawnPosition)
        {
            if (_followCharacter == null || _followMode == VfxConstants.FollowMode.None)
                return;

            if (_followAnchorMode != VfxConstants.FollowAnchorMode.SpawnPosition || _hasFollowSpawnOffset)
                return;

            _followSpawnOffset = spawnPosition - _followCharacter.transform.position;
            _hasFollowSpawnOffset = true;
        }

        /// <summary>
        /// VFX를 생성한 오브젝트를 캐릭터로 해석하고 위치와 Flip을 즉시 반영합니다.
        /// </summary>
        /// <param name="character">VFX를 생성한 오브젝트입니다.</param>
        public void SetCreateCharacter(GameObject character)
        {
            SetCreateCharacter(character != null ? character.GetComponent<CharacterBase>() : null);
        }

        /// <summary>
        /// VFX를 생성한 캐릭터를 기록하고 위치와 Flip을 즉시 반영합니다.
        /// </summary>
        /// <param name="character">VFX를 생성한 캐릭터입니다.</param>
        public void SetCreateCharacter(CharacterBase character)
        {
            SetCreateCharacter(character, true);
        }

        /// <summary>
        /// VFX를 생성한 오브젝트를 캐릭터로 해석하고 요청한 시각 보정만 즉시 반영합니다.
        /// </summary>
        /// <param name="character">VFX를 생성한 오브젝트입니다.</param>
        /// <param name="applyFlip">true이면 캐릭터의 현재 Flip 상태를 VFX에 적용합니다.</param>
        /// <param name="applyPosition">true이면 캐릭터 위치를 기준으로 VFX 위치를 즉시 보정합니다.</param>
        public void SetCreateCharacter(GameObject character, bool applyFlip, bool applyPosition = true)
        {
            SetCreateCharacter(character != null ? character.GetComponent<CharacterBase>() : null, applyFlip, applyPosition);
        }

        /// <summary>
        /// VFX를 생성한 캐릭터를 기록하고 요청한 시각 보정만 즉시 반영합니다.
        /// </summary>
        /// <param name="character">VFX를 생성한 캐릭터입니다.</param>
        /// <param name="applyFlip">true이면 캐릭터의 현재 Flip 상태를 VFX에 적용합니다.</param>
        /// <param name="applyPosition">true이면 캐릭터 위치를 기준으로 VFX 위치를 즉시 보정합니다.</param>
        public void SetCreateCharacter(CharacterBase character, bool applyFlip, bool applyPosition = true)
        {
            _character = character;
            if (_character == null)
                return;

            if (applyPosition)
                ApplySpawnPositionImmediate(character.transform.position, _character);

            if (applyFlip)
                SetFlip(_character.IsFlipped());
        }

        protected virtual void Update()
        {
            UpdateTimelineFade();

            if (_followCharacter == null || _followMode == VfxConstants.FollowMode.None)
                return;

            RefreshFollowPositionImmediate();
        }

        protected void EnsureFadeController()
        {
            if (_fadeController != null)
                return;

            _fadeController = GetComponent<VfxFadeController>();
            if (_fadeController == null)
                _fadeController = gameObject.AddComponent<VfxFadeController>();

            _fadeController.EnsureInitialized();
        }

        /// <summary>
        /// 글로벌 VFX Fade 시간과 이번 생성 요청의 예외 정책을 조합해 실제 Fade 시간을 갱신합니다.
        /// </summary>
        private void ApplyFadeDurationPolicy()
        {
            _vfxFadeInSec = _disableFadeIn ? 0f : Mathf.Max(0f, _defaultVfxFadeInSec);
            _vfxFadeOutSec = Mathf.Max(0f, _defaultVfxFadeOutSec);
        }

        private float GetCurrentAlpha()
        {
            return _fadeController != null ? _fadeController.CurrentAlpha : 1f;
        }

        protected void RefreshFadeControllerVisualBaseline()
        {
            if (_fadeController == null)
                return;

            float currentAlpha = _fadeController.CurrentAlpha;
            _fadeController.RefreshOriginalColorsFromCurrentState();
            _fadeController.SetAlpha(currentAlpha);
        }

        private void PrepareSpawnFadeState()
        {
            EnsureFadeController();
            if (_fadeController == null)
                return;

            _fadeController.SetAlpha(_vfxFadeInSec > 0f ? 0f : 1f);
        }

        private void RestoreVisibleState()
        {
            if (_fadeController == null)
                return;

            _fadeController.RestoreFullAlpha();
        }

        private void StartFadeInIfNeeded()
        {
            if (_fadeController == null)
                return;

            float fadeInDuration = _vfxFadeInSec;
            if (fadeInDuration <= 0f)
            {
                _fadeController.SetAlpha(1f);
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration, _vfxFadeInEase));
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Easing.EaseType easeType)
        {
            if (_fadeController == null)
                yield break;

            float elapsed = 0f;
            _fadeController.SetAlpha(startAlpha);

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Clamp01(Easing.Apply(t, easeType));
                float alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                _fadeController.SetAlpha(alpha);
                yield return null;
            }

            _fadeController.SetAlpha(endAlpha);
            _fadeCoroutine = null;
        }

        private void StopManagedCoroutines()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (CoroutineTickTimeDamage != null)
            {
                StopCoroutine(CoroutineTickTimeDamage);
                CoroutineTickTimeDamage = null;
            }
        }

        private bool ShouldUseTimelineFade()
        {
            if (_spawnPolicy == null || _duration <= 0f)
                return false;

            return _spawnPolicy.LifecycleType == VfxConstants.LifecycleType.Duration
                || _spawnPolicy.LifecycleType == VfxConstants.LifecycleType.AutoRelease;
        }

        private void UpdateTimelineFade()
        {
            if (!_useTimelineFade || _isReleasing || _fadeController == null || _duration <= 0f)
                return;

            _lifetimeElapsed += GetDeltaTime();

            float fadeInAlpha = EvaluateFadeInAlpha(_lifetimeElapsed);
            float fadeOutAlpha = UseTimelineFadeOutAlpha
                ? EvaluateFadeOutAlpha(_lifetimeElapsed, _duration)
                : 1f;
            float finalAlpha = Mathf.Min(fadeInAlpha, fadeOutAlpha);
            _fadeController.SetAlpha(finalAlpha);

            if (_lifetimeElapsed >= _duration && !_timelineDurationElapsedHandled)
            {
                _timelineDurationElapsedHandled = true;
                OnTimelineDurationElapsed();
            }
        }

        protected virtual void OnTimelineDurationElapsed()
        {
            if (_fadeController != null)
                _fadeController.SetAlpha(0f);

            ReleaseImmediateInternal();
        }

        private float EvaluateFadeInAlpha(float elapsed)
        {
            float fadeInDuration = Mathf.Max(0f, _vfxFadeInSec);
            if (fadeInDuration <= 0f)
                return 1f;

            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float eased = Mathf.Clamp01(Easing.Apply(t, _vfxFadeInEase));
            return Mathf.Lerp(0f, 1f, eased);
        }

        private float EvaluateFadeOutAlpha(float elapsed, float duration)
        {
            float fadeOutDuration = Mathf.Clamp(_vfxFadeOutSec, 0f, duration);
            if (fadeOutDuration <= 0f)
                return 1f;

            float fadeOutStart = Mathf.Max(0f, duration - fadeOutDuration);
            if (elapsed <= fadeOutStart)
                return 1f;

            float t = Mathf.Clamp01((elapsed - fadeOutStart) / fadeOutDuration);
            float eased = Mathf.Clamp01(Easing.Apply(t, _vfxFadeOutEase));
            return Mathf.Lerp(1f, 0f, eased);
        }

        private float GetDeltaTime()
        {
            return UseUnscaledTime() ? Time.unscaledDeltaTime : Time.deltaTime;
        }
    }

    /// <summary>
    /// 피어스 샷 레이저 VFX의 생성부터 풀 반환까지 수명주기를 추적하는 에디터 전용 진단 도구입니다.
    /// 지정된 VFX UID만 기록하여 일반 전투 로그와 프레임당 할당을 최소화합니다.
    /// </summary>
    internal static class VfxLifecycleDiagnostics
    {
        private const int TargetVfxUid = 300106;

        /// <summary>
        /// 대상 GameObject가 진단 대상 VFX일 때 현재 수명주기 상태를 콘솔에 기록합니다.
        /// 플레이어 빌드에서는 호출과 인수 평가가 모두 제거됩니다.
        /// </summary>
        /// <param name="target">진단할 VFX GameObject입니다.</param>
        /// <param name="stage">현재 수명주기 단계입니다.</param>
        /// <param name="details">단계별 추가 상태 정보입니다.</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal static void Log(GameObject target, string stage, string details = null)
        {
            if (!target)
                return;

            VfxBehaviourBase behaviour = target.GetComponent<VfxBehaviourBase>();
            if (behaviour == null || behaviour.RuntimeUid != TargetVfxUid)
                return;

            Transform parent = target.transform.parent;
            string parentName = parent != null ? parent.name : "<null>";
            string suffix = string.IsNullOrWhiteSpace(details) ? string.Empty : $", {details}";
            Animator animator = target.GetComponent<Animator>();
            string animatorState = ResolveAnimatorState(animator);
        }

        /// <summary>
        /// Animator의 활성 상태와 현재 상태 해시 및 정규화 시간을 진단 문자열로 변환합니다.
        /// </summary>
        /// <param name="animator">확인할 Animator입니다.</param>
        /// <returns>Animator가 없으면 animatorNull=True이며, 있으면 현재 상태 정보가 포함된 문자열입니다.</returns>
        private static string ResolveAnimatorState(Animator animator)
        {
            if (animator == null)
                return "animatorNull=True";

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            string controllerName = animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "None";

            return $"animatorNull=False, animatorEnabled={animator.enabled}, " +
                   $"controller={controllerName}, stateShortHash={state.shortNameHash}, " +
                   $"stateFullHash={state.fullPathHash}, normalizedTime={state.normalizedTime:F3}";
        }
    }
}
