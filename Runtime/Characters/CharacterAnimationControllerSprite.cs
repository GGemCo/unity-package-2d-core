using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 유니티 애니메이션 처리
    /// </summary>
    public class CharacterAnimationControllerSprite : Animator2dController, ICharacterAnimationController
    {
        private CharacterBase characterBase;
        private string currentAnimationNameAttack;
        private SpriteRenderer spriteRenderer;
        
        protected override void Awake()
        {
            base.Awake();
            characterBase = GetComponent<CharacterBase>();
            if (characterBase == null)
            {
                GcLogger.LogError("CharacterBase is missing! This component will not function.");
                enabled = false; // 컴포넌트를 비활성화하여 다른 함수들이 실행되지 않도록 합니다.
                return;
            }
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        protected override void Start()
        {
            base.Start();
            // 스파인 height 값 구하고 character 에 넘겨주기
            characterBase.SetHeight(GetHeight());
        }
        /// <summary>
        /// wait 애니메이션 처리 
        /// </summary>
        public void PlayWaitAnimation()
        {
            if (!characterBase || characterBase.IsStatusDead()) return;
            string idleAnim = characterBase.directionPrev.y != 0 
                ? (characterBase.directionPrev.y > 0 ? ICharacterAnimationController.WaitBackwardAnim : ICharacterAnimationController.WaitForwardAnim) 
                : ICharacterAnimationController.WaitForwardAnim;
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(idleAnim)) return;
            PlayAnimation(idleAnim,true, characterBase.GetCurrentMoveSpeed());
        }
        /// <summary>
        /// run 애니메이션 처리
        /// </summary>
        public void PlayRunAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            string moveAnim = characterBase.direction.y != 0 
                ? (characterBase.direction.y > 0 ? ICharacterAnimationController.WalkBackwardAnim : ICharacterAnimationController.WalkForwardAnim) 
                : ICharacterAnimationController.WalkForwardAnim;
            
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(moveAnim)) return;
            PlayAnimation(moveAnim, true, characterBase.GetCurrentMoveSpeed());
        }
        public void PlayDamageAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            PlayAnimation(ICharacterAnimationController.DamageAnim);
        }
        /// <summary>
        /// 스파인의 height 값을 구해서 가져오기
        /// </summary>
        /// <returns></returns>
        public float GetCharacterHeight()
        {
            return 0f;
        }
        /// <summary>
        /// 스파인의 width 값을 구해서 가져오기
        /// </summary>
        /// <returns></returns>
        public float GetCharacterWidth()
        {
            return 0f;
        }
        /// <summary>
        /// 스파인의 width, height 값을 구해서 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCharacterSize()
        {
            return Vector2.zero;
        }
        /// <summary>
        /// 특정 슬롯에 이미지를 변경하기
        /// </summary>
        /// <param name="partIndex"></param>
        /// <param name="itemUid"></param>
        public void ChangeCharacterImageInSlot(int partIndex, int itemUid = 0)
        {
            List<string> slotNames = new List<string>();
            string folderName = "";
            string imagePath = "";
            if (itemUid > 0)
            {
                StruckTableItem info = TableLoaderManager.Instance.TableItem.GetDataByUid(itemUid);
                if (info == null) return;

                ItemConstants.PartsType partsType = (ItemConstants.PartsType)partIndex;
                slotNames = ItemConstants.SlotNameByPartsType[partsType];
                folderName = ItemConstants.FolderNameByPartsType[partsType];
                imagePath = info.ImagePath;
            }

            List<StruckChangeSlotImage> changeImages = new List<StruckChangeSlotImage>();
            foreach (var slotName in slotNames)
            {
                string attachmentName = ItemConstants.AttachmentNameBySlotName[slotName];
                
                string changeSpritePath = $"Images/Parts/{folderName}/{attachmentName}";
                if (imagePath != "")
                {
                    changeSpritePath = $"Images/Parts/{folderName}/{imagePath}_{slotName}";
                }

                var sprite = Resources.Load<Sprite>(changeSpritePath);

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
            
        }
        /// <summary>
        /// 공격 애니메이션 처리
        /// </summary>
        public void PlayAttackAnimation(string animName = "")
        {
            currentAnimationNameAttack = animName != "" ? animName : ICharacterAnimationController.AttackAnim;
            PlayAnimation(currentAnimationNameAttack, false, characterBase.GetCurrentAttackSpeed());
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
        public override void OnAnimationComplete()
        {
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            // GcLogger.Log($"OnAnimationComplete: {animator.GetCurrentAnimatorClipInfo(0)?.}");
            // GcLogger.Log("OnAnimationInterrupt gameobject: " + this.gameObject.name + " / animationName: " + entry.Animation.Name);
            if (Animator == null) return;
            if (state.IsName(currentAnimationNameAttack))
            {
                if (characterBase.IsStatusDead()) return;
                characterBase.SetStatusIdle(); // 공격 상태 해제
                PlayWaitAnimation();
            }
            else
            {
                characterBase?.Stop();
            }
        }

        public override void OnAnimationEventCameraShake(float intensity) 
        {
        
        }
        /// <summary>
        /// 공격 모션에서 몬스터에 직접적인 공격이 가해지는 타이밍에 발생하는 이벤트
        /// </summary>
        public override void OnAnimationEventAttack()
        {
            characterBase.OnEventAttack();
        }

        public override void OnAnimationEventSound(string soundName) 
        {
        }
        public IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            float elapsedTime = 0f;
            float startAlpha = fadeIn ? 0 : 1;
            float endAlpha = fadeIn ? 1 : 0;

            Color color = spriteRenderer.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                spriteRenderer.color = color;
                yield return null;
            }

            characterBase.SetIsStartFade(false);
        }
        /// <summary>
        /// track index 의 time scale 변경해주기
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index"></param>
        public void UpdateTimeScaleByTrackIndex(float value, int index = 0)
        {
            
        }
        /// <summary>
        /// walk, run 애니메이션 time scale 변경하기
        /// </summary>
        /// <param name="value"></param>
        public void UpdateTimeScaleMove(float value)
        {
            
        }
        /// <summary>
        /// 색상 변경 하기
        /// </summary>
        /// <param name="color"></param>
        public void SetCharacterColor(Color color)
        {
            SetColor(color);
        }
        
        public void PlayCharacterAnimation(string animationName, bool loop = false, float timeScale = 1)
        {
            PlayAnimation(animationName, loop, timeScale);
        }

        public void PlayAttackEndAnimation()
        {
            if (characterBase.IsStatusDead()) return;
            string aniName = $"{currentAnimationNameAttack}_end";
            PlayAnimation(aniName, false, characterBase.GetCurrentAttackSpeed());
        }

        public void SetCharacterFillColor(Color color)
        {
            SetColor(color);
        }

        public AnimationClip GetCurrentClip(string animName)
        {
            return Animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        }

        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            return Animator.GetCurrentAnimatorStateInfo(layerIndex);
        }

        public float GetCharacterAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            return GetAnimationDuration(animationName, isMilliseconds);
        }
        
        public override void OnAnimationEventPlayEffect(int effectUid)
        {
            // GcLogger.Log($"OnAnimationEventPlayEffect effectUid: {effectUid}");
            var effect = EffectManager.CreateEffect(effectUid);
            if (effect == null) return;
            // 캐릭터 하위에 붙이기
            effect.transform.SetParent(characterBase.transform);
            effect.transform.localPosition = Vector3.zero;
        }
        public override void OnAnimationEventProjectile(int projectileUid)
        {
            characterBase?.LaunchProjectile(projectileUid);
        }

    }
}