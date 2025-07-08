using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 정보 Hud
    /// </summary>
    public class UIWindowHud : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        // exp 는 UITextPlayerExp 에서 처리한다.
        [Tooltip("생명력 Slider")]
        public Slider sliderHp;
        [Tooltip("마력 Slider")]
        public Slider sliderMp;
        [Tooltip("현재 플레이어 생명력 수치")]
        public TextMeshProUGUI textHp;
        [Tooltip("현재 플레이어 마력 수치")]
        public TextMeshProUGUI textMp;
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.Hud;
            base.Awake();
        }

        /// <summary>
        /// 인벤토리 열기 
        /// </summary>
        public void OnClickShowInventory()
        {
            SceneGame.Instance.uIWindowManager.ShowWindow(UIWindowConstants.WindowUid.Inventory, true);
        }
        /// <summary>
        /// 스킬 열기 
        /// </summary>
        public void OnClickShowSkill()
        {
            SceneGame.Instance.uIWindowManager.ShowWindow(UIWindowConstants.WindowUid.Skill, true);
        }
        public void SetSliderHp(long currentValue, long totalHp)
        {
            sliderHp.value = (float)currentValue / totalHp;
            textHp.text = $"{currentValue} / {totalHp}";
        }
        public void SetSliderMp(long currentValue, long totalMp)
        {
            sliderMp.value = (float)currentValue / totalMp;
            textMp.text = $"{currentValue} / {totalMp}";
        }
    }
}