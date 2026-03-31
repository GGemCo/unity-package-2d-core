using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 유니티 애니메이션 처리
    /// </summary>
    public class CharacterAnimationControllerSprite : Animation2dController, ICharacterAnimationController
    {
        public string CurrentAnimationNameAttack { get; set; }
        private CharacterBase _characterBase;
        private SpriteRenderer _spriteRenderer;
        private ConfigCommon.FacingDirectionType _facingDirection;

        protected override void Awake()
        {
            base.Awake();
            _characterBase = GetComponent<CharacterBase>();
            if (_characterBase == null)
            {
                GcLogger.LogError("CharacterBase is missing! This component will not function.");
                enabled = false; // 컴포넌트를 비활성화하여 다른 함수들이 실행되지 않도록 합니다.
                return;
            }

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _facingDirection = ConfigCommon.FacingDirectionType.TwoWay;
            if (AddressableLoaderSettings.Instance && AddressableLoaderSettings.Instance.settings)
            {
                _facingDirection = AddressableLoaderSettings.Instance.settings.facingDirectionType;
            }
        }

        /// <summary>
        /// wait 애니메이션 처리 
        /// </summary>
        public void PlayWaitAnimation()
        {
            if (!_characterBase || _characterBase.IsStatusDead()) return;
            
            // todo 정리 필요
            string idleAnim = ICharacterAnimationController.WaitForwardAnim;
            if (_facingDirection == ConfigCommon.FacingDirectionType.FourWay ||
                _facingDirection == ConfigCommon.FacingDirectionType.EightWay)
            {
                if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Up)
                {
                    idleAnim = ICharacterAnimationController.WaitUpAnim;
                }
                else if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Down)
                {
                    idleAnim = ICharacterAnimationController.WaitDownAnim;
                }
            }

            if (_characterBase.IsEquipSeed())
            {
                idleAnim = ICharacterAnimationController.WaitPickUpAnim;

                if (_facingDirection == ConfigCommon.FacingDirectionType.FourWay ||
                    _facingDirection == ConfigCommon.FacingDirectionType.EightWay)
                {
                    if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Up)
                    {
                        idleAnim = ICharacterAnimationController.WaitPickUpUpAnim;
                    }
                    else if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Down)
                    {
                        idleAnim = ICharacterAnimationController.WaitPickUpDownAnim;
                    }
                }
            }
            
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            
            if (state.IsName(idleAnim)) return;
            
            PlayAnimation(idleAnim, true, _characterBase.GetCurrentMoveSpeed());
        }

        /// <summary>
        /// run 애니메이션 처리
        /// </summary>
        public void PlayRunAnimation()
        {
            if (_characterBase.IsStatusDead()) return;
            
            
            string moveAnim = ICharacterAnimationController.WalkForwardAnim;

            if (_facingDirection == ConfigCommon.FacingDirectionType.FourWay || _facingDirection == ConfigCommon.FacingDirectionType.EightWay)
            {
                if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Up)
                {
                    moveAnim = ICharacterAnimationController.WalkUpAnim;
                }
                else if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Down)
                {
                    moveAnim = ICharacterAnimationController.WalkDownAnim;
                }
            }

            if (_facingDirection == ConfigCommon.FacingDirectionType.EightWay)
            {
                if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.UpLeft ||
                    _characterBase.CurrentFacing == CharacterConstants.FacingDirection8.UpRight)
                {
                    moveAnim = ICharacterAnimationController.WalkBackwardAnim;
                }
                else if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.DownLeft ||
                         _characterBase.CurrentFacing == CharacterConstants.FacingDirection8.DownRight)
                {
                    moveAnim = ICharacterAnimationController.WalkForwardAnim;
                }
            }


            if (_characterBase.IsEquipSeed())
            {
                moveAnim = ICharacterAnimationController.WalkPickUpAnim;
                if (_facingDirection == ConfigCommon.FacingDirectionType.FourWay ||
                    _facingDirection == ConfigCommon.FacingDirectionType.EightWay)
                {
                    if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Up)
                    {
                        moveAnim = ICharacterAnimationController.WalkPickUpUpAnim;
                    }
                    else if (_characterBase.CurrentFacing == CharacterConstants.FacingDirection8.Down)
                    {
                        moveAnim = ICharacterAnimationController.WalkPickUpDownAnim;
                    }
                }
            }

            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(moveAnim)) return;
            PlayAnimation(moveAnim, true, _characterBase.GetCurrentMoveSpeed());
        }

        public void PlayDamageAnimation()
        {
            if (_characterBase.IsStatusDead()) return;
            PlayAnimation(ICharacterAnimationController.DamageAnim);
        }

        public void PlayAnimationGroggy()
        {
            if (_characterBase.IsStatusDead()) return;
            PlayAnimation(ICharacterAnimationController.AnimGroggy, true);
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
                StruckTableItem info = TableLoaderManager.Instance.GetItemData(itemUid);
                if (info == null) return;

                ItemConstants.PartsType partsType = (ItemConstants.PartsType)partIndex;
                slotNames = ItemConstants.SlotNameByPartsType.GetValueOrDefault(partsType);
                folderName = ItemConstants.FolderNameByPartsType.GetValueOrDefault(partsType);
                imagePath = info.ImagePath;
            }

            if (slotNames == null || folderName == null) return;

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

                StruckChangeSlotImage struckChangeSlotImage =
                    new StruckChangeSlotImage(slotName, attachmentName, sprite);
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
            CurrentAnimationNameAttack = animName != "" ? animName : ICharacterAnimationController.AttackAnim;
            PlayAnimation(CurrentAnimationNameAttack, false, _characterBase.GetCurrentAttackSpeed());
        }

        public bool PlayAttackWaitAnimation()
        {
            if (_characterBase.IsStatusDead()) return false;
            string aniName = $"{CurrentAnimationNameAttack}{ICharacterAnimationController.SuffixWait}";
            if (!HasAnimation(aniName)) return false;
            PlayAnimation(aniName, true, _characterBase.GetCurrentAttackSpeed());
            return true;
        }

        public void PlayAttackEndAnimation()
        {
            if (_characterBase.IsStatusDead()) return;
            string aniName = $"{CurrentAnimationNameAttack}{ICharacterAnimationController.SuffixEnd}";
            PlayAnimation(aniName, false, _characterBase.GetCurrentAttackSpeed());
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
        public override void GGemCoAniEventComplete()
        {
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            // GcLogger.Log($"OnAnimationComplete: {animator.GetCurrentAnimatorClipInfo(0)?.}");
            // GcLogger.Log("OnAnimationInterrupt gameobject: " + this.gameObject.name + " / animationName: " + entry.Animation.Name);
            if (Animator == null) return;
            if (state.IsName(CurrentAnimationNameAttack))
            {
                _characterBase.OnAnimationCompleteAttack();
            }
            else if (state.IsName($"{CurrentAnimationNameAttack}_end"))
            {
                _characterBase.OnAnimationCompleteAttackEnd();
            }
            else if (state.IsName($"{ICharacterAnimationController.DeadAnim}"))
            {
                _characterBase.OnAnimationCompleteDead();
            }
            else
            {
                _characterBase.Stop();
            }
        }

        public IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            float elapsedTime = 0f;
            float startAlpha = fadeIn ? 0 : 1;
            float endAlpha = fadeIn ? 1 : 0;

            Color color = _spriteRenderer.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                _spriteRenderer.color = color;
                yield return null;
            }

            _characterBase.SetIsStartFade(false);
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
            SetPlaybackTimeScale(value);
        }

        public void SetPlaybackTimeScale(float value)
        {
            if (Animator == null)
            {
                return;
            }

            Animator.speed = Mathf.Max(0f, value);
            Animator.Update(0f);
        }

        public float GetPlaybackTimeScale()
        {
            return Animator != null ? Animator.speed : 1f;
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

        public void SetCharacterFillColor(Color color)
        {
            SetColor(color);
        }

        private AnimationClip GetCurrentClip(string animName)
        {
            return Animator.GetCurrentAnimatorClipInfo(0).Length <= 0 ? null : Animator.GetCurrentAnimatorClipInfo(0)[0].clip;
        }

        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            return Animator.GetCurrentAnimatorStateInfo(layerIndex);
        }

        public float GetCharacterAnimationDuration(string animationName, bool isMilliseconds = true)
        {
            return GetAnimationDuration(animationName, isMilliseconds);
        }
        public bool HasAnimation(string animationName)
        {
            return GetClipByName(animationName) != null;
        }

        /// <summary>
        /// 스킬 애니메이션을 재생합니다.
        /// </summary>
        public void PlaySkillAnimation(in SkillAnimationRequest request)
        {
            if (_characterBase != null && _characterBase.IsStatusDead()) return;

            string animationName = !string.IsNullOrEmpty(request.OverrideAnimationName)
                ? request.OverrideAnimationName
                : SkillAnimationNaming.GetName(request.SkillUid, request.Phase);

            if (string.IsNullOrEmpty(animationName)) return;
            if (!HasAnimation(animationName)) return;

            PlayAnimation(animationName, request.Loop, request.TimeScale);
        }

        /// <summary>
        /// 스킬 애니메이션 재생을 중단합니다.
        /// </summary>
        public void StopSkillAnimation()
        {
            if (Animator == null) return;
            if (_characterBase != null && _characterBase.IsStatusDead()) return;

            // 기본 정책: 대기 애니메이션으로 복귀
            PlayWaitAnimation();
        }

        public void FreezeCurrentAnimationAtLastFrame()
        {
            if (Animator == null)
                return;

            var clips = Animator.GetCurrentAnimatorClipInfo(0);
            if (clips == null || clips.Length == 0 || clips[0].clip == null)
                return;

            string clipName = clips[0].clip.name;
            Animator.speed = 1f;
            Animator.Play(clipName, 0, 0.999f);
            Animator.Update(0f);
            Animator.speed = 0f;
        }
    }
}