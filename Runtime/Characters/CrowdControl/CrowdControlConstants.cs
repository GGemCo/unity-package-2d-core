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
            KnockDownAir = 4,
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
            
            /// <summary>
            /// Target → Source 방향으로 적용합니다.
            /// </summary>
            FromTargetToSource = 4,
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
        /// CrowdControl 종료 위치를 현재 카메라 화면 안쪽으로 보정하는 정책입니다.
        /// </summary>
        public enum EndViewportPolicy
        {
            /// <summary>
            /// 화면 경계 보정을 적용하지 않습니다.
            /// </summary>
            None = 0,

            /// <summary>
            /// 플레이어의 최종 위치가 화면을 벗어나면 화면 안쪽으로 보정합니다.
            /// </summary>
            ClampPlayerToViewport = 1,

            /// <summary>
            /// 플레이어의 최종 위치를 화면 안쪽으로 보정하되,
            /// UseParallax 맵에서 게임 카메라가 플레이어를 정상 추적할 수 있으면 보정하지 않습니다.
            /// </summary>
            ClampPlayerExceptFreeCameraFollow = 2,

            /// <summary>
            /// 플레이어 또는 몬스터의 위치를 화면 안쪽으로 보정합니다.
            /// </summary>
            ClampCombatCharacterToViewport = 3,

            /// <summary>
            /// 플레이어 또는 몬스터의 위치를 화면 안쪽으로 보정하되,
            /// 현재 카메라가 해당 캐릭터를 정상 추적할 수 있으면 보정하지 않습니다.
            /// </summary>
            ClampCombatCharacterExceptFreeCameraFollow = 4,
        }

        /// <summary>
        /// CrowdControl 화면 경계 보정을 적용할 실행 단계를 정의합니다.
        /// </summary>
        public enum ViewportConstraintPhase
        {
            /// <summary>
            /// 기존 동작처럼 CrowdControl 종료 위치에만 화면 경계 보정을 적용합니다.
            /// </summary>
            EndOnly = 0,

            /// <summary>
            /// CrowdControl 이동 중과 종료 위치 모두에 화면 경계 보정을 적용합니다.
            /// </summary>
            DuringAndEnd = 1,
        }

        /// <summary>
        /// CrowdControl 종료 위치의 화면 경계 보정 축을 정의합니다.
        /// </summary>
        public enum EndViewportClampAxis
        {
            /// <summary>
            /// 횡스크롤 이동에 필요한 X축만 보정합니다.
            /// </summary>
            Horizontal = 0,

            /// <summary>
            /// X축과 Y축을 모두 보정합니다.
            /// </summary>
            Both = 1,
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

        /// <summary>
        /// CC 종료 시 재생할 종료 애니메이션의 접미사입니다.
        /// - Animator에서 Start → Wait 전환을 구성하는 경우, Wait는 자동으로 전환됩니다.
        /// - End는 본 컨트롤러가 명시적으로 재생합니다.
        /// </summary>
        public const string StaggerAnimationWaitSuffix = "_wait";
        public const string StaggerAnimationEndSuffix = "_end";
    }
}
