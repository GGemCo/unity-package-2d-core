using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxBehaviourBase : MonoBehaviour
    {
        public IVfxAnimationController VfxAnimationController;

        protected CharacterBase _character;
        protected CharacterBase _targetCharacter;
        protected float _duration;
        protected string _color;
        protected Vector3 _direction;
        protected float _originalScaleX;
        protected float _mapSizeHeight;
        protected CharacterBase _followCharacter;
        protected float _positionY;
        protected ConfigCommon.PositionYType _positionYType;
        protected Renderer _effectRenderer;
        protected RectTransform _effectRectTransform;
        protected Animator _animator;
        protected Coroutine _coroutineTickTimeDamage;
        protected StruckTableVfx _struckTableVfx;

        private bool _started;
        private int _pooledVfxUid;
        private Action<int, GameObject> _releaseAction;

        public delegate void DelegateEffectDestroy();
        public event DelegateEffectDestroy OnVfxDestroy;

        protected virtual void Awake()
        {
            _color = string.Empty;
            _originalScaleX = transform.localScale.x;
            _effectRenderer = GetComponent<Renderer>();
            _effectRectTransform = GetComponent<RectTransform>();
        }

        public virtual void Initialize(StruckTableVfx struckTableVfx, Action<int, GameObject> releaseAction = null)
        {
            _struckTableVfx = struckTableVfx;
            _pooledVfxUid = struckTableVfx != null ? struckTableVfx.Uid : 0;
            _releaseAction = releaseAction;
            _started = false;
        }

        protected virtual void OnEnable()
        {
            if (_struckTableVfx == null)
                return;

            if (_started)
                PlayOnSpawn();
        }

        protected virtual void Start()
        {
            if (_struckTableVfx == null)
                return;

            _started = true;
            PlayOnSpawn();
        }

        protected virtual void PlayOnSpawn()
        {
            ApplyCommonVisuals();
            if (_duration > 0)
                StartCoroutine(RemoveEffectDuration(_duration));

            if (VfxAnimationController != null)
                VfxAnimationController.Play(_duration);
        }

        protected void ApplyCommonVisuals()
        {
            if (!string.IsNullOrEmpty(_color))
            {
                VfxAnimationController?.SetEffectColor(NormalizeColorHex(_color));
            }
            else if (!string.IsNullOrEmpty(_struckTableVfx.Color))
            {
                VfxAnimationController?.SetEffectColor(NormalizeColorHex(_struckTableVfx.Color));
            }

            SetSize(_struckTableVfx.Width, _struckTableVfx.Height);

            if (SceneGame.Instance != null && SceneGame.Instance.mapManager)
            {
                Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
                _mapSizeHeight = size.y;
            }

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
            yield return new WaitForSeconds(f);
            OnEndAnimationComplete();
        }

        protected void UpdateSortingOrder()
        {
            int baseSortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            if (_effectRenderer)
                _effectRenderer.sortingOrder = baseSortingOrder;
        }

        public void SetDuration(float f) => _duration = f;

        public void SetRotation(Vector2 directionByTarget, Vector2 vector2)
        {
            if (_struckTableVfx == null || !_struckTableVfx.NeedRotation) return;

            float angle = Mathf.Atan2(directionByTarget.y, directionByTarget.x) * Mathf.Rad2Deg;
            if (_struckTableVfx.DefaultDirection == ConfigCommon.DirectionType.Left && vector2.x < 0)
                angle += 180;

            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public virtual void DestroyForce()
        {
            StopAllCoroutines();
            ReleaseOrDestroy();
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
            if (VfxAnimationController != null)
                VfxAnimationController.PlayEnd();
            else
                OnEndAnimationComplete();
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

        public void SetFollowCharacter(CharacterBase character) => _followCharacter = character;
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
            if (_followCharacter == null)
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
        }
    }
}
