using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// Slider의 메인 Fill(실제 값)은 즉시 반영하고,
    /// 보조 Fill(예: 흰색 '딜레이 HP')은 일정 시간 대기 후 천천히 따라오게 하는 컴포넌트.
    ///
    /// 설계 의도
    /// - HP/MP/Shield 등 다양한 게이지에 재사용 가능
    /// - Slider.value 변경 경로(코드/바인딩/애니메이션)가 무엇이든 동일하게 동작
    /// - 코루틴 없이 Update 기반으로 처리하여 GC Alloc 최소화
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISliderDelayedFill : MonoBehaviour
    {
        private enum IncreaseBehavior
        {
            /// <summary>값이 증가하면 보조 Fill도 즉시 따라감(권장).</summary>
            Snap,
            /// <summary>값이 증가해도 보조 Fill은 감소 로직과 동일하게(대기+감속) 따라감.</summary>
            Animate
        }

        [Header("References")]
        [SerializeField] private Slider slider;

        [Tooltip("보조 Fill로 사용할 Image. Slider의 Fill과 동일한 타입(Image.Type.Filled)이어야 합니다.")]
        [SerializeField] private Image delayedFill;

        [Header("Tuning")]
        [Min(0f)]
        [Tooltip("메인 값이 감소했을 때 보조 Fill이 줄어들기 시작하기 전 대기 시간(초)")]
        [SerializeField] private float delaySeconds = 0.2f;

        [Min(0f)]
        [Tooltip("보조 Fill이 목표 값으로 줄어드는 속도(정규화 0..1 기준 / 초). 예: 1이면 1초에 1만큼 이동")]
        [SerializeField] private float decreaseSpeedPerSecond = 1.5f;

        [Tooltip("메인 값이 증가할 때 보조 Fill 동작 방식")]
        [SerializeField] private IncreaseBehavior increaseBehavior = IncreaseBehavior.Snap;

        [Tooltip("Time.timeScale의 영향을 받지 않게 하려면 체크")]
        [SerializeField] private bool useUnscaledTime;

        private float _currentDelayed;
        private float _target;
        private float _delayRemaining;
        private bool _isAnimating;

        private void Reset()
        {
            slider = GetComponent<Slider>();
        }

        private void Awake()
        {
            if (slider == null)
                slider = GetComponent<Slider>();

            if (slider == null)
            {
                GcLogger.LogError("UISliderDelayedFill: Slider 컴포넌트가 없습니다.");
                enabled = false;
                return;
            }

            if (delayedFill == null)
            {
                GcLogger.LogError("UISliderDelayedFill: delayedFill(Image) 레퍼런스가 없습니다.");
                enabled = false;
                return;
            }

            // Slider.value 변경을 감지
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnEnable()
        {
            SyncImmediately();
        }

        private void OnDestroy()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        /// <summary>
        /// 현재 Slider 값을 보조 Fill에 즉시 동기화합니다.
        /// (초기화/리스폰/프리팹 활성화 시 호출)
        /// </summary>
        public void SyncImmediately()
        {
            var v = GetNormalized(slider);
            _currentDelayed = v;
            _target = v;
            _delayRemaining = 0f;
            _isAnimating = false;
            ApplyDelayed(_currentDelayed);
        }

        private void OnSliderValueChanged(float _)
        {
            var v = GetNormalized(slider);

            // 감소: 메인은 즉시, 보조는 대기 후 천천히
            if (v < _currentDelayed)
            {
                _target = v;
                _delayRemaining = delaySeconds;
                _isAnimating = true;
                return;
            }

            // 증가: 옵션에 따라 처리
            if (v > _currentDelayed)
            {
                if (increaseBehavior == IncreaseBehavior.Snap)
                {
                    _currentDelayed = v;
                    _target = v;
                    _delayRemaining = 0f;
                    _isAnimating = false;
                    ApplyDelayed(_currentDelayed);
                }
                else
                {
                    _target = v;
                    _delayRemaining = delaySeconds;
                    _isAnimating = true;
                }
            }
        }

        private void Update()
        {
            if (!_isAnimating)
                return;

            var dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (_delayRemaining > 0f)
            {
                _delayRemaining -= dt;
                return;
            }

            if (Mathf.Approximately(_currentDelayed, _target))
            {
                _currentDelayed = _target;
                _isAnimating = false;
                ApplyDelayed(_currentDelayed);
                return;
            }

            if (decreaseSpeedPerSecond <= 0f)
            {
                _currentDelayed = _target;
                _isAnimating = false;
                ApplyDelayed(_currentDelayed);
                return;
            }

            _currentDelayed = Mathf.MoveTowards(_currentDelayed, _target, decreaseSpeedPerSecond * dt);
            ApplyDelayed(_currentDelayed);
        }

        private void ApplyDelayed(float normalized)
        {
            // Image.fillAmount는 0..1 범위
            delayedFill.fillAmount = Mathf.Clamp01(normalized);
        }

        private static float GetNormalized(Slider s)
        {
            // Slider.normalizedValue는 min~max를 0..1로 변환해줍니다.
            return Mathf.Clamp01(s.normalizedValue);
        }
    }
}
