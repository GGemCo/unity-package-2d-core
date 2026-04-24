using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class VfxEffectUI : VfxAnimationControllerSprite
    {
        [Tooltip("자동 재생 여부")]
        public bool autoStart = true;
        [Tooltip("재생 속도")]
        public float timeScale;
        
        private void OnEnable()
        {
            if (!autoStart) return;
            Play(-1f, timeScale);
        }

        public void PlayEffect(bool forceReset = false)
        {
            Play(-1f, timeScale, forceReset);
        }
    }
}