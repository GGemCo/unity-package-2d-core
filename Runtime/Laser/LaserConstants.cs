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

            /// <summary>
            /// 타겟의 좌우 위치만 판정하고, 해당 수평 방향을 기준으로 설정된 각도를 적용합니다.
            /// </summary>
            TowardTargetHorizontal = 2,
        }

        /// <summary>
        /// VFX 각도를 레이캐스트 각도에 동기화할지 정의합니다.
        /// </summary>
        public enum VfxAngleSyncMode
        {
            /// <summary>
            /// VFX 각도를 레이저 진행 방향과 동기화하지 않습니다.
            /// </summary>
            None = -1,

            /// <summary>
            /// 매 프레임 레이캐스트 방향을 따라 VFX 각도를 동기화합니다.
            /// </summary>
            FollowRaycast = 0,

            /// <summary>
            /// 발사 시점 각도로 VFX 각도를 고정합니다.
            /// </summary>
            LockAtLaunch = 1,
        }

        /// <summary>
        /// 레이저 VFX를 실제 레이저 길이에 맞춰 표현할지, 원본 VFX 모양을 유지할지 정의합니다.
        /// </summary>
        public enum VfxPresentationPolicy
        {
            /// <summary>
            /// VfxEffectLaser를 사용하여 레이저 길이와 두께에 맞게 VFX를 변형합니다.
            /// </summary>
            StretchToBeam = 0,

            /// <summary>
            /// VFX의 길이, 두께, 스케일을 변경하지 않고 원본 형태로 재생합니다.
            /// </summary>
            PreserveShape = 1,
        }

        /// <summary>
        /// 레이저 시작점을 어떤 방식으로 오버라이드할지 정의합니다.
        /// </summary>
        public enum StartPositionOverrideMode
        {
            /// <summary>
            /// laser 테이블의 StartPosition 값을 그대로 사용합니다.
            /// </summary>
            UseLaserTable = 0,

            /// <summary>
            /// laser 테이블의 StartPosition 대신 오버라이드 오프셋을 사용합니다.
            /// </summary>
            ReplaceTableOffset = 1,

            /// <summary>
            /// laser 테이블의 StartPosition에 오버라이드 오프셋을 더해서 사용합니다.
            /// </summary>
            AddToTableOffset = 2,

            /// <summary>
            /// 시전자 위치를 무시하고 월드 좌표를 시작점으로 직접 사용합니다.
            /// </summary>
            WorldPosition = 3,
        }

        /// <summary>
        /// 레이저 시작점을 언제 갱신할지 정의합니다.
        /// </summary>
        public enum StartPointUpdateMode
        {
            /// <summary>
            /// 활성 시간 동안 시전자 기준 시작점을 계속 갱신합니다.
            /// </summary>
            FollowOwner = 0,

            /// <summary>
            /// 발사 시점의 시작점을 고정하여 유지합니다.
            /// </summary>
            SnapshotAtLaunch = 1,
        }

    }
}
