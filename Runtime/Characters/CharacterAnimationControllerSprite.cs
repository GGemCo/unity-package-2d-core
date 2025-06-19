using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 캐릭터 유니티 애니메이션 처리
    /// </summary>
    public class CharacterAnimationControllerSprite : MonoBehaviour, ICharacterAnimationController
    {
        public void PlayWaitAnimation()
        {
        }

        public void PlayRunAnimation()
        {
        }

        public void PlayAttackAnimation()
        {
        }

        public void PlayDeadAnimation()
        {
        }

        public void UpdateAnimation()
        {
        }

        public float GetCharacterHeight()
        {
            return 0f;
        }

        public float GetCharacterWidth()
        {
            return 0f;
        }

        public Vector2 GetCharacterSize()
        {
            return Vector2.zero;
        }

        public void ChangeCharacterImageInSlot(List<StruckChangeSlotImage> changeSlotImages)
        {
            
        }

        public void RemoveCharacterImageInSlot(List<StruckChangeSlotImage> changeSlotImages)
        {
            
        }

        public IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            yield break;
        }

        public void UpdateTimeScaleByTrackIndex(float value, int index = 0)
        {
            
        }

        public void SetCharacterColor(Color red)
        {
        }

        public void UpdateTimeScaleMove(float value)
        {
            
        }

        public void PlayCharacterAnimation(string animationName, bool loop = false, float timeScale = 1f)
        {
        }
    }
}