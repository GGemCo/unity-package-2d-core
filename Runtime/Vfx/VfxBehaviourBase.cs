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
        private CharacterBase _followCharacter;
        private VfxConstants.FollowMode _followMode;
        private float _positionY;
        private ConfigCommon.PositionYType _positionYType;
        protected Coroutine CoroutineTickTimeDamage;
        private VfxRuntimeData _runtimeData;
        private VfxSpawnPolicy _spawnPolicy;

        private bool _releaseOnAnimationComplete;
        private int _pooledVfxUid;
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
            _originalScaleX = transform.localScale.x;

            _vfxFadeInSec = AddressableLoaderSettings.Instance.settings.vfxFadeInSec;
            _vfxFadeOutSec = AddressableLoaderSettings.Instance.settings.vfxFadeOutSec;
            _vfxFadeInEase = AddressableLoaderSettings.Instance.settings.vfxFadeInEase;
            _vfxFadeOutEase = AddressableLoaderSettings.Instance.settings.vfxFadeOutEase;
        }

        public virtual void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, Action<int, GameObject> releaseAction = null)
        {
            _runtimeData = runtimeData;
            _spawnPolicy = spawnPolicy ?? runtimeData?.DefaultSpawnPolicy?.Clone() ?? new VfxSpawnPolicy();
            _pooledVfxUid = runtimeData?.Uid ?? 0;
            _releaseAction = releaseAction;
            _followMode = _spawnPolicy.FollowMode;
            _releaseOnAnimationComplete = false;
            _isReleasing = false;
            _lifetimeElapsed = 0f;
            _timelineDurationElapsedHandled = false;
            _useTimelineFade = ShouldUseTimelineFade();
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
            BeginReleaseSequence();
        }

        public void SetScale(float scale)
        {
            if (scale <= 0f)
                return;

            transform.localScale = new Vector2(scale, scale);
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

            float fadeOutDuration = _vfxFadeInSec;
            if (fadeOutDuration <= 0f || _fadeController == null)
            {
                ReleaseImmediateInternal();
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeAndReleaseRoutine(GetCurrentAlpha(), 0f, fadeOutDuration, _vfxFadeOutEase));
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

        private void ReleaseOrDestroy()
        {
            if (_releaseAction != null && _pooledVfxUid > 0)
            {
                _releaseAction.Invoke(_pooledVfxUid, gameObject);
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

        public virtual void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
        }

        public virtual void SetSortingOrder(int sortingOrder)
        {
        }

        public virtual void SetRotation(Vector2 directionByTarget, Vector2 sourceDirection)
        {
        }

        public void SetFollowCharacter(CharacterBase character, VfxConstants.FollowMode followMode = VfxConstants.FollowMode.Position)
        {
            _followCharacter = character;
            _followMode = followMode;
        }

        public void SetPositionY(float y) => _positionY = y;
        public void SetPositionYType(ConfigCommon.PositionYType type) => _positionYType = type;

        public void SetCreateCharacter(GameObject character)
        {
            SetCreateCharacter(character != null ? character.GetComponent<CharacterBase>() : null);
        }

        public void SetCreateCharacter(CharacterBase character)
        {
            _character = character;
            if (_character == null)
                return;

            transform.position = character.transform.position;
            SetFlip(_character.IsFlipped());
        }

        protected virtual void Update()
        {
            UpdateTimelineFade();

            if (_followCharacter == null || _followMode == VfxConstants.FollowMode.None)
                return;

            transform.position = _followCharacter.transform.position;
            if (_positionY > 0f)
                transform.position += new Vector3(0f, _positionY, 0f);

            if (_positionYType == ConfigCommon.PositionYType.CharacterHeight)
            {
                var heightOwner = _followCharacter != null ? _followCharacter : _character;
                if (heightOwner != null)
                    transform.position += new Vector3(0f, heightOwner.GetHeightByScale(), 0f);
            }

            if (_followMode == VfxConstants.FollowMode.PositionAndFlip)
                SetFlip(_followCharacter.IsFlipped());
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
            float fadeOutDuration = Mathf.Clamp(_vfxFadeInSec, 0f, duration);
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
