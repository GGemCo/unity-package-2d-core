using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class StruckChangeSlotImage
    {
        public readonly string SlotName;
        public readonly string AttachmentName;
        public readonly Sprite Sprite;

        public StruckChangeSlotImage(string slotName, string attachmentName, Sprite sprite)
        {
            SlotName = slotName;
            AttachmentName = attachmentName;
            Sprite = sprite;
        }
    }
    public class StruckAddAnimation
    {
        public readonly string AnimationName;
        public readonly bool Loop;
        public readonly float Delay;
        public readonly float TimeScale;
        public readonly float StartTime;
        public readonly float EndTime;

        public StruckAddAnimation(string animationName, bool loop = false, float delay = 0, float timeScale = 1, float startTime = 0, float endTime = 0)
        {
            AnimationName = animationName;
            Loop = loop;
            Delay = delay;
            TimeScale = timeScale;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
    /// <summary>
    /// 애니메이션 관리
    /// </summary>
    public interface ICharacterAnimationController
    {
        public const string WalkForwardAnim = "run";
        public const string WalkBackwardAnim = "run";
        public const string WalkUpAnim = "run_up";
        public const string WalkDownAnim = "run_down";
        
        public const string WalkPickUpAnim = "run_pickup";
        public const string WalkPickUpUpAnim = "run_pickup_up";
        public const string WalkPickUpDownAnim = "run_pickup_down";
        
        public const string WaitForwardAnim = "wait";
        public const string WaitBackwardAnim = "wait";
        public const string WaitUpAnim = "wait_up";
        public const string WaitDownAnim = "wait_down";
        
        public const string WaitPickUpAnim = "wait_pickup";
        public const string WaitPickUpUpAnim = "wait_pickup_up";
        public const string WaitPickUpDownAnim = "wait_pickup_down";
        
        public const string AttackAnim = "attack";
        
        public const string HoeAnim = "hoe";
        public const string HoeDownAnim = "hoe_down";
        public const string HoeUpAnim = "hoe_up";
        
        public const string AxeAnim = "axe";
        public const string AxeDownAnim = "axe_down";
        public const string AxeUpAnim = "axe_up";
        
        public const string WateringAnim = "watering";
        public const string WateringDownAnim = "watering_down";
        public const string WateringUpAnim = "watering_up";
        
        public const string PickUpAnim = "pickup";
        public const string PickUpDownAnim = "pickup_down";
        public const string PickUpUpAnim = "pickup_up";
        
        public const string PickAxeAnim = "pickaxe";
        public const string PickAxeDownAnim = "pickaxe_down";
        public const string PickAxeUpAnim = "pickaxe_up";
        
        public const string SickleAnim = "sickle";
        public const string SickleDownAnim = "sickle_down";
        public const string SickleUpAnim = "sickle_up";
        
        public const string SeedAnim = "seed";
        public const string SeedDownAnim = "seed_down";
        public const string SeedUpAnim = "seed_up";
        
        public const string DeadAnim = "die";
        public const string DamageAnim = "damage";
        public const string AnimGroggy = "groggy";
        
        public const string SuffixWait = "_wait";
        public const string SuffixEnd = "_end";
        public string CurrentAnimationNameAttack { get; set; }

        void PlayWaitAnimation();
        void PlayRunAnimation();
        void PlayAttackAnimation(string animName = "");
        void PlayDeadAnimation();
        void PlayDamageAnimation();
        void PlayAnimationGroggy();
        void PlayCharacterAnimation(string animationName, bool loop = false, float timeScale = 1f, bool forceReset = false);
        void PlayAttackEndAnimation();
        bool PlayAttackWaitAnimation();
        
        void ChangeCharacterImageInSlot(int partIndex, int itemUid = 0);
        void RemoveCharacterImageInSlot(List<StruckChangeSlotImage> changeSlotImages);
        IEnumerator FadeEffect(float duration, bool fadeIn);
        void SetCharacterColor(Color red);
        void UpdateTimeScaleMove(float value);
        void SetPlaybackTimeScale(float value);
        float GetPlaybackTimeScale();
        float GetCharacterAnimationDuration(string animationName, bool isMilliseconds = true);
        bool HasAnimation(string stateName);
        Dictionary<string, float> GetAnimationAllLength();

        // ----------------------
        // Skill Animation (Core)
        // ----------------------
        /// <summary>
        /// 스킬 애니메이션을 재생한다.
        /// </summary>
        void PlaySkillAnimation(in SkillAnimationRequest request);

        /// <summary>
        /// 스킬 애니메이션 재생을 중단한다.
        /// (구현체 정책에 따라 빈 애니메이션/기본 대기 애니메이션으로 전환할 수 있다)
        /// </summary>
        void StopSkillAnimation();

        /// <summary>
        /// 현재 재생 중인 애니메이션의 마지막 프레임에 즉시 고정합니다.
        /// </summary>
        void FreezeCurrentAnimationAtLastFrame();

        void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete);
    }
}