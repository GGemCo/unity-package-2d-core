using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 자동 이동 유형
    /// </summary>
    public enum AutoMoveType
    {
        Target = 0,
        Direction = 1
    }

    /// <summary>
    /// 목표 지점 없이 좌/우로만 이동하는 방향
    /// </summary>
    public enum AutoMoveDirection
    {
        Left = -1,
        Right = 1
    }

    /// <summary>
    /// 자동 이동 도중 수동 입력이 들어왔을 때 취소 정책
    /// </summary>
    public enum AutoMoveCancelPolicy
    {
        NeverCancel = 0,
        MoveInputCancel = 1,
        AnyInputCancel = 2
    }

    /// <summary>
    /// 자동 이동 중 발생한 입력 타입(취소 정책 판정용)
    /// </summary>
    public enum AutoMoveInputType
    {
        Move,
        Attack,
        Guard,
        Jump,
        Dash,
        Interaction,
        SimulationTool,
        Other = 99,
    }

    /// <summary>
    /// 자동 이동 요청 데이터
    /// </summary>
    [Serializable]
    public sealed class AutoMoveRequest
    {
        public AutoMoveType moveType = AutoMoveType.Target;

        // ===== Target 기반 이동 =====
        public Vector2? targetPosition;
        public Transform targetTransform;
        public float stopDistance = 0.1f;

        // ===== Direction 기반 이동 =====
        public AutoMoveDirection direction = AutoMoveDirection.Right;
        public bool infiniteMove = true;
        public float duration = 1.0f;

        // ===== Direction 기반 이동 보정 =====
        /// <summary>
        /// 전투 중 타겟을 지나쳤을 때, 타겟 방향으로 자동 복귀할지 여부입니다.
        /// </summary>
        public bool enableCombatTargetRecovery = true;

        /// <summary>
        /// 타겟 지나침 판정에 사용할 X축 오차 허용값입니다.
        /// </summary>
        public float combatTargetPassedEpsilon = 0.05f;

        /// <summary>
        /// 전투 타겟 복귀 완료로 판단할 X축 거리입니다.
        /// </summary>
        public float combatTargetRecoveryStopDistance = 0.35f;

        /// <summary>
        /// 전투 타겟 복귀 종료 후 재진입을 막는 시간입니다.
        /// </summary>
        public float combatTargetRecoveryCooldownSeconds = 0.2f;

        /// <summary>
        /// 전투 타겟 복귀가 완료되면 자동 이동을 종료할지 여부입니다.
        /// </summary>
        public bool stopAutoMoveOnCombatTargetRecovered = true;

        // ===== 공통 옵션 =====
        public float speedScale = 1.0f;
        public AutoMoveCancelPolicy cancelPolicy = AutoMoveCancelPolicy.AnyInputCancel;

        /// <summary>
        /// Target 도착 또는 Direction 종료 조건 달성 시 호출
        /// </summary>
        [NonSerialized] public Action onArrived;
    }
}
