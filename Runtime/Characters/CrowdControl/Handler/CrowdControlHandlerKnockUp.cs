using UnityEngine;

namespace GGemCo2DCore
{
    internal sealed class CrowdControlHandlerKnockUp : CrowdControlHandlerBaseAirborne
    {
        public override CrowdControlConstants.Type CrowdControlType => CrowdControlConstants.Type.KnockUp;

        public override bool TryBuildMotionRequest(
            CrowdControlRuntimeData crowdControl,
            Vector2 travelDirection,
            float travelDistance,
            out MotionRequest request)
        {
            request = default;
            if (crowdControl == null) return false;

            float riseTime = Mathf.Max(0f, crowdControl.KnockUpRiseTime);
            float airTime = Mathf.Max(0f, crowdControl.KnockUpAirTime);
            float fallTime = Mathf.Max(0f, crowdControl.KnockUpFallTime);
            float totalTime = riseTime + airTime + fallTime;
            if (totalTime <= 0f)
                totalTime = Mathf.Max(0f, crowdControl.Duration);

            float riseRatio = totalTime > 0f ? (riseTime / totalTime) : 0.5f;
            float airRatio = totalTime > 0f ? (airTime / totalTime) : 0f;
            float fallRatio = totalTime > 0f ? (fallTime / totalTime) : 0.5f;

            request = new MotionRequest(
                MotionChannel.CrowdControl,
                MotionKind.Arc,
                travelDirection,
                totalTime,
                Mathf.Max(0f, travelDistance),
                crowdControl.EaseType,
                stopAtEnd: true,
                useMovePosition: true,
                allowReplace: true,
                holdSecondsAfter: 0f,
                arcHeight: Mathf.Max(0f, crowdControl.Height),
                arcMode: MotionArcMode.DistancePhased,
                arcRiseEaseType: crowdControl.KnockUpRiseEaseType,
                arcFallEaseType: crowdControl.KnockUpFallEaseType,
                arcApexHoldNormalized: airRatio,
                arcRiseRatioNormalized: riseRatio,
                arcFallRatioNormalized: fallRatio,
                stopOnWall: crowdControl.IsStopOnWall);

            return true;
        }

        /// <summary>
        /// KnockUp은 Arc 모션 시간이 끝나도 실제 지면 도착이 확인될 때까지 종료하지 않습니다.
        /// </summary>
        /// <param name="crowdControl">현재 적용 중인 Crowd Control 데이터입니다.</param>
        /// <returns>항상 <see langword="true"/>를 반환하여 착지 기반 종료 정책을 사용합니다.</returns>
        /// <remarks>
        /// FallTime은 하강 보간 시간으로만 사용하고, LandEnd 전환은 Ground Probe가 확인한 실제 착지 시점에만 수행합니다.
        /// </remarks>
        public override bool IsLandingDriven(CrowdControlRuntimeData crowdControl)
        {
            return true;
        }

        /// <summary>
        /// Arc 모션이 완료된 후에도 지면이 충분히 가까울 때만 KnockUp을 종료할 수 있도록 판정합니다.
        /// </summary>
        /// <param name="controller">착지 판정을 수행할 Crowd Control 컨트롤러입니다.</param>
        /// <param name="crowdControl">현재 적용 중인 Crowd Control 데이터입니다.</param>
        /// <returns>실제 지면과의 거리가 착지 허용 범위 안이면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// 기존 최종 스냅 거리처럼 넓은 탐색 거리를 사용하면 높은 위치에서 바닥을 감지하여
        /// 공중에서 LandEnd 애니메이션으로 전환될 수 있으므로, FallLoop 착지 판정과 같은 짧은 거리만 허용합니다.
        /// 아직 지면에 가깝지 않다면 전용 하강 모션을 이어서 실행하고 종료 처리는 보류합니다.
        /// </remarks>
        public override bool TryHandleCompletedLanding(
            CharacterCrowdControlController controller,
            CrowdControlRuntimeData crowdControl)
        {
            if (TrySnapLandingWithinDistance(
                    controller,
                    CharacterCrowdControlController.KnockUpLandingTriggerDistance))
                return true;

            controller?.TryStartCrowdControlLandingFall(
                ResolveLandingFallSpeed(crowdControl),
                crowdControl != null && crowdControl.IsStopOnWall);
            return false;
        }

        /// <summary>
        /// KnockUp Arc 모션 완료 후 실제 착지까지 이어서 사용할 하강 속도를 계산합니다.
        /// </summary>
        /// <param name="crowdControl">현재 적용 중인 Crowd Control 데이터입니다.</param>
        /// <returns>0보다 큰 하강 속도입니다.</returns>
        /// <remarks>
        /// FallTime이 있으면 Height를 FallTime 동안 내려오는 속도로 환산하고,
        /// 데이터가 비어있으면 최소 1 월드 단위/초 이상으로 내려오도록 보정합니다.
        /// </remarks>
        private static float ResolveLandingFallSpeed(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return 1f;

            float height = Mathf.Max(0f, crowdControl.Height);
            float fallTime = Mathf.Max(0f, crowdControl.KnockUpFallTime);
            if (height > CharacterCrowdControlController.Epsilon && fallTime > CharacterCrowdControlController.Epsilon)
                return Mathf.Max(CharacterCrowdControlController.Epsilon, height / fallTime);

            return Mathf.Max(1f, height);
        }

        public override float GetAdditionalEndWaitTime(CrowdControlRuntimeData crowdControl)
        {
            return Mathf.Max(0f, crowdControl?.KnockUpLandEndWaitTime ?? 0f);
        }
    }

}