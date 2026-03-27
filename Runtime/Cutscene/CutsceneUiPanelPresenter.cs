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
        }

        private readonly Dictionary<string, PanelHandle> _panels = new();

        public void Initialize()
        {
            ResetPresentation();
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

        private static string NormalizePanelId(string panelId)
        {
            return string.IsNullOrWhiteSpace(panelId) ? "Panel" : panelId.Trim();
        }
    }
}
