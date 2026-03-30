using UnityEngine;

namespace GGemCo2DCore
{
    
    internal sealed class CrowdControlHandlerKnockDownAir : CrowdControlHandlerBaseAirborne
    {
        public override CrowdControlConstants.Type CrowdControlType => CrowdControlConstants.Type.KnockDownAir;

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
            float totalPreFallTime = riseTime + airTime;
            if (totalPreFallTime <= 0f)
                totalPreFallTime = Mathf.Max(0f, crowdControl.Duration);

            float riseRatio = totalPreFallTime > 0f ? (riseTime / totalPreFallTime) : 1f;
            float airRatio = totalPreFallTime > 0f ? (airTime / totalPreFallTime) : 0f;

            request = new MotionRequest(
                MotionChannel.CrowdControl,
                MotionKind.KnockDownAir,
                travelDirection,
                totalPreFallTime,
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
                arcFallRatioNormalized: 0f,
                fallSpeed: Mathf.Max(0f, crowdControl.KnockDownAirFallSpeed));

            return true;
        }

        public override bool IsLandingDriven(CrowdControlRuntimeData crowdControl)
        {
            return true;
        }

        public override float GetAdditionalEndWaitTime(CrowdControlRuntimeData crowdControl)
        {
            return Mathf.Max(0f, crowdControl?.KnockDownAirLandEndWaitTime ?? 0f);
        }

        protected override float GetFallTime(CrowdControlRuntimeData crowdControl)
        {
            return Mathf.Max(CharacterCrowdControlController.Epsilon, crowdControl?.KnockUpFallTime ?? 0f);
        }

        protected override bool ShouldLoopPhase(CrowdControlRuntimeData crowdControl, CrowdControlAirborneAnimationPhase phase)
        {
            if (phase == CrowdControlAirborneAnimationPhase.Air)
                return crowdControl != null && crowdControl.KnockDownAirAnimationIsLoop;

            return base.ShouldLoopPhase(crowdControl, phase);
        }
    }
}