using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 스킬 아이콘
    /// </summary>
    public class UIIconSkill : UIIcon, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private TableSkill tableSkill;
        private StruckTableSkill struckSkill;
        private UIWindowSkillInfo uiWindowSkillInfo;
        private SkillData skillData;
        protected override void Awake()
        {
            base.Awake();
            IconType = IconConstants.Type.Skill;
            tableSkill = TableLoaderManager.Instance.TableSkill;
            struckSkill = null;
        }

        protected override void Start()
        {
            base.Start();
            uiWindowSkillInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowSkillInfo>(
                    UIWindowManager.WindowUid.SkillInfo);
            skillData = SceneGame.Instance.saveDataManager.Skill;
        }
        /// <summary>
        /// 다른 uid 로 변경하기
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="iconIsLearn"></param>
        /// <param name="remainCoolTime"></param>
        public override bool ChangeInfoByUid(int iconUid, int iconCount = 0, int iconLevel = 0, bool iconIsLearn = false, int remainCoolTime = 0)
        {
            if (!base.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, remainCoolTime)) return false;
            var info = tableSkill.GetDataByUidLevel(iconUid, iconLevel);
            if (info == null)
            {
                GcLogger.LogError("스킬 테이블에 없는 아이템 입니다.");
                return false;
            }
            struckSkill = info;
            UpdateInfo();
            return true;
        }
        /// <summary>
        /// 아이콘 이미지 업데이트 하기
        /// </summary>
        protected override void UpdateIconImage()
        {
            if (ImageIcon == null)
            {
                ImageIcon.sprite = null;
                return;
            }
            string path = GetIconImagePath();
            if (path == null || path == "")
            {
                ImageIcon.sprite = null;
                return;
            }
            ImageIcon.sprite = Resources.Load<Sprite>(path);
        }
        /// <summary>
        /// 아이콘 이미지 경로 가져오기 
        /// </summary>
        /// <returns></returns>
        protected override string GetIconImagePath()
        {
            if (struckSkill == null) return null;
            return $"Images/Icon/Skill/{struckSkill.IconFileName}";
        }
        public override bool CheckRequireLevel()
        {
            return SceneGame.Instance.player.GetComponent<Player>().IsRequireLevel(struckSkill.NeedPlayerLevel);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // GcLogger.Log("OnPointerEnter "+eventData);
            // uiWindowSkillInfo.SetSkillUid(uid);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // GcLogger.Log("OnPointerExit "+eventData);
            // uiWindowSkillInfo.Show(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button == PointerEventData.InputButton.Left)
            {
                if (!window) return;
                window.SetSelectedIcon(index);
            }
            else if(eventData.button == PointerEventData.InputButton.Middle)
            {
            }
            else if(eventData.button == PointerEventData.InputButton.Right)
            {
                if (uid <= 0 || GetCount() <= 0) return;
                window.OnRightClick(this);
            }
        }

        public StruckTableSkill GetTableInfo()
        {
            return struckSkill;
        }

        public SaveDataIcon GetSaveDataInfo()
        {
            return skillData.GetData(slotIndex);
        }
    }
}