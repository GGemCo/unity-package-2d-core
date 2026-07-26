using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 구매하기 윈도우
    /// </summary>
    public class UIWindowItemBuy : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("아이템 이름")]
        public TextMeshProUGUI textItemName;
        [Tooltip("아이템 개수")]
        public TextMeshProUGUI textItemCount;
        [Tooltip("최종 금액")]
        public TextMeshProUGUI textTotalPrice;
        [Tooltip("나누기 슬라이드")]
        public Slider sliderSplit;
        [Tooltip("구매하기 버튼")]
        public Button buttonConfirm;
        [Tooltip("취소 버트")]
        public Button buttonCancel;

        // 내가 가지고 있는 아이템 개수
        private int _maxItemCount;
        // 구매 하는 아이템 uid
        private int _itemUid;
        // 구매 하는 아이템 개수
        private int _buyItemCount;
        // 구매 하는 아이템의 인벤토리 slot index
        private int _buyItemIndex;
        // 판매하는 아이템의 shop 테이블 정보
        private StruckTableShopItem _struckTableShop;
        private ShopDisplayItem _shopDisplayItem;
        
        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.ItemBuy;
            base.Awake();
            buttonConfirm.onClick.AddListener(OnClickConfirm);
            buttonCancel.onClick.AddListener(OnClickCancel);
            sliderSplit.onValueChanged.AddListener(OnValueChanged);
            
            IconPoolManager.SetSetIconHandler(new SetIconHandlerItemBuy());
        }
        public void UpdateInfo(int iconUid, int iconCount)
        {
            var info = TableLoaderManager.Instance.GetItemData(iconUid);
            if (info == null) return;
            var icon = GetIconByUid(iconUid);
            if (icon)
            {
                icon.SetCount(0);
            }
            textItemName.text = ItemDisplayNameUtility.GetDisplayName(info);
            textItemCount.text = $"{iconCount / iconCount}";
            _maxItemCount = iconCount;
            Show(true);
            sliderSplit.value = 0.5f;
            // 강제로 특정 값으로 이벤트 호출 (값은 그대로 유지)
            sliderSplit.onValueChanged.Invoke(sliderSplit.value);
        }
        private void OnValueChanged(float value)
        {
            if (sliderSplit == null) return;
            _buyItemCount = (int)(_maxItemCount * value);
            if (_buyItemCount == 0)
            {
                _buyItemCount = 1;
                sliderSplit.value = (float)_buyItemCount / _maxItemCount;
            }
            textItemCount.text = $"{_buyItemCount} / {_maxItemCount}";
            textTotalPrice.text = "0";
            if (_shopDisplayItem != null)
            {
                textTotalPrice.text = $"{CurrencyConstants.GetNameByCurrencyType(_shopDisplayItem.CurrencyType)} {_shopDisplayItem.CurrencyValue * _buyItemCount}";
            }
            else if (_struckTableShop != null)
            {
                textTotalPrice.text = $"{CurrencyConstants.GetNameByCurrencyType(_struckTableShop.CurrencyType)} {_struckTableShop.CurrencyValue * _buyItemCount}";
            }
        }
        /// <summary>
        /// 아이템 나누기
        /// </summary>
        private void OnClickConfirm()
        {
            if (_struckTableShop == null) return;
            if (_shopDisplayItem != null &&
                !ShopAvailabilityService.Instance.CanBuy(_shopDisplayItem, out var disabledReason))
            {
                if (!string.IsNullOrEmpty(disabledReason))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning(disabledReason);
                }
                else
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Shop_CannotBuyItem");
                }

                return;
            }

            if (_shopDisplayItem != null)
            {
                var shopPurchaseData = SceneGame.saveDataManager?.ShopPurchase;
                if (shopPurchaseData != null && !shopPurchaseData.CanBuy(_shopDisplayItem, _buyItemCount, out disabledReason))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning(
                        string.IsNullOrEmpty(disabledReason) ? "Shop_CannotBuyItem" : disabledReason);
                    return;
                }
            }

            // 구매 하기
            if (_shopDisplayItem != null)
            {
                SceneGame.Instance.BuyItem(_shopDisplayItem, _buyItemCount);
            }
            else
            {
                SceneGame.Instance.BuyItem(
                    _struckTableShop.ItemUid,
                    _struckTableShop.CurrencyType,
                    _struckTableShop.CurrencyValue,
                    _buyItemCount);
            }

            _struckTableShop = null;
            _shopDisplayItem = null;
            Show(false);
        }

        private void OnClickCancel()
        {
            _struckTableShop = null;
            _shopDisplayItem = null;
            Show(false);
        }

        public void SetPriceInfo(StruckTableShop pstruckTableShop)
        {
            _struckTableShop = StruckTableShopItem.FromLegacyShopRow(pstruckTableShop);
            _shopDisplayItem = null;
        }

        public void SetPriceInfo(ShopDisplayItem shopDisplayItem)
        {
            _shopDisplayItem = shopDisplayItem;
            _struckTableShop = shopDisplayItem?.Source;
        }
        /// <summary>
        /// 창 닫힐때 register 됬던 아이콘 정보 지워주기
        /// </summary>
        /// <param name="show"></param>
        public override void OnShow(bool show)
        {
            base.OnShow(show);
            if (show) return;
            _shopDisplayItem = null;
            UnRegisterAllIcons(uid);
        }
    }
}
