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
    /// 특정 속성 임계 반응으로 표시 중인 HP 구간 스냅샷입니다.
    /// </summary>
    public readonly struct ElementTriggeredHpSnapshot
    {
        public ElementTriggeredHpSnapshot(
            ConfigCommon.DamageType damageType,
            string visualStateKey,
            long triggeredBaseHp,
            long triggeredTempItemHp,
            long triggeredTempRuntimeHp,
            long triggeredTempPassiveHp)
        {
            DamageType = damageType;
            VisualStateKey = visualStateKey;
            TriggeredBaseHp = triggeredBaseHp;
            TriggeredTempItemHp = triggeredTempItemHp;
            TriggeredTempRuntimeHp = triggeredTempRuntimeHp;
            TriggeredTempPassiveHp = triggeredTempPassiveHp;
        }

        public ConfigCommon.DamageType DamageType { get; }
        public string VisualStateKey { get; }
        public long TriggeredBaseHp { get; }
        public long TriggeredTempItemHp { get; }
        public long TriggeredTempRuntimeHp { get; }
        public long TriggeredTempPassiveHp { get; }
        public long TotalTriggeredTempHp => TriggeredTempItemHp + TriggeredTempRuntimeHp + TriggeredTempPassiveHp;
        public long TotalTriggeredHp => TriggeredBaseHp + TotalTriggeredTempHp;
        public bool HasAny => TotalTriggeredHp > 0;

        public HpCorruptionSnapshot ToLegacyCorruptionSnapshot()
        {
            return new HpCorruptionSnapshot(
                TriggeredBaseHp,
                TriggeredTempItemHp,
                TriggeredTempRuntimeHp,
                TriggeredTempPassiveHp);
        }
    }

    /// <summary>
    /// HUD 등 외부 시스템에서 참조할 수 있는 전체 속성 임계 HP 스냅샷입니다.
    /// </summary>
    public readonly struct ElementTriggeredHpCollectionSnapshot
    {
        private readonly ElementTriggeredHpSnapshot[] _entries;

        public ElementTriggeredHpCollectionSnapshot(ElementTriggeredHpSnapshot[] entries)
        {
            _entries = entries ?? Array.Empty<ElementTriggeredHpSnapshot>();
        }

        public static ElementTriggeredHpCollectionSnapshot Empty => new(Array.Empty<ElementTriggeredHpSnapshot>());

        public ElementTriggeredHpSnapshot[] Entries => _entries ?? Array.Empty<ElementTriggeredHpSnapshot>();
        public int Count => _entries?.Length ?? 0;
        public bool HasAny
        {
            get
            {
                if (_entries == null || _entries.Length == 0)
                    return false;

                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].HasAny)
                        return true;
                }

                return false;
            }
        }

        public bool TryGet(ConfigCommon.DamageType damageType, out ElementTriggeredHpSnapshot snapshot)
        {
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].DamageType != damageType)
                        continue;

                    snapshot = _entries[i];
                    return true;
                }
            }

            snapshot = default;
            return false;
        }

        public HpCorruptionSnapshot GetLegacyCorruptionSnapshot(ConfigCommon.DamageType damageType)
        {
            return TryGet(damageType, out var snapshot)
                ? snapshot.ToLegacyCorruptionSnapshot()
                : default;
        }
    }

    /// <summary>
    /// 독 하트 오염 상태 스냅샷입니다.
    /// 레거시 HUD 바인딩 호환을 위해 유지합니다.
    /// </summary>
    public readonly struct HpCorruptionSnapshot
    {
        public HpCorruptionSnapshot(long corruptedBaseHp, long corruptedTempItemHp, long corruptedTempRuntimeHp, long corruptedTempPassiveHp)
        {
            CorruptedBaseHp = corruptedBaseHp;
            CorruptedTempItemHp = corruptedTempItemHp;
            CorruptedTempRuntimeHp = corruptedTempRuntimeHp;
            CorruptedTempPassiveHp = corruptedTempPassiveHp;
        }

        public long CorruptedBaseHp { get; }
        public long CorruptedTempItemHp { get; }
        public long CorruptedTempRuntimeHp { get; }
        public long CorruptedTempPassiveHp { get; }
        public long TotalCorruptedTempHp => CorruptedTempItemHp + CorruptedTempRuntimeHp + CorruptedTempPassiveHp;
        public long TotalCorruptedHp => CorruptedBaseHp + TotalCorruptedTempHp;
        public bool HasAny => TotalCorruptedHp > 0;

        public ElementTriggeredHpSnapshot ToTriggeredHpSnapshot(string visualStateKey = "poison")
        {
            return new ElementTriggeredHpSnapshot(
                ConfigCommon.DamageType.Poison,
                visualStateKey,
                CorruptedBaseHp,
                CorruptedTempItemHp,
                CorruptedTempRuntimeHp,
                CorruptedTempPassiveHp);
        }
    }
}
