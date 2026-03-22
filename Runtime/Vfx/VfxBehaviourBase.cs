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
        private Coroutine _lifecycleCoroutine;
        private Coroutine _fadeCoroutine;
        private VfxFadeController _fadeController;
        private bool _isReleasing;

        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnVfxDestroy;

        protected VfxRuntimeData RuntimeData => _runtimeData;
        protected VfxEffectRuntimeData EffectRuntimeData => _runtimeData as VfxEffectRuntimeData;
        protected VfxParticleRuntimeData ParticleRuntimeData => _runtimeData as VfxParticleRuntimeData;
        protected VfxSpawnPolicy SpawnPolicy => _spawnPolicy;

        protected virtual void Awake()
        {
            _originalScaleX = transform.localScale.x;
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
            EnsureFadeController();
            RestoreVisibleState();
        }

        protected virtual void OnEnable()
        {
            if (_runtimeData == null)
                return;

            _releaseOnAnimationComplete = false;
            _isReleasing = false;
            StopManagedCoroutines();
            PrepareSpawnFadeState();
            PlayOnSpawn();
            StartFadeInIfNeeded();
        }

        protected virtual void OnDisable()
        {
            StopManagedCoroutines();
            RestoreVisibleState();
            _releaseOnAnimationComplete = false;
            _isReleasing = false;
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

            float fadeOutDuration = GetDurationOverlapFadeOutDuration(duration);
            float preFadeDuration = Mathf.Max(0f, duration - fadeOutDuration);

            if (preFadeDuration > 0f)
            {
                if (UseUnscaledTime())
                    yield return new WaitForSecondsRealtime(preFadeDuration);
                else
                    yield return new WaitForSeconds(preFadeDuration);
            }

            if (ShouldOverlapFadeOutDuringPlayback(duration, fadeOutDuration))
            {
                StartFadeOutOnly(fadeOutDuration);

                if (fadeOutDuration > 0f)
                {
                    if (UseUnscaledTime())
                        yield return new WaitForSecondsRealtime(fadeOutDuration);
                    else
                        yield return new WaitForSeconds(fadeOutDuration);
                }

                ReleaseImmediateInternal();
                yield break;
            }

            PlayEndAnimation();
        }

        protected void StartLifecycleTimerIfNeeded()
        {
            if (_spawnPolicy == null)
                return;

            if ((_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.Duration && _duration > 0f) ||
                (_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.AutoRelease && _duration > 0f))
            {
                StartLifecycleCoroutine(RemoveEffectDuration(_duration));
            }
        }

        protected virtual bool ShouldOverlapFadeOutDuringPlayback(float duration, float fadeOutDuration)
        {
            if (_spawnPolicy == null)
                return false;

            if (duration <= 0f || fadeOutDuration <= 0f)
                return false;

            return _spawnPolicy.LifecycleType == VfxConstants.LifecycleType.Duration
                || _spawnPolicy.LifecycleType == VfxConstants.LifecycleType.AutoRelease;
        }

        protected virtual float GetDurationOverlapFadeOutDuration(float duration)
        {
            if (duration <= 0f)
                return 0f;

            return Mathf.Clamp(ConfigCommon.VfxFadeOutSec, 0f, duration);
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

        public void SetDuration(float duration) => _duration = duration;

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

            float fadeOutDuration = ConfigCommon.VfxFadeOutSec;
            if (fadeOutDuration <= 0f || _fadeController == null)
            {
                ReleaseImmediateInternal();
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeAndReleaseRoutine(GetCurrentAlpha(), 0f, fadeOutDuration, ConfigCommon.VfxFadeOutEase));
        }

        private void StartFadeOutOnly(float duration)
        {
            if (_fadeController == null)
                return;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (duration <= 0f)
            {
                _fadeController.SetAlpha(0f);
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeRoutine(GetCurrentAlpha(), 0f, duration, ConfigCommon.VfxFadeOutEase));
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
                elapsed += UseUnscaledTime() ? Time.unscaledDeltaTime : Time.deltaTime;
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

            _fadeController.SetAlpha(ConfigCommon.VfxFadeInSec > 0f ? 0f : 1f);
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

            float fadeInDuration = ConfigCommon.VfxFadeInSec;
            if (fadeInDuration <= 0f)
            {
                _fadeController.SetAlpha(1f);
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration, ConfigCommon.VfxFadeInEase));
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Easing.EaseType easeType)
        {
            if (_fadeController == null)
                yield break;

            float elapsed = 0f;
            _fadeController.SetAlpha(startAlpha);

            while (elapsed < duration)
            {
                elapsed += UseUnscaledTime() ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.Clamp01(Easing.Apply(t, easeType));
                float alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                _fadeController.SetAlpha(alpha);
                yield return null;
            }

            _fadeController.SetAlpha(endAlpha);
            _fadeCoroutine = null;
        }

        private void StartLifecycleCoroutine(IEnumerator routine)
        {
            if (_lifecycleCoroutine != null)
                StopCoroutine(_lifecycleCoroutine);

            _lifecycleCoroutine = StartCoroutine(routine);
        }

        private void StopManagedCoroutines()
        {
            if (_lifecycleCoroutine != null)
            {
                StopCoroutine(_lifecycleCoroutine);
                _lifecycleCoroutine = null;
            }

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
    }
}
