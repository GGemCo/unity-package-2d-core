namespace GGemCo2DCore
{
    /// <summary>
    /// 중앙 틱커(<see cref="MonsterBrainTicker"/>)가 호출할 수 있는 Brain 틱 인터페이스.
    /// </summary>
    /// <remarks>
    /// - Core는 Update/FixedUpdate를 직접 호출하지 않고, Brain의 틱을 표준화된 방식으로 실행한다.
    /// - BT/Legacy 등 모든 Brain 구현은 본 인터페이스를 구현하여 중앙 루프에 참여한다.
    /// </remarks>
    public interface IMonsterBrainTickable : IMonsterBrain
    {
        /// <summary>
        /// Brain 의사결정 틱.
        /// </summary>
        void Tick();
    }
}
