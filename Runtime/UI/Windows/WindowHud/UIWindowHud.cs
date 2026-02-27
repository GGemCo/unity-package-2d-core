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
        public UIWindowHudResourceBase sliderHp;
        [Tooltip("마력 Slider")]
        public UIWindowHudResourceBase sliderMp;
        
        [Tooltip("스테미나 Slider")]
        public Slider sliderStamina;
        [Tooltip("현재 플레이어 생명력 수치")]
        public TextMeshProUGUI textHp;
        [Tooltip("현재 플레이어 마력 수치")]
        public TextMeshProUGUI textMp;
        [Tooltip("현재 플레이어 스테미나 수치")]
        public TextMeshProUGUI textStamina;
        
        // todo. 정리 필요
        [Tooltip("전투 상태")]
        public TMP_Text textBattleStatus;
        
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
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.Inventory, true);
        }
        /// <summary>
        /// 스킬 열기 
        /// </summary>
        public void OnClickShowSkill()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.Skill, true);
        }
        public void OnClickShowOption()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.Option, true);
        }
        public void SetSliderHp(long currentValue, long total)
        {
            sliderHp.SetValue(UIWindowHudResourceType.Hp, currentValue, total);
        }
        public void SetSliderMp(long currentValue, long total)
        {
            sliderMp.SetValue(UIWindowHudResourceType.Mp, currentValue, total);
        }
        public void SetSliderStamina(long currentValue, long total)
        {
            sliderStamina.value = (float)currentValue / total;
            textStamina.text = $"{currentValue} / {total}";
        }

        public void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            if (!textBattleStatus) return;
            textBattleStatus.text = value.ToString();
        }
    }
}