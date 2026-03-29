using UnityEngine;
using TMPro;

namespace GGemCo2DCore
{
    /// <summary>
    /// CutsceneManager가 사용하는 오버레이 텍스트 전용 UI 프레젠터입니다.
    /// Screen Fade는 별도 ScreenFadePresenter에서 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneOverlayPresenter : MonoBehaviour
    {
        private RectTransform _textRoot;
        private TextMeshProUGUI _overlayText;
        private CanvasGroup _textCanvasGroup;

        public void Initialize()
        {
            EnsureOverlayText();
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            SetOverlayTextVisible(false);
            SetOverlayTextAlpha(0f);
            SetOverlayTextContent(string.Empty);
        }

        public void ConfigureOverlayText(OverlayTextData data, string displayText)
        {
            EnsureOverlayText();
            if (data == null)
            {
                SetOverlayTextVisible(false);
                SetOverlayTextContent(string.Empty);
                return;
            }

            SetOverlayTextContent(displayText);
            _overlayText.fontSize = Mathf.Max(1, data.fontSize);
            _overlayText.color = new Color(data.textColor.r, data.textColor.g, data.textColor.b, 1f);
            _textRoot.anchoredPosition = data.anchoredPosition.ToVector2();
            _textRoot.sizeDelta = data.sizeDelta.ToVector2();
        }

        public void SetOverlayTextContent(string text)
        {
            EnsureOverlayText();
            _overlayText.text = text ?? string.Empty;
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

        private void EnsureOverlayText()
        {
            if (_overlayText != null)
            {
                return;
            }

            var go = new GameObject("OverlayText", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI));
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

            _overlayText = go.GetComponent<TextMeshProUGUI>();
            _overlayText.raycastTarget = false;
            _overlayText.alignment = TextAlignmentOptions.Center;
            _overlayText.textWrappingMode = TextWrappingModes.Normal;
            _overlayText.overflowMode = TextOverflowModes.Overflow;
            _overlayText.text = string.Empty;
            _overlayText.color = Color.white;

            if (TMP_Settings.defaultFontAsset != null)
            {
                _overlayText.font = TMP_Settings.defaultFontAsset;
            }

            go.SetActive(false);
        }
    }
}
