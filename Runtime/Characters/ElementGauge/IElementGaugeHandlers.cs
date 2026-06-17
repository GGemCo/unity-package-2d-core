namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 게이지가 처음 임계값에 도달했을 때 호출되는 확장 핸들러입니다.
    /// </summary>
    /// <remarks>
    /// Core는 어떤 효과를 발동할지 알지 않고, 프로젝트별 구현체가 이 인터페이스를 통해 Affect 적용, 추가 데미지, 연출 등을 처리합니다.
    /// </remarks>
    public interface IElementGaugeThresholdHandler
    {
        /// <summary>
        /// 속성 게이지가 임계값에 처음 도달했을 때 호출됩니다.
        /// </summary>
        /// <param name="snapshot">임계 도달 직후의 게이지 스냅샷입니다.</param>
        /// <param name="context">누적 원인과 대상 정보를 담은 컨텍스트입니다.</param>
        void OnThresholdReached(ElementGaugeSnapshot snapshot, ElementGaugeAccumulationContext context);
    }

    /// <summary>
    /// 이미 임계 상태인 속성에 같은 속성 데미지가 다시 들어왔을 때 호출되는 확장 핸들러입니다.
    /// </summary>
    public interface IElementGaugeRepeatedHitHandler
    {
        /// <summary>
        /// 임계 상태에서 같은 속성 데미지를 다시 받았을 때 호출됩니다.
        /// </summary>
        /// <param name="snapshot">현재 게이지 스냅샷입니다.</param>
        /// <param name="context">누적 원인과 대상 정보를 담은 컨텍스트입니다.</param>
        void OnRepeatedElementDamage(ElementGaugeSnapshot snapshot, ElementGaugeAccumulationContext context);
    }

    /// <summary>
    /// 아무 동작도 하지 않는 기본 임계 도달 핸들러입니다.
    /// </summary>
    public sealed class NullElementGaugeThresholdHandler : IElementGaugeThresholdHandler
    {
        public static readonly NullElementGaugeThresholdHandler Instance = new NullElementGaugeThresholdHandler();

        private NullElementGaugeThresholdHandler()
        {
        }

        public void OnThresholdReached(ElementGaugeSnapshot snapshot, ElementGaugeAccumulationContext context)
        {
        }
    }

    /// <summary>
    /// 아무 동작도 하지 않는 기본 임계 상태 재피격 핸들러입니다.
    /// </summary>
    public sealed class NullElementGaugeRepeatedHitHandler : IElementGaugeRepeatedHitHandler
    {
        public static readonly NullElementGaugeRepeatedHitHandler Instance = new NullElementGaugeRepeatedHitHandler();

        private NullElementGaugeRepeatedHitHandler()
        {
        }

        public void OnRepeatedElementDamage(ElementGaugeSnapshot snapshot, ElementGaugeAccumulationContext context)
        {
        }
    }
}
