namespace GGemCo2DCore
{
    /// <summary>
    /// 보스 HP 변화를 외부 시나리오 시스템에 전달하기 위한 범용 스냅샷입니다.
    /// Core는 특정 보스 이름이나 게임 전용 규칙을 알지 않고 현재 수치만 제공합니다.
    /// </summary>
    public readonly struct BossHpSnapshot
    {
        public readonly int MonsterUid;
        public readonly int PhaseIndex;
        public readonly long CurrentHp;
        public readonly long MaxHp;
        public readonly float NormalizedHp;
        public readonly long DeltaHp;

        /// <summary>
        /// 보스 HP 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="monsterUid">몬스터 UID입니다.</param>
        /// <param name="phaseIndex">현재 페이즈입니다.</param>
        /// <param name="currentHp">현재 HP입니다.</param>
        /// <param name="maxHp">최대 HP입니다.</param>
        /// <param name="deltaHp">직전 스냅샷 대비 변화량입니다.</param>
        public BossHpSnapshot(int monsterUid, int phaseIndex, long currentHp, long maxHp, long deltaHp)
        {
            MonsterUid = monsterUid;
            PhaseIndex = phaseIndex;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            DeltaHp = deltaHp;
            NormalizedHp = maxHp > 0 ? UnityEngine.Mathf.Clamp01((float)currentHp / maxHp) : 0f;
        }
    }
}
