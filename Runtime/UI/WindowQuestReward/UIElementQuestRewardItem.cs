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
        
        private UIWindowQuestReward _uiWindowQuestReward;
        private UIWindowItemInfo _uiWindowItemInfo;
        private int _itemUid;
        private int _itemCount;
        private TableItem _tableItem;
        
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="puiWindowQuestReward"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        public void Initialize(UIWindowQuestReward puiWindowQuestReward, int itemUid, int itemCount)
        {
            _uiWindowQuestReward = puiWindowQuestReward;
            this._itemUid = itemUid;
            this._itemCount = itemCount;
        }
        private void Start()
        {
            _uiWindowItemInfo =
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(
                    UIWindowManager.WindowUid.ItemInfo);
            _tableItem = TableLoaderManager.Instance.TableItem;
            var info = _tableItem.GetDataByUid(_itemUid);
            if (info == null) return;
            if (textName == null) return;
            textName.text = $"{info.Name} x {_itemCount}";
            imageIcon.sprite = Resources.Load<Sprite>(info.ImageItemPath);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _uiWindowItemInfo.SetItemUid(_itemUid, gameObject,
                UIWindowItemInfo.PositionType.None, _uiWindowQuestReward.containerIcon.cellSize, new Vector2(0, 1f),
                new Vector2(
                    transform.position.x + _uiWindowQuestReward.containerIcon.cellSize.x / 2f,
                    transform.position.y + _uiWindowQuestReward.containerIcon.cellSize.y / 2f));
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            _uiWindowItemInfo.Show(false);
        }
    }
}