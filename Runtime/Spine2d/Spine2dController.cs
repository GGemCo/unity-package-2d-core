#if GGEMCO_USE_SPINE
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
        protected SkeletonAnimation SkeletonAnimation;
        private Skeleton skeleton;
        private SkeletonData skeletonData;
        private Material sourceMaterial;
        private Skin customSkin;

        protected virtual void Awake() {
            // Spine 오브젝트의 SkeletonAnimation 컴포넌트 가져오기
            SkeletonAnimation = GetComponent<SkeletonAnimation>();
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
            if (e.Data.Name == Spine2dConstants.EventNameAttack)
            {
                // GcLogger.Log("hit event " + this.gameObject.name + " | json: " + e.String);
                OnSpineEventAttack(e);
            }
            else if (e.Data.Name == Spine2dConstants.EventNameSound)
            {
                OnSpineEventSound(e);
            }
            else if (e.Data.Name == Spine2dConstants.EventNameShake)
            {
                if (e.Float <= 0) return;
                OnSpineEventShake(e);
            }
        }

        protected virtual void OnSpineEventShake(Event eEvent) 
        {
        
        }

        protected virtual void OnSpineEventAttack(Event eEvent) 
        {
            
        }

        protected virtual void OnSpineEventSound(Event eEvent) 
        {
        }
        protected virtual void Start()
        {
        }
        /// <summary>
        /// 애니메이션 재생
        /// </summary>
        /// <param name="animationName"></param>
        /// <param name="loop"></param>
        /// <param name="timeScale"></param>
        /// <param name="addAnimations"></param>
        protected void PlayAnimation(string animationName, bool loop = false, float timeScale = 1.0f, List<StruckAddAnimation> addAnimations = null)
        {
            if (SkeletonAnimation == null) return;
            // GcLogger.Log("PlayAnimation gameobject: " + this.gameObject.name + " / animationName: " + animationName + " / " + loop);
            TrackEntry trackEntry = SkeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            trackEntry.TimeScale = timeScale;
            if (addAnimations == null) return;
            foreach (StruckAddAnimation info in addAnimations)
            {
                if (info == null) continue;
                TrackEntry entry = SkeletonAnimation.AnimationState.AddAnimation(0, info.AnimationName, info.Loop, info.Delay);
                if (info.TimeScale > 0)
                {
                    entry.TimeScale = info.TimeScale;
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
        protected float GetHeight()
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
                Skin equipSkin = skeletonData.FindSkin(equipSkinName);
                if (equipSkin == null)
                {
                    equipSkin = new Skin(equipSkinName);
                }
                ChangeImageInSlot(info.SlotName, info.AttachmentName, info.Sprite, baseSkin, equipSkin);
                customSkin.AddSkin(equipSkin);
            }
            skeleton.SetSkin(customSkin);
            skeleton.SetSlotsToSetupPose();
            SkeletonAnimation.Update(0);
        }
        protected void RemoveImageInSlot(List<StruckChangeSlotImage> changeImages)
        {
            string baseSkinName = "default";
            Skin baseSkin = skeletonData.FindSkin(baseSkinName);

            foreach (var info in changeImages)
            {
                string equipSkinName = info.SlotName;
                Skin equipSkin = skeletonData.FindSkin(equipSkinName);
                if (equipSkin == null) continue;
                var slotData = skeletonData.FindSlot(equipSkinName);
                int slotIndex = slotData.Index;
                equipSkin.RemoveAttachment(slotIndex, equipSkinName);
            }
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
            var findAnimation = SkeletonAnimation.Skeleton.Data.FindAnimation(animationName);

            if (findAnimation == null)
            {
                GcLogger.LogWarning($"애니메이션 클립을 찾을 수 없습니다. AnimationName: {animationName}");
                return 0;
            }

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
            SetColor(ColorHelper.HexToColor(colorHex));
        }
    }
}
#endif