namespace GGemCo2DCore
{
    internal readonly struct ElementGaugeApplyResult
    {
        public static ElementGaugeApplyResult None => new(false, false, false, ConfigCommon.DamageType.None, default);

        public ElementGaugeApplyResult(
            bool gaugeChanged,
            bool thresholdReached,
            bool repeatedGaugeInput,
            ConfigCommon.DamageType damageType,
            ElementGaugeSnapshot snapshot)
        {
            GaugeChanged = gaugeChanged;
            ThresholdReached = thresholdReached;
            RepeatedGaugeInput = repeatedGaugeInput;
            DamageType = damageType;
            Snapshot = snapshot;
        }

        public bool GaugeChanged { get; }
        public bool ThresholdReached { get; }
        public bool RepeatedGaugeInput { get; }
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

}
