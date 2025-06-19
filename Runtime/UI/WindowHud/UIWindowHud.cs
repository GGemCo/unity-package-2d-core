using TMPro;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 정보 Hud
    /// </summary>
    public class UIWindowHud : UIWindow
    {
        // exp 는 UITextPlayerExp 에서 처리한다.
        public Slider sliderHp;
        public Slider sliderMp;
        
        public TextMeshProUGUI textHp;
        public TextMeshProUGUI textMp;
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowManager.WindowUid.Hud;
            base.Awake();
        }

        /// <summary>
        /// 인벤토리 열기 
        /// </summary>
        public void OnClickShowInventory()
        {
            SceneGame.Instance.uIWindowManager.ShowWindow(UIWindowManager.WindowUid.Inventory, true);
        }
        /// <summary>
        /// 스킬 열기 
        /// </summary>
        public void OnClickShowSkill()
        {
            SceneGame.Instance.uIWindowManager.ShowWindow(UIWindowManager.WindowUid.Skill, true);
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