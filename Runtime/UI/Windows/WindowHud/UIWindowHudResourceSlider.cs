using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public sealed class UIWindowHudResourceSlider : UIWindowHudResourceBase
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;

        private bool _hasLast;
        private long _lastCurrent;
        private long _lastTotal;

        public override void SetValue(UIWindowHudResourceType type, long current, long total)
        {
            // 값이 동일하면 UI 갱신을 생략하여 Canvas 리빌드/문자열 할당을 줄입니다.
            if (_hasLast && _lastCurrent == current && _lastTotal == total)
            {
                return;
            }

            _hasLast = true;
            _lastCurrent = current;
            _lastTotal = total;

            if (total <= 0)
            {
                if (slider != null) slider.value = 0f;
                if (text != null) text.text = "0 / 0";
                return;
            }

            if (slider != null) slider.value = (float)current / total;
            if (text != null) text.text = $"{current} / {total}";
        }

        public override void SetMaxValue(UIWindowHudResourceType hpTemp, long total)
        {
        }
    }
}