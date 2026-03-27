using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// CutsceneManager가 사용하는 공용 오버레이 UI 프레젠터입니다.
    /// Screen Fade, Overlay Text 같은 전역 UI 연출을 한 곳에서 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneOverlayPresenter : MonoBehaviour
    {
        private Image _screenFadeImage;
        private RectTransform _textRoot;
        private Text _overlayText;
        private CanvasGroup _textCanvasGroup;
        private Outline _textOutline;

        public void Initialize()
        {
            EnsureScreenFadeImage();
            EnsureOverlayText();
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            SetScreenFade(Color.black, 0f, false);
            SetOverlayTextVisible(false);
            SetOverlayTextAlpha(0f);
        }

        public void SetScreenFade(Color color, float alpha, bool visible = true)
        {
            EnsureScreenFadeImage();
            alpha = Mathf.Clamp01(alpha);
            color.a = alpha;
            _screenFadeImage.color = color;
            _screenFadeImage.gameObject.SetActive(visible && alpha > 0f);
        }

        public void ConfigureOverlayText(OverlayTextData data)
        {
            EnsureOverlayText();
            if (data == null)
            {
                SetOverlayTextVisible(false);
                return;
            }

            _overlayText.text = data.text ?? string.Empty;
            _overlayText.fontSize = Mathf.Max(1, data.fontSize);
            _overlayText.color = new Color(data.textColor.r, data.textColor.g, data.textColor.b, 1f);
            _textRoot.anchoredPosition = data.anchoredPosition.ToVector2();
            _textRoot.sizeDelta = data.sizeDelta.ToVector2();
        }

        public void SetOverlayTextVisible(bool visible)
        {
            EnsureOverlayText();
            _textRoot.gameObject.SetActive(visible);
        }

        public void SetOverlayTextAlpha(float alpha)
        {
            EnsureOverlayText();
            _textCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        private void EnsureScreenFadeImage()
        {
            if (_screenFadeImage != null)
            {
                return;
            }

            var go = new GameObject("ScreenFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _screenFadeImage = go.GetComponent<Image>();
            _screenFadeImage.raycastTarget = false;
            _screenFadeImage.color = new Color(0f, 0f, 0f, 0f);
        }

        private void EnsureOverlayText()
        {
            if (_overlayText != null)
            {
                return;
            }

            var go = new GameObject("OverlayText", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Text), typeof(Outline));
            go.transform.SetParent(transform, false);
            _textRoot = go.GetComponent<RectTransform>();
            _textRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _textRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _textRoot.pivot = new Vector2(0.5f, 0.5f);
            _textRoot.sizeDelta = new Vector2(1000f, 220f);

            _textCanvasGroup = go.GetComponent<CanvasGroup>();
            _textCanvasGroup.alpha = 0f;
            _textCanvasGroup.interactable = false;
            _textCanvasGroup.blocksRaycasts = false;

            _overlayText = go.GetComponent<Text>();
            _overlayText.raycastTarget = false;
            _overlayText.alignment = TextAnchor.MiddleCenter;
            _overlayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _overlayText.verticalOverflow = VerticalWrapMode.Overflow;
            _overlayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _overlayText.text = string.Empty;
            _overlayText.color = Color.white;

            _textOutline = go.GetComponent<Outline>();
            _textOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            _textOutline.effectDistance = new Vector2(2f, -2f);

            go.SetActive(false);
        }
    }
}
