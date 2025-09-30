#if GGEMCO_USE_SPINE
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 스파인 애니메이션 처리
    /// </summary>
    public class MapObjectAnimationControllerSpine : Spine2dController, ICharacterAnimationController
    {
        public string CurrentAnimationNameAttack { get; set; }
        private CharacterBase characterBase;
        private readonly List<StruckChangeSlotImage> changeImages = new List<StruckChangeSlotImage>();

        protected override void Awake()
        {
            base.Awake();
            changeImages.Clear();
            characterBase = GetComponent<CharacterBase>();
            if (characterBase == null)
            {
                GcLogger.LogError("CharacterBase is missing! This component will not function.");
                enabled = false; // 컴포넌트를 비활성화하여 다른 함수들이 실행되지 않도록 합니다.
                return;
            }
            // 초기에는 빈값으로 넣어줘야 PlayAnimation 함수가 호출된다.
            SkeletonAnimation.AnimationState.SetEmptyAnimation(0, 0);
        }

        /// <summary>
        /// wait 애니메이션 처리 
        /// </summary>
        public void PlayWaitAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            string idleAnim = ICharacterAnimationController.WaitForwardAnim;
            if (GetCurrentAnimation() == idleAnim) return;
            PlayAnimation(idleAnim,true, characterBase.GetCurrentMoveSpeed());
        }
        /// <summary>
        /// run 애니메이션 처리
        /// </summary>
        public void PlayRunAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            string moveAnim = characterBase.directionNormalize.y != 0 
                ? (characterBase.directionNormalize.y > 0 ? ICharacterAnimationController.WalkBackwardAnim : ICharacterAnimationController.WalkForwardAnim) 
                : ICharacterAnimationController.WalkForwardAnim;
            if (GetCurrentAnimation() == moveAnim) return;
            PlayAnimation(moveAnim, true, characterBase.GetCurrentMoveSpeed());
        }
        public void PlayDamageAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            // damage 애니메이션이 없을 경우, Status 처리를 위해 바로 Stop 처리 
            if (FindAnimation(ICharacterAnimationController.DamageAnim) == null)
            {
                characterBase.Stop();
                return;
            }
            PlayAnimation(ICharacterAnimationController.DamageAnim);
        }
        /// <summary>
        /// 스파인의 width 값을 구해서 가져오기
        /// </summary>
        /// <returns></returns>
        public float GetCharacterWidth()
        {
            return GetWidth();
        }
        /// <summary>
        /// 스파인의 width, height 값을 구해서 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCharacterSize()
        {
            return GetSize();
        }
        /// <summary>
        /// 특정 슬롯에 이미지를 변경하기
        /// </summary>
        /// <param name="partIndex"></param>
        /// <param name="itemUid"></param>
        public void ChangeCharacterImageInSlot(int partIndex, int itemUid = 0)
        {
            ItemConstants.PartsType partsType = (ItemConstants.PartsType)partIndex;
            List<string> slotNames = ItemConstants.SlotNameByPartsType[partsType];
            string imagePath = "";
            if (itemUid > 0)
            {
                StruckTableItem info = TableLoaderManager.Instance.GetItemData(itemUid);
                if (info == null) return;
                imagePath = info.FileName;
            }

            changeImages.Clear();
            foreach (var slotName in slotNames)
            {
                string attachmentName = ItemConstants.AttachmentNameBySlotName[slotName];
                
                string changeSpritePath = attachmentName;
                if (imagePath != "")
                {
                    changeSpritePath = $"{imagePath}_{slotName}";
                }

                var sprite = AddressableLoaderItem.Instance.GetImageEquipByName(changeSpritePath);

                StruckChangeSlotImage struckChangeSlotImage = new StruckChangeSlotImage(slotName, attachmentName, sprite);
                changeImages.Add(struckChangeSlotImage);
            }

            ChangeImageInSlot(changeImages);
        }

        /// <summary>
        /// 특정 슬롯에 이미지를 지우기
        /// </summary>
        /// <param name="changeSlotImages"></param>
        public void RemoveCharacterImageInSlot(List<StruckChangeSlotImage> changeSlotImages)
        {
            RemoveImageInSlot(changeSlotImages);
        }
        /// <summary>
        /// 공격 애니메이션 처리
        /// </summary>
        public void PlayAttackAnimation(string animName = "")
        {
            CurrentAnimationNameAttack = animName != "" ? animName : ICharacterAnimationController.AttackAnim;
            PlayAnimation(CurrentAnimationNameAttack, false, characterBase.GetCurrentAttackSpeed());
        }
        public void PlayAttackWaitAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            string aniName = $"{CurrentAnimationNameAttack}{ICharacterAnimationController.SuffixWait}";
            PlayAnimation(aniName, true, characterBase.GetCurrentAttackSpeed());
        }

        public void PlayAttackEndAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            string aniName = $"{CurrentAnimationNameAttack}{ICharacterAnimationController.SuffixEnd}";
            PlayAnimation(aniName, false, characterBase.GetCurrentAttackSpeed());
        }
        /// <summary>
        /// 죽음 애니메이션 처리
        /// </summary>
        public void PlayDeadAnimation()
        {
            PlayAnimation(ICharacterAnimationController.DeadAnim);
        }
        /// <summary>
        /// 애니메이션이 중단되면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        protected override void OnAnimationComplete(TrackEntry entry)
        {
            // GcLogger.Log("OnAnimationInterrupt gameobject: " + this.gameObject.name + " / animationName: " + entry.Animation.Name);
            if (SkeletonAnimation == null) return;
            if (entry.Loop != true) 
            {
                if (characterBase.IsStatusDead()) return;
                characterBase.SetStatusIdle(); // 공격 상태 해제
                PlayWaitAnimation();
            }
        }
        // /// <summary>
        // /// 공격 모션에서 몬스터에 직접적인 공격이 가해지는 타이밍에 발생하는 이벤트
        // /// </summary>
        // /// <param name="eEvent"></param>
        // protected override void OnSpineEventAttack(Event eEvent)
        // {
        //     characterBase.OnEventAttack();
        // }
        //
        // protected override void OnSpineEventSound(Event eEvent) 
        // {
        // }
        // protected override void OnSpineEventProjectile(Event eEvent) 
        // {
        //     int projectileUid = eEvent.Int;
        //     characterBase.LaunchProjectile(projectileUid);
        // }

        public IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            float elapsedTime = 0f;
            float startAlpha = fadeIn ? 0 : 1;
            float endAlpha = fadeIn ? 1 : 0;

            Color color = SkeletonAnimation.Skeleton.GetColor();

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                SkeletonAnimation.Skeleton.SetColor(color);
                yield return null;
            }

            characterBase.SetIsStartFade(false);
        }
        /// <summary>
        /// 애니메이션 time scale 변경하기
        /// </summary>
        /// <param name="value"></param>
        public void UpdateTimeScaleMove(float value)
        {
            TrackEntry trackEntry = SkeletonAnimation.AnimationState.GetCurrent(0);
            if (trackEntry == null) return;
            trackEntry.TimeScale = value;
        }
        /// <summary>
        /// 색상 변경 하기
        /// </summary>
        /// <param name="color"></param>
        public void SetCharacterColor(Color color)
        {
            SetColor(color);
        }

        public void PlayCharacterAnimation(string animationName, bool loop = false, float timeScale = 1f)
        {
            PlayAnimation(animationName, loop, timeScale);
        }
        public float GetCharacterAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            return GetAnimationDuration(animationName, isMilliseconds);
        }

        public bool HasAnimation(string animationName)
        {
            return FindAnimation(animationName) != null;
        }

    }
}
#endif