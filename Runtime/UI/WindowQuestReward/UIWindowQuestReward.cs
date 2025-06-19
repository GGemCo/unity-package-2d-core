using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    public class UIWindowQuestReward : UIWindow
    {
        [Header("필수속성")]
        [Tooltip("타이틀")]
        public TextMeshProUGUI textTitle;
        [Tooltip("경험치 보상")]
        public TextMeshProUGUI textExp;
        [Tooltip("골드 보상")]
        public TextMeshProUGUI textGold;
        [Tooltip("실버 보상")]
        public TextMeshProUGUI textSilver;
        [Tooltip("확인 버튼")] 
        public Button buttonConfirm;
        
        private int currentQuestUid;
        private UIWindowItemInfo uiWindowItemInfo;
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.QuestReward;
            base.Awake();
            buttonConfirm?.onClick.AddListener(OnClickConfirm);
        }

        protected override void Start()
        {
            base.Start();
            uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowManager.WindowUid.ItemInfo);
        }
        protected void OnEnable()
        {
            if (currentQuestUid <= 0) return;
            Quest info = SceneGame.Instance.QuestManager.GetQuestInfo(currentQuestUid);
            if (info == null)
            {
                GcLogger.LogError("quest json 정보가 없습니다. quest Uid: " + currentQuestUid);
                return;
            }

            if (textTitle != null)
            {
                textTitle.text = info.title;
            }

            if (textExp != null)
            {
                textExp.text = $"경험치: {info.reward.experience}";
            }
            if (textGold != null)
            {
                textGold.text = $"골드: {info.reward.gold}";
            }
            if (textSilver != null)
            {
                textSilver.text = $"실버: {info.reward.silver}";
            }

            if (info.reward.items.Count <= 0) return;
            maxCountIcon = info.reward.items.Count;
            iconPoolManager.Initialize();
            int index = 0;
            foreach (var rewardItem in info.reward.items)
            {
                if (rewardItem == null || rewardItem.itemUid <= 0) continue;
                var icon = GetIconByIndex(index);
                if (icon == null) continue;
                icon.ChangeInfoByUid(rewardItem.itemUid, rewardItem.amount);
                ++index;
            }
        }

        public void SetRewardInfoByQuestUid(int questUid)
        {
            currentQuestUid = questUid;
            Show(true);
        }

        private void OnClickConfirm()
        {
            Show(false);
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            uiWindowItemInfo?.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Left, slotSize);
        }
    }
}