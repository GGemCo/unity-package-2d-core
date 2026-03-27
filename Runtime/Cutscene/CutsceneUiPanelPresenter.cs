using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// CutsceneManager가 사용하는 UI Panel 전용 프레젠터입니다.
    /// panelId 기준으로 패널을 재사용하거나 제거할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneUiPanelPresenter : MonoBehaviour
    {
        private sealed class PanelHandle
        {
            public string Id;
            public GameObject GameObject;
            public RectTransform RectTransform;
            public CanvasGroup CanvasGroup;
            public Image Image;
            public Canvas Canvas;
        }

        private readonly Dictionary<string, PanelHandle> _panels = new();
        private Canvas _canvas;
        private RectTransform _rootRect;
        private ScreenFadeRenderMode _currentRenderMode = ScreenFadeRenderMode.OverlayUi;

        public void Initialize()
        {
            EnsureCanvas();
            ResetPresentation();
        }

        public void ApplyRenderSettings(UiPanelData data, SceneGame sceneGame)
        {
            EnsureCanvas();

            var resolved = data ?? new UiPanelData();
            _currentRenderMode = resolved.renderMode;
            var mainCamera = sceneGame != null ? sceneGame.mainCamera : Camera.main;
            string sortingLayerName = ResolveSortingLayerName(resolved.sortingLayerName);

            switch (_currentRenderMode)
            {
                case ScreenFadeRenderMode.ScreenSpaceCamera:
                    _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _canvas.worldCamera = mainCamera;
                    _canvas.planeDistance = Mathf.Max(0.01f, resolved.planeDistance);
                    break;

                case ScreenFadeRenderMode.OverlayUi:
                default:
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.worldCamera = null;
                    _canvas.planeDistance = Mathf.Max(0.01f, resolved.planeDistance);
                    break;
            }

            _canvas.overrideSorting = true;
            _canvas.sortingLayerName = sortingLayerName;
            _canvas.sortingOrder = resolved.useIndependentCanvasSorting ? 0 : resolved.orderInLayer;

            if (_rootRect != null)
            {
                _rootRect.anchorMin = Vector2.zero;
                _rootRect.anchorMax = Vector2.one;
                _rootRect.offsetMin = Vector2.zero;
                _rootRect.offsetMax = Vector2.zero;
                _rootRect.anchoredPosition3D = Vector3.zero;
                _rootRect.localScale = Vector3.one;
                _rootRect.localRotation = Quaternion.identity;
            }
        }

        public void ResetPresentation()
        {
            if (_panels.Count <= 0)
            {
                return;
            }

            foreach (var pair in _panels)
            {
                if (pair.Value?.GameObject != null)
                {
                    Destroy(pair.Value.GameObject);
                }
            }

            _panels.Clear();
        }

        public bool HasPanel(string panelId)
        {
            return TryGetHandle(panelId, out _);
        }

        public bool EnsurePanel(string panelId)
        {
            return GetOrCreateHandle(panelId, true) != null;
        }

        public void ConfigurePanel(string panelId, UiPanelData data)
        {
            var handle = GetOrCreateHandle(panelId, data == null || data.createIfMissing);
            if (handle == null)
            {
                return;
            }

            ApplyLayout(handle, data);
            ApplyVisualOptions(handle, data);
            ApplySorting(handle, data);
        }

        public void ApplyState(string panelId, Vec2 anchoredPosition, Vec2 sizeDelta, Color color, float alpha)
        {
            if (!TryGetHandle(panelId, out var handle))
            {
                return;
            }

            handle.RectTransform.anchoredPosition = anchoredPosition.ToVector2();
            handle.RectTransform.sizeDelta = sizeDelta.ToVector2();
            handle.Image.color = new Color(color.r, color.g, color.b, 1f);
            handle.CanvasGroup.alpha = Mathf.Clamp01(alpha);
            handle.GameObject.SetActive(true);
        }

        public void SetPanelVisible(string panelId, bool visible)
        {
            if (!TryGetHandle(panelId, out var handle))
            {
                return;
            }

            handle.GameObject.SetActive(visible);
        }

        public void DestroyPanel(string panelId)
        {
            if (!TryGetHandle(panelId, out var handle))
            {
                return;
            }

            if (handle.GameObject != null)
            {
                Destroy(handle.GameObject);
            }

            _panels.Remove(panelId);
        }

        private PanelHandle GetOrCreateHandle(string panelId, bool createIfMissing)
        {
            if (TryGetHandle(panelId, out var existing))
            {
                return existing;
            }

            if (!createIfMissing)
            {
                return null;
            }

            panelId = NormalizePanelId(panelId);
            var go = new GameObject($"UiPanel_{panelId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400f, 200f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            var canvasGroup = go.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(0f, 0f, 0f, 1f);

            var handle = new PanelHandle
            {
                Id = panelId,
                GameObject = go,
                RectTransform = rect,
                CanvasGroup = canvasGroup,
                Image = image,
            };

            _panels[panelId] = handle;
            go.SetActive(false);
            return handle;
        }

        private bool TryGetHandle(string panelId, out PanelHandle handle)
        {
            return _panels.TryGetValue(NormalizePanelId(panelId), out handle);
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

        private static void ApplyLayout(PanelHandle handle, UiPanelData data)
        {
            if (handle == null || data == null)
            {
                return;
            }

            handle.RectTransform.anchorMin = data.anchorMin.ToVector2();
            handle.RectTransform.anchorMax = data.anchorMax.ToVector2();
            handle.RectTransform.pivot = data.pivot.ToVector2();

            if (data.siblingIndex >= 0)
            {
                handle.RectTransform.SetSiblingIndex(data.siblingIndex);
            }
        }

        private static void ApplyVisualOptions(PanelHandle handle, UiPanelData data)
        {
            if (handle == null || data == null)
            {
                return;
            }

            handle.Image.raycastTarget = data.raycastTarget;
            handle.CanvasGroup.blocksRaycasts = data.raycastTarget;
            handle.CanvasGroup.interactable = data.raycastTarget;
        }

        private static void ApplySorting(PanelHandle handle, UiPanelData data)
        {
            if (handle == null)
            {
                return;
            }

            if (data == null || !data.useIndependentCanvasSorting)
            {
                if (handle.Canvas != null)
                {
                    Object.Destroy(handle.Canvas);
                    handle.Canvas = null;
                }

                return;
            }

            handle.Canvas ??= handle.GameObject.GetComponent<Canvas>() ?? handle.GameObject.AddComponent<Canvas>();
            handle.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            handle.Canvas.worldCamera = null;
            handle.Canvas.planeDistance = Mathf.Max(0.01f, data.planeDistance);
            handle.Canvas.overrideSorting = true;
            handle.Canvas.sortingLayerName = ResolveSortingLayerName(data.sortingLayerName);
            handle.Canvas.sortingOrder = data.orderInLayer;
        }

        private static string NormalizePanelId(string panelId)
        {
            return string.IsNullOrWhiteSpace(panelId) ? "Panel" : panelId.Trim();
        }

        private static string ResolveSortingLayerName(string sortingLayerName)
        {
            return string.IsNullOrWhiteSpace(sortingLayerName)
                ? nameof(ConfigSortingLayer.Keys.UI)
                : sortingLayerName;
        }
    }
}
