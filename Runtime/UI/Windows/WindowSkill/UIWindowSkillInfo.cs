using System.Collections;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 스킬 정보 윈도우
    /// </summary>
    public class UIWindowSkillInfo : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Header("기본정보")] 
        [Tooltip("스킬 이름")]
        public TextMeshProUGUI textName;
        [Tooltip("스킬 레벨")]
        public TextMeshProUGUI textLevel;
        [Tooltip("필요 레벨")]
        public TextMeshProUGUI textNeedLevel;
        [Tooltip("스킬 타겟")]
        public TextMeshProUGUI textTarget;
        [Tooltip("데미지 타입")]
        public TextMeshProUGUI textDamageType;
        [Tooltip("데미지 범위")]
        public TextMeshProUGUI textDamageRange;
        [Tooltip("사거리")]
        public TextMeshProUGUI textDistance;
        [Tooltip("소모 Mp")]
        public TextMeshProUGUI textNeedMp;
        [Tooltip("효과 지속시간(초)")]
        public TextMeshProUGUI textDuration;
        [Tooltip("재사용 쿨타입(초)")]
        public TextMeshProUGUI textCoolTime;

        [Header("어펙트")]
        [Tooltip("어펙트 설명")]
        public TextMeshProUGUI textAffect;
        
        private StruckTableSkill _struckTableSkill;
        private TableSkill _tableSkill;
        private TableAffect _tableAffect;
        private LocalizationManager _localizationManager;
        
        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.SkillInfo;
            if (TableLoaderManager.Instance == null) return;
            _tableSkill = TableLoaderManager.Instance.TableSkill;
            _tableAffect = TableLoaderManager.Instance.TableAffect;
            _localizationManager = LocalizationManager.Instance;
            base.Awake();
        }
        public void SetSkillUid(int skillUid, int skillLevel, Vector2 pivot, Vector2 position)
        {
            if (skillUid <= 0) return;
            _struckTableSkill = _tableSkill.GetDataByUidLevel(skillUid, skillLevel);
            if (_struckTableSkill is not { Uid: > 0 }) return;
            
            SetBasicInfo();
            SetAffectInfo();
            Show(true);
            // active 된 후 위치 조정한다.
            SetPosition(pivot, position);
        }
        /// <summary>
        /// 이름 설정하기
        /// </summary>
        private void SetBasicInfo()
        {
            if (_struckTableSkill == null) return;
            textName.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_Name"), _struckTableSkill.Name);
            textLevel.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_Level"), _struckTableSkill.Level);
            textNeedLevel.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_NeedLevel"), _struckTableSkill.NeedPlayerLevel);
            textTarget.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_Target"), _struckTableSkill.Target);
            
            textDamageType.text = $"{SkillConstants.NameByDamageType[_struckTableSkill.DamageType]} : {_struckTableSkill.DamageValue}";

            textDamageType.gameObject.SetActive(_struckTableSkill.DamageValue > 0);

            textNeedMp.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_NeedMp"), _struckTableSkill.NeedMp);
            textCoolTime.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_CoolTime"), _struckTableSkill.CoolTime);
            textCoolTime.gameObject.SetActive(_struckTableSkill.CoolTime > 0);
            
            textDamageRange.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_DamageRange"), _struckTableSkill.DamageRange);
            textDamageRange.gameObject.SetActive(_struckTableSkill.DamageRange > 0);
            
            textDistance.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_Distance"), _struckTableSkill.Distance);
            textDistance.gameObject.SetActive(_struckTableSkill.Distance > 0);
            textDuration.text = string.Format(_localizationManager.GetUIWindowSkillInfoByKey("Text_Duration"), _struckTableSkill.Duration);
            textDuration.gameObject.SetActive(_struckTableSkill.Duration > 0);
        }

        private string GetValueText(ConfigCommon.SuffixType suffixType, float value)
        {
            string valueText = $"{value}";
            foreach (var suffix in ItemConstants.StatusSuffixFormats.Keys)
            {
                if (suffixType == suffix)
                {
                    valueText = string.Format(ItemConstants.StatusSuffixFormats[suffix], value);
                    break; // 첫 번째로 매칭된 값만 적용
                }
            }

            return valueText;
        }

        private string GetStatusName(string statusId)
        {
            if (string.IsNullOrEmpty(statusId)) return "";
            // string cleanedId = ItemConstants.StatusSuffixFormats.Aggregate(statusId, (current, suffix) => current.Replace(suffix.Key, ""));
            // todo. 정리 필요
            return "";
        }

        private void SetAffectInfo()
        {
            if (_struckTableSkill.AffectUid <= 0)
            {
                textAffect.gameObject.SetActive(false);
                return;
            }
            var info = _tableAffect.GetDataByUid(_struckTableSkill.AffectUid);
            if (info == null) return;
            textAffect.gameObject.SetActive(true);

            // 신규 Affect 시스템: 모디파이어 기반으로 설명을 자동 생성하고, Smart String으로 출력한다.
            // - 스킬 자체에 '발동 확률'이 별도로 존재하므로, 접두 문구로 함께 표시한다.
            var desc = AffectDescriptionService.Instance.GetDescriptionWithChancePrefix(
                _struckTableSkill.AffectUid,
                _struckTableSkill.AffectRate);

            // UI 윈도우 전용 문장(필요 시)로 감싸고 싶다면, 아래 Smart String 키로 조절할 수 있다.
            // ex) "{ChancePercentText}% 확률로\n{Description}" 형태
            if (!string.IsNullOrWhiteSpace(desc))
            {
                // Text_Affect_Smart 키가 존재하면 그것을 우선한다.
                var args = new SkillAffectTextArgs
                {
                    ChancePercentText = _struckTableSkill.AffectRate.ToString(),
                    Description = desc
                };
                var wrapped = _localizationManager.GetUIWindowSkillInfoSmart("Text_Affect_Smart", args);
                textAffect.text = string.IsNullOrWhiteSpace(wrapped) ? desc : wrapped;
            }
            else
            {
                textAffect.text = string.Empty;
            }
        }
        /// <summary>
        /// 위치 보정하기
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="position"></param>
        private void SetPosition(Vector2 pivot, Vector2 position)
        {
            RectTransform itemInfoRect = GetComponent<RectTransform>();
            itemInfoRect.pivot = pivot;
            transform.position = position;

            // 화면 밖 체크 & 보정
            StartCoroutine(DelayClampToScreen(itemInfoRect));
        }
        /// <summary>
        /// 위치 보정 코루틴
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        private IEnumerator DelayClampToScreen(RectTransform rectTransform)
        {
            yield return null; // 한 프레임 대기
            MathHelper.ClampToScreen(rectTransform);
        }

    }

    /// <summary>
    /// UIWindowSkillInfo의 Smart String 포맷 Arguments.
    /// </summary>
    public sealed class SkillAffectTextArgs
    {
        public string ChancePercentText { get; set; }
        public string Description { get; set; }
    }
}