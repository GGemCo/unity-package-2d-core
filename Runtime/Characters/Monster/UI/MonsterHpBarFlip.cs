using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class MonsterHpBarFlip : MonsterHpBar
    {
        [Tooltip("좌우 반전한 Slider")]
        public Slider flipSlider;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (flipSlider)
            {
                flipSlider.value = 1f;
            }
        }
        public override void SetValue(long value)
        {
            base.SetValue(value);
            if (!flipSlider) return;
            flipSlider.value = (float)value / Monster.TotalHp.Value;
        }
    }
}