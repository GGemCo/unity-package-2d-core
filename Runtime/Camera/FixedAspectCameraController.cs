using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라의 출력 영역을 지정한 화면 비율로 제한하고 남는 화면 영역을 단색 여백으로 표시합니다.
    /// Orthographic Size는 변경하지 않으므로 기존 카메라 줌과 컷신 연출을 그대로 사용할 수 있습니다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class FixedAspectCameraController : MonoBehaviour
    {
        private const int LetterboxSortingOrder = short.MaxValue;
        private const string LetterboxRootName = "__FixedAspectLetterbox";
        private const int BarCount = 4;

        [Header("Target Aspect Ratio")]
        [Tooltip("기준 화면 비율의 가로 값입니다.")]
        [Min(1f)]
        [SerializeField] private float targetWidth = 16f;

        [Tooltip("기준 화면 비율의 세로 값입니다.")]
        [Min(1f)]
        [SerializeField] private float targetHeight = 9f;

        [Header("Letterbox")]
        [Tooltip("카메라 출력 영역 밖을 단색 여백으로 가릴지 여부입니다.")]
        [SerializeField] private bool showLetterboxBars = true;

        [Tooltip("카메라 출력 영역 밖에 표시할 여백 색상입니다.")]
        [SerializeField] private Color letterboxColor = Color.black;

        private readonly RectTransform[] _barRects = new RectTransform[BarCount];
        private readonly Image[] _barImages = new Image[BarCount];

        private Camera _camera;
        private Rect _originalViewportRect;
        private Rect _contentViewportRect = new Rect(0f, 0f, 1f, 1f);
        private GameObject _letterboxRoot;
        private Canvas _letterboxCanvas;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        /// <summary>
        /// 현재 화면에 적용된 정규화 16:9 출력 영역을 반환합니다.
        /// </summary>
        public Rect ContentViewportRect => _contentViewportRect;

        /// <summary>
        /// 현재 화면에 적용된 픽셀 단위 16:9 출력 영역을 반환합니다.
        /// </summary>
        public Rect ContentPixelRect => _camera != null ? _camera.pixelRect : Rect.zero;

        /// <summary>
        /// 카메라 참조와 원래 Viewport를 저장합니다.
        /// </summary>
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _originalViewportRect = _camera.rect;
        }

        /// <summary>
        /// 컴포넌트가 활성화되면 현재 해상도에 맞는 출력 영역을 즉시 적용합니다.
        /// </summary>
        private void OnEnable()
        {
            RefreshViewport(force: true);
        }

        /// <summary>
        /// 화면 크기가 변경된 경우에만 출력 영역과 레터박스를 다시 계산합니다.
        /// </summary>
        private void Update()
        {
            if (_lastScreenWidth == Screen.width && _lastScreenHeight == Screen.height)
            {
                return;
            }

            RefreshViewport(force: false);
        }

        /// <summary>
        /// 컴포넌트가 비활성화되면 원래 Viewport를 복원하고 런타임 레터박스를 제거합니다.
        /// </summary>
        private void OnDisable()
        {
            RestoreOriginalViewport();
            DestroyLetterboxCanvas();
            _lastScreenWidth = -1;
            _lastScreenHeight = -1;
        }

        /// <summary>
        /// 인스펙터 입력값을 유효한 범위로 보정합니다.
        /// Play Mode에서 값이 변경되면 변경된 비율과 색상을 즉시 반영합니다.
        /// </summary>
        private void OnValidate()
        {
            targetWidth = Mathf.Max(1f, targetWidth);
            targetHeight = Mathf.Max(1f, targetHeight);

            if (Application.isPlaying && isActiveAndEnabled)
            {
                RefreshViewport(force: true);
            }
        }

        /// <summary>
        /// 전달된 화면 좌표가 실제 게임 카메라 출력 영역 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="screenPoint">검사할 픽셀 단위 화면 좌표입니다.</param>
        /// <returns>카메라 출력 영역 안이면 true를 반환합니다.</returns>
        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            return _camera != null && _camera.pixelRect.Contains(screenPoint);
        }

        /// <summary>
        /// 화면 크기와 목표 비율을 기준으로 중앙 정렬된 정규화 Viewport를 계산합니다.
        /// </summary>
        /// <param name="screenWidth">현재 출력 화면의 픽셀 너비입니다.</param>
        /// <param name="screenHeight">현재 출력 화면의 픽셀 높이입니다.</param>
        /// <param name="targetAspect">유지할 가로/세로 화면 비율입니다.</param>
        /// <returns>0~1 범위로 정규화된 카메라 Viewport입니다.</returns>
        public static Rect CalculateViewportRect(int screenWidth, int screenHeight, float targetAspect)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || targetAspect <= 0f ||
                float.IsNaN(targetAspect) || float.IsInfinity(targetAspect))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float screenAspect = screenWidth / (float)screenHeight;
            if (screenAspect > targetAspect)
            {
                // 화면이 기준보다 넓으면 좌우에 동일한 여백을 둡니다.
                float normalizedWidth = targetAspect / screenAspect;
                return new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
            }

            // 화면이 기준보다 좁으면 상하에 동일한 여백을 둡니다.
            float normalizedHeight = screenAspect / targetAspect;
            return new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
        }

        /// <summary>
        /// 현재 화면 크기를 기준으로 카메라 Viewport와 레터박스 표시를 갱신합니다.
        /// </summary>
        /// <param name="force">화면 크기가 같아도 강제로 다시 적용할지 여부입니다.</param>
        private void RefreshViewport(bool force)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
                if (_camera == null)
                {
                    return;
                }
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            if (!force && _lastScreenWidth == screenWidth && _lastScreenHeight == screenHeight)
            {
                return;
            }

            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;

            float targetAspect = targetWidth / targetHeight;
            _contentViewportRect = CalculateViewportRect(screenWidth, screenHeight, targetAspect);
            _camera.rect = _contentViewportRect;

            // 수동 Aspect 값이 남아 있더라도 변경된 Viewport 기준으로 다시 계산되도록 초기화합니다.
            _camera.ResetAspect();
            UpdateLetterboxPresentation();
        }

        /// <summary>
        /// 현재 Viewport 밖의 네 영역을 검은 여백 이미지로 갱신합니다.
        /// </summary>
        private void UpdateLetterboxPresentation()
        {
            bool hasMargin = _contentViewportRect.xMin > 0f ||
                             _contentViewportRect.yMin > 0f ||
                             _contentViewportRect.xMax < 1f ||
                             _contentViewportRect.yMax < 1f;

            if (!showLetterboxBars || !hasMargin)
            {
                if (_letterboxRoot != null)
                {
                    _letterboxRoot.SetActive(false);
                }

                return;
            }

            EnsureLetterboxCanvas();
            _letterboxRoot.SetActive(true);

            // 왼쪽, 오른쪽, 아래쪽, 위쪽 순서로 Viewport 바깥 영역을 채웁니다.
            SetBarRect(_barRects[0], Vector2.zero, new Vector2(_contentViewportRect.xMin, 1f));
            SetBarRect(_barRects[1], new Vector2(_contentViewportRect.xMax, 0f), Vector2.one);
            SetBarRect(_barRects[2], new Vector2(_contentViewportRect.xMin, 0f),
                new Vector2(_contentViewportRect.xMax, _contentViewportRect.yMin));
            SetBarRect(_barRects[3], new Vector2(_contentViewportRect.xMin, _contentViewportRect.yMax),
                new Vector2(_contentViewportRect.xMax, 1f));

            for (int i = 0; i < _barImages.Length; i++)
            {
                _barImages[i].color = letterboxColor;
            }
        }

        /// <summary>
        /// 레터박스 전용 Overlay Canvas와 네 방향의 여백 이미지를 생성합니다.
        /// 기존 게임 UI Canvas의 설정과 계층은 변경하지 않습니다.
        /// </summary>
        private void EnsureLetterboxCanvas()
        {
            if (_letterboxRoot != null)
            {
                return;
            }

            _letterboxRoot = new GameObject(
                LetterboxRootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            _letterboxCanvas = _letterboxRoot.GetComponent<Canvas>();
            _letterboxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _letterboxCanvas.overrideSorting = true;
            _letterboxCanvas.sortingOrder = LetterboxSortingOrder;

            RectTransform rootRect = _letterboxRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            for (int i = 0; i < BarCount; i++)
            {
                GameObject barObject = new GameObject(
                    $"LetterboxBar_{i}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                barObject.transform.SetParent(_letterboxRoot.transform, false);

                _barRects[i] = barObject.GetComponent<RectTransform>();
                _barImages[i] = barObject.GetComponent<Image>();
                // 검은 여백 뒤에 있는 기존 Overlay UI가 포인터 이벤트를 받지 않도록 차단합니다.
                _barImages[i].raycastTarget = true;
                _barImages[i].color = letterboxColor;
            }
        }

        /// <summary>
        /// 레터박스 이미지의 정규화 앵커 범위를 적용합니다.
        /// </summary>
        /// <param name="barRect">범위를 변경할 이미지 RectTransform입니다.</param>
        /// <param name="anchorMin">왼쪽 아래 정규화 앵커입니다.</param>
        /// <param name="anchorMax">오른쪽 위 정규화 앵커입니다.</param>
        private static void SetBarRect(RectTransform barRect, Vector2 anchorMin, Vector2 anchorMax)
        {
            barRect.anchorMin = anchorMin;
            barRect.anchorMax = anchorMax;
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            barRect.anchoredPosition3D = Vector3.zero;
            barRect.localScale = Vector3.one;
        }

        /// <summary>
        /// 카메라 Viewport를 컴포넌트 활성화 전 상태로 복원합니다.
        /// </summary>
        private void RestoreOriginalViewport()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.rect = _originalViewportRect;
            _camera.ResetAspect();
            _contentViewportRect = _originalViewportRect;
        }

        /// <summary>
        /// 런타임에 생성한 레터박스 Canvas를 안전하게 제거합니다.
        /// </summary>
        private void DestroyLetterboxCanvas()
        {
            if (_letterboxRoot == null)
            {
                return;
            }

            _letterboxRoot.SetActive(false);
            Destroy(_letterboxRoot);
            _letterboxRoot = null;
            _letterboxCanvas = null;

            for (int i = 0; i < BarCount; i++)
            {
                _barRects[i] = null;
                _barImages[i] = null;
            }
        }
    }
}
