using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 파랄럭스 레이어가 기준으로 삼을 대표 좌표 계산 방식을 정의합니다.
    /// </summary>
    public enum ParallaxAnchorMode
    {
        /// <summary>
        /// Transform.position 을 기준점으로 사용합니다.
        /// </summary>
        TransformPosition = 0,

        /// <summary>
        /// Sprite 의 Pivot 위치를 기준점으로 사용합니다.
        /// SpriteRenderer 에서는 Transform.position 과 동일한 의미를 가집니다.
        /// </summary>
        SpritePivot = 1,

        /// <summary>
        /// Renderer Bounds 의 중앙을 기준점으로 사용합니다.
        /// </summary>
        RendererBoundsCenter = 2,

        /// <summary>
        /// Renderer Bounds 의 하단 중앙을 기준점으로 사용합니다.
        /// </summary>
        RendererBoundsBottomCenter = 3,

        /// <summary>
        /// Renderer Bounds 의 좌하단을 기준점으로 사용합니다.
        /// </summary>
        RendererBoundsBottomLeft = 4,

        /// <summary>
        /// Renderer Bounds 안의 정규화 좌표를 기준점으로 사용합니다.
        /// </summary>
        RendererBoundsNormalizedPoint = 5,

        /// <summary>
        /// 사용자가 지정한 로컬 오프셋을 기준점으로 사용합니다.
        /// </summary>
        CustomLocalOffset = 6,
    }

    /// <summary>
    /// 파랄럭스 대상 레이어의 기준 위치와 카메라 영향 비율을 관리합니다.
    /// </summary>
    public class ParallaxLayer2D : MonoBehaviour
    {
        [Header("축별 적용")]
        [SerializeField] private bool useHorizontalParallax = true;
        [SerializeField] private bool useVerticalParallax = true;

        [Header("카메라 영향 비율")]
        [Tooltip("0이면 일반 월드 오브젝트처럼 동작하고, 1이면 카메라를 완전히 따라가 화면상 위치가 거의 고정됩니다.")]
        [SerializeField] private float horizontalCameraInfluence = 0.75f;
        [Tooltip("0이면 일반 월드 오브젝트처럼 동작하고, 1이면 카메라를 완전히 따라가 화면상 위치가 거의 고정됩니다.")]
        [SerializeField] private float verticalCameraInfluence = 0.5f;

        [Header("기준 좌표")]
        [Tooltip("True 이면 localPosition 기준으로, False 이면 world position 기준으로 파랄럭스를 적용합니다.")]
        [SerializeField] private bool useLocalPosition = true;

        [Header("기준점 계산")]
        [Tooltip("파랄럭스 기준점으로 사용할 좌표 계산 방식을 설정합니다.")]
        [SerializeField] private ParallaxAnchorMode anchorMode = ParallaxAnchorMode.TransformPosition;
        [Tooltip("비워두면 현재 오브젝트의 SpriteRenderer 를 자동으로 찾습니다.")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [Tooltip("SpriteRenderer 가 없거나 Bounds 기준 계산을 명시적으로 지정하고 싶을 때 사용할 Renderer 입니다.")]
        [SerializeField] private Renderer targetRenderer;
        [Tooltip("RendererBoundsNormalizedPoint 모드에서 사용할 정규화 좌표입니다. (0,0)=좌하단, (0.5,0)=하단 중앙, (0.5,0.5)=중앙")]
        [SerializeField] private Vector2 normalizedAnchor = new(0.5f, 0f);
        [Tooltip("CustomLocalOffset 모드에서 사용할 로컬 기준점 오프셋입니다.")]
        [SerializeField] private Vector2 customLocalOffset = Vector2.zero;

        private Vector3 _baselineLocalPosition;
        private Vector3 _baselineWorldPosition;
        private Vector3 _baselineAnchorLocalPosition;
        private Vector3 _baselineAnchorWorldPosition;
        private bool _hasBaseline;

        private void OnValidate()
        {
            normalizedAnchor.x = Mathf.Clamp01(normalizedAnchor.x);
            normalizedAnchor.y = Mathf.Clamp01(normalizedAnchor.y);
        }

        /// <summary>
        /// 현재 위치와 현재 기준점 좌표를 파랄럭스 기준값으로 저장합니다.
        /// </summary>
        public void CaptureBaseline()
        {
            _baselineLocalPosition = transform.localPosition;
            _baselineWorldPosition = transform.position;
            _baselineAnchorWorldPosition = ResolveCurrentAnchorWorldPosition();
            _baselineAnchorLocalPosition = ResolveCurrentAnchorLocalPosition();
            _hasBaseline = true;
        }

        /// <summary>
        /// 저장된 기준 위치로 레이어를 복원합니다.
        /// </summary>
        public void ResetToBaseline()
        {
            if (!_hasBaseline)
            {
                CaptureBaseline();
            }

            if (useLocalPosition)
            {
                transform.localPosition = _baselineLocalPosition;
                return;
            }

            transform.position = _baselineWorldPosition;
        }

        /// <summary>
        /// 카메라 이동량을 기준으로 레이어의 파랄럭스 위치를 계산하여 적용합니다.
        /// </summary>
        /// <param name="cameraDelta">기준 시점 대비 카메라 이동량입니다.</param>
        public void ApplyParallax(Vector3 cameraDelta)
        {
            if (!_hasBaseline)
            {
                CaptureBaseline();
            }

            Vector3 offset = new Vector3(
                useHorizontalParallax ? cameraDelta.x * horizontalCameraInfluence : 0f,
                useVerticalParallax ? cameraDelta.y * verticalCameraInfluence : 0f,
                0f);

            if (useLocalPosition)
            {
                Vector3 targetAnchorLocalPosition = _baselineAnchorLocalPosition + offset;
                targetAnchorLocalPosition.z = _baselineAnchorLocalPosition.z;
                ApplyAnchorLocalPosition(targetAnchorLocalPosition);
                return;
            }

            Vector3 targetAnchorWorldPosition = _baselineAnchorWorldPosition + offset;
            targetAnchorWorldPosition.z = _baselineAnchorWorldPosition.z;
            ApplyAnchorWorldPosition(targetAnchorWorldPosition);
        }

        /// <summary>
        /// 현재 계산된 기준점의 월드 좌표를 반환합니다.
        /// </summary>
        /// <returns>현재 프레임 기준 기준점 월드 좌표입니다.</returns>
        public Vector3 GetCurrentAnchorWorldPosition()
        {
            return ResolveCurrentAnchorWorldPosition();
        }

        /// <summary>
        /// 현재 계산된 기준점의 로컬 좌표를 반환합니다.
        /// </summary>
        /// <returns>부모 기준의 기준점 로컬 좌표입니다.</returns>
        public Vector3 GetCurrentAnchorLocalPosition()
        {
            return ResolveCurrentAnchorLocalPosition();
        }

        /// <summary>
        /// 현재 레이어가 기준 위치를 이미 저장했는지 반환합니다.
        /// </summary>
        public bool HasBaseline => _hasBaseline;

        /// <summary>
        /// 수평 카메라 영향 비율을 반환합니다.
        /// </summary>
        public float HorizontalCameraInfluence => horizontalCameraInfluence;

        /// <summary>
        /// 수직 카메라 영향 비율을 반환합니다.
        /// </summary>
        public float VerticalCameraInfluence => verticalCameraInfluence;

        /// <summary>
        /// 현재 기준점 계산 방식을 반환합니다.
        /// </summary>
        public ParallaxAnchorMode AnchorMode => anchorMode;

        /// <summary>
        /// 기준점의 목표 로컬 좌표에 맞도록 Transform.localPosition 을 보정합니다.
        /// </summary>
        /// <param name="targetAnchorLocalPosition">이동 후 기준점이 위치해야 하는 목표 로컬 좌표입니다.</param>
        private void ApplyAnchorLocalPosition(Vector3 targetAnchorLocalPosition)
        {
            Vector3 currentAnchorLocalPosition = ResolveCurrentAnchorLocalPosition();
            Vector3 deltaLocalPosition = targetAnchorLocalPosition - currentAnchorLocalPosition;
            Vector3 nextLocalPosition = transform.localPosition + deltaLocalPosition;
            nextLocalPosition.z = _baselineLocalPosition.z;
            transform.localPosition = nextLocalPosition;
        }

        /// <summary>
        /// 기준점의 목표 월드 좌표에 맞도록 Transform.position 을 보정합니다.
        /// </summary>
        /// <param name="targetAnchorWorldPosition">이동 후 기준점이 위치해야 하는 목표 월드 좌표입니다.</param>
        private void ApplyAnchorWorldPosition(Vector3 targetAnchorWorldPosition)
        {
            Vector3 currentAnchorWorldPosition = ResolveCurrentAnchorWorldPosition();
            Vector3 deltaWorldPosition = targetAnchorWorldPosition - currentAnchorWorldPosition;
            Vector3 nextWorldPosition = transform.position + deltaWorldPosition;
            nextWorldPosition.z = _baselineWorldPosition.z;
            transform.position = nextWorldPosition;
        }

        /// <summary>
        /// 현재 설정 기준으로 기준점의 월드 좌표를 계산합니다.
        /// </summary>
        /// <returns>현재 레이어에서 계산된 기준점 월드 좌표입니다.</returns>
        private Vector3 ResolveCurrentAnchorWorldPosition()
        {
            switch (anchorMode)
            {
                case ParallaxAnchorMode.TransformPosition:
                    return transform.position;

                case ParallaxAnchorMode.SpritePivot:
                    return ResolveSpritePivotWorldPosition();

                case ParallaxAnchorMode.RendererBoundsCenter:
                    if (TryResolveRendererBounds(out Bounds centerBounds))
                    {
                        return new Vector3(centerBounds.center.x, centerBounds.center.y, transform.position.z);
                    }
                    break;

                case ParallaxAnchorMode.RendererBoundsBottomCenter:
                    if (TryResolveRendererBounds(out Bounds bottomCenterBounds))
                    {
                        return new Vector3(bottomCenterBounds.center.x, bottomCenterBounds.min.y, transform.position.z);
                    }
                    break;

                case ParallaxAnchorMode.RendererBoundsBottomLeft:
                    if (TryResolveRendererBounds(out Bounds bottomLeftBounds))
                    {
                        return new Vector3(bottomLeftBounds.min.x, bottomLeftBounds.min.y, transform.position.z);
                    }
                    break;

                case ParallaxAnchorMode.RendererBoundsNormalizedPoint:
                    if (TryResolveRendererBounds(out Bounds normalizedBounds))
                    {
                        return new Vector3(
                            Mathf.Lerp(normalizedBounds.min.x, normalizedBounds.max.x, Mathf.Clamp01(normalizedAnchor.x)),
                            Mathf.Lerp(normalizedBounds.min.y, normalizedBounds.max.y, Mathf.Clamp01(normalizedAnchor.y)),
                            transform.position.z);
                    }
                    break;

                case ParallaxAnchorMode.CustomLocalOffset:
                    return transform.TransformPoint(new Vector3(customLocalOffset.x, customLocalOffset.y, 0f));
            }

            return transform.position;
        }

        /// <summary>
        /// 현재 설정 기준으로 기준점의 로컬 좌표를 계산합니다.
        /// </summary>
        /// <returns>부모 기준의 기준점 로컬 좌표입니다.</returns>
        private Vector3 ResolveCurrentAnchorLocalPosition()
        {
            Vector3 anchorWorldPosition = ResolveCurrentAnchorWorldPosition();
            Transform parentTransform = transform.parent;
            if (parentTransform == null)
            {
                return anchorWorldPosition;
            }

            return parentTransform.InverseTransformPoint(anchorWorldPosition);
        }

        /// <summary>
        /// SpriteRenderer 의 Pivot 위치를 월드 좌표로 계산합니다.
        /// </summary>
        /// <returns>Sprite Pivot 의 월드 좌표입니다.</returns>
        private Vector3 ResolveSpritePivotWorldPosition()
        {
            SpriteRenderer spriteRenderer = ResolveSpriteRenderer();
            if (spriteRenderer == null)
            {
                return transform.position;
            }

            return spriteRenderer.transform.position;
        }

        /// <summary>
        /// 현재 레이어에서 Bounds 기반 계산에 사용할 Renderer 를 찾습니다.
        /// </summary>
        /// <param name="bounds">찾은 Renderer 의 월드 Bounds 입니다.</param>
        /// <returns>사용 가능한 Renderer 가 있으면 True 를 반환합니다.</returns>
        private bool TryResolveRendererBounds(out Bounds bounds)
        {
            Renderer resolvedRenderer = ResolveRenderer();
            if (resolvedRenderer != null)
            {
                bounds = resolvedRenderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        /// <summary>
        /// Bounds 계산에 사용할 Renderer 참조를 결정합니다.
        /// </summary>
        /// <returns>사용 가능한 Renderer 인스턴스입니다.</returns>
        private Renderer ResolveRenderer()
        {
            if (targetSpriteRenderer != null)
            {
                return targetSpriteRenderer;
            }

            if (targetRenderer != null)
            {
                return targetRenderer;
            }

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                return spriteRenderer;
            }

            if (TryGetComponent(out Renderer renderer))
            {
                return renderer;
            }

            return null;
        }

        /// <summary>
        /// Sprite Pivot 계산에 사용할 SpriteRenderer 참조를 결정합니다.
        /// </summary>
        /// <returns>사용 가능한 SpriteRenderer 인스턴스입니다.</returns>
        private SpriteRenderer ResolveSpriteRenderer()
        {
            if (targetSpriteRenderer != null)
            {
                return targetSpriteRenderer;
            }

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                return spriteRenderer;
            }

            return null;
        }
    }
}
