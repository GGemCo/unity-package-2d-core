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
            StopAtGround = 0,
            StopAtHostile = 1,
            StopAtGroundOrHostile = 2,
            IgnoreBlocking = 3,
        }

        /// <summary>
        /// 레이저가 적대 대상을 처리하는 방식을 정의합니다.
        /// </summary>
        public enum HitMode
        {
            FirstHitOnly = 0,
            PierceHostiles = 1,
        }

        /// <summary>
        /// 레이저 조준 방향을 언제 갱신할지를 정의합니다.
        /// </summary>
        public enum AimUpdateMode
        {
            Snapshot = 0,
            Continuous = 1,
        }
    }
}
