namespace GGemCo2DCore
{
    /// <summary>
    /// 피격/사망 등 외부 인터럽트로 인해 진행 중인 입력 액션을 정리할 수 있는 공용 인터페이스입니다.
    /// Core 패키지는 구체적인 입력 구현에 의존하지 않고, 이 인터페이스를 통해 액션 취소를 요청합니다.
    /// </summary>
    public interface IIncomingHitActionCanceler
    {
        /// <summary>
        /// 피격 인터럽트 사유에 맞춰 진행 중인 액션을 취소합니다.
        /// </summary>
        /// <param name="reason">액션 취소 사유입니다.</param>
        void CancelActionsOnIncomingHit(IncomingHitCancelReason reason);
    }

    /// <summary>
    /// 피격으로 인한 액션 취소 사유입니다.
    /// </summary>
    public enum IncomingHitCancelReason
    {
        /// <summary>
        /// 일반 피격 리액션으로 인한 취소입니다.
        /// </summary>
        Damage,

        /// <summary>
        /// 사망 처리 직전의 강제 취소입니다.
        /// </summary>
        Death,
    }
}
