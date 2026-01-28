using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터의 이동/전투 실행(Execution)을 외부 의사결정 시스템(AI)에서 호출할 수 있도록 추상화한 드라이버 인터페이스.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Core 패키지가 특정 AI 구현(BT/FSM 등)에 의존하지 않도록, 최소 호출 집합만 제공한다.
    /// </para>
    /// <para>
    /// 구현체는 내부에서 기존 컨트롤러 로직(예: Wait/Run/Attack)을 그대로 재사용해야 한다.
    /// </para>
    /// </remarks>
    public interface IMonsterCombatDriver
    {
        /// <summary>
        /// 현재 전투 대상(어그로 대상)을 가져온다.
        /// </summary>
        /// <returns>대상이 있으면 true, 없으면 false.</returns>
        bool TryGetTarget(out Transform target);

        /// <summary>
        /// 어그로 상태인지 반환한다.
        /// </summary>
        bool IsAggro { get; }

        /// <summary>
        /// 몬스터가 사망 상태인지 반환한다.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// 자신의 HP 비율(0~1)을 반환한다.
        /// </summary>
        float HpPercent { get; }

        /// <summary>
        /// 타겟이 현재 공격 범위 내인지(근접 판정) 반환한다.
        /// </summary>
        bool IsTargetInAttackRange();

        /// <summary>
        /// 몬스터를 대기 상태로 전환한다.
        /// </summary>
        void RequestWait();

        /// <summary>
        /// 이동을 요청한다. direction은 월드 기준 방향 벡터(정규화 전 값도 허용)이다.
        /// </summary>
        void RequestMove(Vector2 direction);

        /// <summary>
        /// 현재 타겟 방향을 바라보도록 한다.
        /// </summary>
        void RequestFaceToTarget();

        /// <summary>
        /// 기본 공격(또는 주 공격)을 1회 요청한다.
        /// </summary>
        void RequestAttackOnce();

        void RequestClearAggro();
    }
}
