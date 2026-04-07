using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타격/버프 등으로 인해 누적될 속성 게이지 1건을 표현합니다.
    /// </summary>
    [Serializable]
    public struct ElementGaugeApplication
    {
        public ConfigCommon.DamageType DamageType;
        public float GaugeValue;

        public ElementGaugeApplication(ConfigCommon.DamageType damageType, float gaugeValue)
        {
            DamageType = damageType;
            GaugeValue = gaugeValue;
        }

        public bool IsValid => DamageType != ConfigCommon.DamageType.None && DamageType != ConfigCommon.DamageType.Physic && GaugeValue > 0f;
    }

    /// <summary>
    /// HUD 등 외부 시스템에서 참조할 수 있는 속성 게이지 스냅샷입니다.
    /// </summary>
    public readonly struct ElementGaugeSnapshot
    {
        public ElementGaugeSnapshot(ConfigCommon.DamageType damageType, float currentValue, float maxValue, bool isBlockedByTriggeredState)
        {
            DamageType = damageType;
            CurrentValue = currentValue;
            MaxValue = maxValue;
            IsBlockedByTriggeredState = isBlockedByTriggeredState;
        }

        public ConfigCommon.DamageType DamageType { get; }
        public float CurrentValue { get; }
        public float MaxValue { get; }
        public bool IsBlockedByTriggeredState { get; }
    }

    /// <summary>
    /// 독 하트 오염 상태 스냅샷입니다.
    /// </summary>
    public readonly struct HpCorruptionSnapshot
    {
        public HpCorruptionSnapshot(long corruptedBaseHp, long corruptedTempItemHp, long corruptedTempPassiveHp)
        {
            CorruptedBaseHp = corruptedBaseHp;
            CorruptedTempItemHp = corruptedTempItemHp;
            CorruptedTempPassiveHp = corruptedTempPassiveHp;
        }

        public long CorruptedBaseHp { get; }
        public long CorruptedTempItemHp { get; }
        public long CorruptedTempPassiveHp { get; }
        public long TotalCorruptedHp => CorruptedBaseHp + CorruptedTempItemHp + CorruptedTempPassiveHp;
        public bool HasAny => TotalCorruptedHp > 0;
    }
}
