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

            request = new MotionRequest(
                MotionChannel.CrowdControl,
                MotionKind.Arc,
                travelDirection,
                Mathf.Max(0f, crowdControl.Duration),
                Mathf.Max(0f, travelDistance),
                crowdControl.EaseType,
                stopAtEnd: true,
                useMovePosition: true,
                allowReplace: true,
                holdSecondsAfter: 0f,
                arcHeight: Mathf.Max(0f, crowdControl.Height),
                arcMode: MotionArcMode.DistancePhased,
                arcRiseEaseType: crowdControl.EaseType,
                arcFallEaseType: crowdControl.EaseType,
                arcApexHoldNormalized: 0f);

            return true;
        }
    }
}
