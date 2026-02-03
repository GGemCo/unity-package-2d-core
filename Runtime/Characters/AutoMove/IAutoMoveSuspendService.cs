namespace GGemCo2DCore
{
    /// <summary>
    /// AutoMove 일시정지(Resume 가능한 Pause) 제어 계약입니다.
    /// - 여러 시스템(컷씬, 벽 액션, UI 잠금 등)이 동시에 AutoMove를 멈출 수 있도록 토큰 기반으로 제공합니다.
    /// - Suspend 중에는 이동 벡터가 0으로 제공되고, 완료 조건(시간/거리) 판단도 진행하지 않습니다.
    /// </summary>
    public interface IAutoMoveSuspendService
    {
        /// <summary>현재 AutoMove가 일시정지 상태인지 여부</summary>
        bool IsAutoMoveSuspended { get; }

        /// <summary>
        /// AutoMove를 일시정지합니다.
        /// - 반환되는 토큰은 반드시 <see cref="ReleaseSuspend"/>로 해제되어야 합니다.
        /// </summary>
        AutoMoveSuspendToken AcquireSuspend(AutoMoveSuspendReason reason);

        /// <summary>
        /// <see cref="AcquireSuspend"/>로 획득한 토큰을 해제합니다.
        /// </summary>
        void ReleaseSuspend(AutoMoveSuspendToken token);
    }
}
