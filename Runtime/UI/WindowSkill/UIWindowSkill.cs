﻿using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 스킬 윈도우
    /// </summary>
    public class UIWindowSkill : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("스킬 Element 프리팹")]
        public GameObject prefabUIElementSkill;

        public TableSkill TableSkill;
        public readonly Dictionary<int, UIElementSkill> UIElementSkills = new Dictionary<int, UIElementSkill>();
        // private SkillData _skillData;
        private QuickSlotData _quickSlotData;
        
        private UIWindowQuickSlot _uiWindowQuickSlot;
        private UIWindowSkillInfo _uIWindowSkillInfo;
        
        protected override void Awake()
        {
            UIElementSkills.Clear();
            uid = UIWindowConstants.WindowUid.Skill;
            if (TableLoaderManager.Instance == null) return;
            TableSkill = TableLoaderManager.Instance.TableSkill;
            maxCountIcon = TableSkill.GetSkills().Count;
            base.Awake();
            SetSetIconHandler(new SetIconHandlerSkill());
            DragDropHandler.SetStrategy(new DragDropStrategySkill());
        }

        protected override void Start()
        {
            base.Start();
            // _skillData = SceneGame.saveDataManager.Skill;
            _quickSlotData = SceneGame.saveDataManager.QuickSlot;
            _uIWindowSkillInfo =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowSkillInfo>(UIWindowConstants.WindowUid
                    .SkillInfo);
            _uiWindowQuickSlot =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowQuickSlot>(UIWindowConstants.WindowUid
                    .QuickSlot);
        }
        /// <summary>
        /// 슬롯 위치 정해주기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="index"></param>
        public void SetPositionUiSlot(UISlot slot, int index)
        {
            UIElementSkill uiElementSkill = UIElementSkills[index];
            if (uiElementSkill == null) return;
            Vector3 position = uiElementSkill.GetIconPosition();
            if (position == Vector3.zero) return;
            slot.transform.localPosition = position;
        }

        public override void OnShow(bool show)
        {
            if (SceneGame.Instance == null || TableLoaderManager.Instance == null) return;
            if (!show)
            {
                _uIWindowSkillInfo?.Show(false);
                return;
            }
            LoadIcons();
        }
        
        /// <summary>
        /// 저장되어있는 스킬 정보로 아이콘 셋팅하기
        /// 스킬창이 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.saveDataManager.Skill.GetAllDatas();
            if (datas == null) return;
            for (int index = 0; index < maxCountIcon; index++)
            {
                if (index >= icons.Length) continue;
                var icon = icons[index];
                if (icon == null) continue;
                UIIconSkill uiIcon = icon.GetComponent<UIIconSkill>();
                if (uiIcon == null) continue;
                SaveDataIcon saveDataIcon = datas.GetValueOrDefault(index);
                if (saveDataIcon == null) continue;
                
                int skillUid = saveDataIcon.Uid;
                int skillCount = saveDataIcon.Count;
                int skillLevel = saveDataIcon.Level;
                bool skillIsLearned = saveDataIcon.IsLearned;
                var info = TableLoaderManager.Instance.TableSkill.GetDataByUidLevel(skillUid, skillLevel);
                if (info == null) continue;
                uiIcon.ChangeInfoByUid(skillUid, skillCount, skillLevel, skillIsLearned);
                UIElementSkill uiElementSkill = UIElementSkills[index];
                if (uiElementSkill != null)
                {
                    uiElementSkill.UpdateInfos(info, saveDataIcon);
                }
            }
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            AddToQuickSlot(icon);
        }

        public void AddToQuickSlot(UIIcon icon)
        {
            float time = SceneGame.uIIconCoolTimeManager.GetCurrentCoolTime(uid, icon.uid);
            if (time > 0)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("Cannot be changed during cooldown.");//"쿨타임 중에는 바꿀 수 없습니다."
                return;
            }
            if (!icon.IsLearn())
            {
                SceneGame.systemMessageManager.ShowMessageWarning("You can use this after learning it.");//"배운 후 사용할 수 있습니다."
                return;
            }
            if (!icon.CheckRequireLevel()) return;
            if (_uiWindowQuickSlot == null) return;
            // 퀵슬롯에 하나 넣기
            var result = _quickSlotData.AddSkill(icon.uid, icon.GetCount(), icon.GetLevel(), icon.IsLearn());
            _uiWindowQuickSlot.SetIcons(result);
        }
        public UIElementSkill GetElementSkillByIndex(int slotIndex)
        {
            return UIElementSkills[slotIndex];
        }
    }
}