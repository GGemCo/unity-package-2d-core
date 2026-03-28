using UnityEngine;

namespace GGemCo2DCore
{
    public interface ICrowdControlHandler
    {
        CrowdControlConstants.Type CrowdControlType { get; }

        bool TryBuildMotionRequest(
            CrowdControlRuntimeData crowdControl,
            Vector2 travelDirection,
            float travelDistance,
            out MotionRequest request);
    }

    internal abstract class LinearCrowdControlHandlerBase : ICrowdControlHandler
    {
        public abstract CrowdControlConstants.Type CrowdControlType { get; }

        public bool TryBuildMotionRequest(
            CrowdControlRuntimeData crowdControl,
            Vector2 travelDirection,
            float travelDistance,
            out MotionRequest request)
        {
            request = default;
            if (crowdControl == null) return false;

            request = new MotionRequest(
                MotionChannel.CrowdControl,
                MotionKind.Linear,
                travelDirection,
                Mathf.Max(0f, crowdControl.Duration),
                Mathf.Max(0f, travelDistance),
                crowdControl.EaseType,
                stopAtEnd: true,
                useMovePosition: true,
                allowReplace: true,
                holdSecondsAfter: Mathf.Max(0f, crowdControl.DownWaitTime));

            return true;
        }
    }

    internal sealed class KnockBackCrowdControlHandler : LinearCrowdControlHandlerBase
    {
        public override CrowdControlConstants.Type CrowdControlType => CrowdControlConstants.Type.KnockBack;
    }

    internal sealed class KnockDownCrowdControlHandler : LinearCrowdControlHandlerBase
    {
        public override CrowdControlConstants.Type CrowdControlType => CrowdControlConstants.Type.KnockDown;
    }

    internal sealed class KnockUpCrowdControlHandler : ICrowdControlHandler
    {
        public CrowdControlConstants.Type CrowdControlType => CrowdControlConstants.Type.KnockUp;

        public bool TryBuildMotionRequest(
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
                arcFallRatioNormalized: fallRatio);

            return true;
        }
    }

    internal sealed class KnockDownAirCrowdControlHandler : ICrowdControlHandler
    {
        public CrowdControlConstants.Type CrowdControlType => CrowdControlConstants.Type.KnockDownAir;

        public bool TryBuildMotionRequest(
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
    }
}
