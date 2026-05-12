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
    }
}
