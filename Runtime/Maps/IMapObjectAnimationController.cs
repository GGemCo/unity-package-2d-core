using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 애니메이션 관리
    /// </summary>
    public interface IMapObjectAnimationController
    {
        public const string WalkForwardAnim = "run";
        public const string WalkBackwardAnim = "run";
        public const string WaitForwardAnim = "wait";
        public const string WaitBackwardAnim = "wait";
        public const string AttackAnim = "attack";
        public const string DeadAnim = "die";
        public const string DamageAnim = "damage";
        public const string SuffixWait = "_wait";
        public const string SuffixEnd = "_end";
        public string CurrentAnimationNameAttack { get; set; }
        
        void PlayWaitAnimation();
        void PlayRunAnimation();
        void PlayAttackAnimation(string animName = "");
        void PlayDeadAnimation();
        void PlayDamageAnimation();
        void PlayCharacterAnimation(string animationName, bool loop = false, float timeScale = 1f);
        void PlayAttackEndAnimation();
        void PlayAttackWaitAnimation();
        
        IEnumerator FadeEffect(float duration, bool fadeIn);
        void SetCharacterColor(Color red);
        void UpdateTimeScaleMove(float value);
        float GetCharacterAnimationDuration(string animationName, bool isMilliseconds = true);
        bool HasAnimation(string stateName);
        Dictionary<string, float> GetAnimationAllLength();
    }
}