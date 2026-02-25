using System;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 메인 <see cref="Slider"/>의 값을 보조(Flip) <see cref="Slider"/>에 동기화하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 좌/우 대칭 HP 바처럼 Fill 방향이 다른 두 개의 Slider를 함께 사용할 때,
    /// 메인 Slider만 제어해도 Flip Slider가 동일한 값으로 따라가도록 합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class UISliderFlip : MonoBehaviour
    {
        /// <summary>
        /// 메인 Slider 값에 동기화될 보조(Flip) Slider 참조입니다.
        /// </summary>
        [Tooltip("Flip한 슬라이더 오브젝트")]
        [SerializeField] private Slider flipSlider;

        /// <summary>
        /// 이 컴포넌트가 부착된 GameObject의 메인 Slider 캐시입니다.
        /// </summary>
        private Slider _mainSlider;

        /// <summary>
        /// 메인/플립 Slider 참조를 검증하고 초기 값을 동기화합니다.
        /// </summary>
        private void Awake()
        {
            _mainSlider = GetComponent<Slider>();
            if (GcLogger.IsNull(_mainSlider, nameof(Slider)))
            {
                enabled = false;
                return;
            }

            if (GcLogger.IsNull(flipSlider, nameof(flipSlider)))
            {
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// 메인 Slider의 값 변경 이벤트를 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_mainSlider == null) return;
            _mainSlider.onValueChanged.AddListener(OnMainValueChanged);

            SyncInitialValue();
        }

        /// <summary>
        /// 메인 Slider의 값 변경 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_mainSlider == null) return;
            _mainSlider.onValueChanged.RemoveListener(OnMainValueChanged);
        }

        /// <summary>
        /// 시작 시 Flip Slider의 값을 메인 Slider 값으로 1회 동기화합니다.
        /// </summary>
        private void SyncInitialValue()
        {
            flipSlider.value = _mainSlider.value;
        }

        /// <summary>
        /// 메인 Slider 값이 변경될 때 Flip Slider 값을 동일하게 갱신합니다.
        /// </summary>
        /// <param name="value">메인 Slider의 변경된 값입니다.</param>
        private void OnMainValueChanged(float value)
        {
            if (flipSlider == null) return;
            flipSlider.value = value;
        }

        /// <summary>
        /// (current / total) 비율로 메인/Flip Slider 값을 설정합니다.
        /// </summary>
        /// <param name="currentValue">현재 값(분자)입니다.</param>
        /// <param name="totalValue">최대/전체 값(분모)입니다. 0 이하이면 0으로 설정됩니다.</param>
        public void SetValue(long currentValue, long totalValue)
        {
            if (_mainSlider == null) return;

            if (totalValue <= 0)
            {
                _mainSlider.value = 0f;
                return;
            }

            currentValue = Math.Max(0, currentValue);

            var normalized = (float)currentValue / totalValue;
            SetNormalizedValue(normalized);
        }

        /// <summary>
        /// 0~1 범위의 정규화 값으로 메인/Flip Slider 값을 설정합니다.
        /// </summary>
        /// <param name="normalizedValue">설정할 정규화 값입니다(0~1 범위를 벗어나면 Clamp됩니다).</param>
        private void SetNormalizedValue(float normalizedValue)
        {
            if (_mainSlider == null) return;
            _mainSlider.value = Mathf.Clamp01(normalizedValue);
        }
    }
}