using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 시스템에서 사용하는 전용 열거형 정의입니다.
    /// - ProjectileConstants와 분리된 레이저 전용 정책을 관리합니다.
    /// </summary>
    public static class LaserConstants
    {
        /// <summary>
        /// 레이저가 무엇에 의해 차단될지를 정의합니다.
        /// </summary>
        public enum BlockMode
        {
            /// <summary>
            /// 지형 또는 월드 충돌체를 만나면 해당 지점에서 레이저를 종료합니다.
            /// </summary>
            StopAtGround = 0,

            /// <summary>
            /// 적대 대상과 처음 충돌한 지점에서 레이저를 종료합니다.
            /// </summary>
            StopAtHostile = 1,

            /// <summary>
            /// 지형 또는 적대 대상 중 먼저 충돌한 지점에서 레이저를 종료합니다.
            /// </summary>
            StopAtGroundOrHostile = 2,

            /// <summary>
            /// 차단 대상을 무시하고 최대 거리까지 레이저를 유지합니다.
            /// </summary>
            IgnoreBlocking = 3,
        }

        /// <summary>
        /// 레이저가 적대 대상을 처리하는 방식을 정의합니다.
        /// </summary>
        public enum HitMode
        {
            /// <summary>
            /// 가장 먼저 적중한 적대 대상 하나에게만 판정을 적용합니다.
            /// </summary>
            FirstHitOnly = 0,

            /// <summary>
            /// 선 경로 상의 적대 대상을 관통하며 여러 대상에게 판정을 적용합니다.
            /// </summary>
            PierceHostiles = 1,
        }

        /// <summary>
        /// 레이저 조준 방향을 언제 갱신할지를 정의합니다.
        /// </summary>
        public enum AimUpdateMode
        {
            /// <summary>
            /// 생성 시점의 조준 방향을 고정하여 유지합니다.
            /// </summary>
            Snapshot = 0,

            /// <summary>
            /// 활성 시간 동안 타겟 또는 조준 방향을 지속적으로 갱신합니다.
            /// </summary>
            Continuous = 1,
        }

        /// <summary>
        /// 레이캐스트 방향을 어떤 기준으로 계산할지 정의합니다.
        /// </summary>
        public enum RaycastDirectionMode
        {
            /// <summary>
            /// 시작점에서 타겟(캐릭터/좌표)을 향하는 방향으로 계산합니다.
            /// </summary>
            TowardTarget = 0,

            /// <summary>
            /// 타겟을 무시하고 설정된 각도로 계산합니다.
            /// </summary>
            ByAngle = 1,
        }

        /// <summary>
        /// VFX 각도를 레이캐스트 각도에 동기화할지 정의합니다.
        /// </summary>
        public enum VfxAngleSyncMode
        {
            /// <summary>
            /// 매 프레임 레이캐스트 방향을 따라 VFX 각도를 동기화합니다.
            /// </summary>
            FollowRaycast = 0,

            /// <summary>
            /// 발사 시점 각도로 VFX 각도를 고정합니다.
            /// </summary>
            LockAtLaunch = 1,
        }
    }
}
