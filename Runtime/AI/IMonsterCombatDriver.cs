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

        /// <summary>Leash 홈 복귀가 일반 AI 이동보다 우선하는 상태임.</summary>
        LeashReturning,

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
        /// BT가 등록한 이동 의도 캐시만 중단한다.
        /// </summary>
        /// <remarks>
        /// 스킬 실행 중, 타겟 소실, 이동 거부처럼 대기 애니메이션으로 전환하면 안 되는 상황에서
        /// 이전 추적 입력이 프레임 루프에 남아 계속 이동하는 것을 방지하기 위해 사용한다.
        /// </remarks>
        void RequestStopMoveIntent();

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

    /// <summary>
    /// 몬스터 추적 이동 정지 판정을 공격 가능 판정과 분리해서 제공하는 선택 인터페이스.
    /// </summary>
    /// <remarks>
    /// 기존 <see cref="IMonsterCombatDriver"/> 구현체와의 하위 호환성을 유지하기 위해 별도 인터페이스로 분리한다.
    /// 구현체가 이 인터페이스를 제공하지 않으면 상위 AI는 기존 공격 범위 판정을 이동 정지 판정으로 사용한다.
    /// </remarks>
    public interface IMonsterMoveStopRangeProvider
    {
        /// <summary>
        /// 타겟이 추적 이동을 멈출 범위 안인지 반환한다.
        /// </summary>
        /// <remarks>
        /// 실제 공격 가능 판정과 이동 정지 판정은 다를 수 있다.
        /// 예를 들어 공중 타겟을 계속 추적해야 하는 몬스터는 공격 범위에 닿아도 이동 정지를 보류할 수 있다.
        /// </remarks>
        bool IsTargetInMoveStopRange();
    }

    /// <summary>
    /// 몬스터 전투 범위 프로필과 현재 타겟 거리 정보를 상위 AI에 제공하는 선택 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 실제 피해 판정 Collider와 AI 의사결정용 거리를 분리하기 위해 사용합니다.
    /// 구현체가 이 인터페이스를 제공하지 않으면 상위 AI는 기존 노드 파라미터와 블랙보드 값을 사용합니다.
    /// </remarks>
    public interface IMonsterCombatRangeProvider
    {
        /// <summary>현재 몬스터에 적용된 전투 범위 프로필입니다.</summary>
        MonsterCombatRangeProfile CombatRangeProfile { get; }

        /// <summary>
        /// 현재 타겟과의 축별 거리 및 2D 중심 거리를 조회합니다.
        /// </summary>
        /// <param name="horizontalDistance">타겟 HitArea 가장자리까지의 X축 거리입니다.</param>
        /// <param name="verticalDistance">타겟 HitArea 가장자리까지의 Y축 거리입니다.</param>
        /// <param name="distance2D">몬스터와 타겟 중심 사이의 2D 거리입니다.</param>
        /// <returns>유효한 타겟과 거리를 계산했으면 <see langword="true"/>입니다.</returns>
        bool TryGetTargetDistances(
            out float horizontalDistance,
            out float verticalDistance,
            out float distance2D);

        /// <summary>
        /// 현재 타겟이 선호 전투 거리 구간 안인지 반환합니다.
        /// </summary>
        bool IsTargetInPreferredCombatRange();

        /// <summary>
        /// 현재 타겟이 추적 거리 한계를 초과했는지 반환합니다.
        /// </summary>
        bool IsTargetBeyondChaseRange();
    }

    /// <summary>
    /// 몬스터의 Threat 목록과 현재 타겟 선택 결과를 상위 AI에 제공하는 선택 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 기존 <see cref="IMonsterCombatDriver"/> 구현체와의 하위 호환성을 유지하기 위해 별도 인터페이스로 분리합니다.
    /// AI는 이 인터페이스가 없으면 기존 <see cref="IMonsterCombatDriver.TryGetTarget"/> 결과만 사용합니다.
    /// </remarks>
    public interface IMonsterThreatProvider
    {
        /// <summary>현재 기억 중인 유효 Threat 대상 수입니다.</summary>
        int ThreatTargetCount { get; }

        /// <summary>
        /// 현재 선택된 타겟에 누적된 총 Threat를 조회합니다.
        /// </summary>
        /// <param name="threat">현재 타겟의 총 Threat입니다.</param>
        /// <returns>유효한 현재 타겟과 Threat가 있으면 <see langword="true"/>입니다.</returns>
        bool TryGetCurrentTargetThreat(out float threat);

        /// <summary>
        /// 지정한 Transform에 대응하는 Threat 값을 조회합니다.
        /// </summary>
        /// <param name="target">조회할 캐릭터 Transform 또는 하위 Transform입니다.</param>
        /// <param name="threat">대상에게 누적된 총 Threat입니다.</param>
        /// <returns>대상이 Threat 목록에 있으면 <see langword="true"/>입니다.</returns>
        bool TryGetThreat(Transform target, out float threat);

        /// <summary>
        /// 현재 Threat 목록을 다시 평가하여 최종 전투 타겟을 갱신합니다.
        /// </summary>
        /// <returns>유효한 타겟을 선택했으면 <see langword="true"/>입니다.</returns>
        bool RefreshCombatTarget();
    }

    /// <summary>
    /// 몬스터의 홈 및 Leash 상태를 상위 AI에 제공하는 선택 인터페이스입니다.
    /// </summary>
    public interface IMonsterLeashProvider
    {
        /// <summary>현재 Leash 런타임 상태입니다.</summary>
        MonsterLeashState LeashState { get; }

        /// <summary>홈 복귀 또는 재활성 대기 중인지 여부입니다.</summary>
        bool IsReturningHome { get; }

        /// <summary>현재 몬스터와 홈 사이의 2D 거리입니다.</summary>
        float DistanceFromHome { get; }

        /// <summary>현재 전투 타겟과 홈 사이의 2D 거리입니다.</summary>
        float TargetDistanceFromHome { get; }

        /// <summary>현재 홈 위치를 조회합니다.</summary>
        bool TryGetHomePosition(out Vector3 homePosition);

        /// <summary>외부 AI 또는 전투 규칙에서 홈 복귀를 명시적으로 시작합니다.</summary>
        bool RequestBeginEvade(MonsterLeashTrigger trigger = MonsterLeashTrigger.Manual);
    }


    /// <summary>
    /// 몬스터의 다수 공격 슬롯 예약 상태와 제어 API를 상위 AI에 제공하는 선택 인터페이스입니다.
    /// </summary>
    public interface IMonsterAttackSlotProvider
    {
        /// <summary>현재 공격 슬롯 정책이 활성화되어 있는지 여부입니다.</summary>
        bool IsAttackSlotEnabled { get; }

        /// <summary>현재 유효한 공격 슬롯 예약을 보유하는지 여부입니다.</summary>
        bool HasAttackSlotReservation { get; }

        /// <summary>현재 예약된 0 기반 슬롯 인덱스입니다. 예약이 없으면 -1입니다.</summary>
        int ReservedAttackSlotIndex { get; }

        /// <summary>현재 전투 대상의 슬롯을 예약할 수 있는지 확인합니다.</summary>
        bool CanReserveAttackSlot();

        /// <summary>현재 전투 대상의 공격 슬롯을 예약합니다.</summary>
        bool TryReserveAttackSlot();

        /// <summary>현재 보유한 공격 슬롯을 즉시 반환합니다.</summary>
        void ReleaseAttackSlot();
    }

}
