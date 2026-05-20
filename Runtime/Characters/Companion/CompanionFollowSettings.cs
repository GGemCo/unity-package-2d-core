using System;
using UnityEngine;

namespace GGemCo2DCore
{
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
        }
    }
}
