using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 구매 리스트 element
    /// </summary>
    public class UIElementShop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
        
        [Header("마우스 이벤트 On/Off")]
        [Tooltip("활성화 할 경우, 아이템에서 마우스 오버하면, 정보창이 나타납니다.")]
        [SerializeField] private bool usePointerEnterEvent = true;
        [Tooltip("활성화 할 경우, 아이템에서 마우스 아웃하면, 정보창이 사라집니다.")]
        [SerializeField] private bool usePointerExitEvent = true;
        [Tooltip("활성화 할 경우, 아이템에서 마우스 클릭하면, 정보창이 나타납니다.")]
        [SerializeField] private bool usePointerClickEvent = false;
        
        private UIWindowShop _uiWindowShop;
        private UIWindowItemBuy _uIWindowItemBuy;
        private UIWindowItemInfo _uIWindowItemInfo;
        
        private ShopDisplayItem _shopDisplayItem;
        private TableItem _tableItem;
        private PlayerData _playerData;
        private int _slotIndex;

        private void Start()
        {
            EnsureWindows();
        }

        private void EnsureWindows()
        {
            _uIWindowItemBuy ??= 
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemBuy>(UIWindowConstants.WindowUid
                    .ItemBuy);
            _uIWindowItemInfo ??= 
                SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid
                    .ItemInfo);
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="uiWindowShop"></param>
        /// <param name="slotIndex"></param>
        /// <param name="struckTableShop"></param>
        public void Initialize(UIWindowShop uiWindowShop, int slotIndex, StruckTableShop struckTableShop)
        {
            Initialize(uiWindowShop, slotIndex, new ShopDisplayItem(struckTableShop));
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="uiWindowShop"></param>
        /// <param name="slotIndex"></param>
        /// <param name="shopDisplayItem"></param>
        public void Initialize(UIWindowShop uiWindowShop, int slotIndex, ShopDisplayItem shopDisplayItem)
        {
            _playerData = SceneGame.Instance.saveDataManager.Player;
            _shopDisplayItem = shopDisplayItem;
            _slotIndex = slotIndex;
            if (buttonBuy != null)
            {
                buttonBuy.onClick.AddListener(OnClickBuy);
            }

            _uiWindowShop = uiWindowShop;
            _tableItem = TableLoaderManager.Instance.TableItem;
            
            UpdateInfos(shopDisplayItem);
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// </summary>
        public void UpdateInfos(StruckTableShop struckTableShop)
        {
            UpdateInfos(new ShopDisplayItem(struckTableShop));
        }

        /// <summary>
        /// slotIndex 로 아이템 정보를 가져온다.
        /// </summary>
        public void UpdateInfos(ShopDisplayItem shopDisplayItem)
        {
            _shopDisplayItem = shopDisplayItem;
            if (_shopDisplayItem == null || _shopDisplayItem.Source == null || _shopDisplayItem.IsEmpty)
            {
                GcLogger.LogError($"shop 테이블에 정보가 없습니다. struckTableItem is null");
                return;
            }
            var info = _tableItem.GetDataByUid(_shopDisplayItem.ItemUid);
            if (info == null)
            {
                GcLogger.LogError($"item 테이블에 정보가 없습니다. item uid: {_shopDisplayItem.ItemUid}");
                return;
            }

            if (textName != null)
            {
                textName.text = info.Name;
            }
            if (textPrice != null) 
                textPrice.text = $"{_shopDisplayItem.CurrencyType} {_shopDisplayItem.CurrencyValue}";
            RefreshAvailability();
        }

        public void RefreshAvailability()
        {
            if (_shopDisplayItem == null) return;
            if (_uiWindowShop != null)
            {
                bool isBuyable = _uiWindowShop.CanBuy(_shopDisplayItem, out var disabledReason);
                _shopDisplayItem.SetAvailability(isBuyable, disabledReason);
            }

            if (buttonBuy)
            {
                buttonBuy.gameObject.SetActive(true);
                buttonBuy.interactable = _shopDisplayItem.IsBuyable;
            }
        }
        
        /// <summary>
        /// 구매하기
        /// </summary>
        public void OnClickBuy()
        {
            if (_shopDisplayItem == null || _shopDisplayItem.Source == null) return;
            string disabledReason = string.Empty;
            if (!_shopDisplayItem.IsBuyable || !_uiWindowShop.CanBuy(_shopDisplayItem, out disabledReason))
            {
                if (!string.IsNullOrEmpty(disabledReason))
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning(disabledReason);
                }
                else
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning("Shop_CannotBuyItem");
                }

                return;
            }

            // 여러개 살 수 있는지
                // 팝업창 띄어서 개수 정하기
                // 골드가 충분하지 체크
            if (_shopDisplayItem.MaxBuyCount > 1)
            {
                // 구매할 수 있는 최대 수량으로 등록
                int count = (int)_playerData.GetPossibleBuyCount(_shopDisplayItem.CurrencyType, _shopDisplayItem.CurrencyValue);
                if (count <= 0)
                {
                    SceneGame.Instance.systemMessageManager.ShowWarningCurrency(_shopDisplayItem.CurrencyType);
                    return;
                }

                if (_shopDisplayItem.MaxBuyCount > 0 && count > _shopDisplayItem.MaxBuyCount)
                {
                    count = _shopDisplayItem.MaxBuyCount;
                }

                var info = _tableItem.GetDataByUid(_shopDisplayItem.ItemUid);
                if (info != null && count > info.MaxOverlayCount)
                {
                    count = info.MaxOverlayCount;
                }

                var shopPurchaseData = SceneGame.Instance?.saveDataManager?.ShopPurchase;
                if (shopPurchaseData != null)
                {
                    int remainingCount = shopPurchaseData.GetRemainingCount(_shopDisplayItem);
                    if (remainingCount <= 0)
                    {
                        SceneGame.Instance.systemMessageManager.ShowMessageWarning("Shop_SoldOut");
                        RefreshAvailability();
                        return;
                    }

                    if (count > remainingCount)
                    {
                        count = remainingCount;
                    }
                }
                
                _uIWindowItemBuy?.SetPriceInfo(_shopDisplayItem);
                SceneGame.Instance.uIWindowManager.RegisterIcon(_uiWindowShop.uid, _slotIndex, UIWindowConstants.WindowUid.ItemBuy, count);
            }
            // 한번에 하나만 살 수 있는지
            // 골드가 충분하지 체크
            else
            {
                var result = SceneGame.Instance.BuyItem(_shopDisplayItem.ItemUid, _shopDisplayItem.CurrencyType,
                    _shopDisplayItem.CurrencyValue);
                if (result is { Result: ResultCommon.ResultType.Success })
                {
                    SceneGame.Instance.saveDataManager?.ShopPurchase?.AddBoughtCount(_shopDisplayItem, 1);
                }
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!usePointerEnterEvent) return;
            SetSelected(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!usePointerClickEvent) return;
            SetSelected(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!usePointerExitEvent) return;
            _uIWindowItemInfo.Show(false);
        }

        public Vector3 GetIconPosition() => iconPosition;

        public void SetSelected(bool value)
        {
            if (_shopDisplayItem == null) return;
            if (value)
            {
                _uiWindowShop.SetSelectItem(this);
                
                EnsureWindows();
                _uIWindowItemInfo.SetItemUid(_shopDisplayItem.ItemUid, 0, gameObject, UIWindowItemInfo.PositionType.Fixed,
                    _uiWindowShop.containerIcon.cellSize);
            }
            else
            {
                
            }
        }

        public (CurrencyConstants.Type, int) GetPrice()
        {
            if (_shopDisplayItem == null) return (CurrencyConstants.Type.None, 0);
            return (_shopDisplayItem.CurrencyType, _shopDisplayItem.CurrencyValue);
        }

        public ShopDisplayItem GetDisplayItem() => _shopDisplayItem;
    }
}
