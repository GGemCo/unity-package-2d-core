using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 카메라와 UI ViewportRoot를 지정한 화면 비율로 제한합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FixedAspectViewport : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private RectTransform viewportRoot;

        [Header("Aspect Ratio")]
        [SerializeField, Min(1)]
        private int targetWidth = 16;

        [SerializeField, Min(1)]
        private int targetHeight = 9;

        private int _cachedScreenWidth;
        private int _cachedScreenHeight;

        public Rect NormalizedViewportRect { get; private set; } =
            new Rect(0f, 0f, 1f, 1f);

        private void Awake()
        {
            Apply(force: true);
        }

        private void OnEnable()
        {
            Apply(force: true);
        }

        private void Update()
        {
            Apply(force: false);
        }

#if UNITY_EDITOR
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

        public void Refresh()
        {
            Apply(force: true);
        }

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

        private void ApplyCameraRect(Rect rect)
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.rect = rect;
        }

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

        private static Rect CalculateViewportRect(
            int screenWidth,
            int screenHeight,
            int targetWidth,
            int targetHeight)
        {
            float screenAspect = (float)screenWidth / screenHeight;
            float targetAspect = (float)targetWidth / targetHeight;

            // 디바이스가 기준보다 넓음: 좌우 여백
            if (screenAspect > targetAspect)
            {
                float normalizedWidth = targetAspect / screenAspect;
                float x = (1f - normalizedWidth) * 0.5f;

                return new Rect(
                    x,
                    0f,
                    normalizedWidth,
                    1f);
            }

            // 디바이스가 기준보다 좁음: 상하 여백
            float normalizedHeight = screenAspect / targetAspect;
            float y = (1f - normalizedHeight) * 0.5f;

            return new Rect(
                0f,
                y,
                1f,
                normalizedHeight);
        }
    }
}