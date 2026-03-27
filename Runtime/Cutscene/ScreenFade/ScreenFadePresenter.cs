using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Screen Fade 전용 Canvas와 Image를 관리하는 프레젠터입니다.
    /// OverlayText와 분리하여 렌더 계층을 독립적으로 제어합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFadePresenter : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _rootRect;
        private Image _screenFadeImage;
        private ScreenFadeRenderMode _currentRenderMode = ScreenFadeRenderMode.OverlayUi;

        public void Initialize(ScreenFadeData data, SceneGame sceneGame)
        {
            EnsureCanvas();
            EnsureScreenFadeImage();
            ApplyRenderSettings(data, sceneGame);
            ResetPresentation();
        }

        public void ApplyRenderSettings(ScreenFadeData data, SceneGame sceneGame)
        {
            EnsureCanvas();
            EnsureScreenFadeImage();

            var resolved = data ?? new ScreenFadeData();
            _currentRenderMode = resolved.renderMode;

            var mainCamera = sceneGame != null ? sceneGame.mainCamera : Camera.main;

            switch (_currentRenderMode)
            {
                case ScreenFadeRenderMode.ScreenSpaceCamera:
                    _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _canvas.worldCamera = mainCamera;
                    _canvas.planeDistance = Mathf.Max(0.01f, resolved.planeDistance);
                    _canvas.overrideSorting = true;
                    _canvas.sortingLayerName = string.IsNullOrWhiteSpace(resolved.sortingLayerName)
                        ? nameof(ConfigSortingLayer.Keys.UI)
                        : resolved.sortingLayerName;
                    _canvas.sortingOrder = resolved.orderInLayer;
                    break;

                case ScreenFadeRenderMode.OverlayUi:
                default:
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.worldCamera = null;
                    _canvas.planeDistance = Mathf.Max(0.01f, resolved.planeDistance);
                    _canvas.overrideSorting = true;
                    _canvas.sortingLayerName = string.IsNullOrWhiteSpace(resolved.sortingLayerName)
                        ? nameof(ConfigSortingLayer.Keys.UI)
                        : resolved.sortingLayerName;
                    _canvas.sortingOrder = resolved.orderInLayer;
                    break;
            }

            if (_rootRect != null)
            {
                _rootRect.anchorMin = Vector2.zero;
                _rootRect.anchorMax = Vector2.one;
                _rootRect.offsetMin = Vector2.zero;
                _rootRect.offsetMax = Vector2.zero;
                _rootRect.anchoredPosition3D = Vector3.zero;
                _rootRect.localScale = Vector3.one;
            }
        }

        public void ResetPresentation()
        {
            SetFade(Color.black, 0f, false);
        }

        public void SetFade(Color color, float alpha, bool visible)
        {
            EnsureScreenFadeImage();
            alpha = Mathf.Clamp01(alpha);
            color.a = alpha;
            _screenFadeImage.color = color;
            _screenFadeImage.gameObject.SetActive(visible && alpha > 0f);
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _rootRect = GetComponent<RectTransform>();
            if (_rootRect == null)
            {
                _rootRect = gameObject.AddComponent<RectTransform>();
            }
        }

        private void EnsureScreenFadeImage()
        {
            if (_screenFadeImage != null)
            {
                return;
            }

            var child = transform.Find("ScreenFade");
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject("ScreenFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;

            _screenFadeImage = go.GetComponent<Image>();
            _screenFadeImage.raycastTarget = false;
            _screenFadeImage.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
