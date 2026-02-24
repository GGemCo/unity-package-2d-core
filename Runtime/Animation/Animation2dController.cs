using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Animator 기반 2D 캐릭터 컨트롤러
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class Animation2dController : MonoBehaviour
    {
        public IAnimationEventListener EventListener { get; set; }
        protected Animator Animator;
        private AnimationClip[] animationClips;

        private readonly Dictionary<string, SpriteRenderer> spriteRenderers = new();

        protected virtual void Awake()
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderers[sr.gameObject.name] = sr;
            }
            Animator = GetComponent<Animator>();
            if (!Animator || !Animator.runtimeAnimatorController)
            {
                Debug.LogError("Animator component 가 없습니다.");
                return;
            }
            animationClips = Animator.runtimeAnimatorController.animationClips;
        }

        protected void PlayAnimation(string animationName, bool loop = false, float timeScale = 1.0f,
            List<StruckAddAnimation> addAnimations = null, float startTime = 0, float endTime = 0)
        {
            if (!Animator) return;
            if (!GetClipByName(animationName))
            {
                GcLogger.LogError($"애니메이션 clip 이 없습니다. clip name: {animationName}");
                return;
            }
            
            // AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            // if (state.IsName(animationName))
            // {
            //     GcLogger.Log("PlayAnimation 같은 이름 플레이 중");
            // }
            // GcLogger.Log($"PlayAnimation {gameObject.name} | {animationName} | {loop} | {timeScale}");
            if (timeScale <= 0)
            {
                timeScale = 1.0f;
            }
            Animator.speed = timeScale;
            Animator.Play(animationName);
            // animator.Play(animationName, 0,0f);
            Animator.Update(0); // 즉시 반영 (옵션)

            // Loop 설정은 Animator Controller의 상태 설정에서 해야 합니다
            // addAnimations 처리
            if (addAnimations is { Count: > 0 })
            {
                StartCoroutine(PlayAddAnimations(addAnimations));
            }
        }
        private IEnumerator<WaitForSeconds> PlayAddAnimations(List<StruckAddAnimation> addAnimations)
        {
            foreach (var add in addAnimations)
            {
                if (!GetClipByName(add.AnimationName))
                {
                    GcLogger.LogWarning($"AddAnimation: 애니메이션 없음: {add.AnimationName}");
                    continue;
                }

                if (add.Delay > 0)
                    yield return new WaitForSeconds(add.Delay);

                float clipLength = GetAnimationDuration(add.AnimationName, false);

                Animator.speed = add.TimeScale > 0 ? add.TimeScale : 1.0f;
                Animator.Play(add.AnimationName, 0);
                Animator.Update(0); // 즉시 반영

                yield return new WaitForSeconds(clipLength / Animator.speed);
            }
        }

        protected void StopAnimation()
        {
            if (!Animator) return;
            Animator.speed = 0;
        }

        protected bool IsPlaying()
        {
            if (!Animator) return false;
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            return state.length > 0;
        }

        private float GetHeight()
        {
            Bounds bounds = GetComponent<Renderer>().bounds;
            return bounds.size.y;
        }

        private float GetWidth()
        {
            Bounds bounds = GetComponent<Renderer>().bounds;
            return bounds.size.x;
        }

        protected Vector2 GetSize()
        {
            return new Vector2(GetWidth(), GetHeight());
        }

        protected void ChangeImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
        }

        protected void RemoveImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
        }

        protected float GetAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            // Animator에서 직접 길이를 구하는 방법은 제한적임. AnimationClip 참조 필요
            foreach (var clip in animationClips)
            {
                if (clip.name == animationName)
                {
                    return isMilliseconds ? clip.length * 1000f : clip.length;
                }
            }

            Debug.LogWarning($"애니메이션 클립을 찾을 수 없습니다. AnimationName: {animationName}");
            return 0f;
        }

        protected void SetColor(Color color)
        {
            foreach (var sr in spriteRenderers.Values)
            {
                sr.color = color;
            }
        }

        protected void SetColor(string colorHex)
        {
            if (ColorUtility.TryParseHtmlString(colorHex, out var color))
            {
                SetColor(color);
            }
        }

        // 이벤트 콜백은 애니메이션 이벤트(Inspector에서 이벤트 등록)에서 호출해야 함
        public virtual void GGemCoAniEventComplete()
        {
        }
        public void GGemCoAniEventEffect(string json)
        {
            EventListener?.OnAnimationEventEffect(json, gameObject);
        }
        public void GGemCoAniEventAttack(string json)
        {
            EventListener?.OnAnimationEventAttack(json, gameObject);
        }
        public void GGemCoAniEventCameraShake(string json)
        {
            EventListener?.OnAnimationEventCameraShake(json);
        }

        public void GGemCoAniEventSound(string json)
        {
            EventListener?.OnAnimationEventSound(json);
        }
        public void GGemCoAniEventSkill(string json)
        {
            EventListener?.OnAnimationEventSkill(json, gameObject);
        }
        public void GGemCoAniEventUseTool(string json)
        {
            EventListener?.OnAnimationEventUseTool(json, gameObject);
        }
        public void GGemCoAniEventUseSeed(string json)
        {
            EventListener?.OnAnimationEventUseSeed(json, gameObject);
        }

        public void GGemCoAniEventGuardEnd()
        {
            EventListener?.OnAnimationEventGuardEnd(gameObject);
        }

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 시작.
        /// - AnimationEvent의 string 파라미터에 JSON을 전달하여 런타임 설정을 오버라이드할 수 있습니다.
        /// </summary>
        public void GGemCoAniEventStartBackstepTrail(string json)
        {
            EventListener?.OnAnimationEventStartBackstepTrail(json, gameObject);
        }

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 종료.
        /// </summary>
        public void GGemCoAniEventStopBackstepTrail(string json)
        {
            EventListener?.OnAnimationEventStopBackstepTrail(json, gameObject);
        }
#if GGEMCO_2D_CONTROL
        /// <summary>
        /// 점프 시작 애니메이션 마지막 프레임에서 호출
        /// </summary>
        public void GGemCoAniEventJumpUp()
        {
            EventListener?.OnAnimationEventJump(gameObject, AnimationConstants.EventNameJumpUp);
        }
        /// <summary>
        /// 점프 정점에서 떨어지는 애니메이션 마지막 프레임에서 호출
        /// </summary>
        public void GGemCoAniEventJumpFall()
        {
            EventListener?.OnAnimationEventJump(gameObject, AnimationConstants.EventNameJumpFall);
        }
        /// <summary>
        /// 점프 착지 후 end 애니메이션 마지막 프레임에서 호출
        /// </summary>
        public void GGemCoAniEventJumpEnd()
        {
            EventListener?.OnAnimationEventJump(gameObject, AnimationConstants.EventNameJumpEnd);
        }
        /// <summary>
        /// 대시 중 애니메이션 마지막 프레임에서 호출
        /// </summary>
        public void GGemCoAniEventDashPlay()
        {
            EventListener?.OnAnimationEventDash(gameObject, AnimationConstants.EventNameDashPlay);
        }
        /// <summary>
        /// 대시 종료 후 end 애니메이션 마지막 프레임에서 호출
        /// </summary>
        public void GGemCoAniEventDashEnd()
        {
            EventListener?.OnAnimationEventDash(gameObject, AnimationConstants.EventNameDashEnd);
        }
#endif
        protected AnimationClip GetClipByName(string animationName)
        {
            if (animationClips == null) return null;
            foreach (var clip in animationClips)
            {
                if (clip.name == animationName)
                {
                    return clip;
                }
            }

            return null;
        }

        private AnimationClip GetCurrentAnimationClip(int layerIndex)
        {
            return Animator.GetCurrentAnimatorClipInfo(layerIndex)[0].clip;
        }

        protected void SetAnimationLoop(bool shouldLoop, int layerIndex = 0)
        {
            AnimationClip currentClip = GetCurrentAnimationClip(layerIndex);
            if (currentClip == null) return;
            // 애니메이션 클립의 루프 설정 변경
            currentClip.wrapMode = shouldLoop ? WrapMode.Loop : WrapMode.Once;
        }
        /// <summary>
        /// 이벤트 시간.
        /// </summary>
        /// <param name="aniName"></param>
        /// <param name="eventName"></param>
        /// <param name="exceptEventName"></param>
        /// <returns>단위: 초</returns>
        private float GetEventTime(string aniName, string eventName, List<string> exceptEventName = null) 
        {
            if (!Animator) return -1;
            var findAnimation = GetClipByName(aniName);
            if(findAnimation == null) return -1;
            float eventTime = 0;
            
            if (findAnimation == null || string.IsNullOrEmpty(eventName))
                return -1f;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(findAnimation);
            foreach (var evt in events)
            {
                if (evt.functionName == eventName)
                    return evt.time;
            }
            return eventTime;
        }
        
        /// <summary>
        /// Loop start, End 이벤트가 있을때
        ///             loop_start        loop_end
        /// /---------------/---------------/---------------/
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="duration"></param>
        /// 
        protected void PlayAnimationWidthLoopEvent(string animationName, float duration)
        {
            float eventTimeLoopStart = GetEventTime(animationName, "loop_start");
            if(eventTimeLoopStart < 0) {
                GcLogger.LogWarning($"check loop_start event {animationName}");
                return;
            }
            float eventTimeLoopEnd = GetEventTime(animationName, "loop_end");
            if(eventTimeLoopEnd < 0) {
                GcLogger.LogWarning($"check loop_end event {animationName}");
                return;
            }
            float aniDurationTime = GetAnimationDuration(animationName, false);
            if(aniDurationTime == 0) {
                GcLogger.LogWarning($"check animation duration {animationName}");
                return;
            }
            //  startDuration    loopDuration     endDuration
            //---------------/---------------/---------------/

            float startDuration = eventTimeLoopEnd;
            float loopDuration = eventTimeLoopEnd - eventTimeLoopStart;
            float endDuration = aniDurationTime - eventTimeLoopEnd;

            // duration이 없는경우 그냥 한번 재생
            if(duration <= 0){
                PlayAnimation(animationName);
            }
            else if(startDuration + endDuration < duration){
                //loopAni
                var realLoopDuration = duration - startDuration - endDuration;
                var loopCnt = realLoopDuration/loopDuration;
                var loopCntCeil = Math.Ceiling(realLoopDuration/loopDuration);
                float newTimeScale = (float)loopCntCeil/loopCnt;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();
                
                for(var i = 0; i< loopCntCeil; i++){
                    // this.drawObject.state.addAnimation(0, animationName, false, 0, eventTimeLoopEnd, eventTimeLoopStart, timeScale);
                    StruckAddAnimation struckAddAnimation = new StruckAddAnimation(animationName, false, 0, newTimeScale, eventTimeLoopStart, eventTimeLoopEnd);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //endAni
                if (!Mathf.Approximately(aniDurationTime, eventTimeLoopStart))
                {
                    // this.drawObject.state.addAnimation(0, animationName, false, 0, eventTimeLoopStart, aniDurationTime, 1);
                    StruckAddAnimation struckAddAnimation = new StruckAddAnimation(animationName, false, 0, 1, eventTimeLoopEnd, aniDurationTime);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //startAni
                PlayAnimation(animationName, false, 1, newAddAnimations, 0, eventTimeLoopEnd);
            }
            // duration이 너무 작기때문에 전체 애니메이션을 스케일해서 한번 실행
            else{
                PlayAnimation(animationName, false, aniDurationTime/duration);
            }
        }

        public Dictionary<string, float> GetAnimationAllLength()
        {
            Dictionary<string, float> clipLength = new Dictionary<string, float>();
            if (!Animator || !Animator.runtimeAnimatorController) return clipLength;
            foreach (var clip in animationClips)
            {
                if (!clipLength.ContainsKey(clip.name))
                    clipLength.Add(clip.name, Mathf.Max(0f, clip.length));
            }

            return clipLength;
        }
    }
}