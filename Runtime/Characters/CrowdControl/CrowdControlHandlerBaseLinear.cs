using UnityEngine;

namespace GGemCo2DCore
{
    internal abstract class CrowdControlHandlerBaseLinear : CrowdControlHandlerBase
    {
        public override bool TryBuildMotionRequest(
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

}