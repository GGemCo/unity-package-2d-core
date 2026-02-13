namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 Brain(BehaviorTree 등)의 평가를 일시 중지해야 하는지 제공하는 인터페이스입니다.
    /// - BT 패키지는 Core를 참조할 수 있으므로, 런너가 본 인터페이스를 조회하여 Tick을 스킵할 수 있습니다.
    /// - Core는 BT 패키지를 참조하지 않습니다(의존성 단방향 유지).
    /// </summary>
    public interface IMonsterBrainSuspendProvider
    {
        /// <summary>
        /// true 이면 몬스터 Brain 평가를 중지합니다(그로기/기절/컷씬/사망 등).
        /// </summary>
        bool ShouldSuspendBrain { get; }
    }
}
