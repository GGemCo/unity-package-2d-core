#if GGEMCO_USE_SPINE
using System;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;
using Event = Spine.Event;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스파인 컨트롤러
    /// </summary>
    public class Spine2dController : MonoBehaviour
    {
        public IAnimationEventListener EventListener { get; set; }
        protected SkeletonAnimation SkeletonAnimation;
        private Skeleton skeleton;
        private SkeletonData skeletonData;
        private Material sourceMaterial;
        private Skin customSkin;

        protected virtual void Awake() {
            // Spine 오브젝트의 SkeletonAnimation 컴포넌트 가져오기
            SkeletonAnimation = GetComponent<SkeletonAnimation>();
            if (SkeletonAnimation == null)
            {
                GcLogger.LogError("SkeletonAnimation is missing! This component will not function.");
                enabled = false; // 컴포넌트를 비활성화하여 다른 함수들이 실행되지 않도록 합니다.
                return;
            }
            sourceMaterial = GetComponent<MeshRenderer>().material;

            if (SkeletonAnimation == null)
            {
                GcLogger.LogError("SkeletonAnimation component 가 없습니다.");
                return;
            }
            skeleton = SkeletonAnimation.Skeleton;
            skeletonData = skeleton.Data;
            
            // 애니메이션 이벤트 리스너 등록
            SkeletonAnimation.AnimationState.Complete += OnAnimationComplete;
            SkeletonAnimation.AnimationState.Interrupt += OnAnimationInterrupt;
            SkeletonAnimation.AnimationState.Event += HandleEvent;
            
            customSkin = new Skin("customSkin");
        }

        private void OnDestroy()
        {
            if (SkeletonAnimation == null) return;
            SkeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
            SkeletonAnimation.AnimationState.End -= OnAnimationEnd;
            SkeletonAnimation.AnimationState.Interrupt -= OnAnimationInterrupt;
            SkeletonAnimation.AnimationState.Event -= HandleEvent;
        }

        private void HandleEvent(TrackEntry trackEntry, Event e)
        {
            // Logger.Log("effect spine event: "+e.Data.Name);
            switch (e.Data.Name)
            {
                case AnimationConstants.EventNameAttack:
                    EventListener?.OnAnimationEventAttack(e.String, gameObject);
                    break;
                case AnimationConstants.EventNameSound:
                    EventListener?.OnAnimationEventSound(e.String);
                    break;
                case AnimationConstants.EventNameCameraShake:
                    EventListener?.OnAnimationEventCameraShake(e.String);
                    break;
                case AnimationConstants.EventNameVfx:
                    EventListener?.OnAnimationEventVfx(e.String, gameObject);
                    break;
                case AnimationConstants.EventNamePlayerHit:
                    EventListener?.OnAnimationEventPlayerHit(gameObject);
                    break;
                case AnimationConstants.EventNameSkill:
                    EventListener?.OnAnimationEventSkill(e.String, gameObject);
                    break;
                case AnimationConstants.EventNameCrowdControl:
                    EventListener?.OnAnimationEventCrowdControl(e.String, gameObject);
                    break;
                case AnimationConstants.EventNameStartBackstepTrail:
                    EventListener?.OnAnimationEventStartBackstepTrail(e.String, gameObject);
                    break;
                case AnimationConstants.EventNameStopBackstepTrail:
                    EventListener?.OnAnimationEventStopBackstepTrail(e.String, gameObject);
                    break;
#if GGEMCO_2D_CONTROL
                case AnimationConstants.EventNameJumpUp:
                case AnimationConstants.EventNameJumpFall:
                case AnimationConstants.EventNameJumpEnd:
                    EventListener?.OnAnimationEventJump(gameObject, e.Data.Name);
                    break;
                case AnimationConstants.EventNameDashPlay:
                case AnimationConstants.EventNameDashEnd:
                    EventListener?.OnAnimationEventDash(gameObject, e.Data.Name);
                    break;
#endif
            }
        }
        protected Spine.Animation FindAnimation(string animationName, bool showLog = true)
        {
            if (SkeletonAnimation == null) return null;
            var findAnimation = SkeletonAnimation.Skeleton.Data.FindAnimation(animationName);
            if (findAnimation != null) return findAnimation;
            if (showLog)
            {
                GcLogger.LogWarning($"애니메이션 클립을 찾을 수 없습니다. AnimationName: {animationName}");
            }
            return null;
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
            if (!SkeletonAnimation) return -1;
            var findAnimation = SkeletonAnimation.Skeleton.Data.FindAnimation(aniName);
            if(findAnimation == null) return -1;
            ExposedList<Timeline> timelines = findAnimation.Timelines;
            float eventTime = 0;
            foreach (var timeline in timelines)
            {
                var eventTimeline = timeline as EventTimeline;
                if (eventTimeline == null) continue;
                for (int i = 0; i < eventTimeline.FrameCount; ++i)
                {
                    Event spineEvent = eventTimeline.Events[i];
                    if (spineEvent == null || spineEvent.Data.Name != eventName) continue;
                    eventTime = spineEvent.Time;
                }
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

        /// <summary>
        /// 애니메이션 재생
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="addAnimations"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        protected void PlayAnimation(string animationName, bool loop = false, float timeScale = 1.0f,
            List<StruckAddAnimation> addAnimations = null, float startTime = 0, float endTime = 0)
        {
            if (SkeletonAnimation == null) return;
            var findAnimation = FindAnimation(animationName);
            if (findAnimation == null) return;
            // GcLogger.Log("PlayAnimation GameObject: " + this.gameObject.name + " / animationName: " + animationName + " / " + loop);
            TrackEntry trackEntry = SkeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            if (trackEntry == null)
            {
                GcLogger.LogError($"Can't SetAnimation. name: {animationName}");
                return;
            }

            trackEntry.TimeScale = timeScale;
            if (startTime > 0)
            {
                trackEntry.AnimationStart = startTime;
            }

            if (endTime > 0)
            {
                trackEntry.AnimationEnd = endTime;
            }

            if (addAnimations == null) return;
            foreach (StruckAddAnimation info in addAnimations)
            {
                if (info == null) continue;
                findAnimation = FindAnimation(info.AnimationName);
                if (findAnimation == null) continue;

                TrackEntry entry =
                    SkeletonAnimation.AnimationState.AddAnimation(0, info.AnimationName, info.Loop, info.Delay);
                if (entry == null)
                {
                    GcLogger.LogError($"Can't AddAnimation. name: {info.AnimationName}");
                    continue;
                }

                if (info.TimeScale > 0)
                {
                    entry.TimeScale = info.TimeScale;
                }

                if (info.StartTime > 0)
                {
                    entry.AnimationStart = info.StartTime;
                }

                if (info.EndTime > 0)
                {
                    entry.AnimationEnd = info.EndTime;
                }
            }
        }

        /// <summary>
        /// 현재 재생 중인 애니메이션 이름 가져오기
        /// </summary>
        /// <returns></returns>
        protected string GetCurrentAnimation()
        {
            if (SkeletonAnimation == null || SkeletonAnimation.AnimationState == null) return null;
            TrackEntry currentEntry = SkeletonAnimation.AnimationState.GetCurrent(0);
            return currentEntry?.Animation.Name;
        }
        protected void SetTrackNoEnd(int trackId = 0)
        {
            if (SkeletonAnimation == null) return;
            TrackEntry trackEntry = SkeletonAnimation.AnimationState.GetCurrent(trackId);
            if(trackEntry == null) return;
            trackEntry.AnimationEnd = 999999f;
        }

        protected void StopAnimation(int trackId = 0)
        {
            if (SkeletonAnimation == null) return;
            SkeletonAnimation.AnimationState.SetEmptyAnimation(trackId, 0);
            SkeletonAnimation.AnimationState.ClearTrack(trackId);
        }
        protected bool IsPlaying()
        {
            if (SkeletonAnimation == null) return false;
            var state = SkeletonAnimation.AnimationState;
            // 각 트랙에서 현재 애니메이션이 있는지 확인
            for (int i = 0; i < state.Tracks.Count; i++)
            {
                if (state.Tracks.Items[i] != null && state.Tracks.Items[i].Animation != null)
                {
                    return true; // 재생 중인 애니메이션이 존재함
                }
            }
            return false; // 재생 중인 애니메이션이 없음
        }
        /// <summary>
        /// 캐릭터 height 값 구하기
        /// </summary>
        /// <returns></returns>
        private float GetHeight()
        {
            // Skeleton에서 바운딩 박스 계산
            float[] vertexBuffer = new float[8];
            SkeletonAnimation.Skeleton.GetBounds(out float x, out float y, out float width, out float height, ref vertexBuffer);
            return height;
        }
        /// <summary>
        /// 캐릭터 width 값 구하기
        /// </summary>
        /// <returns></returns>
        protected float GetWidth()
        {
            // Skeleton에서 바운딩 박스 계산
            float[] vertexBuffer = new float[8];
            SkeletonAnimation.Skeleton.GetBounds(out float x, out float y, out float width, out float height, ref vertexBuffer);
            return width;
        }
        protected Vector2 GetSize()
        {
            return new Vector2(GetWidth(), GetHeight());
        }
        /// <summary>
        /// slot 위치에 Attachment 이미지 바꾸기 
        /// </summary>
        /// <param name="slotName"></param>
        /// <param name="attachmentName"></param>
        /// <param name="sprite"></param>
        /// <param name="baseSkin"></param>
        /// <param name="targetSkin"></param>
        private void ChangeImageInSlot(string slotName, string attachmentName, Sprite sprite, Skin baseSkin, Skin targetSkin) 
        {
            var slotData = skeletonData.FindSlot(slotName);
            int slotIndex = slotData.Index;
            
            Attachment templateAttachment = baseSkin.GetAttachment(slotIndex, attachmentName);

            // Clone the template gun Attachment, and map the sprite onto it.
            // This sample uses the sprite and material set in the inspector.
            Attachment newAttachment = templateAttachment.GetRemappedClone(sprite, sourceMaterial); // This has some optional parameters. See below.

            // Add the gun to your new custom skin.
            if (newAttachment != null) targetSkin.SetAttachment(slotIndex, attachmentName, newAttachment);
        }
        /// <summary>
        /// 슬롯 이미지 바꾸기
        /// </summary>
        /// <param name="changeImages"></param>
        protected void ChangeImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
            string baseSkinName = "default";
            Skin baseSkin = skeletonData.FindSkin(baseSkinName);

            foreach (var info in changeImages)
            {
                string equipSkinName = info.SlotName;
                Skin equipSkin = skeletonData.FindSkin(equipSkinName) ?? new Skin(equipSkinName);
                ChangeImageInSlot(info.SlotName, info.AttachmentName, info.Sprite, baseSkin, equipSkin);
                // AddSkin 할때 Slot 별로 들어가기 때문에, 계속 쌓이지는 않는다.
                customSkin.AddSkin(equipSkin);
            }
            changeImages.Clear();
            skeleton.SetSkin(customSkin);
            skeleton.SetSlotsToSetupPose();
            SkeletonAnimation.Update(0);
        }
        protected void RemoveImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
            foreach (var info in changeImages)
            {
                string equipSkinName = info.SlotName;
                Skin equipSkin = skeletonData.FindSkin(equipSkinName);
                if (equipSkin == null) continue;
                var slotData = skeletonData.FindSlot(equipSkinName);
                int slotIndex = slotData.Index;
                equipSkin.RemoveAttachment(slotIndex, equipSkinName);
            }
            changeImages.Clear();
            skeleton.SetSkin(customSkin);
            skeleton.SetSlotsToSetupPose();
            SkeletonAnimation.Update(0);
        }
        /// <summary>
        /// 클립 재생 시간 구하기
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="isMilliseconds"></param>
        /// <returns></returns>
        protected float GetAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            var findAnimation = FindAnimation(animationName);
            if (findAnimation == null) return 0;

            float duration = findAnimation.Duration;
            return isMilliseconds ? duration * 1000 : duration;
        }
        /// <summary>
        /// 애니메이션이 끝나면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        protected virtual void OnAnimationComplete(TrackEntry entry)
        {
        }
        /// <summary>
        /// 애니메이션이 끝나면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        protected virtual void OnAnimationEnd(TrackEntry entry)
        {
        }
        /// <summary>
        /// 애니메이션이 중단되면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        protected virtual void OnAnimationInterrupt(TrackEntry entry)
        {
        }
        /// <summary>
        /// 색상 바꾸기
        /// </summary>
        /// <param name="color"></param>
        protected void SetColor(Color color)
        {
            SkeletonAnimation.Skeleton.SetColor(color);
        }
        /// <summary>
        /// hex code 로 색상 바꾸기
        /// </summary>
        /// <param name="colorHex"></param>
        protected void SetColor(string colorHex)
        {
            if (!SkeletonAnimation) return;
            SetColor(ColorHelper.HexToColor(colorHex, Color.white));
        }
        
        public Dictionary<string, float> GetAnimationAllLength()
        {
            Dictionary<string, float> animationDurations = new();

            if (skeletonData == null) return animationDurations;

            foreach (var ani in skeletonData.Animations)
            {
                if (!animationDurations.ContainsKey(ani.Name))
                {
                    animationDurations.Add(ani.Name, Mathf.Max(0f, ani.Duration));
                }
            }

            return animationDurations;
        }
    }
}
#endif
