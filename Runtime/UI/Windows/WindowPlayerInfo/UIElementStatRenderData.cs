namespace GGemCo2DCore
{
    /// <summary>
    /// UIElementStat 1줄을 그리기 위한 렌더 데이터입니다.
    /// 값 계산은 Window/Presenter 쪽에서 수행하고, UIElementStat는 이 데이터를 소비해 표시만 담당합니다.
    /// </summary>
    public readonly struct UIElementStatRenderData
    {
        public readonly string Label;
        public readonly long CurrentValue;
        public readonly bool HasPreview;
        public readonly long PreviewValue;
        public readonly long ValueDelta;
        public readonly bool IsChanged;
        public readonly bool IsIncrease;
        public readonly bool IsDecrease;
        public readonly bool IsStatPointTarget;
        public readonly int DraftInvested;
        public readonly int InvestedDelta;
        public readonly bool CanIncrease;
        public readonly bool CanDecrease;

        public UIElementStatRenderData(
            string label,
            long currentValue,
            bool hasPreview,
            long previewValue,
            bool isStatPointTarget,
            int draftInvested,
            int investedDelta,
            bool canIncrease,
            bool canDecrease)
        {
            Label = label;
            CurrentValue = currentValue;
            HasPreview = hasPreview;
            PreviewValue = previewValue;

            ValueDelta = previewValue - currentValue;
            IsChanged = hasPreview && previewValue != currentValue;
            IsIncrease = hasPreview && previewValue > currentValue;
            IsDecrease = hasPreview && previewValue < currentValue;

            IsStatPointTarget = isStatPointTarget;
            DraftInvested = draftInvested;
            InvestedDelta = investedDelta;
            CanIncrease = canIncrease;
            CanDecrease = canDecrease;
        }
    }
}
