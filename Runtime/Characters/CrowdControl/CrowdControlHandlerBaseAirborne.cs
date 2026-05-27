using UnityEngine;

namespace GGemCo2DCore
{
    public enum CrowdControlAirborneAnimationPhase
    {
        None = 0,
        Rise = 1,
        Air = 2,
        FallLoop = 3,
        LandEnd = 4,
    }
    internal abstract class CrowdControlHandlerBaseAirborne : CrowdControlHandlerBase
    {
        public override void UpdateRuntime(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            if (controller == null || crowdControl == null)
                return;

            if (!HasPhasedAnimation(crowdControl))
                return;

            if (!controller.TryGetCrowdControlMotionProgress(out float progress01))
                return;

            var nextPhase = EvaluateAnimationPhase(crowdControl, progress01);
            PlayPhaseAnimation(controller, crowdControl, nextPhase, force: false);
        }

        public override bool TryHandleActiveLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            if (controller == null || crowdControl == null)
                return false;

            if (!IsLandingPhase(controller, crowdControl))
                return false;

            if (!controller.TryProbeGroundBelow(out float groundY, out float bottomY))
                return false;

            float distanceToGround = bottomY - groundY;
            if (distanceToGround < -CharacterCrowdControlController.KnockUpLandingProbeUpOffset ||
                distanceToGround > CharacterCrowdControlController.KnockUpLandingTriggerDistance)
                return false;

            controller.SnapCharacterBottomToGround(groundY, bottomY);
            controller.CancelCrowdControlMotion(reason: 201);
            return true;
        }

        public override bool TryHandleCompletedLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            if (controller == null || crowdControl == null)
                return false;

            float snapProbeDistance = Mathf.Max(
                CharacterCrowdControlController.KnockUpLandingFinalSnapDistance,
                Mathf.Max(1f, crowdControl.Height + Mathf.Abs(crowdControl.EndYOffset)));

            if (controller.TryProbeGroundBelow(snapProbeDistance, out float groundY, out float bottomY))
            {
                float distanceToGround = bottomY - groundY;
                if (distanceToGround >= -CharacterCrowdControlController.KnockUpLandingProbeUpOffset &&
                    distanceToGround <= snapProbeDistance)
                {
                    controller.SnapCharacterBottomToGround(groundY, bottomY);
                    return true;
                }
            }

            return controller.IsCurrentlyGrounded(CharacterCrowdControlController.KnockUpLandingTriggerDistance);
        }

        public override bool TryGetInitialAnimation(
            CharacterCrowdControlController controller,
            CrowdControlRuntimeData crowdControl,
            out string animationName,
            out bool loop,
            out CrowdControlAirborneAnimationPhase phase)
        {
            animationName = null;
            loop = false;
            phase = CrowdControlAirborneAnimationPhase.None;

            if (controller == null || crowdControl == null || !HasPhasedAnimation(crowdControl))
                return false;

            phase = EvaluateAnimationPhase(crowdControl, 0f);
            animationName = GetPhaseAnimationName(crowdControl, phase);
            if (string.IsNullOrWhiteSpace(animationName))
                return false;

            loop = ShouldLoopPhase(crowdControl, phase);
            return true;
        }

        public override string ResolveEndAnimationName(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            var animationController = controller?.AnimationController;
            if (animationController == null)
                return base.ResolveEndAnimationName(controller, crowdControl);

            string landEndName = GetPhaseAnimationName(crowdControl, CrowdControlAirborneAnimationPhase.LandEnd);
            if (!string.IsNullOrWhiteSpace(landEndName) && animationController.HasAnimation(landEndName))
            {
                controller.CurrentAirborneAnimationPhase = CrowdControlAirborneAnimationPhase.LandEnd;
                controller.CurrentPhaseAnimationName = landEndName;
                return landEndName;
            }

            return base.ResolveEndAnimationName(controller, crowdControl);
        }

        protected void PlayPhaseAnimation(
            CharacterCrowdControlController controller,
            CrowdControlRuntimeData crowdControl,
            CrowdControlAirborneAnimationPhase phase,
            bool force)
        {
            var animationController = controller?.AnimationController;
            if (animationController == null)
                return;

            if (!force && controller.CurrentAirborneAnimationPhase == phase)
                return;

            string animationName = GetPhaseAnimationName(crowdControl, phase);
            if (string.IsNullOrWhiteSpace(animationName) || !animationController.HasAnimation(animationName))
                return;

            bool loop = ShouldLoopPhase(crowdControl, phase);
            // 강제 갱신 모드가 켜져 있으면 phase 전환 시에도 동일 상태를 첫 프레임부터 재생합니다.
            bool shouldForceReset = force || controller.ForceRefreshAnimationOnCurrentCrowdControl;
            animationController.PlayCharacterAnimation(
                animationName,
                loop,
                timeScale: 1f,
                forceReset: shouldForceReset);
            controller.CurrentPhaseAnimationName = animationName;
            controller.CurrentAirborneAnimationPhase = phase;
        }

        protected virtual bool ShouldLoopPhase(CrowdControlRuntimeData crowdControl, CrowdControlAirborneAnimationPhase phase)
        {
            return phase == CrowdControlAirborneAnimationPhase.FallLoop;
        }

        protected bool HasPhasedAnimation(CrowdControlRuntimeData crowdControl)
        {
            if (crowdControl == null)
                return false;

            return !string.IsNullOrWhiteSpace(crowdControl.KnockUpRiseAnimationName)
                   || !string.IsNullOrWhiteSpace(crowdControl.KnockUpAirAnimationName)
                   || !string.IsNullOrWhiteSpace(crowdControl.KnockUpFallAnimationName)
                   || !string.IsNullOrWhiteSpace(crowdControl.KnockUpLandEndAnimationName);
        }

        protected bool IsLandingPhase(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            if (controller.CurrentAirborneAnimationPhase == CrowdControlAirborneAnimationPhase.FallLoop)
                return true;

            if (!controller.TryGetCrowdControlMotionProgress(out float progress01))
                return false;

            return EvaluateAnimationPhase(crowdControl, progress01) == CrowdControlAirborneAnimationPhase.FallLoop;
        }

        protected CrowdControlAirborneAnimationPhase EvaluateAnimationPhase(CrowdControlRuntimeData crowdControl, float progress01)
        {
            if (crowdControl == null)
                return CrowdControlAirborneAnimationPhase.None;

            float riseTime = Mathf.Max(0f, crowdControl.KnockUpRiseTime);
            float airTime = Mathf.Max(0f, crowdControl.KnockUpAirTime);
            float fallTime = GetFallTime(crowdControl);
            float totalTime = riseTime + airTime + fallTime;
            if (totalTime <= CharacterCrowdControlController.Epsilon)
                return CrowdControlAirborneAnimationPhase.Rise;

            float riseEnd = riseTime / totalTime;
            float airEnd = (riseTime + airTime) / totalTime;
            float normalized = Mathf.Clamp01(progress01);

            if (normalized < riseEnd)
                return CrowdControlAirborneAnimationPhase.Rise;
            if (normalized < airEnd)
                return CrowdControlAirborneAnimationPhase.Air;
            return CrowdControlAirborneAnimationPhase.FallLoop;
        }

        protected virtual float GetFallTime(CrowdControlRuntimeData crowdControl)
        {
            return Mathf.Max(0f, crowdControl?.KnockUpFallTime ?? 0f);
        }

        protected string GetPhaseAnimationName(CrowdControlRuntimeData crowdControl, CrowdControlAirborneAnimationPhase phase)
        {
            if (crowdControl == null)
                return string.Empty;

            switch (phase)
            {
                case CrowdControlAirborneAnimationPhase.Rise:
                    return !string.IsNullOrWhiteSpace(crowdControl.KnockUpRiseAnimationName)
                        ? crowdControl.KnockUpRiseAnimationName
                        : crowdControl.StaggerAnimationName;

                case CrowdControlAirborneAnimationPhase.Air:
                    return crowdControl.KnockUpAirAnimationName;

                case CrowdControlAirborneAnimationPhase.FallLoop:
                    return crowdControl.KnockUpFallAnimationName;

                case CrowdControlAirborneAnimationPhase.LandEnd:
                    return crowdControl.KnockUpLandEndAnimationName;

                default:
                    return crowdControl.StaggerAnimationName;
            }
        }
    }
}
