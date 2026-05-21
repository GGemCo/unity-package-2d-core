using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 동행 캐릭터가 거리 임계값을 해석하는 정책입니다.
    /// </summary>
    public enum CompanionFollowDistancePolicy
    {
        /// <summary>
        /// 매 프레임 거리 임계값을 연속적으로 재평가하는 기본 정책입니다.
        /// </summary>
        DefaultContinuousCheck = 0,

        /// <summary>
        /// Max Distance를 넘으면 Follow Offset 목표점을 고정하고, 도착할 때까지 Max Distance 재평가를 보류합니다.
        /// </summary>
        RecoverOffsetThenRecheck = 1
    }

    /// <summary>
    /// 동행 캐릭터가 추적 대상과 유지할 거리 및 이동 방식을 정의합니다.
    /// </summary>
    [Serializable]
    public sealed class CompanionFollowSettings
    {
        [Tooltip("대상과 이 거리 이하이면 이동하지 않습니다.")]
        public float minDistance = 1.5f;

        [Tooltip("대상과 이 거리 이상이면 추적을 시작합니다.")]
        public float maxDistance = 3.0f;

        [Tooltip("대상 기준으로 유지할 상대 위치입니다.")]
        public Vector2 followOffset = new(-2f, 0f);

        [Tooltip("동행 캐릭터 이동 속도입니다.")]
        public float moveSpeed = 4f;

        [Tooltip("대상과 이 거리 이상 멀어지면 즉시 위치를 보정합니다. 0 이하면 사용하지 않습니다.")]
        public float teleportDistance = 8f;

        [Tooltip("거리 판정 정책입니다. 기본은 기존 연속 거리 체크를 사용합니다.")]
        public CompanionFollowDistancePolicy distancePolicy = CompanionFollowDistancePolicy.DefaultContinuousCheck;

        [Tooltip("RecoverOffsetThenRecheck 정책에서 Follow Offset 도착으로 간주할 거리 오차입니다.")]
        public float offsetArriveThreshold = 0.05f;

        [Tooltip("이동 방향을 기준으로 캐릭터 좌우 방향을 갱신합니다.")]
        public bool flipByMoveDirection = true;

        [Tooltip("이동/정지 애니메이션을 자동으로 재생합니다.")]
        public bool updateAnimation = true;

        /// <summary>
        /// 인스펙터 값이 잘못 입력되어도 런타임에서 안전하게 사용할 수 있도록 보정합니다.
        /// </summary>
        public void Normalize()
        {
            minDistance = Mathf.Max(0f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            teleportDistance = Mathf.Max(0f, teleportDistance);
            offsetArriveThreshold = Mathf.Max(0f, offsetArriveThreshold);
        }
    }
}


