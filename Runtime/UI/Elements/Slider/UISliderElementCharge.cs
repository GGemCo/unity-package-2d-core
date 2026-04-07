using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 특정 속성 게이지 1종을 Slider로 표시하는 HUD View 입니다.
    /// 실제 게이지 계산은 CharacterElementGaugeController가 담당하고,
    /// 이 클래스는 바인딩된 컨트롤러의 스냅샷만 화면에 반영합니다.
    /// </summary>
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
        [SerializeField] private GameObject blockedOverlay;

        [Header("Policy")]
        [SerializeField] private bool hideWhenEmpty = false;
        [SerializeField] private bool showNumericText = false;

        private CharacterElementGaugeController _controller;
        private bool _isSubscribed;
        private float _lastNormalized = -1f;
        private bool _lastBlocked;

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

        private void RefreshView()
        {
            if (_controller == null)
            {
                ApplyEmptyView();
                return;
            }

            var snapshots = _controller.GetGaugeSnapshots();
            bool found = false;
            ElementGaugeSnapshot snapshot = default;

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].DamageType != damageType)
                    continue;

                snapshot = snapshots[i];
                found = true;
                break;
            }

            if (!found)
            {
                ApplyEmptyView();
                return;
            }

            float maxValue = Mathf.Max(1f, snapshot.MaxValue);
            float normalized = Mathf.Clamp01(snapshot.CurrentValue / maxValue);
            bool blocked = snapshot.IsBlockedByTriggeredState;

            if (Mathf.Approximately(_lastNormalized, normalized) && _lastBlocked == blocked)
                return;

            if (slider != null)
                slider.normalizedValue = normalized;

            if (textValue != null)
            {
                if (showNumericText)
                    textValue.text = $"{Mathf.RoundToInt(snapshot.CurrentValue)} / {Mathf.RoundToInt(snapshot.MaxValue)}";
                else
                    textValue.text = string.Empty;
            }

            if (blockedOverlay != null)
                blockedOverlay.SetActive(blocked);

            if (canvasGroup != null && hideWhenEmpty)
            {
                bool visible = normalized > 0f || blocked;
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _lastNormalized = normalized;
            _lastBlocked = blocked;
        }

        private void ApplyEmptyView()
        {
            if (slider != null)
                slider.normalizedValue = 0f;

            if (textValue != null)
                textValue.text = showNumericText ? "0 / 0" : string.Empty;

            if (blockedOverlay != null)
                blockedOverlay.SetActive(false);

            if (canvasGroup != null && hideWhenEmpty)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _lastNormalized = 0f;
            _lastBlocked = false;
        }
    }
}