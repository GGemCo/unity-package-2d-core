using System.Collections;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 스킬 정보 윈도우
    /// </summary>
    public class UIWindowSkillInfo : UIWindow
    {
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
        
        private StruckTableSkill struckTableSkill;
        private TableStatus tableStatus;
        private TableSkill tableSkill;
        private TableAffect tableAffect;
        
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.SkillInfo;
            if (TableLoaderManager.Instance == null) return;
            tableSkill = TableLoaderManager.Instance.TableSkill;
            tableStatus = TableLoaderManager.Instance.TableStatus;
            tableAffect = TableLoaderManager.Instance.TableAffect;
            base.Awake();
        }
        public void SetSkillUid(int skillUid, int skillLevel, Vector2 pivot, Vector2 position)
        {
            if (skillUid <= 0) return;
            struckTableSkill = tableSkill.GetDataByUidLevel(skillUid, skillLevel);
            if (struckTableSkill is not { Uid: > 0 }) return;
            
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
            if (struckTableSkill == null) return;
            textName.text = $"이름: {struckTableSkill.Name}";
            textLevel.text = $"레벨: {struckTableSkill.Level}";
            textNeedLevel.text = $"필요레벨: {struckTableSkill.NeedPlayerLevel}";
            textTarget.text = $"타겟: {struckTableSkill.Target}";
            textDamageType.text = $"{SkillConstants.NameByDamageType[struckTableSkill.DamageType]} : {struckTableSkill.DamageValue}";
            textDamageType.gameObject.SetActive(struckTableSkill.DamageValue > 0);

            textNeedMp.text = $"소모Mp: {struckTableSkill.NeedMp}";
            textCoolTime.text = $"쿨타임: {struckTableSkill.CoolTime}";
            textCoolTime.gameObject.SetActive(struckTableSkill.CoolTime > 0);
            
            textDamageRange.text = $"데미지범위: {struckTableSkill.DamageRange}";
            textDamageRange.gameObject.SetActive(struckTableSkill.DamageRange > 0);
            
            textDistance.text = $"사거리: {struckTableSkill.Distance}";
            textDistance.gameObject.SetActive(struckTableSkill.Distance > 0);
            textDuration.text = $"효과지속시간: {struckTableSkill.Duration}";
            textDuration.gameObject.SetActive(struckTableSkill.Duration > 0);
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
            var info = tableStatus.GetDataById(statusId);
            return info?.Name ?? "";
        }

        private void SetAffectInfo()
        {
            if (struckTableSkill.AffectUid <= 0)
            {
                textAffect.gameObject.SetActive(false);
                return;
            }
            var info = tableAffect.GetDataByUid(struckTableSkill.AffectUid);
            if (info == null) return;
            textAffect.gameObject.SetActive(true);
            string option =
                $"{struckTableSkill.AffectRate}% 확률로 {GetStatusName(info.StatusID)} {GetValueText(info.StatusSuffix, info.Value)} 가 {info.Duration} 초 동안 발동합니다.";
            textAffect.text = option;
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
}