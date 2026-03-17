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
    /// HUD 자원 표현 공용 베이스 클래스입니다.
    /// </summary>
    public abstract class UIWindowHudResourceBase : MonoBehaviour
    {
        [Header("Affect Visual")]
        public HeartHudAffectVisualSettings affectVisualSettings;

        protected string CurrentAffectVisualStateKey;

        /// <summary>
        /// 자원 변화 문맥 정보입니다.
        /// </summary>
        protected readonly struct ResourceChangeContext
        {
            public ResourceChangeContext(
                bool hasPreviousValue,
                long previousCurrent,
                long previousTotal,
                long current,
                long total,
                bool isIncrease,
                bool isDecrease,
                bool isMaxValueChanged)
            {
                HasPreviousValue = hasPreviousValue;
                PreviousCurrent = previousCurrent;
                PreviousTotal = previousTotal;
                Current = current;
                Total = total;
                DeltaCurrent = current - previousCurrent;
                DeltaTotal = total - previousTotal;
                IsIncrease = isIncrease;
                IsDecrease = isDecrease;
                IsMaxValueChanged = isMaxValueChanged;
            }

            public bool HasPreviousValue { get; }
            public long PreviousCurrent { get; }
            public long PreviousTotal { get; }
            public long Current { get; }
            public long Total { get; }
            public long DeltaCurrent { get; }
            public long DeltaTotal { get; }
            public bool IsIncrease { get; }
            public bool IsDecrease { get; }
            public bool IsMaxValueChanged { get; }
        }

        private bool _hasLastValue;
        private long _lastCurrent;
        private long _lastTotal;

        /// <summary>
        /// current/total이 변경될 때마다 호출됩니다.
        /// 구현체는 내부 UI(Slider, Heart 등)를 갱신합니다.
        /// </summary>
        public void SetValue(UIWindowHudResourceType type, long current, long total)
        {
            ResourceChangeContext context = BuildChangeContext(current, total);
            ApplyValue(type, current, total, context);
            _hasLastValue = true;
            _lastCurrent = current;
            _lastTotal = total;
        }

        protected abstract void ApplyValue(
            UIWindowHudResourceType type,
            long current,
            long total,
            ResourceChangeContext context);

        public abstract void SetMaxValue(UIWindowHudResourceType hpTemp, long total);

        private ResourceChangeContext BuildChangeContext(long current, long total)
        {
            if (!_hasLastValue)
            {
                return new ResourceChangeContext(false, current, total, current, total, false, false, false);
            }

            bool isIncrease = current > _lastCurrent;
            bool isDecrease = current < _lastCurrent;
            bool isMaxValueChanged = total != _lastTotal;
            return new ResourceChangeContext(true, _lastCurrent, _lastTotal, current, total, isIncrease, isDecrease, isMaxValueChanged);
        }

        /// <summary>
        /// 지정한 Affect 상태 키에 대응하는 하트 HUD 시각 상태를 적용합니다.
        /// </summary>
        public void SetAffectVisualState(string stateKey)
        {
            CurrentAffectVisualStateKey = string.IsNullOrWhiteSpace(stateKey)
                ? affectVisualSettings != null ? affectVisualSettings.DefaultStateKey : null
                : stateKey.Trim();

            ApplyAffectVisualProfile();
        }

        /// <summary>
        /// 하트 HUD 시각 상태를 기본 상태로 복원합니다.
        /// </summary>
        public void ResetAffectVisualState()
        {
            CurrentAffectVisualStateKey = affectVisualSettings != null ? affectVisualSettings.DefaultStateKey : null;
            ApplyAffectVisualProfile();
        }

        /// <summary>
        /// 지정한 상태 키의 우선순위를 반환합니다.
        /// </summary>
        public int GetAffectVisualPriority(string stateKey)
        {
            return affectVisualSettings != null ? affectVisualSettings.GetPriority(stateKey) : 0;
        }

        protected abstract void ApplyAffectVisualProfile();
    }
}
