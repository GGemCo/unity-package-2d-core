using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public class VfxBehaviourEffect : VfxBehaviourBase
    {
        public IVfxAnimationController VfxAnimationController;

        private string _color;
        private float _mapSizeHeight;
        private Renderer _effectRenderer;
        private RectTransform _effectRectTransform;
        private Animator _animator;

        protected override void Awake()
        {
            base.Awake();
            _color = string.Empty;
            EnsureCachedReferences();
        }

        protected override void PlayOnSpawn()
        {
            EnsureCachedReferences();
            ApplyCommonVisuals();
            base.PlayOnSpawn();

            if (VfxAnimationController == null)
                return;

            bool started = VfxAnimationController.Play(GetPlaybackDuration());
            if (!started)
            {
                GcLogger.LogWarning($"VFX animation play failed. name: {gameObject.name}, uid: {RuntimeData?.Uid ?? 0}");
                OnEndAnimationComplete();
            }
        }

        private void EnsureCachedReferences()
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();

            if (_effectRectTransform == null)
                _effectRectTransform = GetComponent<RectTransform>();

            if (_animator == null)
                _animator = GetComponent<Animator>();
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
            if (width <= 0f || height <= 0f)
                return;

            if (_effectRectTransform != null)
            {
                _effectRectTransform.sizeDelta = new Vector2(width, height);
            }
            else if (_effectRenderer != null)
            {
                Bounds bounds = _effectRenderer.bounds;
                if (bounds.size.x <= 0f || bounds.size.y <= 0f)
                    return;
            }
        }

        protected void UpdateSortingOrder()
        {
            int baseSortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            if (_effectRenderer != null)
                _effectRenderer.sortingOrder = baseSortingOrder;
        }

        public override bool TryPlayEndAnimation(DelegateEffectDestroy onEffectDestroy = null)
        {
            if (VfxAnimationController == null || !VfxAnimationController.HasEndAnimation())
                return false;

            if (onEffectDestroy != null)
                OnVfxDestroy += onEffectDestroy;

            PlayEndAnimation();
            return true;
        }

        public override void PlayEndAnimation()
        {
            BeginReleaseOnAnimationComplete();

            if (VfxAnimationController != null)
            {
                VfxAnimationController.PlayEnd();
                return;
            }

            BeginReleaseSequence();
        }

        public override void SetColor(string color)
        {
            _color = color;
        }

        public override void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer == null)
                return;

            _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(sortingLayer);
        }

        public override void SetSortingOrder(int sortingOrder)
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer == null)
                return;

            _effectRenderer.sortingOrder = sortingOrder;
        }

        public override void SetRotation(Vector2 directionByTarget, Vector2 sourceDirection)
        {
            if (EffectRuntimeData == null || !EffectRuntimeData.NeedRotation)
                return;

            float angle = Mathf.Atan2(directionByTarget.y, directionByTarget.x) * Mathf.Rad2Deg;
            if (EffectRuntimeData.DefaultDirection == ConfigCommon.DirectionType.Left && sourceDirection.x < 0f)
                angle += 180f;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
