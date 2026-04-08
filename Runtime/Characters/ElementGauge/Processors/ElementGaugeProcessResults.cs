namespace GGemCo2DCore
{
    internal readonly struct ElementGaugeApplyResult
    {
        public static ElementGaugeApplyResult None => new(false, false, ConfigCommon.DamageType.None);

        public ElementGaugeApplyResult(bool gaugeChanged, bool thresholdReached, ConfigCommon.DamageType damageType)
        {
            GaugeChanged = gaugeChanged;
            ThresholdReached = thresholdReached;
            DamageType = damageType;
        }

        public bool GaugeChanged { get; }
        public bool ThresholdReached { get; }
        public ConfigCommon.DamageType DamageType { get; }
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
        public static ElementTriggeredHpTickResult None => new(false, false);

        public ElementTriggeredHpTickResult(bool triggeredHpChanged, bool requiresDeathFinalize)
        {
            TriggeredHpChanged = triggeredHpChanged;
            RequiresDeathFinalize = requiresDeathFinalize;
        }

        public bool TriggeredHpChanged { get; }
        public bool RequiresDeathFinalize { get; }
    }

    internal readonly struct ElementTriggeredHpConsumeResult
    {
        public static ElementTriggeredHpConsumeResult None => new(false, false);

        public ElementTriggeredHpConsumeResult(bool triggeredHpChanged, bool requiresDeathFinalize)
        {
            TriggeredHpChanged = triggeredHpChanged;
            RequiresDeathFinalize = requiresDeathFinalize;
        }

        public bool TriggeredHpChanged { get; }
        public bool RequiresDeathFinalize { get; }
    }
}
