using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Slider 기반 HUD 자원 표현 클래스입니다.
    /// </summary>
    public sealed class UIWindowHudResourceSlider : UIWindowHudResourceBase
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private UIEffectTarget effectTarget;
        [SerializeField] private UIEffectPreset increasePreset;
        [SerializeField] private UIEffectPreset decreasePreset;
        [SerializeField] private UIEffectPreset maxValueChangedPreset;

        private void Awake()
        {
            if (effectTarget == null)
                effectTarget = UIEffectTarget.GetOrAdd(gameObject);
            else
                effectTarget.AutoBind();
        }

        protected override void ApplyValue(
            UIWindowHudResourceType type,
            long current,
            long total,
            ResourceChangeContext context)
        {
            if (context.HasPreviousValue && context.Current == context.PreviousCurrent && context.Total == context.PreviousTotal)
            {
                return;
            }

            if (total <= 0)
            {
                if (slider != null) slider.value = 0f;
                if (text != null) text.text = "0 / 0";
                return;
            }

            if (slider != null) slider.value = (float)current / total;
            if (text != null) text.text = $"{current} / {total}";

            if (!context.HasPreviousValue || effectTarget == null)
            {
                return;
            }

            if (context.IsDecrease && decreasePreset != null)
            {
                UIEffectService.Play(this, effectTarget, decreasePreset);
                return;
            }

            if (context.IsIncrease && increasePreset != null)
            {
                UIEffectService.Play(this, effectTarget, increasePreset);
                return;
            }

            if (context.IsMaxValueChanged && maxValueChangedPreset != null)
            {
                UIEffectService.Play(this, effectTarget, maxValueChangedPreset);
            }
        }

        public override void SetMaxValue(UIWindowHudResourceType hpTemp, long total)
        {
        }
        protected override void ApplyAffectVisualProfile()
        {
        }
    }
}
