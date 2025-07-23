#if GGEMCO_USE_SPINE != true
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class EffectAnimationControllerSprite : Animator2dController, IEffectAnimationController
    {
        private DefaultEffect _defaultEffect;
        private Renderer _effectRenderer;
        protected override void Awake()
        {
            base.Awake();
            _defaultEffect = GetComponent<DefaultEffect>();
            _effectRenderer = GetComponent<Renderer>();
            _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);
        }
        public bool PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1, List<StruckAddAnimation> addAnimations = null)
        {
            var findAnimation = GetClipByName(animationName);
            if (findAnimation == null) return false;
            
            PlayAnimation(animationName, loop, timeScale, addAnimations);
            return true;
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }
        /// <summary>
        /// 애니메이션이 중단되면 호출되는 콜백 함수
        /// </summary>
        public override void OnAnimationComplete()
        {
            if (!GetClipByName(IEffectAnimationController.KeyClipNameEnd))
            {
                _defaultEffect.DestroyForce();
                return;
            }
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(IEffectAnimationController.KeyClipNameEnd)) return;
            _defaultEffect.SetEnd();
        }
        public void SetLoop(bool loop, int layerIndex = 0)
        {
            SetAnimationLoop(loop, layerIndex);
        }
        public float GetAnimationEventTime(string aniName, string eventName, List<string> exceptEventName = null)
        {
            return 0;
        }
		public bool PlayEffectAnimation(string animationName, float duration) 
		{
            var findAnimation = GetClipByName(animationName);
            if (findAnimation == null) return false;

            return true;
        }

        public bool PlayEndAnimation()
        {
            var findAnimation = GetClipByName(IEffectAnimationController.KeyClipNameEnd);
            if (findAnimation == null) return false;
            PlayAnimation(IEffectAnimationController.KeyClipNameEnd);
            return true;
        }
    }
}
#endif