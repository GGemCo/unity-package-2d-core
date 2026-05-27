using UnityEngine;

namespace GGemCo2DCore
{
    internal abstract class CrowdControlHandlerBase : ICrowdControlHandler
    {
        public abstract CrowdControlConstants.Type CrowdControlType { get; }

        public abstract bool TryBuildMotionRequest(
            CrowdControlRuntimeData crowdControl,
            Vector2 travelDirection,
            float travelDistance,
            out MotionRequest request);

        public virtual void UpdateRuntime(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
        }

        public virtual bool TryHandleActiveLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            return false;
        }

        public virtual bool TryHandleCompletedLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            return false;
        }

        public virtual bool IsLandingDriven(CrowdControlRuntimeData crowdControl)
        {
            return false;
        }

        public virtual float GetAdditionalEndWaitTime(CrowdControlRuntimeData crowdControl)
        {
            return 0f;
        }

        public virtual bool TryGetInitialAnimation(
            CharacterCrowdControlController controller,
            CrowdControlRuntimeData crowdControl,
            out string animationName,
            out bool loop,
            out CrowdControlAirborneAnimationPhase phase)
        {
            animationName = null;
            loop = false;
            phase = CrowdControlAirborneAnimationPhase.None;
            return false;
        }

        public virtual string ResolveEndAnimationName(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            var animationController = controller?.AnimationController;
            if (animationController == null)
                return null;

            if (!string.IsNullOrWhiteSpace(controller.CurrentPhaseAnimationName))
            {
                string phaseEndName = controller.CurrentPhaseAnimationName + CrowdControlConstants.StaggerAnimationEndSuffix;
                if (animationController.HasAnimation(phaseEndName))
                    return phaseEndName;
            }

            if (!string.IsNullOrWhiteSpace(controller.CurrentStaggerAnimationName))
            {
                string defaultEndName = controller.CurrentStaggerAnimationName + CrowdControlConstants.StaggerAnimationEndSuffix;
                if (animationController.HasAnimation(defaultEndName))
                    return defaultEndName;
            }

            return null;
        }
    }
}