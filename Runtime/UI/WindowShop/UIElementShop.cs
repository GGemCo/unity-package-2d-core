using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 구매 리스트 element
    /// </summary>
    public class UIElementShop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("기본속성")]
        [Tooltip("아이콘 위치")]
        public Vector3 iconPosition;
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textName;
        [Tooltip("구매 가격")]
        public TextMeshProUGUI textPrice;
        [Tooltip("구매하기 버튼")]
        public Button buttonBuy;
        
        private UIWindowShop uiWindowShop;
        private UIWindowItemBuy uIWindowItemBuy;
        private UIWindowItemInfo uIWindowItemInfo;
        private SceneGame sceneGame;
        
        private StruckTableShop struckTableShop;
        private TableItem tableItem;
        private PlayerData playerData;
        private int slotIndex;

        private void Start()
        {
            sceneGame = SceneGame.Instance;
            uIWindowItemBuy  = 
                sceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemBuy>(UIWindowManager.WindowUid
                    .ItemBuy);
            uIWindowItemInfo  = 
                sceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowManager.WindowUid
                    .ItemInfo);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="puiWindowShop"></param>
        /// <param name="pslotIndex"></param>
        /// <param name="pstruckTableShop"></param>
        public void Initialize(UIWindowShop puiWindowShop, int pslotIndex, StruckTableShop pstruckTableShop)
        {
            playerData = SceneGame.Instance.saveDataManager.Player;
            struckTableShop = pstruckTableShop;
            slotIndex = pslotIndex;
            if (buttonBuy != null)
            {
                buttonBuy.onClick.RemoveAllListeners();
                buttonBuy.onClick.AddListener(OnClickBuy);
            }

            uiWindowShop = puiWindowShop;
            tableItem = TableLoaderManager.Instance.TableItem;
            
            UpdateInfos(pstruckTableShop);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// </summary>
        public void UpdateInfos(StruckTableShop pstruckTableShop)
        {
            struckTableShop = pstruckTableShop;
            if (struckTableShop == null)
            {
                GcLogger.LogError($"shop 테이블에 정보가 없습니다. struckTableItem is null");
                return;
            }
            var info = tableItem.GetDataByUid(struckTableShop.ItemUid);
            if (info == null)
            {
                GcLogger.LogError($"item 테이블에 정보가 없습니다. item uid: {struckTableShop.ItemUid}");
                return;
            }

            if (textName != null)
            {
                textName.text = info.Name;
            }
            if (textPrice != null) textPrice.text = $"{struckTableShop.CurrencyType} {struckTableShop.CurrencyValue}";
            buttonBuy.gameObject.SetActive(true);
        }
        /// <summary>
        /// 구매하기
        /// </summary>
        private void OnClickBuy()
        {
            // 여러개 살 수 있는지
                // 팝업창 띄어서 개수 정하기
                // 골드가 충분하지 체크
            if (struckTableShop.MaxBuyCount > 1)
            {
                // 구매할 수 있는 최대 수량으로 등록
                int count = (int)playerData.GetPossibleBuyCount(struckTableShop.CurrencyType, struckTableShop.CurrencyValue);
                if (count <= 0)
                {
                    SceneGame.Instance.systemMessageManager.ShowWarningCurrency(struckTableShop.CurrencyType);
                    return;
                }
                var info = tableItem.GetDataByUid(struckTableShop.ItemUid);
                if (info != null && count > info.MaxOverlayCount)
                {
                    count = info.MaxOverlayCount;
                }
                
                uIWindowItemBuy?.SetPriceInfo(struckTableShop);
                SceneGame.Instance.uIWindowManager.RegisterIcon(uiWindowShop.uid, slotIndex, UIWindowManager.WindowUid.ItemBuy, count);
            }
            // 한번에 하나만 살 수 있는지
            // 골드가 충분하지 체크
            else
            {
                SceneGame.Instance.BuyItem(struckTableShop.ItemUid, struckTableShop.CurrencyType,
                    struckTableShop.CurrencyValue);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            uIWindowItemInfo.SetItemUid(struckTableShop.ItemUid, gameObject, UIWindowItemInfo.PositionType.None, uiWindowShop.containerIcon.cellSize, new Vector2(0, 1f), new Vector2(
                transform.position.x + uiWindowShop.containerIcon.cellSize.x / 2f,
                transform.position.y + uiWindowShop.containerIcon.cellSize.y / 2f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            uIWindowItemInfo.Show(false);
        }

        public Vector3 GetIconPosition() => iconPosition;
    }
}