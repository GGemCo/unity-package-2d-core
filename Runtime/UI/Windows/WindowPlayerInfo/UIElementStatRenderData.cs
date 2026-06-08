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
        public readonly bool HasBaseValue;
        public readonly long CurrentBaseValue;
        public readonly bool HasBasePreview;
        public readonly long PreviewBaseValue;
        public readonly long BaseValueDelta;
        public readonly bool IsBaseChanged;
        public readonly bool IsBaseIncrease;
        public readonly bool IsBaseDecrease;

        /// <summary>
        /// 스탯 라인 표시 데이터와 선택적 Base 표시 데이터를 생성합니다.
        /// </summary>
        /// <param name="label">라인에 표시할 스탯 이름입니다.</param>
        /// <param name="currentValue">메인 텍스트에 표시할 현재 스탯 값입니다.</param>
        /// <param name="hasPreview">메인 텍스트 미리보기 값 사용 여부입니다.</param>
        /// <param name="previewValue">메인 텍스트 미리보기 스탯 값입니다.</param>
        /// <param name="isStatPointTarget">스탯 포인트 투자 대상 여부입니다.</param>
        /// <param name="draftInvested">드래프트 기준 투자 포인트입니다.</param>
        /// <param name="investedDelta">원본 투자 포인트 대비 드래프트 증감량입니다.</param>
        /// <param name="canIncrease">증가 버튼 활성 가능 여부입니다.</param>
        /// <param name="canDecrease">감소 버튼 활성 가능 여부입니다.</param>
        /// <param name="hasBaseValue">BaseText에 Base 값을 표시할지 여부입니다.</param>
        /// <param name="currentBaseValue">BaseText에 표시할 현재 Base 값입니다.</param>
        /// <param name="hasBasePreview">BaseText 미리보기 값 사용 여부입니다.</param>
        /// <param name="previewBaseValue">BaseText에 표시할 미리보기 Base 값입니다.</param>
        public UIElementStatRenderData(
            string label,
            long currentValue,
            bool hasPreview,
            long previewValue,
            bool isStatPointTarget,
            int draftInvested,
            int investedDelta,
            bool canIncrease,
            bool canDecrease,
            bool hasBaseValue = false,
            long currentBaseValue = 0,
            bool hasBasePreview = false,
            long previewBaseValue = 0)
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

            HasBaseValue = hasBaseValue;
            CurrentBaseValue = currentBaseValue;
            HasBasePreview = hasBasePreview;
            PreviewBaseValue = previewBaseValue;
            BaseValueDelta = previewBaseValue - currentBaseValue;
            IsBaseChanged = hasBasePreview && previewBaseValue != currentBaseValue;
            IsBaseIncrease = hasBasePreview && previewBaseValue > currentBaseValue;
            IsBaseDecrease = hasBasePreview && previewBaseValue < currentBaseValue;
        }
    }
}
