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

        private float _vfxFadeInSec;
        private float _vfxFadeOutSec;
        private Easing.EaseType _vfxFadeInEase;
        private Easing.EaseType _vfxFadeOutEase;
        
        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnVfxDestroy;

        protected VfxRuntimeData RuntimeData => _runtimeData;
        protected VfxEffectRuntimeData EffectRuntimeData => _runtimeData as VfxEffectRuntimeData;
        protected VfxParticleRuntimeData ParticleRuntimeData => _runtimeData as VfxParticleRuntimeData;
        protected VfxSpawnPolicy SpawnPolicy => _spawnPolicy;
        protected virtual bool UseTimelineFadeOutAlpha => true;

        protected virtual void Awake()
        {
            CaptureDefaultTransformIfNeeded();

            _vfxFadeInSec = AddressableLoaderSettings.Instance.settings.vfxFadeInSec;
            _vfxFadeOutSec = AddressableLoaderSettings.Instance.settings.vfxFadeOutSec;
            _vfxFadeInEase = AddressableLoaderSettings.Instance.settings.vfxFadeInEase;
            _vfxFadeOutEase = AddressableLoaderSettings.Instance.settings.vfxFadeOutEase;
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
            _positionY = 0f;
            _positionYType = ConfigCommon.PositionYType.None;
            _duration = 0f;
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

        public void SetFollowCharacter(CharacterBase character, VfxConstants.FollowMode followMode = VfxConstants.FollowMode.Position)
        {
            _followCharacter = character;
            _followMode = followMode;
        }

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
            Vector3 result = basePosition;

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
            transform.position = ResolveSpawnPosition(basePosition, heightOwner);

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

            ApplySpawnPositionImmediate(_followCharacter.transform.position, _followCharacter);
        }

        public void SetCreateCharacter(GameObject character)
        {
            SetCreateCharacter(character != null ? character.GetComponent<CharacterBase>() : null);
        }

        public void SetCreateCharacter(CharacterBase character)
        {
            _character = character;
            if (_character == null)
                return;

            ApplySpawnPositionImmediate(character.transform.position, _character);
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
}
