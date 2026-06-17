using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 특정 속성 데미지 누적 게이지 1종을 Slider로 표시하는 HUD View입니다.
    /// </summary>
    /// <remarks>
    /// 실제 누적 계산은 <see cref="CharacterElementGaugeController"/>가 담당하고,
    /// 이 클래스는 바인딩된 컨트롤러의 <see cref="ElementGaugeSnapshot"/>만 화면에 반영합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class UISliderElementCharge : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private ConfigCommon.DamageType damageType = ConfigCommon.DamageType.Poison;

        [Header("UI")]
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI textValue;
        [SerializeField] private CanvasGroup canvasGroup;
        [FormerlySerializedAs("blockedOverlay")]
        [SerializeField] private GameObject thresholdOverlay;

        [Header("Policy")]
        [SerializeField] private bool hideWhenEmpty = false;
        [SerializeField] private bool showNumericText = false;
        [SerializeField] private bool showWhenThresholdReached = true;

        private CharacterElementGaugeController _controller;
        private bool _isSubscribed;
        private float _lastNormalized = -1f;
        private bool _lastThresholdReached;

        private void Awake()
        {
            if (slider == null)
                slider = GetComponent<Slider>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.interactable = false;
            }

            ApplyEmptyView();
        }

        private void OnEnable()
        {
            SubscribeIfNeeded();
            RefreshView();
        }

        private void OnDisable()
        {
            UnsubscribeIfNeeded();
        }

        /// <summary>
        /// 게이지 컨트롤러를 바인딩하고 즉시 표시 상태를 갱신합니다.
        /// </summary>
        /// <param name="controller">표시할 속성 게이지 컨트롤러입니다.</param>
        public void Bind(CharacterElementGaugeController controller)
        {
            if (ReferenceEquals(_controller, controller))
            {
                RefreshView();
                return;
            }

            UnsubscribeIfNeeded();
            _controller = controller;
            SubscribeIfNeeded();
            RefreshView();
        }

        /// <summary>
        /// 현재 컨트롤러 바인딩을 해제하고 빈 상태로 표시합니다.
        /// </summary>
        public void Unbind()
        {
            UnsubscribeIfNeeded();
            _controller = null;
            ApplyEmptyView();
        }

        private void SubscribeIfNeeded()
        {
            if (_isSubscribed || _controller == null)
                return;

            _controller.GaugeChanged += OnGaugeChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeIfNeeded()
        {
            if (!_isSubscribed || _controller == null)
                return;

            _controller.GaugeChanged -= OnGaugeChanged;
            _isSubscribed = false;
        }

        private void OnGaugeChanged()
        {
            RefreshView();
        }

        /// <summary>
        /// 현재 스냅샷을 기준으로 Slider, 텍스트, 임계 오버레이 표시를 갱신합니다.
        /// </summary>
        private void RefreshView()
        {
            if (_controller == null)
            {
                ApplyEmptyView();
                return;
            }

            if (!_controller.TryGetGaugeSnapshot(damageType, out ElementGaugeSnapshot snapshot))
            {
                ApplyEmptyView();
                return;
            }

            float normalized = Mathf.Clamp01(snapshot.CurrentValue / Mathf.Max(1f, snapshot.MaxValue));
            bool thresholdReached = snapshot.IsThresholdReached;

            if (Mathf.Approximately(_lastNormalized, normalized) && _lastThresholdReached == thresholdReached)
                return;

            if (slider != null)
                slider.normalizedValue = normalized;

            if (textValue != null)
            {
                textValue.text = showNumericText
                    ? $"{Mathf.RoundToInt(snapshot.CurrentValue)} / {Mathf.RoundToInt(snapshot.MaxValue)}"
                    : string.Empty;
            }

            if (thresholdOverlay != null)
                thresholdOverlay.SetActive(thresholdReached);

            if (canvasGroup != null && hideWhenEmpty)
            {
                bool visible = normalized > 0f || (showWhenThresholdReached && thresholdReached);
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _lastNormalized = normalized;
            _lastThresholdReached = thresholdReached;
        }

        /// <summary>
        /// 연결된 데이터가 없을 때 UI를 빈 상태로 초기화합니다.
        /// </summary>
        private void ApplyEmptyView()
        {
            if (slider != null)
                slider.normalizedValue = 0f;

            if (textValue != null)
                textValue.text = showNumericText ? "0 / 0" : string.Empty;

            if (thresholdOverlay != null)
                thresholdOverlay.SetActive(false);

            if (canvasGroup != null && hideWhenEmpty)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _lastNormalized = 0f;
            _lastThresholdReached = false;
        }
    }
}
