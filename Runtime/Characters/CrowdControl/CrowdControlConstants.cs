
namespace GGemCo2DCore
{
    public static class CrowdControlConstants
    {
        /// <summary>
        /// CrowdControl의 종류를 정의합니다.
        /// </summary>
        public enum Type
        {
            None = 0,
            Knockback = 1,
            Knockdown = 2,
        }

        /// <summary>
        /// CrowdControl 방향 결정 방식입니다.
        /// </summary>
        public enum DirectionType
        {
            None = 0,

            /// <summary>
            /// Source → Target 방향으로 적용합니다.
            /// </summary>
            FromSourceToTarget = 1,

            /// <summary>
            /// Target의 현재 바라보는 방향(좌/우)을 기준으로 적용합니다.
            /// </summary>
            FromTargetFacing = 2,

            /// <summary>
            /// 테이블에 정의된 고정 방향(FixedDirectionX/Y)을 사용합니다.
            /// </summary>
            Fixed = 3,
        }

        /// <summary>
        /// CrowdControl 적용 시 재생할 경직(피격) 애니메이션 정책입니다.
        /// </summary>
        public enum StaggerAnimationType
        {
            None = 0,
            Damage = 1,
            Groggy = 2,
        }
    }
}
