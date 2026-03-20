
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
            KnockBack = 1,
            KnockDown = 2,
            KnockUp = 3,
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
        /// CrowdControl 종료 시 최종 Y 위치를 어떻게 결정할지 정의합니다.
        /// </summary>
        public enum EndYMode
        {
            /// <summary>
            /// 기존 동작 유지. 계산된 이동 벡터의 Y를 그대로 사용합니다.
            /// </summary>
            None = 0,

            /// <summary>
            /// CC 시작 시점의 Y를 유지합니다.
            /// </summary>
            KeepStartY = 1,

            /// <summary>
            /// CC 시작 시점의 Y에 <c>EndYOffset</c>를 더한 값을 사용합니다.
            /// </summary>
            AddOffsetFromStart = 2,

            /// <summary>
            /// 월드 절대 Y 값(<c>EndYAbsolute</c>)을 사용합니다.
            /// </summary>
            Absolute = 3,

            /// <summary>
            /// 종료 X 위치에서 바닥을 다시 탐색한 뒤, 탐지된 지면 Y에 <c>EndYOffset</c>를 더해 사용합니다.
            /// </summary>
            GroundAtEndX = 4,
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
