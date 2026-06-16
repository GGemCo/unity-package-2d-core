using System;
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
        /// <param name="poolKeyOverride">동일 VfxUid를 Behaviour 정책별로 분리해 풀링할 때 사용하는 키입니다.</param>
        public override void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, System.Action<int, GameObject> releaseAction = null, int poolKeyOverride = 0)
        {
            base.Initialize(runtimeData, spawnPolicy, releaseAction, poolKeyOverride);
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

            // 테이블 기본 정렬값이 있으면 먼저 적용하고,
            // 그 외(기본값)에는 기존 동적 정렬 정책을 유지합니다.
            ApplyTableDefaultSortingIfNeeded();
            UpdateSortingOrder();
            RefreshFadeControllerVisualBaseline();
        }

        /// <summary>
        /// vfx_effect 테이블 기본 정렬값(SortingLayer, SortingOrder)을 적용합니다.
        /// </summary>
        /// <remarks>
        /// - SortingLayer가 "None"(또는 빈 값)이면 기존 런타임 기본 정렬을 유지합니다.
        /// - SortingOrder가 0이면 기존 런타임 기본 정렬을 유지합니다.
        /// - 외부 스폰 요청 override가 이미 있으면 테이블 기본값은 적용하지 않습니다.
        /// </remarks>
        private void ApplyTableDefaultSortingIfNeeded()
        {
            if (EffectRuntimeData == null)
                return;

            if (!_hasSortingLayerOverride &&
                TryResolveSortingLayerKey(EffectRuntimeData.SortingLayer, out ConfigSortingLayer.Keys sortingLayerKey))
            {
                SetSortingLayer(sortingLayerKey);
            }

            if (!_hasSortingOrderOverride && EffectRuntimeData.SortingOrder != 0)
                SetSortingOrder(EffectRuntimeData.SortingOrder);
        }

        /// <summary>
        /// 테이블 문자열에서 실제 적용 가능한 Sorting Layer 키를 해석합니다.
        /// </summary>
        /// <param name="rawValue">테이블의 SortingLayer 원본 문자열입니다.</param>
        /// <param name="sortingLayerKey">해석 성공 시 반환할 Sorting Layer 키입니다.</param>
        /// <returns>해석 성공 시 true, "None"/빈 값/미등록 값이면 false를 반환합니다.</returns>
        private static bool TryResolveSortingLayerKey(string rawValue, out ConfigSortingLayer.Keys sortingLayerKey)
        {
            sortingLayerKey = default;

            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            string normalized = rawValue.Trim();
            if (normalized.Equals("None", StringComparison.OrdinalIgnoreCase))
                return false;

            if (Enum.TryParse(normalized, true, out ConfigSortingLayer.Keys parsedByKey)
                && Enum.IsDefined(typeof(ConfigSortingLayer.Keys), parsedByKey))
            {
                sortingLayerKey = parsedByKey;
                return true;
            }

            foreach (ConfigSortingLayer.Keys candidate in Enum.GetValues(typeof(ConfigSortingLayer.Keys)))
            {
                string layerName = ConfigSortingLayer.GetValue(candidate);
                if (string.Equals(layerName, normalized, StringComparison.Ordinal))
                {
                    sortingLayerKey = candidate;
                    return true;
                }
            }

            return false;
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
            if (!CanPlayEndAnimationNow())
                return false;

            if (onEffectDestroy != null)
                OnVfxDestroy += onEffectDestroy;

            PlayEndAnimation();
            return true;
        }

        /// <summary>
        /// 현재 Effect VFX가 End 애니메이션을 실제로 재생할 수 있는 상태인지 확인합니다.
        /// 풀 반환이나 비활성화가 먼저 일어난 VFX는 Animator 재생 호출 시 Unity 경고가 발생하므로 재생 대상에서 제외합니다.
        /// </summary>
        /// <returns>End 애니메이션을 안전하게 재생할 수 있으면 true를 반환합니다.</returns>
        private bool CanPlayEndAnimationNow()
        {
            if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
                return false;

            return VfxAnimationController != null && VfxAnimationController.HasEndAnimation();
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

        /// <summary>
        /// VFX 방향 정보를 바탕으로 DefaultDirection 반전과 NeedRotation 회전을 순서대로 적용합니다.
        /// </summary>
        /// <param name="direction">VFX가 바라볼 주 방향입니다.</param>
        /// <param name="sourceDirection">주 방향의 X축이 불명확할 때 좌우 기준으로 사용할 보조 방향입니다.</param>
        /// <param name="applyDefaultDirectionFlip">true이면 vfx_effect.DefaultDirection 기준 좌우 반전을 적용합니다.</param>
        /// <param name="applyRotation">true이면 vfx_effect.NeedRotation 기준 각도 보정을 적용합니다.</param>
        public override void ApplyDirectionVisual(
            Vector2 direction,
            Vector2 sourceDirection,
            bool applyDefaultDirectionFlip = true,
            bool applyRotation = true)
        {
            if (EffectRuntimeData == null)
                return;

            Vector2 resolvedDirection = ResolveDirection(direction, sourceDirection);
            if (resolvedDirection.sqrMagnitude <= 0.0001f)
                return;

            Vector2 resolvedSourceDirection = ResolveDirection(sourceDirection, resolvedDirection);
            if (applyDefaultDirectionFlip)
                ApplyDefaultDirectionFlip(resolvedDirection, resolvedSourceDirection);

            if (applyRotation && EffectRuntimeData.NeedRotation)
                ApplyRotationAfterFlip(resolvedDirection, resolvedSourceDirection);
        }

        /// <summary>
        /// 기존 SetRotation 호출 경로를 방향 시각 보정 API로 연결합니다.
        /// </summary>
        /// <param name="directionByTarget">타겟 기준 방향입니다.</param>
        /// <param name="sourceDirection">좌우 기준으로 사용할 보조 방향입니다.</param>
        public override void SetRotation(Vector2 directionByTarget, Vector2 sourceDirection)
        {
            ApplyDirectionVisual(directionByTarget, sourceDirection, false, true);
        }

        /// <summary>
        /// 주 방향이 비어 있을 경우 보조 방향으로 대체하고 정규화합니다.
        /// </summary>
        /// <param name="direction">검사할 방향입니다.</param>
        /// <param name="fallback">대체 방향입니다.</param>
        /// <returns>정규화된 방향입니다.</returns>
        private static Vector2 ResolveDirection(Vector2 direction, Vector2 fallback)
        {
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;

            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector2.zero;
        }

        /// <summary>
        /// vfx_effect.DefaultDirection과 실제 진행 방향을 비교하여 좌우 반전을 적용합니다.
        /// </summary>
        /// <param name="direction">정규화된 주 방향입니다.</param>
        /// <param name="sourceDirection">수직 방향일 때 좌우 기준으로 사용할 보조 방향입니다.</param>
        private void ApplyDefaultDirectionFlip(Vector2 direction, Vector2 sourceDirection)
        {
            if (!TryResolveFacingRight(direction, sourceDirection, out bool desiredRight))
                return;

            bool defaultRight = EffectRuntimeData.DefaultDirection == ConfigCommon.DirectionType.Right;
            SetFlip(desiredRight != defaultRight);
        }

        /// <summary>
        /// 방향 벡터에서 좌우 기준을 해석합니다.
        /// </summary>
        /// <param name="direction">정규화된 주 방향입니다.</param>
        /// <param name="sourceDirection">수직 방향일 때 사용할 보조 방향입니다.</param>
        /// <param name="facingRight">오른쪽을 향해야 하면 true입니다.</param>
        /// <returns>좌우 기준을 계산할 수 있으면 true입니다.</returns>
        private static bool TryResolveFacingRight(Vector2 direction, Vector2 sourceDirection, out bool facingRight)
        {
            float x = Mathf.Abs(direction.x) > 0.0001f ? direction.x : sourceDirection.x;
            if (Mathf.Abs(x) <= 0.0001f)
            {
                facingRight = true;
                return false;
            }

            facingRight = x > 0f;
            return true;
        }

        /// <summary>
        /// 좌우 반전이 적용된 상태를 기준으로 상하 각도만 보정합니다.
        /// </summary>
        /// <param name="direction">정규화된 주 방향입니다.</param>
        /// <param name="sourceDirection">수직 방향일 때 사용할 보조 방향입니다.</param>
        private void ApplyRotationAfterFlip(Vector2 direction, Vector2 sourceDirection)
        {
            bool hasFacing = TryResolveFacingRight(direction, sourceDirection, out bool facingRight);
            if (!hasFacing)
                facingRight = EffectRuntimeData.DefaultDirection == ConfigCommon.DirectionType.Right;

            float angle;
            if (Mathf.Abs(direction.x) <= 0.0001f)
            {
                angle = direction.y >= 0f ? 90f : -90f;
                if (!facingRight)
                    angle = -angle;
            }
            else
            {
                angle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
                if (!facingRight)
                    angle = -angle;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete)
        {
            VfxAnimationController.AnimationEventComplete(struckAnimationEventComplete);
        }
    }
}
