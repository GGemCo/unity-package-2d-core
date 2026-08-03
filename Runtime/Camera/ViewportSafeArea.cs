using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 전체 화면의 Safe Area와 부모 Viewport 영역의 교집합을
    /// RectTransform Anchor로 적용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ViewportSafeArea : MonoBehaviour
    {
        [SerializeField]
        private RectTransform target;

        [SerializeField]
        private RectTransform viewportRoot;

        private Rect _cachedSafeArea;
        private int _cachedScreenWidth;
        private int _cachedScreenHeight;

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

        public void Refresh()
        {
            Apply(force: true);
        }

        private void Apply(bool force)
        {
            if (target == null ||
                viewportRoot == null ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;

            if (!force &&
                _cachedSafeArea == safeArea &&
                _cachedScreenWidth == Screen.width &&
                _cachedScreenHeight == Screen.height)
            {
                return;
            }

            _cachedSafeArea = safeArea;
            _cachedScreenWidth = Screen.width;
            _cachedScreenHeight = Screen.height;

            Rect viewportPixelRect = GetScreenRect(viewportRoot);
            Rect intersection = Intersect(viewportPixelRect, safeArea);

            if (viewportPixelRect.width <= 0f ||
                viewportPixelRect.height <= 0f ||
                intersection.width <= 0f ||
                intersection.height <= 0f)
            {
                ResetToFullRect();
                return;
            }

            Vector2 anchorMin = new(
                (intersection.xMin - viewportPixelRect.xMin) /
                viewportPixelRect.width,
                (intersection.yMin - viewportPixelRect.yMin) /
                viewportPixelRect.height);

            Vector2 anchorMax = new(
                (intersection.xMax - viewportPixelRect.xMin) /
                viewportPixelRect.width,
                (intersection.yMax - viewportPixelRect.yMin) /
                viewportPixelRect.height);

            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private void ResetToFullRect()
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static Rect GetScreenRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            return Rect.MinMaxRect(
                corners[0].x,
                corners[0].y,
                corners[2].x,
                corners[2].y);
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            if (xMax <= xMin || yMax <= yMin)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}