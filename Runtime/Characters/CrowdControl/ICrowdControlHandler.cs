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

        float GetAdditionalEndWaitTime(CrowdControlRuntimeData crowdControl);

        string ResolveEndAnimationName(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl);

        void UpdateRuntime(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl);

        bool TryHandleActiveLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl);

        bool TryHandleCompletedLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl);

        bool IsLandingDriven(CrowdControlRuntimeData crowdControl);
        bool TryGetInitialAnimation(
            CharacterCrowdControlController controller,
            CrowdControlRuntimeData crowdControl,
            out string animationName,
            out bool loop,
            out CrowdControlAirborneAnimationPhase phase);
        
    }
}