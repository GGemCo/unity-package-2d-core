using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 스킬 윈도우 - 스킬 리스트 element
    /// </summary>
    public class UIElementSkill : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler 
    {
        public Vector3 iconPosition;
        public TextMeshProUGUI textName;
        public TextMeshProUGUI textLevel;
        public TextMeshProUGUI textNeedLevel;
        public TextMeshProUGUI textNeedCurrency;
        public Button buttonLearn;
        public Button buttonLevelUp;
        
        private UIWindowSkill _uiWindowSkill;
        private UIWindowSkillInfo _uiWindowSkillInfo;
        private StruckTableSkill _struckTableSkill;
        private SaveDataIcon _saveDataIcon;
        private TableSkill _tableSkill;
        private int _slotIndex;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="puiWindowSkill"></param>
        /// <param name="pslotIndex"></param>
        /// <param name="pstruckTableSkill"></param>
        /// <param name="pstructSkillIcon"></param>
        public void Initialize(UIWindowSkill puiWindowSkill, int pslotIndex, StruckTableSkill pstruckTableSkill, SaveDataIcon pstructSkillIcon = null)
        {
            _slotIndex = pslotIndex;
            _struckTableSkill = pstruckTableSkill;
            _saveDataIcon = pstructSkillIcon;
            if (buttonLearn != null)
            {
                buttonLearn.gameObject.SetActive(true);
                buttonLearn.onClick.RemoveAllListeners();
                buttonLearn.onClick.AddListener(OnClickLearn);
            }
            if (buttonLevelUp != null)
            {
                buttonLevelUp.gameObject.SetActive(false);
                buttonLevelUp.onClick.RemoveAllListeners();
                buttonLevelUp.onClick.AddListener(OnClickLevelUp);
            }

            _uiWindowSkill = puiWindowSkill;
            _uiWindowSkillInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowSkillInfo>(
                    UIWindowManager.WindowUid.SkillInfo);
            _tableSkill = TableLoaderManager.Instance.TableSkill;
            
            if (textName != null) textName.text = _struckTableSkill.Name;
            UpdateInfos(pstruckTableSkill, _saveDataIcon);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// SaveDataIcon 정보에 따라 버튼 visible 업데이트
        /// </summary>
        public void UpdateInfos(StruckTableSkill pstruckTableSkill, SaveDataIcon psaveDataIcon)
        {
            _struckTableSkill = pstruckTableSkill;
            _saveDataIcon = psaveDataIcon;
            if (_struckTableSkill == null)
            {
                GcLogger.LogError($"스킬 테이블에 없는 스킬입니다. struckTableSkill is null");
                return;
            }

            int level = _saveDataIcon?.Level ?? 1;
            if (textLevel != null) textLevel.text = $"Lv.{level}";
            if (textNeedLevel != null) textNeedLevel.text = $"NeedLevel : {_struckTableSkill.NeedPlayerLevel}";

            // 필요 재화
            if (textNeedCurrency != null)
            {
                textNeedCurrency.text = $"{_struckTableSkill.NeedCurrencyType} {_struckTableSkill.NeedCurrencyValue}";
                if (_struckTableSkill.NeedCurrencyType == CurrencyConstants.Type.None)
                {
                    textNeedCurrency.gameObject.SetActive(false);
                }
            }
            
            // 최대 레벨
            if (_saveDataIcon != null && _struckTableSkill != null && _saveDataIcon.Level >= _struckTableSkill.Maxlevel)
            {
                buttonLearn.gameObject.SetActive(false);
                buttonLevelUp.GetComponentInChildren<TextMeshProUGUI>().text = "MaxLevel";
                buttonLevelUp.gameObject.SetActive(true);
                buttonLevelUp.interactable = false;
            }
            // 레벨업 할때는 다음 레벨 정보로 셋팅
            else if (_saveDataIcon is { IsLearned: true })
            {
                buttonLearn.gameObject.SetActive(false);
                buttonLevelUp.gameObject.SetActive(true);
                int nextLevel = level + 1;
                var infoNextLevel = _tableSkill.GetDataByUidLevel(_struckTableSkill.Uid, nextLevel);
                if (infoNextLevel == null)
                {
                    GcLogger.LogError("skill 테이블에 정보가 없습니다. skill uid: " + _struckTableSkill.Uid + " / Level: " + nextLevel);
                    return;
                }
                if (textNeedLevel != null) textNeedLevel.text = $"NeedLevel : {infoNextLevel.NeedPlayerLevel}";
                
                // 필요 재화
                if (textNeedCurrency != null)
                {
                    textNeedCurrency.text = $"{infoNextLevel.NeedCurrencyType} {infoNextLevel.NeedCurrencyValue}";
                    if (infoNextLevel.NeedCurrencyType == CurrencyConstants.Type.None)
                    {
                        textNeedCurrency.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                buttonLearn.gameObject.SetActive(true);
                buttonLevelUp.gameObject.SetActive(false); 
            }
        }
        /// <summary>
        /// 레벨업
        /// </summary>
        private void OnClickLevelUp()
        {
            bool result = SceneGame.Instance.player.GetComponent<Player>().IsRequireLevel(_struckTableSkill.NeedPlayerLevel);
            if (!result) return;
            // 다음 레벨 있는지 체크, 아니면 최대 레벨
            int nextLevel = _struckTableSkill.Level + 1;
            if (nextLevel > _struckTableSkill.Maxlevel)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("This is the maximum level.");
                return;
            }
            var infoNextLevel = _tableSkill.GetDataByUidLevel(_struckTableSkill.Uid, nextLevel);
            if (infoNextLevel == null)
            {
                GcLogger.LogError("skill 테이블에 정보가 없습니다. skill uid: " + _struckTableSkill.Uid + " / Level: " + nextLevel);
                return;
            }
            bool resultRequireLevel = CheckLevelCurrency(infoNextLevel.NeedPlayerLevel, infoNextLevel.NeedCurrencyType,
                infoNextLevel.NeedCurrencyValue);
            if (!resultRequireLevel) return;

            var result2 =
                SceneGame.Instance.saveDataManager.Skill.SetSkillLevelUp(_slotIndex, _struckTableSkill.Uid, 1, nextLevel,
                    true);
            if (result2.Code == ResultCommon.Type.Success)
            {
                MinusNeedCurrency(infoNextLevel.NeedCurrencyType, infoNextLevel.NeedCurrencyValue);
            }
            _uiWindowSkill.SetIcons(result2);
        }
        /// <summary>
        /// 레벨, 재화 체크
        /// </summary>
        /// <param name="needPlayerLevel"></param>
        /// <param name="needCurrencyType"></param>
        /// <param name="needCurrencyValue"></param>
        /// <returns></returns>
        private bool CheckLevelCurrency(int needPlayerLevel, CurrencyConstants.Type needCurrencyType, int needCurrencyValue)
        {
            // 레벨 체크
            bool result = SceneGame.Instance.player.GetComponent<Player>().IsRequireLevel(needPlayerLevel);
            if (!result) return false;
            // 재화 체크
            if (needCurrencyType != CurrencyConstants.Type.None)
            {
                var checkNeedCurrency = SceneGame.Instance.saveDataManager.Player.CheckNeedCurrency(needCurrencyType, needCurrencyValue);
                if (checkNeedCurrency.Code == ResultCommon.Type.Fail)
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning(checkNeedCurrency.Message);
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 필요 재화 처리
        /// </summary>
        /// <param name="needCurrencyType"></param>
        /// <param name="needCurrencyValue"></param>
        /// <returns></returns>
        private bool MinusNeedCurrency(CurrencyConstants.Type needCurrencyType, int needCurrencyValue)
        {
            if (needCurrencyType == CurrencyConstants.Type.None) return true;
            // 재화 빼주기
            var minusCurrency = SceneGame.Instance.saveDataManager.Player.MinusCurrency(needCurrencyType, needCurrencyValue);
            if (minusCurrency.Code == ResultCommon.Type.Fail)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning(minusCurrency.Message);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 배우기
        /// </summary>
        private void OnClickLearn()
        {
            // GcLogger.Log("click learn");
            bool result = CheckLevelCurrency(_struckTableSkill.NeedPlayerLevel, _struckTableSkill.NeedCurrencyType,
                _struckTableSkill.NeedCurrencyValue);
            if (!result) return;

            var result2 = SceneGame.Instance.saveDataManager.Skill.SetSkillLearn(_slotIndex, _struckTableSkill.Uid, 1, _struckTableSkill.Level, true);
            if (result2.Code == ResultCommon.Type.Success)
            {
                MinusNeedCurrency(_struckTableSkill.NeedCurrencyType, _struckTableSkill.NeedCurrencyValue);
            }
            _uiWindowSkill.SetIcons(result2);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _uiWindowSkillInfo.SetSkillUid(_struckTableSkill.Uid, _struckTableSkill.Level, new Vector2(1f, 1f), new Vector3(transform.position.x - _uiWindowSkill.containerIcon.cellSize.x / 2f,
                transform.position.y + _uiWindowSkill.containerIcon.cellSize.y / 2f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _uiWindowSkillInfo.Show(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }
        public Vector3 GetIconPosition() => iconPosition;
    }
}