using UnityEngine;

namespace GGemCo2DCore
{
    public enum UIWindowHudResourceType
    {
        Hp,
        HpTemp,
        Mp,
        Stamina
    }

    /// <summary>
    /// HUD 리소스 표현의 공통 베이스입니다.
    /// 값 변경 감지와 효과 문맥 계산은 베이스에서 처리하고,
    /// 실제 UI 반영은 구현체가 담당합니다.
    /// </summary>
    public abstract class UIWindowHudResourceBase : MonoBehaviour
    {
        private bool _hasLastValue;
        private long _lastCurrent;
        private long _lastTotal;

        /// <summary>
        /// current/total이 변경될 때마다 호출됩니다.
        /// 구현체는 내부 UI(Slider, Heart 등)를 갱신합니다.
        /// </summary>
        public virtual void SetValue(UIWindowHudResourceType type, long current, long total)
        {
            var context = BuildEffectContext(type, current, total);
            ApplyValue(type, current, total, context);

            _hasLastValue = true;
            _lastCurrent = current;
            _lastTotal = total;
        }

        public abstract void SetMaxValue(UIWindowHudResourceType hpTemp, long total);

        /// <summary>
        /// 실제 UI 반영을 수행합니다.
        /// </summary>
        protected abstract void ApplyValue(UIWindowHudResourceType type, long current, long total, UIEffectContext context);

        /// <summary>
        /// 베이스 캐시에 저장된 이전 값과 비교하여 효과 문맥을 생성합니다.
        /// </summary>
        protected virtual UIEffectContext BuildEffectContext(UIWindowHudResourceType type, long current, long total)
        {
            if (!_hasLastValue)
            {
                return new UIEffectContext
                {
                    ResourceType = type,
                    PreviousCurrent = current,
                    PreviousTotal = total,
                    Current = current,
                    Total = total,
                    DeltaCurrent = 0,
                    DeltaTotal = 0,
                    IsInitial = true
                };
            }

            return new UIEffectContext
            {
                ResourceType = type,
                PreviousCurrent = _lastCurrent,
                PreviousTotal = _lastTotal,
                Current = current,
                Total = total,
                DeltaCurrent = current - _lastCurrent,
                DeltaTotal = total - _lastTotal,
                IsInitial = false
            };
        }
    }
}
