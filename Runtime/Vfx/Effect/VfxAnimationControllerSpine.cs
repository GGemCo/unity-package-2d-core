#if GGEMCO_USE_SPINE
using System;
using System.Collections.Generic;
using Spine;

namespace GGemCo2DCore
{
    public class VfxAnimationControllerSpine : Spine2dController, IVfxAnimationController
    {
        private VfxBehaviourBase _defaultEffect;
        private float durationStart;
        private float durationPlay;
        private float durationEnd;
        private float durationTotal;

        protected override void Awake()
        {
            base.Awake();
            _defaultEffect = GetComponent<VfxBehaviourBase>();
            if (_defaultEffect == null)
            {
                enabled = false;
                GcLogger.LogError("VfxBehaviourBase not found");
                return;
            }
            durationStart = GetAnimationDuration(IVfxAnimationController.KeyClipNameStart, false);
            durationPlay = GetAnimationDuration(IVfxAnimationController.KeyClipNamePlay, false);
            durationEnd = GetAnimationDuration(IVfxAnimationController.KeyClipNameEnd, false);
            durationTotal = durationStart + durationPlay + durationEnd; 
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }
        /// <summary>
        /// 애니메이션 클립이 플레이가 완료되면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        protected override void OnAnimationComplete(TrackEntry entry)
        {
            if (FindAnimation(IEffectAnimationController.KeyClipNameEnd) == null)
            {
                _defaultEffect.DestroyForce();
                return;
            }

            if (entry.Animation.Name != IEffectAnimationController.KeyClipNameEnd) return;
            _defaultEffect.OnEndAnimationComplete();
        }
        /// <summary>
        /// duration 정책에 맞춰 Spine start/play/end 애니메이션 재생 순서를 구성합니다.
        /// </summary>
        /// <param name="duration">재생 시간입니다. 0 이하는 1회 재생, 음수는 무제한 재생입니다.</param>
        /// <param name="timeScale">기본 재생 속도입니다.</param>
        /// <param name="forceReset">Spine은 SetAnimation 호출 시 트랙을 다시 설정하므로 호환용으로만 유지합니다.</param>
        /// <returns>시작 애니메이션을 찾고 재생을 요청했으면 true를 반환합니다.</returns>
        public bool Play(float duration, float timeScale = 1f, bool forceReset = false)
        {
            var findAnimation = FindAnimation(IEffectAnimationController.KeyClipNameStart);
            if (findAnimation == null) return false;

            // 무제한 플레이
            if (duration < 0)
            {
                float playTimeScale = timeScale > 0f ? timeScale : 1f;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>
                {
                    new(IEffectAnimationController.KeyClipNamePlay, true, 0, playTimeScale),
                };
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, playTimeScale, newAddAnimations);
            }
            // 한번만 재생
            else if (duration <= 0)
            {
                float playTimeScale = timeScale > 0f ? timeScale : 1f;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>
                {
                    new(IEffectAnimationController.KeyClipNamePlay, false, 0, playTimeScale),
                    new(IEffectAnimationController.KeyClipNameEnd, false, 0, playTimeScale)
                };
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, playTimeScale, newAddAnimations);
            }
            // play 클립 loop 하기
            else if (durationTotal < duration)
            {
                //loopAni
                var realLoopDuration = duration - durationStart - durationEnd;
                var loopCnt = realLoopDuration/durationPlay;
                var loopCntCeil = Math.Ceiling(realLoopDuration/durationPlay);
                float newTimeScale = (float)loopCntCeil/loopCnt;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();
                
                for(var i = 0; i< loopCntCeil; i++)
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(IEffectAnimationController.KeyClipNamePlay, false, 0, newTimeScale);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //endAni
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(IEffectAnimationController.KeyClipNameEnd);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //startAni
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, 1, newAddAnimations);
            }
            // 전체 클립 timescale 빠르게 
            else
            {
                float playTimeScale = durationTotal / duration;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>
                {
                    new(IEffectAnimationController.KeyClipNamePlay, false, 0, playTimeScale),
                    new(IEffectAnimationController.KeyClipNameEnd, false, 0, playTimeScale)
                };
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, playTimeScale, newAddAnimations);
            }

            return true;
        }

        public bool HasEndAnimation()
        {
            return FindAnimation(IEffectAnimationController.KeyClipNameEnd) != null;
        }

        /// <summary>
        /// Spine 종료 애니메이션을 재생합니다.
        /// </summary>
        public void PlayEnd()
        {
            PlayAnimation(IEffectAnimationController.KeyClipNameEnd);
        }

        /// <summary>
        /// Sprite VFX와의 인터페이스 호환을 위한 AnimationEvent 완료 처리입니다.
        /// </summary>
        /// <param name="struckAnimationEventComplete">애니메이션 완료 이벤트 데이터입니다.</param>
        public void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete)
        {
        }

    }
}
#endif
