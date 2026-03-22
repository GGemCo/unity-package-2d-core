using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxBehaviourBase : MonoBehaviour
    {
        public IVfxAnimationController VfxAnimationController;

        private CharacterBase _character;
        protected CharacterBase TargetCharacter;
        private float _duration;
        private string _color;
        protected Vector3 Direction;
        private float _originalScaleX;
        private float _mapSizeHeight;
        private CharacterBase _followCharacter;
        private VfxConstants.FollowMode _followMode;
        private float _positionY;
        private ConfigCommon.PositionYType _positionYType;
        private Renderer _effectRenderer;
        private RectTransform _effectRectTransform;
        private Animator _animator;
        protected Coroutine CoroutineTickTimeDamage;
        private VfxRuntimeData _runtimeData;
        private VfxSpawnPolicy _spawnPolicy;

        private bool _started;
        private bool _releaseOnAnimationComplete;
        private int _pooledVfxUid;
        private Action<int, GameObject> _releaseAction;

        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnVfxDestroy;

        protected VfxRuntimeData RuntimeData => _runtimeData;
        protected VfxEffectRuntimeData EffectRuntimeData => _runtimeData as VfxEffectRuntimeData;
        protected VfxParticleRuntimeData ParticleRuntimeData => _runtimeData as VfxParticleRuntimeData;
        protected VfxSpawnPolicy SpawnPolicy => _spawnPolicy;

        protected virtual void Awake()
        {
            _color = string.Empty;
            _originalScaleX = transform.localScale.x;
            _effectRenderer = GetComponent<Renderer>();
            _effectRectTransform = GetComponent<RectTransform>();
            _animator = GetComponent<Animator>();
        }

        public virtual void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, Action<int, GameObject> releaseAction = null)
        {
            _runtimeData = runtimeData;
            _spawnPolicy = spawnPolicy ?? runtimeData?.DefaultSpawnPolicy?.Clone() ?? new VfxSpawnPolicy();
            _pooledVfxUid = runtimeData?.Uid ?? 0;
            _releaseAction = releaseAction;
            _followMode = _spawnPolicy.FollowMode;
            _releaseOnAnimationComplete = false;
            _started = false;
        }

        protected virtual void OnEnable()
        {
            if (_runtimeData == null)
                return;

            if (_started)
                PlayOnSpawn();
        }

        protected virtual void Start()
        {
            if (_runtimeData == null)
                return;

            _started = true;
            PlayOnSpawn();
        }

        protected virtual void PlayOnSpawn()
        {
            ApplyCommonVisuals();
            StartLifecycleTimerIfNeeded();

            if (VfxAnimationController != null)
                VfxAnimationController.Play(GetPlaybackDuration());
        }

        protected void ApplyCommonVisuals()
        {
            if (!string.IsNullOrEmpty(_color))
            {
                VfxAnimationController?.SetEffectColor(NormalizeColorHex(_color));
            }
            else if (EffectRuntimeData != null && !string.IsNullOrEmpty(EffectRuntimeData.Color))
            {
                VfxAnimationController?.SetEffectColor(NormalizeColorHex(EffectRuntimeData.Color));
            }

            if (EffectRuntimeData != null)
                SetSize(EffectRuntimeData.Width, EffectRuntimeData.Height);

            if (SceneGame.Instance != null && SceneGame.Instance.mapManager)
            {
                Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
                _mapSizeHeight = size.y;
            }

            if (_animator != null)
                _animator.updateMode = UseUnscaledTime() ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;

            UpdateSortingOrder();
        }

        protected static string NormalizeColorHex(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return color;
            return color.StartsWith("#") ? color : $"#{color}";
        }

        public void SetSize(float width, float height)
        {
            if (width <= 0 || height <= 0) return;

            if (_effectRectTransform != null)
            {
                _effectRectTransform.sizeDelta = new Vector2(width, height);
            }
            else if (_effectRenderer != null)
            {
                Bounds b = _effectRenderer.bounds;
                if (b.size.x <= 0 || b.size.y <= 0) return;
            }
        }

        protected IEnumerator RemoveEffectDuration(float f)
        {
            if (UseUnscaledTime())
                yield return new WaitForSecondsRealtime(f);
            else
                yield return new WaitForSeconds(f);

            ReleaseNow();
        }

        protected void UpdateSortingOrder()
        {
            int baseSortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            if (_effectRenderer)
                _effectRenderer.sortingOrder = baseSortingOrder;
        }

        protected void StartLifecycleTimerIfNeeded()
        {
            if (_spawnPolicy == null)
                return;

            if (_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.Duration && _duration > 0f)
            {
                StartCoroutine(RemoveEffectDuration(_duration));
                return;
            }

            if (_spawnPolicy.LifecycleType == VfxConstants.LifecycleType.AutoRelease && _duration > 0f)
                StartCoroutine(RemoveEffectDuration(_duration));
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

        public void SetDuration(float f) => _duration = f;

        public void SetRotation(Vector2 directionByTarget, Vector2 vector2)
        {
            if (EffectRuntimeData == null || !EffectRuntimeData.NeedRotation) return;

            float angle = Mathf.Atan2(directionByTarget.y, directionByTarget.x) * Mathf.Rad2Deg;
            if (EffectRuntimeData.DefaultDirection == ConfigCommon.DirectionType.Left && vector2.x < 0)
                angle += 180;

            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public virtual void DestroyForce()
        {
            StopAllCoroutines();
            ReleaseNow();
        }

        public void SetScale(float scale)
        {
            if (scale <= 0) return;
            transform.localScale = new Vector2(scale, scale);
            _originalScaleX = transform.localScale.x;
        }

        private void SetDirection(float dirX)
        {
            transform.localScale = new Vector3(_originalScaleX * dirX, transform.localScale.y, transform.localScale.z);
        }

        public void SetFlip(bool shouldFlip)
        {
            float dirX = shouldFlip ? -1 : 1;
            SetDirection(dirX);
            OnSetFlip(dirX);
        }

        protected virtual void OnSetFlip(float dirX) { }

        public virtual void OnEndAnimationComplete()
        {
            if (!_releaseOnAnimationComplete && !ShouldAutoReleaseOnNaturalComplete())
                return;

            _releaseOnAnimationComplete = false;
            StopAllCoroutines();
            var callback = OnVfxDestroy;
            OnVfxDestroy = null;
            callback?.Invoke();
            ReleaseOrDestroy();
        }

        protected void ReleaseNow()
        {
            _releaseOnAnimationComplete = false;
            StopAllCoroutines();
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

        public bool TryPlayEndAnimation(DelegateEffectDestroy onEffectDestroy = null)
        {
            if (VfxAnimationController == null || !VfxAnimationController.HasEndAnimation())
                return false;

            if (onEffectDestroy != null)
                OnVfxDestroy += onEffectDestroy;

            PlayEndAnimation();
            return true;
        }

        public virtual void PlayEndAnimation()
        {
            _releaseOnAnimationComplete = true;

            if (VfxAnimationController != null)
                VfxAnimationController.PlayEnd();
            else
                ReleaseNow();
        }

        public void SetColor(string color) => _color = color;

        public void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer == null) return;
            _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(sortingLayer);
        }

        public void SetSortingOrder(int sortingOrder)
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer == null) return;
            _effectRenderer.sortingOrder = sortingOrder;
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
            if (_positionY > 0)
                transform.position += new Vector3(0, _positionY, 0);

            if (_positionYType == ConfigCommon.PositionYType.CharacterHeight)
            {
                var heightOwner = _followCharacter != null ? _followCharacter : _character;
                if (heightOwner != null)
                    transform.position += new Vector3(0, heightOwner.GetHeightByScale(), 0);
            }

            if (_followMode == VfxConstants.FollowMode.PositionAndFlip)
                SetFlip(_followCharacter.IsFlipped());
        }
    }
}
