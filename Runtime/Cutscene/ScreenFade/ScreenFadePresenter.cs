using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 화면 전체 페이드 연출에 사용하는 Canvas와 Image를 관리하는 프레젠터입니다.
    /// 렌더 모드와 정렬 순서를 설정하고, 페이드 이미지를 전체 화면에 표시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFadePresenter : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _rootRect;
        private Image _screenFadeImage;
        private ScreenFadeRenderMode _currentRenderMode = ScreenFadeRenderMode.OverlayUi;

        /// <summary>
        /// 프레젠터를 초기화하고 Canvas, Image 및 렌더 설정을 준비합니다.
        /// 마지막으로 페이드 표시 상태를 기본값으로 초기화합니다.
        /// </summary>
        /// <param name="data">초기 렌더 설정에 사용할 페이드 데이터입니다.</param>
        /// <param name="sceneGame">메인 카메라 참조를 얻기 위한 현재 씬 정보입니다.</param>
        public void Initialize(ScreenFadeData data, SceneGame sceneGame)
        {
            EnsureCanvas();
            EnsureScreenFadeImage();
            ApplyRenderSettings(data, sceneGame);
            ResetPresentation();
        }

        /// <summary>
        /// 페이드 표시용 Canvas의 렌더 모드, 카메라, 정렬 레이어 및 정렬 순서를 적용합니다.
        /// 루트 RectTransform도 전체 화면 크기로 다시 정렬합니다.
        /// </summary>
        /// <param name="data">적용할 페이드 렌더 설정 데이터입니다.</param>
        /// <param name="sceneGame">카메라 참조에 사용할 현재 씬 정보입니다.</param>
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

        /// <summary>
        /// 페이드 표시 상태를 기본값으로 초기화합니다.
        /// 기본 상태는 검은색, 알파 0, 비표시입니다.
        /// </summary>
        public void ResetPresentation()
        {
            SetFade(Color.black, 0f, false);
        }

        /// <summary>
        /// 페이드 이미지의 색상과 알파를 설정하고 표시 여부를 반영합니다.
        /// 알파 값은 0~1 범위로 보정되며, 알파가 0이면 오브젝트를 비활성화합니다.
        /// </summary>
        /// <param name="color">적용할 페이드 색상입니다.</param>
        /// <param name="alpha">적용할 알파 값입니다.</param>
        /// <param name="visible">페이드 이미지를 표시할지 여부입니다.</param>
        public void SetFade(Color color, float alpha, bool visible)
        {
            EnsureScreenFadeImage();

            alpha = Mathf.Clamp01(alpha);
            color.a = alpha;
            _screenFadeImage.color = color;
            _screenFadeImage.gameObject.SetActive(visible && alpha > 0f);
        }

        /// <summary>
        /// 페이드 표시용 Canvas와 루트 RectTransform이 존재하도록 보장합니다.
        /// 없으면 현재 GameObject에 필요한 컴포넌트를 추가합니다.
        /// </summary>
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

        /// <summary>
        /// 화면 전체를 덮는 페이드 이미지가 존재하도록 보장합니다.
        /// 자식 오브젝트가 없으면 생성하고, RectTransform과 Image 기본값을 설정합니다.
        /// </summary>
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