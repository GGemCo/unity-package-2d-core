using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 애니메이션 관리
    /// </summary>
    public interface ICharacterAnimationController
    {
        public const string WalkForwardAnim = "run";
        public const string WalkBackwardAnim = "run";
        public const string WaitForwardAnim = "wait";
        public const string WaitBackwardAnim = "wait";
        public const string AttackAnim = "attack";
        public const string DeadAnim = "die";
        
        void PlayWaitAnimation();
        void PlayRunAnimation();
        void PlayAttackAnimation();
        void PlayDeadAnimation();
        void PlayCharacterAnimation(string animationName, bool loop = false, float timeScale = 1f);

        float GetCharacterHeight();

        /// <summary>
        /// 캐릭터 width 값 구하기
        /// </summary>
        /// <returns></returns>
        float GetCharacterWidth();

        Vector2 GetCharacterSize();

        void ChangeCharacterImageInSlot(List<StruckChangeSlotImage> changeSlotImages);
        void RemoveCharacterImageInSlot(List<StruckChangeSlotImage> changeSlotImages);
        IEnumerator FadeEffect(float duration, bool fadeIn);
        void UpdateTimeScaleByTrackIndex(float value, int index = 0);
        void SetCharacterColor(Color red);
        void UpdateTimeScaleMove(float value);
    }
}