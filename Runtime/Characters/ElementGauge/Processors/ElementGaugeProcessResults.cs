namespace GGemCo2DCore
{
    internal readonly struct ElementGaugeApplyResult
    {
        public static ElementGaugeApplyResult None => new(false, false, false, ConfigCommon.DamageType.None, default);

        public ElementGaugeApplyResult(
            bool gaugeChanged,
            bool thresholdReached,
            bool repeatedElementDamage,
            ConfigCommon.DamageType damageType,
            ElementGaugeSnapshot snapshot)
        {
            GaugeChanged = gaugeChanged;
            ThresholdReached = thresholdReached;
            RepeatedElementDamage = repeatedElementDamage;
            DamageType = damageType;
            Snapshot = snapshot;
        }

        public bool GaugeChanged { get; }
        public bool ThresholdReached { get; }
        public bool RepeatedElementDamage { get; }
        public ConfigCommon.DamageType DamageType { get; }
        public ElementGaugeSnapshot Snapshot { get; }
    }

    internal readonly struct ElementGaugeDecayResult
    {
        public static ElementGaugeDecayResult None => new(false);

        public ElementGaugeDecayResult(bool gaugeChanged)
        {
            GaugeChanged = gaugeChanged;
        }

        public bool GaugeChanged { get; }
    }

    internal readonly struct ElementGaugeThresholdResult
    {
        public static ElementGaugeThresholdResult None => new(false);

        public ElementGaugeThresholdResult(bool triggeredHpChanged)
        {
            TriggeredHpChanged = triggeredHpChanged;
        }

        public bool TriggeredHpChanged { get; }
    }

    internal readonly struct ElementTriggeredHpTickResult
    {
        public static ElementTriggeredHpTickResult None => new(false, false, false);

        public ElementTriggeredHpTickResult(bool triggeredHpChanged, bool gaugeChanged, bool requiresDeathFinalize)
        {
            TriggeredHpChanged = triggeredHpChanged;
            GaugeChanged = gaugeChanged;
            RequiresDeathFinalize = requiresDeathFinalize;
        }

        public bool TriggeredHpChanged { get; }
        public bool GaugeChanged { get; }
        public bool RequiresDeathFinalize { get; }
    }

    internal readonly struct ElementTriggeredHpConsumeResult
    {
        public static ElementTriggeredHpConsumeResult None => new(false, false, false);

        public ElementTriggeredHpConsumeResult(bool triggeredHpChanged, bool gaugeChanged, bool requiresDeathFinalize)
        {
            TriggeredHpChanged = triggeredHpChanged;
            GaugeChanged = gaugeChanged;
            RequiresDeathFinalize = requiresDeathFinalize;
        }

        public bool TriggeredHpChanged { get; }
        public bool GaugeChanged { get; }
        public bool RequiresDeathFinalize { get; }
    }
}
