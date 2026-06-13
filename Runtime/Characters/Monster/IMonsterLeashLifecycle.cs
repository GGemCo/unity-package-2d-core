namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 Leash Evade 시작과 홈 복귀 완료 시 상위 패키지가 런타임 상태를 정리할 수 있도록 제공하는 공용 포트입니다.
    /// </summary>
    public interface IMonsterLeashLifecycle
    {
        /// <summary>
        /// 몬스터가 전투를 중단하고 홈 복귀를 시작한 직후 호출됩니다.
        /// </summary>
        /// <param name="owner">Leash Evade를 시작한 몬스터입니다.</param>
        /// <param name="trigger">Evade를 시작한 원인입니다.</param>
        void OnLeashEvadeStarted(Monster owner, MonsterLeashTrigger trigger);

        /// <summary>
        /// 몬스터가 홈 복귀와 재활성 대기까지 모두 완료한 직후 호출됩니다.
        /// </summary>
        /// <param name="owner">홈 복귀를 완료한 몬스터입니다.</param>
        void OnLeashReturnCompleted(Monster owner);
    }
}
