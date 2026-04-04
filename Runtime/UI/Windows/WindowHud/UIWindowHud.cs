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
        [Tooltip("생명력 오브젝트. 예) Slider, UIElement")]
        public UIWindowHudResourceBase gameObjectHp;
        [Tooltip("마력 오브젝트. 예) Slider, UIElement")]
        public UIWindowHudResourceBase gameObjectMp;
        
        [Tooltip("스테미나 오브젝트. 예) Slider, UIElement")]
        public UIWindowHudResourceBase gameObjectStamina;
        [Tooltip("(레거시) 스테미나 Slider")]
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

        #region 윈도우 열기 버튼
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
        public void OnClickShowSkillPassive()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.SkillPassive, true);
        }
        #endregion
        
        public void SetHp(long currentValue, long total)
        {
            gameObjectHp.SetValue(UIWindowHudResourceType.Hp, currentValue, total);
        }

        public void SetHpTemp(long currentValue, long total)
        {
            gameObjectHp.SetValue(UIWindowHudResourceType.HpTemp, currentValue, total);
        }

        public void SetMaxHpTemp(long total)
        {
            gameObjectHp.SetMaxValue(UIWindowHudResourceType.HpTemp, total);
        }
        public void SetMp(long currentValue, long total)
        {
            gameObjectMp.SetValue(UIWindowHudResourceType.Mp, currentValue, total);
        }
        public void SetStamina(long currentValue, long total)
        {
            if (gameObjectStamina)
            {
                gameObjectStamina.SetValue(UIWindowHudResourceType.Stamina, currentValue, total);
            }
            else if (sliderStamina)
            {
                sliderStamina.value = total > 0 ? (float)currentValue / total : 0f;
            }

            if (textStamina)
                textStamina.text = $"{currentValue} / {total}";
        }

        public void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            if (!textBattleStatus) return;
            textBattleStatus.text = value.ToString();
        }

        /// <summary>
        /// 스테미나 HUD에 피격 피드백을 요청합니다.
        /// </summary>
        public void PlayStaminaDamageFeedback()
        {
            if (gameObjectStamina is IHudDamageFeedbackReceiver receiver)
            {
                receiver.PlayDamageFeedback();
            }
        }

        /// <summary>
        /// 스테미나 HUD에 지정 방향 기반 피격 피드백을 요청합니다.
        /// </summary>
        /// <param name="directionMode">런타임에서 지정할 흔들림 방향입니다.</param>
        public void PlayStaminaDamageFeedback(UIEffectShakeDirectionMode directionMode)
        {
            if (gameObjectStamina is IHudDamageFeedbackReceiver receiver)
            {
                receiver.PlayDamageFeedback(directionMode);
            }
        }
    }
}