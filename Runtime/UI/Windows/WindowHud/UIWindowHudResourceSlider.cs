using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Slider 기반 HUD 리소스 표현입니다.
    /// 값 변화 방향에 따라 프리셋 기반 UI 효과를 재생합니다.
    /// </summary>
    public sealed class UIWindowHudResourceSlider : UIWindowHudResourceBase
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private UISliderDelayedFill delayedFill;
        [SerializeField] private bool useEffects = true;

        [Header("UI Effect Presets")]
        [SerializeField] private UIEffectPreset increasePreset;
        [SerializeField] private UIEffectPreset decreasePreset;
        [SerializeField] private UIEffectPreset maxValueChangedPreset;

        protected override void ApplyValue(UIWindowHudResourceType type, long current, long total, UIEffectContext context)
        {
            if (!context.IsInitial && context.DeltaCurrent == 0 && context.DeltaTotal == 0)
            {
                return;
            }

            if (total <= 0)
            {
                if (slider != null) slider.value = 0f;
                if (text != null) text.text = "0 / 0";
                delayedFill?.SyncImmediately();
                return;
            }

            if (slider != null) slider.value = (float)current / total;
            if (text != null) text.text = $"{current} / {total}";

            if (context.IsInitial)
            {
                delayedFill?.SyncImmediately();
                return;
            }

            if (!useEffects)
                return;

            UIEffectService.PlayHudResource(
                this,
                gameObject,
                context,
                increasePreset,
                decreasePreset,
                maxValueChangedPreset);
        }

        public override void SetMaxValue(UIWindowHudResourceType hpTemp, long total)
        {
        }
    }
}
