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

        public override float GetAdditionalEndWaitTime(CrowdControlRuntimeData crowdControl)
        {
            return Mathf.Max(0f, crowdControl?.KnockUpLandEndWaitTime ?? 0f);
        }
    }

}