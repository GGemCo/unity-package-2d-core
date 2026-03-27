using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 애니메이션 이벤트 라우팅을 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        /// <summary>
        /// 공격 애니메이션 완료 이벤트를 발행하고 필요 시 기본 후처리를 수행합니다.
        /// </summary>
        public void OnAnimationCompleteAttack()
        {
            RequestAnimationCompleteAttackWithFallback(LegacyAnimationCompleteAttack);
        }

        /// <summary>
        /// 공격 종료 애니메이션 완료 이벤트를 발행하고 필요 시 기본 후처리를 수행합니다.
        /// </summary>
        public void OnAnimationCompleteAttackEnd()
        {
            RequestAnimationCompleteAttackEndWithFallback(LegacyAnimationCompleteAttackEnd);
        }

        /// <summary>
        /// 점프 관련 애니메이션 이벤트를 외부 구독자에게 전달합니다.
        /// </summary>
        /// <param name="eventName">애니메이션에서 전달된 이벤트 이름입니다.</param>
        public void AnimationEventJump(string eventName)
        {
            var e = new EventArgsOnAnimationEventJump { Handled = false, EventName = eventName };
            OnAnimationEventJump?.Invoke(this, e);
        }

        /// <summary>
        /// 대시 관련 애니메이션 이벤트를 외부 구독자에게 전달합니다.
        /// </summary>
        /// <param name="eventName">애니메이션에서 전달된 이벤트 이름입니다.</param>
        public void AnimationEventDash(string eventName)
        {
            var e = new EventArgsOnAnimationEventDash { Handled = false, EventName = eventName };
            OnAnimationEventDash?.Invoke(this, e);
        }

        /// <summary>
        /// 범용 모션 애니메이션 이벤트를 전달하고 필요 시 레거시 모션 처리로 연결합니다.
        /// </summary>
        /// <param name="motion">애니메이션 이벤트에서 전달된 모션 정보입니다.</param>
        public void AnimationEventMotion(StruckAnimationEventMotion motion)
        {
            motion ??= new StruckAnimationEventMotion();

            var e = new EventArgsOnAnimationEventMotion
            {
                Handled = false,
                Motion = motion
            };

            OnAnimationEventMotion?.Invoke(this, e);
            if (!e.Handled)
            {
                TryHandleMotionEventLegacy(e.Motion);
            }
        }

        /// <summary>
        /// Crowd Control 애니메이션 이벤트를 전달하고 필요 시 레거시 처리로 연결합니다.
        /// </summary>
        /// <param name="crowdControl">애니메이션 이벤트에서 전달된 Crowd Control 정보입니다.</param>
        public void AnimationEventCrowdControl(StruckAnimationEventCrowdControl crowdControl)
        {
            crowdControl ??= new StruckAnimationEventCrowdControl();

            var e = new EventArgsOnAnimationEventCrowdControl
            {
                Handled = false,
                CrowdControl = crowdControl
            };

            OnAnimationEventCrowdControl?.Invoke(this, e);
            if (!e.Handled)
            {
                TryHandleCrowdControlEventLegacy(e.CrowdControl);
            }
        }

        /// <summary>
        /// 가드 종료 애니메이션 이벤트를 외부 구독자에게 전달합니다.
        /// </summary>
        public void AnimationEventGuardEnd()
        {
            var e = new EventArgsOnAnimationEventGuardEnd { Handled = false };
            OnAnimationEventGuardEnd?.Invoke(this, e);
        }

        /// <summary>
        /// 공격 애니메이션 완료 시 기본 정지 동작을 수행합니다.
        /// </summary>
        private void LegacyAnimationCompleteAttack()
        {
            Stop();
        }

        /// <summary>
        /// 공격 종료 애니메이션 완료 시 기본 후처리를 수행합니다.
        /// </summary>
        private void LegacyAnimationCompleteAttackEnd()
        {
        }

        /// <summary>
        /// 공격 완료 이벤트를 발행하고 외부에서 처리하지 않으면 기본 폴백을 실행합니다.
        /// </summary>
        /// <param name="legacyFallback">외부에서 처리하지 않았을 때 실행할 기본 동작입니다.</param>
        private void RequestAnimationCompleteAttackWithFallback(Action legacyFallback)
        {
            var e = new EventArgsAnimationAttack { Handled = false };
            AnimationCompleteAttack?.Invoke(this, e);
            if (!e.Handled)
                legacyFallback?.Invoke();
        }

        /// <summary>
        /// 공격 종료 완료 이벤트를 발행하고 외부에서 처리하지 않으면 기본 폴백을 실행합니다.
        /// </summary>
        /// <param name="legacyFallback">외부에서 처리하지 않았을 때 실행할 기본 동작입니다.</param>
        private void RequestAnimationCompleteAttackEndWithFallback(Action legacyFallback)
        {
            var e = new EventArgsAnimationAttackEnd { Handled = false };
            AnimationCompleteAttackEnd?.Invoke(this, e);
            if (!e.Handled)
                legacyFallback?.Invoke();
        }

        /// <summary>
        /// Crowd Control 이벤트를 기존 컨트롤러 기반 처리로 연결합니다.
        /// </summary>
        /// <param name="crowdControl">적용할 Crowd Control 이벤트 데이터입니다.</param>
        private void TryHandleCrowdControlEventLegacy(StruckAnimationEventCrowdControl crowdControl)
        {
            if (crowdControl == null || crowdControl.CrowdControlUid <= 0)
                return;

            if (_crowdControlController == null)
            {
                _crowdControlController = GetComponent<CharacterCrowdControlController>();
                if (_crowdControlController == null)
                    return;
            }

            GameObject source = crowdControl.UseSelfAsSource ? gameObject : null;
            _crowdControlController.ApplyCrowdControlByUid(crowdControl.CrowdControlUid, source);
        }

        /// <summary>
        /// 모션 이벤트를 기존 모션 컨트롤러 기반 처리로 연결합니다.
        /// </summary>
        /// <param name="motion">처리할 모션 이벤트 데이터입니다.</param>
        private void TryHandleMotionEventLegacy(StruckAnimationEventMotion motion)
        {
            if (motion == null)
                return;

            if (_motionController == null)
            {
                _motionController = GetComponent<ICharacterMotionController>();
                if (_motionController == null)
                    return;
            }

            switch (motion.Action)
            {
                case AnimationMotionEventAction.Trigger:
                    return;
                case AnimationMotionEventAction.Start:
                {
                    MotionRequest request = BuildMotionRequest(motion);
                    _motionController.TryStartMotion(in request);
                    return;
                }
                case AnimationMotionEventAction.Cancel:
                    _motionController.CancelMotion(motion.Channel);
                    return;
                default:
                    return;
            }
        }

        /// <summary>
        /// 애니메이션 이벤트 정보를 모션 요청 객체로 변환합니다.
        /// </summary>
        /// <param name="motion">변환할 애니메이션 모션 이벤트입니다.</param>
        /// <returns>모션 컨트롤러에 전달할 <see cref="MotionRequest"/>입니다.</returns>
        private MotionRequest BuildMotionRequest(StruckAnimationEventMotion motion)
        {
            Vector2 direction = ResolveMotionEventDirection(motion);

            return new MotionRequest(
                motion.Channel,
                motion.Kind,
                direction,
                motion.Duration,
                motion.Distance,
                motion.EaseType,
                motion.StopAtEnd,
                motion.UseMovePosition,
                motion.AllowReplace,
                motion.HoldSecondsAfter,
                motion.Height,
                motion.ArcMode,
                motion.RiseEaseType,
                motion.FallEaseType,
                motion.ApexHoldNormalized,
                motion.RiseRatioNormalized,
                motion.FallRatioNormalized);
        }

        /// <summary>
        /// 애니메이션 모션 이벤트의 실제 이동 방향을 계산합니다.
        /// </summary>
        /// <param name="motion">방향 계산에 사용할 모션 이벤트입니다.</param>
        /// <returns>정규화된 이동 방향 벡터를 반환합니다.</returns>
        private Vector2 ResolveMotionEventDirection(StruckAnimationEventMotion motion)
        {
            if (motion.UseFacingDirection)
            {
                return new Vector2(GetFacingDirection(), 0f);
            }

            Vector2 direction = new Vector2(motion.DirectionX, motion.DirectionY);
            if (direction.sqrMagnitude <= 1e-6f)
            {
                return new Vector2(GetFacingDirection(), 0f);
            }

            return direction.normalized;
        }
    }
}
