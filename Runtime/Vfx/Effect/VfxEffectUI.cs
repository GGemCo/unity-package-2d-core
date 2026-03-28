using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class VfxEffectUI : VfxAnimationControllerSprite
    {
        [Tooltip("재생 속도")]
        public float timeScale;
        
        private void OnEnable()
        {
            Play(-1f, timeScale);
        }
    }
}