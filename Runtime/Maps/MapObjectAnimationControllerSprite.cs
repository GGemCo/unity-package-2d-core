using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 유니티 애니메이션 처리
    /// </summary>
    public class MapObjectAnimationControllerSprite : Animation2dController, IMapObjectAnimationController
    {
        public string CurrentAnimationNameAttack { get; set; }
        private DefaultMapObject _defaultMapObject;
        private SpriteRenderer _spriteRenderer;

        protected override void Awake()
        {
            base.Awake();
            _defaultMapObject = GetComponent<DefaultMapObject>();
            if (_defaultMapObject == null)
            {
                GcLogger.LogError("DefaultMapObject is missing! This component will not function.");
                enabled = false; // 컴포넌트를 비활성화하여 다른 함수들이 실행되지 않도록 합니다.
                return;
            }

            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// wait 애니메이션 처리 
        /// </summary>
        public void PlayWaitAnimation()
        {
            if (!_defaultMapObject || _defaultMapObject.IsStatusDead()) return;
            string idleAnim = ICharacterAnimationController.WaitForwardAnim;
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(idleAnim)) return;
            PlayAnimation(idleAnim, true, _defaultMapObject.GetCurrentMoveSpeed());
        }

        /// <summary>
        /// run 애니메이션 처리
        /// </summary>
        public void PlayRunAnimation()
        {
            if (_defaultMapObject.IsStatusDead()) return;
            string moveAnim = _defaultMapObject.directionNormalize.y != 0
                ? (_defaultMapObject.directionNormalize.y > 0
                    ? ICharacterAnimationController.WalkBackwardAnim
                    : ICharacterAnimationController.WalkForwardAnim)
                : ICharacterAnimationController.WalkForwardAnim;

            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(moveAnim)) return;
            PlayAnimation(moveAnim, true, _defaultMapObject.GetCurrentMoveSpeed());
        }

        public void PlayDamageAnimation()
        {
            if (_defaultMapObject.IsStatusDead()) return;
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
        /// 공격 애니메이션 처리
        /// </summary>
        public void PlayAttackAnimation(string animName = "")
        {
            CurrentAnimationNameAttack = animName != "" ? animName : ICharacterAnimationController.AttackAnim;
            PlayAnimation(CurrentAnimationNameAttack, false, _defaultMapObject.GetCurrentAttackSpeed());
        }

        public void PlayAttackWaitAnimation()
        {
            if (_defaultMapObject.IsStatusDead()) return;
            string aniName = $"{CurrentAnimationNameAttack}{ICharacterAnimationController.SuffixWait}";
            PlayAnimation(aniName, true, _defaultMapObject.GetCurrentAttackSpeed());
        }

        public void PlayAttackEndAnimation()
        {
            if (_defaultMapObject.IsStatusDead()) return;
            string aniName = $"{CurrentAnimationNameAttack}{ICharacterAnimationController.SuffixEnd}";
            PlayAnimation(aniName, false, _defaultMapObject.GetCurrentAttackSpeed());
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
                _defaultMapObject.OnAnimationCompleteAttack();
            }
            else if (state.IsName($"{CurrentAnimationNameAttack}_end"))
            {
                _defaultMapObject.OnAnimationCompleteAttackEnd();
            }
            else if (state.IsName($"{ICharacterAnimationController.DeadAnim}"))
            {
                _defaultMapObject.OnAnimationCompleteDead();
            }
            else
            {
                _defaultMapObject.Stop();
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

            _defaultMapObject.SetIsStartFade(false);
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
            Animator.speed = value;
            Animator.Update(0); // 즉시 반영 (옵션)
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
    }
}