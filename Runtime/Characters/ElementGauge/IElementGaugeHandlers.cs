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
    /// 이미 임계 상태인 속성에 같은 속성 수치가 다시 들어왔을 때 임계 사이클당 한 번 호출되는 확장 핸들러입니다.
    /// </summary>
    public interface IElementGaugeRepeatedHitHandler
    {
        /// <summary>
        /// 임계 상태에서 같은 속성 수치를 처음 다시 받았을 때 호출됩니다.
        /// </summary>
        /// <param name="snapshot">현재 게이지 스냅샷입니다.</param>
        /// <param name="context">누적 원인과 대상 정보를 담은 컨텍스트입니다.</param>
        void OnRepeatedElementDamage(ElementGaugeSnapshot snapshot, ElementGaugeAccumulationContext context);
    }

    /// <summary>
    /// 속성 게이지 누적 가능 여부를 외부 규칙으로 판정하는 정책 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Affect나 게임 전용 상태를 직접 참조하지 않고, 상위 계층이 이 인터페이스를 구현하여
    /// 특정 디버프 적용 중에는 독 게이지를 막는 식의 프로젝트 규칙을 주입합니다.
    /// </remarks>
    public interface IElementGaugeAccumulationPolicy
    {
        /// <summary>
        /// 지정한 속성 게이지를 현재 대상에게 누적할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="owner">속성 게이지를 보유한 캐릭터입니다.</param>
        /// <param name="damageType">누적하려는 속성 타입입니다.</param>
        /// <returns>누적을 허용하면 <see langword="true"/>, 차단하면 <see langword="false"/>입니다.</returns>
        bool CanAccumulateElementGauge(CharacterBase owner, ConfigCommon.DamageType damageType);
    }

    /// <summary>
    /// 모든 속성 게이지 누적을 허용하는 기본 정책입니다.
    /// </summary>
    public sealed class AllowAllElementGaugeAccumulationPolicy : IElementGaugeAccumulationPolicy
    {
        public static readonly AllowAllElementGaugeAccumulationPolicy Instance = new AllowAllElementGaugeAccumulationPolicy();

        private AllowAllElementGaugeAccumulationPolicy()
        {
        }

        public bool CanAccumulateElementGauge(CharacterBase owner, ConfigCommon.DamageType damageType)
        {
            return true;
        }
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
    /// 아무 동작도 하지 않는 기본 임계 상태 반복 입력 핸들러입니다.
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
