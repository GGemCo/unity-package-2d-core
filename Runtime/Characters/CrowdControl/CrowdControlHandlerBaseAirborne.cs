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
            if (crowdControl.AnimationOverride.SuppressRuntimePhaseAnimations)
                return;

            if (!controller.TryGetCrowdControlMotionProgress(out float progress01))
                return;

            var nextPhase = EvaluateAnimationPhase(crowdControl, progress01);
            PlayPhaseAnimation(controller, crowdControl, nextPhase, force: false);
        }

        /// <summary>
        /// 공중형 Crowd Control 진행 중 FallLoop 단계에서 실제 착지 여부를 확인합니다.
        /// </summary>
        /// <param name="controller">착지 판정을 수행할 Crowd Control 컨트롤러입니다.</param>
        /// <param name="crowdControl">현재 적용 중인 Crowd Control 데이터입니다.</param>
        /// <returns>지면에 충분히 가까워져 착지 처리를 완료했으면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// 착지로 인정되는 짧은 거리 안에서만 지면 스냅과 모션 취소를 수행하여,
        /// FallTime 진행 중 멀리 있는 바닥을 감지해 공중에서 종료되는 상황을 방지합니다.
        /// </remarks>
        public override bool TryHandleActiveLanding(CharacterCrowdControlController controller, CrowdControlRuntimeData crowdControl)
        {
            if (controller == null || crowdControl == null)
                return false;

            if (!IsLandingPhase(controller, crowdControl))
                return false;

            if (!TrySnapLandingWithinDistance(
                    controller,
                    CharacterCrowdControlController.KnockUpLandingTriggerDistance))
                return false;

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

        /// <summary>
        /// 캐릭터 하단과 지면 사이의 거리가 지정된 범위 안에 있을 때만 착지 스냅을 수행합니다.
        /// </summary>
        /// <param name="controller">착지 판정을 수행할 Crowd Control 컨트롤러입니다.</param>
        /// <param name="maxLandingDistance">착지로 인정할 최대 거리입니다.</param>
        /// <returns>지면을 찾고 허용 거리 안에서 스냅을 완료했으면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// 공중형 CC가 멀리 떨어진 지면을 잘못 잡아 공중에서 LandEnd로 전환되는 문제를 막기 위해,
        /// Raycast 탐색 거리와 최종 허용 거리를 같은 기준으로 제한합니다.
        /// </remarks>
        protected static bool TrySnapLandingWithinDistance(
            CharacterCrowdControlController controller,
            float maxLandingDistance)
        {
            if (controller == null)
                return false;

            float safeLandingDistance = Mathf.Max(0f, maxLandingDistance);
            if (!controller.TryProbeGroundBelow(safeLandingDistance, out float groundY, out float bottomY))
                return false;

            float distanceToGround = bottomY - groundY;
            if (distanceToGround < -CharacterCrowdControlController.KnockUpLandingProbeUpOffset ||
                distanceToGround > safeLandingDistance)
                return false;

            controller.SnapCharacterBottomToGround(groundY, bottomY);
            return true;
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
