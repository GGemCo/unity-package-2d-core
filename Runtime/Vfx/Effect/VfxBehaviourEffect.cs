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
        private bool _hasDefaultSorting;
        private int _defaultSortingLayerId;
        private int _defaultSortingOrder;
        private bool _hasSortingLayerOverride;
        private ConfigSortingLayer.Keys _sortingLayerOverride;
        private bool _hasSortingOrderOverride;
        private int _sortingOrderOverride;

        protected override void Awake()
        {
            base.Awake();
            EnsureCachedReferences();
        }

        /// <summary>
        /// 풀링된 VFX가 재사용될 때 이전 생성 요청의 정렬 override가 남지 않도록 기본 정렬 상태를 복원합니다.
        /// </summary>
        /// <param name="runtimeData">VFX 테이블에서 해석된 런타임 데이터입니다.</param>
        /// <param name="spawnPolicy">이번 생성 요청에 적용할 생성 정책입니다.</param>
        /// <param name="releaseAction">풀 반환 또는 제거 시 호출할 콜백입니다.</param>
        public override void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, System.Action<int, GameObject> releaseAction = null)
        {
            base.Initialize(runtimeData, spawnPolicy, releaseAction);
            EnsureCachedReferences();
            CaptureDefaultSortingIfNeeded();
            RestoreDefaultSorting();
            _hasSortingLayerOverride = false;
            _hasSortingOrderOverride = false;
        }

        /// <summary>
        /// VFX가 활성화될 때 공통 시각 상태를 적용하고 시작 애니메이션을 재생합니다.
        /// </summary>
        /// <remarks>
        /// 풀에서 재사용되는 VFX는 Animator가 이전 상태를 유지할 수 있으므로,
        /// 시작 애니메이션을 항상 첫 프레임부터 강제 재생합니다.
        /// </remarks>
        protected override void PlayOnSpawn()
        {
            EnsureCachedReferences();
            ApplyCommonVisuals();
            base.PlayOnSpawn();

            if (VfxAnimationController == null)
            {
                ApplySortingOverridesIfNeeded();
                return;
            }

            bool started = VfxAnimationController.Play(GetPlaybackDuration(), 1f, true);
            ApplySortingOverridesIfNeeded();
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

        /// <summary>
        /// VFX 인스턴스가 처음 초기화될 때 기준 Sorting Layer와 Order를 보관합니다.
        /// </summary>
        private void CaptureDefaultSortingIfNeeded()
        {
            if (_hasDefaultSorting)
                return;

            if (_effectRenderer == null)
                return;

            _defaultSortingLayerId = VfxAnimationController is VfxAnimationControllerSprite
                ? SortingLayer.NameToID(ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop))
                : _effectRenderer.sortingLayerID;
            _defaultSortingOrder = _effectRenderer.sortingOrder;
            _hasDefaultSorting = true;
        }

        /// <summary>
        /// 풀에서 다시 꺼낸 VFX에 남아 있을 수 있는 이전 정렬 override를 기본값으로 되돌립니다.
        /// </summary>
        private void RestoreDefaultSorting()
        {
            if (!_hasDefaultSorting || _effectRenderer == null)
                return;

            _effectRenderer.sortingLayerID = _defaultSortingLayerId;
            _effectRenderer.sortingOrder = _defaultSortingOrder;
        }

        /// <summary>
        /// 애니메이션 컨트롤러 초기화가 정렬값을 다시 설정한 경우를 대비해 요청 override를 재적용합니다.
        /// </summary>
        private void ApplySortingOverridesIfNeeded()
        {
            if (_effectRenderer == null)
                return;

            if (_hasSortingLayerOverride)
                _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(_sortingLayerOverride);

            if (_hasSortingOrderOverride)
                _effectRenderer.sortingOrder = _sortingOrderOverride;
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
            RefreshFadeControllerVisualBaseline();
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
            if (_hasSortingOrderOverride)
                return;

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

            string normalizedColor = NormalizeColorHex(_color);
            if (string.IsNullOrWhiteSpace(normalizedColor))
                return;

            VfxAnimationController?.SetEffectColor(normalizedColor);
            RefreshFadeControllerVisualBaseline();
        }

        public override void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer == null)
                return;

            _hasSortingLayerOverride = true;
            _sortingLayerOverride = sortingLayer;
            _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(sortingLayer);
        }

        public override void SetSortingOrder(int sortingOrder)
        {
            if (_effectRenderer == null)
                _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer == null)
                return;

            _hasSortingOrderOverride = true;
            _sortingOrderOverride = sortingOrder;
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

        public void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete)
        {
            VfxAnimationController.AnimationEventComplete(struckAnimationEventComplete);
        }
    }
}
