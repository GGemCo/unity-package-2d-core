using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 전투 AI가 실제 실행 계층(이동/정지/공격)을 호출하기 위한 최소 인터페이스.
    /// </summary>
    /// <remarks>
    /// Core는 상위 AI 구현(BT/FSM)에 의존하지 않으며, 실행 기능만 추상화해 제공한다.
    /// </remarks>
    public enum MonsterMoveRequestFailureReason
    {
        /// <summary>실패 원인이 없거나 성공 상태.</summary>
        None = 0,

        /// <summary>제어 대상 캐릭터를 찾을 수 없음.</summary>
        CharacterMissing,

        /// <summary>입력 방향 벡터가 0에 가까움.</summary>
        ZeroDirection,

        /// <summary>축 제한 적용 후 이동 방향이 소거됨.</summary>
        AxisLocked,

        /// <summary>캐릭터 상태가 이동 금지(DontMove)임.</summary>
        StatusDontMove,

        /// <summary>캐릭터 상태가 공격 중임.</summary>
        StatusAttack,

        /// <summary>캐릭터 상태가 사망임.</summary>
        StatusDead,

        /// <summary>현재 계산된 이동 속도가 0 이하임.</summary>
        SpeedNonPositive,

        /// <summary>알 수 없는 이유로 이동 요청이 거부됨.</summary>
        Unknown,
    }

    /// <summary>
    /// 몬스터의 이동/전투 실행(Execution)을 의사결정 시스템(AI)에서 호출하기 위한 드라이버 인터페이스.
    /// </summary>
    public interface IMonsterCombatDriver
    {
        /// <summary>
        /// 현재 전투 타겟의 Transform을 조회한다.
        /// </summary>
        /// <returns>타겟이 존재하면 true, 아니면 false.</returns>
        bool TryGetTarget(out Transform target);

        /// <summary>
        /// 현재 어그로 상태인지 반환한다.
        /// </summary>
        bool IsAggro { get; }

        /// <summary>
        /// 현재 사망 상태인지 반환한다.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// 자신 HP 비율(0~1)을 반환한다.
        /// </summary>
        float HpPercent { get; }

        /// <summary>
        /// 타겟이 현재 공격 범위 안인지(근접 판정 기준) 반환한다.
        /// </summary>
        bool IsTargetInAttackRange();

        /// <summary>
        /// 몬스터를 대기 상태로 전환한다.
        /// </summary>
        void RequestWait();

        /// <summary>
        /// 이동을 요청한다. direction은 월드 기준 방향 벡터다.
        /// </summary>
        void RequestMove(Vector2 direction);

        /// <summary>
        /// 이동을 요청하고, 실제로 이동이 수락되었는지 결과를 반환한다.
        /// </summary>
        /// <param name="direction">월드 기준 이동 방향 벡터.</param>
        /// <param name="failureReason">실패 시 거부 사유 코드.</param>
        /// <returns>요청이 수락되어 이동이 실행되면 true, 아니면 false.</returns>
        bool TryRequestMove(Vector2 direction, out MonsterMoveRequestFailureReason failureReason);

        /// <summary>
        /// 현재 타겟 방향을 바라보도록 한다.
        /// </summary>
        void RequestFaceToTarget();

        /// <summary>
        /// 기본 공격(또는 주 공격) 1회를 요청한다.
        /// </summary>
        void RequestAttackOnce();

        /// <summary>
        /// 현재 어그로를 해제한다.
        /// </summary>
        void RequestClearAggro();
    }
}
