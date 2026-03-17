using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Slider 기반 HUD 자원 UI에 Affect 시각 상태를 적용하는 전략입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SliderHudAffectVisualStrategy : HudAffectVisualStrategyBase
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image handleImage;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RectTransform pulseTarget;

        private Color _defaultFillColor = Color.white;
        private Color _defaultBackgroundColor = Color.white;
        private Color _defaultHandleColor = Color.white;
        private Color _defaultTextColor = Color.white;
        private Vector3 _baseScale = Vector3.one;
        private Coroutine _pulseRoutine;

        protected override void OnInitialize(UIWindowHudResourceBase owner)
        {
            if (slider == null)
            {
                if (owner is UIWindowHudResourceSlider sliderOwner)
                    slider = sliderOwner.GetSlider();
                else
                    slider = GetComponent<UIWindowHudResourceSlider>()?.GetSlider();
            }

            if (fillImage == null && slider != null && slider.fillRect != null)
                fillImage = slider.fillRect.GetComponent<Image>();

            if (handleImage == null && slider != null && slider.handleRect != null)
                handleImage = slider.handleRect.GetComponent<Image>();

            if (pulseTarget == null)
                pulseTarget = transform as RectTransform;

            CacheDefaults();
        }

        public override void Apply(HudAffectVisualProfileBase profile)
        {
            if (profile is not SliderHudVisualProfile sliderProfile)
            {
                ResetToDefault(null);
                return;
            }

            if (fillImage != null)
                fillImage.color = sliderProfile.FillColor;
            if (backgroundImage != null)
                backgroundImage.color = sliderProfile.BackgroundColor;
            if (handleImage != null)
                handleImage.color = sliderProfile.HandleColor;
            if (text != null)
                text.color = sliderProfile.TextColor;

            UpdatePulse(sliderProfile.UsePulse, sliderProfile.PulseScaleMultiplier, sliderProfile.PulseSpeed);
        }

        public override void ResetToDefault(HudAffectVisualProfileBase defaultProfile)
        {
            CacheDefaults();

            if (fillImage != null)
                fillImage.color = _defaultFillColor;
            if (backgroundImage != null)
                backgroundImage.color = _defaultBackgroundColor;
            if (handleImage != null)
                handleImage.color = _defaultHandleColor;
            if (text != null)
                text.color = _defaultTextColor;

            if (defaultProfile is SliderHudVisualProfile sliderProfile)
            {
                Apply(sliderProfile);
                return;
            }

            UpdatePulse(false, 1f, 1f);
        }

        private void CacheDefaults()
        {
            if (fillImage != null)
                _defaultFillColor = fillImage.color;
            if (backgroundImage != null)
                _defaultBackgroundColor = backgroundImage.color;
            if (handleImage != null)
                _defaultHandleColor = handleImage.color;
            if (text != null)
                _defaultTextColor = text.color;
            if (pulseTarget != null)
                _baseScale = pulseTarget.localScale;
        }

        private void UpdatePulse(bool usePulse, float scaleMultiplier, float speed)
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            if (pulseTarget != null)
                pulseTarget.localScale = _baseScale;

            if (usePulse && isActiveAndEnabled && pulseTarget != null)
                _pulseRoutine = StartCoroutine(CoPulse(Mathf.Max(1f, scaleMultiplier), Mathf.Max(0.01f, speed)));
        }

        private IEnumerator CoPulse(float scaleMultiplier, float speed)
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
                float scale = Mathf.Lerp(1f, scaleMultiplier, t);
                pulseTarget.localScale = _baseScale * scale;
                yield return null;
            }
        }

        private void OnDisable()
        {
            UpdatePulse(false, 1f, 1f);
        }
    }
}
