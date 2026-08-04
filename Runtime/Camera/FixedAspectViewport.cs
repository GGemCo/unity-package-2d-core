using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI ViewportRoot를 지정한 기준 종횡비의 중앙 안전 영역에 맞춥니다.
    /// 기존 사용처와의 호환을 위해 선택적으로 카메라 Viewport에도 같은 영역을 적용할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FixedAspectViewport : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("하위 호환을 위해 UI 영역을 함께 적용할 카메라입니다.")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private RectTransform viewportRoot;

        [Tooltip("기존 사용처처럼 이 컴포넌트가 카메라 Viewport도 함께 변경할지 여부입니다.")]
        [SerializeField]
        private bool applyToCamera = true;

        [Header("Aspect Ratio")]
        [SerializeField, Min(1)]
        private int targetWidth = 16;

        [SerializeField, Min(1)]
        private int targetHeight = 9;

        private int _cachedScreenWidth;
        private int _cachedScreenHeight;

        /// <summary>
        /// 현재 화면 안에서 UI가 사용하는 정규화 기준 종횡비 영역을 반환합니다.
        /// </summary>
        public Rect NormalizedViewportRect { get; private set; } =
            new Rect(0f, 0f, 1f, 1f);

        /// <summary>
        /// 컴포넌트가 초기화되면 현재 화면 크기에 맞는 UI 영역을 즉시 적용합니다.
        /// </summary>
        private void Awake()
        {
            Apply(force: true);
        }

        /// <summary>
        /// 컴포넌트가 활성화될 때 UI 영역을 다시 적용합니다.
        /// </summary>
        private void OnEnable()
        {
            Apply(force: true);
        }

        /// <summary>
        /// 화면 크기가 변경된 경우에만 UI 영역을 다시 계산합니다.
        /// </summary>
        private void Update()
        {
            Apply(force: false);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터 입력값을 보정하고 Edit Mode 미리보기를 갱신합니다.
        /// </summary>
        private void OnValidate()
        {
            targetWidth = Mathf.Max(1, targetWidth);
            targetHeight = Mathf.Max(1, targetHeight);

            if (!Application.isPlaying)
            {
                Apply(force: true);
            }
        }
#endif

        /// <summary>
        /// 현재 화면 크기를 기준으로 UI 영역을 강제로 다시 계산합니다.
        /// </summary>
        public void Refresh()
        {
            Apply(force: true);
        }

        /// <summary>
        /// 화면 크기가 변경되었을 때 기준 종횡비 영역을 계산하고 UI 루트에 적용합니다.
        /// </summary>
        /// <param name="force">화면 크기가 같아도 강제로 다시 적용할지 여부입니다.</param>
        private void Apply(bool force)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            if (!force &&
                _cachedScreenWidth == screenWidth &&
                _cachedScreenHeight == screenHeight)
            {
                return;
            }

            _cachedScreenWidth = screenWidth;
            _cachedScreenHeight = screenHeight;

            NormalizedViewportRect = CalculateViewportRect(
                screenWidth,
                screenHeight,
                targetWidth,
                targetHeight);

            ApplyCameraRect(NormalizedViewportRect);
            ApplyViewportRoot(NormalizedViewportRect);
        }

        /// <summary>
        /// 하위 호환 옵션이 활성화된 경우 계산된 영역을 카메라 Viewport에 적용합니다.
        /// 현재 게임 씬에서는 카메라 정책 컴포넌트가 별도로 처리하므로 이 옵션을 사용하지 않습니다.
        /// </summary>
        /// <param name="rect">카메라에 적용할 정규화 화면 영역입니다.</param>
        private void ApplyCameraRect(Rect rect)
        {
            if (!applyToCamera)
            {
                return;
            }

            if (targetCamera == null && SceneGame.Instance != null)
            {
                targetCamera = SceneGame.Instance.mainCamera;
            }

            if (targetCamera == null)
            {
                return;
            }

            targetCamera.rect = rect;
            targetCamera.ResetAspect();
        }

        /// <summary>
        /// UI 루트에 정규화된 기준 종횡비 앵커를 적용합니다.
        /// </summary>
        /// <param name="rect">UI 루트가 사용할 정규화 화면 영역입니다.</param>
        private void ApplyViewportRoot(Rect rect)
        {
            if (viewportRoot == null)
            {
                return;
            }

            viewportRoot.anchorMin = rect.min;
            viewportRoot.anchorMax = rect.max;
            viewportRoot.offsetMin = Vector2.zero;
            viewportRoot.offsetMax = Vector2.zero;
            viewportRoot.localScale = Vector3.one;
            viewportRoot.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 화면 중앙에 배치할 기준 종횡비의 정규화 영역을 계산합니다.
        /// </summary>
        /// <param name="screenWidth">현재 출력 화면의 픽셀 너비입니다.</param>
        /// <param name="screenHeight">현재 출력 화면의 픽셀 높이입니다.</param>
        /// <param name="targetWidth">기준 종횡비의 가로 값입니다.</param>
        /// <param name="targetHeight">기준 종횡비의 세로 값입니다.</param>
        /// <returns>0~1 범위로 정규화된 중앙 UI 영역입니다.</returns>
        public static Rect CalculateViewportRect(
            int screenWidth,
            int screenHeight,
            int targetWidth,
            int targetHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0 ||
                targetWidth <= 0 || targetHeight <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float screenAspect = (float)screenWidth / screenHeight;
            float targetAspect = (float)targetWidth / targetHeight;

            if (screenAspect > targetAspect)
            {
                // UI는 넓은 화면에서도 기존 16:9 디자인 폭을 유지하도록 중앙에 배치합니다.
                float normalizedWidth = targetAspect / screenAspect;
                float x = (1f - normalizedWidth) * 0.5f;
                return new Rect(x, 0f, normalizedWidth, 1f);
            }

            // 기준보다 좁은 화면에서는 UI 전체가 보이도록 상하에 동일한 여백을 둡니다.
            float normalizedHeight = screenAspect / targetAspect;
            float y = (1f - normalizedHeight) * 0.5f;
            return new Rect(0f, y, 1f, normalizedHeight);
        }
    }
}
