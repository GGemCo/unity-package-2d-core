#if GGEMCO_USE_SPINE
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using Event = Spine.Event;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 스파인 컨트롤러
    /// </summary>
    public class Spine2dUIController : MonoBehaviour
    {
        [HideInInspector] public SkeletonGraphic skeletonGraphic;
        private void Awake() {
            // Spine 오브젝트의 SkeletonGraphic 컴포넌트 가져오기
            skeletonGraphic = GetComponent<SkeletonGraphic>();
            if (skeletonGraphic != null)
            {
                skeletonGraphic.AnimationState.Event += HandleEvent;
            }
        }
        private void HandleEvent(TrackEntry trackEntry, Event e)
        {
            // Logger.Log("effect spine event: "+e.Data.Name);
            if (e.Data.Name == Spine2dConstants.EventNameAttack)
            {
                // GcLogger.Log("hit event " + this.gameObject.name + " | json: " + e.String);
                OnSpineEventHit(e);
            }
            else if (e.Data.Name == Spine2dConstants.EventNameSound)
            {
                OnSpineEventSound(e);
            }
            else if (e.Data.Name == Spine2dConstants.EventNameShake)
            {
                if (e.Float <= 0) return;
                SceneGame.Instance.cameraManager.StartShake(e.Float, 0.1f);
            }
        }
        private void OnSpineEventHit(Event eEvent) 
        {
        
        }
        private void OnSpineEventSound(Event eEvent) 
        {
            int soundUid = eEvent.Int;
            if (soundUid <= 0) return;
        }

        /// <summary>
        /// 애니메이션 재생
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="addAnimations"></param>
        public TrackEntry PlayAnimation(string animationName, bool loop = false, float timeScale = 1.0f, List<StruckAddAnimation> addAnimations = null)
        {
            if (skeletonGraphic == null) return null;
            //  GcLogger.Log("PlayAnimation gameobject: " + this.gameObject.name + " / animationName: " + animationName + " / " + loop);
            TrackEntry trackEntry = skeletonGraphic.AnimationState.SetAnimation(0, animationName, loop);
            trackEntry.TimeScale = timeScale;

            if (addAnimations != null)
            {
                foreach (StruckAddAnimation info in addAnimations)
                {
                    if (info == null) continue;
                    trackEntry =
                        skeletonGraphic.AnimationState.AddAnimation(0, info.AnimationName, info.Loop, info.Delay);
                    if (info.TimeScale > 0)
                    {
                        trackEntry.TimeScale = info.TimeScale;
                    }
                }
            }
            return trackEntry;
        }
        /// <summary>
        /// 현재 재생 중인 애니메이션 이름 가져오기
        /// </summary>
        /// <returns></returns>
        public string GetCurrentAnimation()
        {
            if (skeletonGraphic == null || skeletonGraphic.AnimationState == null) return null;
            TrackEntry currentEntry = skeletonGraphic.AnimationState.GetCurrent(0);
            return currentEntry?.Animation.Name;
        }
        /// <summary>
        /// 애니메이션이 끝나면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        private void OnAnimationComplete(TrackEntry entry)
        {
            if (skeletonGraphic == null) return;
            // 애니메이션 이벤트 리스너 제거
            skeletonGraphic.AnimationState.Complete -= OnAnimationComplete;
        }
        public void AddCompleteEvent() 
        {
            if (skeletonGraphic == null) return;
            skeletonGraphic.AnimationState.Complete += OnAnimationComplete;
        }
        public void RemoveCompleteEvent() 
        {
            if (skeletonGraphic == null) return;
            skeletonGraphic.AnimationState.Complete -= OnAnimationComplete;
        }
        public float GetAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            if (skeletonGraphic == null) return 0;
            var findAnimation = skeletonGraphic.Skeleton.Data.FindAnimation(animationName);

            if (findAnimation == null)
            {
                GcLogger.LogWarning($"애니메이션 클립을 찾을 수 없습니다. AnimationName: {animationName}");
                return 0;
            }

            float duration = findAnimation.Duration;
            return isMilliseconds ? duration * 1000 : duration;
        }
        public void SetTrackNoEnd(int trackId = 0)
        {
            if (skeletonGraphic == null) return;
            TrackEntry trackEntry = skeletonGraphic.AnimationState.GetCurrent(trackId);
            if(trackEntry == null) return;
            trackEntry.AnimationEnd = 999999f;
        }

        public void StopAnimation(int trackId = 0)
        {
            if (skeletonGraphic == null) return;
            skeletonGraphic.AnimationState.SetEmptyAnimation(trackId, 0);
            skeletonGraphic.AnimationState.ClearTrack(trackId);
        }
    }
}
#endif