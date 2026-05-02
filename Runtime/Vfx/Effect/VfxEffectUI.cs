using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class VfxEffectUI : VfxAnimationControllerSprite
    {
        [Tooltip("자동 재생 여부")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("재생 속도")]
        [SerializeField] private  float timeScale;
        [Tooltip("크기")]
        [SerializeField] private  Vector3 scale = Vector3.one;
        
        private void OnEnable()
        {
            if (!autoStart) return;
            InitializeScale();
            Play(-1f, timeScale);
        }

        public void PlayEffect(bool forceReset = false)
        {
            InitializeScale();
            Play(-1f, timeScale, forceReset);
        }

        private void InitializeScale()
        {
            if (scale == Vector3.zero || scale == Vector3.one) return;
            transform.localScale = scale;
        }
    }
}