using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public sealed class UIWindowHudResourceSlider : UIWindowHudResourceBase
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;

        public override void SetValue(UIWindowHudResourceType type, long current, long total)
        {
            if (total <= 0)
            {
                slider.value = 0f;
                if (text != null) text.text = "0 / 0";
                return;
            }

            slider.value = (float)current / total;
            if (text != null) text.text = $"{current} / {total}";
        }
    }
}