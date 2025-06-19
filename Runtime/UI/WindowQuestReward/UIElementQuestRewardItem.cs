using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 스킬 윈도우 - 스킬 리스트 element
    /// </summary>
    public class UIElementQuestRewardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
    {
        [Header("기본 속성")]
        [Tooltip("아이콘 이미지")]
        public Image imageIcon;
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textName;
        
        private UIWindowQuestReward uiWindowQuestReward;
        private UIWindowItemInfo uiWindowItemInfo;
        private int itemUid;
        private int itemCount;
        private TableItem tableItem;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="puiWindowQuestReward"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        public void Initialize(UIWindowQuestReward puiWindowQuestReward, int itemUid, int itemCount)
        {
            uiWindowQuestReward = puiWindowQuestReward;
            this.itemUid = itemUid;
            this.itemCount = itemCount;
        }
        private void Start()
        {
            uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowManager.WindowUid.ItemInfo);
            tableItem = TableLoaderManager.Instance.TableItem;
            var info = tableItem.GetDataByUid(itemUid);
            if (info == null) return;
            if (textName == null) return;
            textName.text = $"{info.Name} x {itemCount}";
            imageIcon.sprite = Resources.Load<Sprite>(info.ImageItemPath);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            uiWindowItemInfo.SetItemUid(itemUid, gameObject,
                UIWindowItemInfo.PositionType.None, uiWindowQuestReward.containerIcon.cellSize, new Vector2(0, 1f),
                new Vector2(
                    transform.position.x + uiWindowQuestReward.containerIcon.cellSize.x / 2f,
                    transform.position.y + uiWindowQuestReward.containerIcon.cellSize.y / 2f));
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            uiWindowItemInfo.Show(false);
        }
    }
}