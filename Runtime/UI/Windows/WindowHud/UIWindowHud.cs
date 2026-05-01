using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어의 주요 상태 정보와 각종 UI 창 진입 버튼을 표시하는 HUD 창입니다.
    /// 생명력, 마력, 스테미나, 중독 게이지와 전투 상태를 화면에 반영합니다.
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

        [Tooltip("중독 게이지")]
        public UISliderElementCharge poisonCharge;

        // TODO: 전투 상태 표기 방식 정리 필요
        [Tooltip("전투 상태")]
        public TMP_Text textBattleStatus;
        [Tooltip("맵 이름 표시 오브젝트")]
        [SerializeField] private TMP_Text textMapName;
        
        private Vector3 _prevPositionHp;

        /// <summary>
        /// 중독 게이지 UI를 원소/상태 이상 게이지 컨트롤러에 바인딩합니다.
        /// </summary>
        /// <param name="controller">중독 게이지와 연결할 컨트롤러입니다.</param>
        public void BindElementGauge(CharacterElementGaugeController controller)
        {
            poisonCharge?.Bind(controller);
        }

        /// <summary>
        /// HUD 창의 고유 식별자를 설정한 뒤 기본 초기화를 수행합니다.
        /// </summary>
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.Hud;
            base.Awake();
            if (gameObjectHp)
                _prevPositionHp = gameObjectHp.transform.localPosition;
            MapManager.OnLoadCompleteMap += OnLoadCompleteMap;
        }

        private void OnDestroy()
        {
            MapManager.OnLoadCompleteMap -= OnLoadCompleteMap;
        }

        #region 윈도우 열기 버튼

        /// <summary>
        /// 인벤토리 창을 엽니다.
        /// </summary>
        public void OnClickShowInventory()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.Inventory, true);
        }

        /// <summary>
        /// 스킬 창을 엽니다.
        /// </summary>
        public void OnClickShowSkill()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.Skill, true);
        }

        /// <summary>
        /// 옵션 창을 엽니다.
        /// </summary>
        public void OnClickShowOption()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.Option, true);
        }

        /// <summary>
        /// 패시브 스킬 창을 엽니다.
        /// </summary>
        public void OnClickShowSkillPassive()
        {
            SceneGame.Instance.uIWindowManager?.ShowWindow(UIWindowConstants.WindowUid.SkillPassive, true);
        }

        #endregion

        /// <summary>
        /// 현재 생명력 값을 HUD에 반영합니다.
        /// </summary>
        /// <param name="currentValue">현재 생명력 값입니다.</param>
        /// <param name="total">최대 생명력 값입니다.</param>
        public void SetHp(long currentValue, long total)
        {
            gameObjectHp.SetValue(UIWindowHudResourceType.Hp, currentValue, total);
        }

        /// <summary>
        /// 임시 생명력 값을 HUD에 반영합니다.
        /// </summary>
        /// <param name="currentValue">현재 임시 생명력 값입니다.</param>
        /// <param name="total">최대 임시 생명력 값입니다.</param>
        public void SetHpTemp(long currentValue, long total)
        {
            gameObjectHp.SetValue(UIWindowHudResourceType.HpTemp, currentValue, total);
        }

        /// <summary>
        /// 임시 생명력의 최대값만 갱신합니다.
        /// </summary>
        /// <param name="total">설정할 최대 임시 생명력 값입니다.</param>
        public void SetMaxHpTemp(long total)
        {
            gameObjectHp.SetMaxValue(UIWindowHudResourceType.HpTemp, total);
        }

        /// <summary>
        /// 현재 마력 값을 HUD에 반영합니다.
        /// </summary>
        /// <param name="currentValue">현재 마력 값입니다.</param>
        /// <param name="total">최대 마력 값입니다.</param>
        public void SetMp(long currentValue, long total)
        {
            gameObjectMp.SetValue(UIWindowHudResourceType.Mp, currentValue, total);
        }

        /// <summary>
        /// 현재 스테미나 값을 HUD에 반영합니다.
        /// 스테미나 전용 HUD가 없으면 레거시 Slider를 대체 사용합니다.
        /// </summary>
        /// <param name="currentValue">현재 스테미나 값입니다.</param>
        /// <param name="total">최대 스테미나 값입니다.</param>
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

        /// <summary>
        /// 현재 전투 상태를 텍스트로 표시합니다.
        /// </summary>
        /// <param name="value">표시할 전투 상태 값입니다.</param>
        public void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            if (!textBattleStatus) return;
            textBattleStatus.text = value.ToString();
        }

        /// <summary>
        /// 스테미나 HUD에 피격 피드백 재생을 요청합니다.
        /// </summary>
        public void PlayStaminaDamageFeedback()
        {
            if (gameObjectStamina is IHudDamageFeedbackReceiver receiver)
            {
                receiver.PlayDamageFeedback();
            }
        }

        /// <summary>
        /// 스테미나 HUD에 지정한 방향 기준의 피격 피드백 재생을 요청합니다.
        /// </summary>
        /// <param name="directionMode">런타임에서 적용할 흔들림 방향입니다.</param>
        public void PlayStaminaDamageFeedback(UIEffectShakeDirectionMode directionMode)
        {
            if (gameObjectStamina is IHudDamageFeedbackReceiver receiver)
            {
                receiver.PlayDamageFeedback(directionMode);
            }
        }

        /// <summary>
        /// 스테미나 HUD 표시 여부를 설정합니다.
        /// </summary>
        /// <param name="value"><see langword="true"/>이면 표시하고, <see langword="false"/>이면 숨깁니다.</param>
        public void ShowStamina(bool value)
        {
            if (!gameObjectStamina) return;
            gameObjectStamina.gameObject.SetActive(value);
        }

        /// <summary>
        /// 플레이어 정보 UI가 열렸을 때, HP 정보를 실시간으로 보여주기 위해 gameObjectHp를 잠시 이동시켰다가, 플레이어 정보 UI가 닫히면 원래 위치로 되돌린다.
        /// </summary>
        public void ResetPositionHeartObject()
        {
            if (!gameObjectHp) return;
            gameObjectHp.transform.localPosition = _prevPositionHp;
            var horizontalLayoutGroup = gameObjectHp.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayoutGroup != null)
            {
                horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        private void OnLoadCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            if (!textMapName) return;
            textMapName.text = SceneGame.mapManager.GetCurrentMapName();
        }
    }
}