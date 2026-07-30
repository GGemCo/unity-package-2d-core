using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 상점 UI에서 개별 아이템을 표현하는 요소입니다.
    /// 아이템 정보 표시, 선택 처리, 구매 로직 연결을 담당합니다.
    /// </summary>
    public class UIElementShop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("아이콘 위치")]
        [SerializeField] private Vector3 iconPosition;

        [Tooltip("아이템 이름")]
        [SerializeField] private TextMeshProUGUI textName;

        [Tooltip("구매 가격")]
        [SerializeField] private TextMeshProUGUI textPrice;

        [Tooltip("구매하기 버튼")]
        [SerializeField] private Button buttonBuy;

        [Header("마우스 이벤트 On/Off")]
        [Tooltip("마우스 오버 시 정보창 표시 여부")]
        [SerializeField] private bool usePointerEnterEvent = true;

        [Tooltip("마우스 아웃 시 정보창 숨김 여부")]
        [SerializeField] private bool usePointerExitEvent = true;

        [Tooltip("마우스 클릭 시 정보창 표시 여부")]
        [SerializeField] private bool usePointerClickEvent = false;
        
        [Tooltip("구매 가능일 때, 아이콘 투명도")]
        [SerializeField] private float normalIconAlpha = 1f;
        [Tooltip("구매 불가능일 때, 아이콘 투명도")]
        [SerializeField] private float soldOutIconAlpha = 0.1f;
        
        [Header("선택 시 색상")]
        [SerializeField] private Color colorSelected = Color.white;
        [SerializeField] private Color colorNotSelected = Color.white;
        
        [Header("할인 이미지 아이콘")]
        [SerializeField] private Image imageDiscount;

        private UIWindowShop _uiWindowShop;
        private UIWindowItemBuy _uIWindowItemBuy;
        private UIIcon _uiIcon;
        private UISlot _uiSlot;
        private ClickSoundEventBroadcaster _clickSoundEventBroadcaster;

        /// <summary>
        /// 현재 표시 중인 상점 아이템 데이터입니다.
        /// </summary>
        private ShopDisplayItem _shopDisplayItem;

        /// <summary>
        /// 아이템 테이블 참조입니다.
        /// </summary>
        private TableItem _tableItem;

        /// <summary>
        /// 플레이어 데이터 참조입니다.
        /// </summary>
        private PlayerData _playerData;

        /// <summary>
        /// 이 UI 요소의 슬롯 인덱스입니다.
        /// </summary>
        private int _slotIndex;

        /// <summary>
        /// 현재 게임 씬 참조입니다.
        /// </summary>
        private SceneGame _sceneGame;

        /// <summary>
        /// 같은 오브젝트에 연결된 수동 클릭 사운드 브로드캐스터를 캐싱합니다.
        /// </summary>
        private void Awake()
        {
            _clickSoundEventBroadcaster = GetComponent<ClickSoundEventBroadcaster>();
        }

        /// <summary>
        /// 초기화 시 필요한 씬 및 윈도우 참조를 확보합니다.
        /// </summary>
        private void Start()
        {
            _sceneGame ??= SceneGame.Instance;
            EnsureWindows();
        }

        /// <summary>
        /// 관련 UI 윈도우(ItemBuy)를 캐싱합니다.
        /// </summary>
        private void EnsureWindows()
        {
            _sceneGame ??= SceneGame.Instance;
            if (_sceneGame == null) return;

            _uIWindowItemBuy ??=
                _sceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemBuy>(UIWindowConstants.WindowUid.ItemBuy);
        }

        /// <summary>
        /// StruckTableShop 데이터를 기반으로 초기화합니다.
        /// </summary>
        /// <param name="uiWindowShop">부모 상점 윈도우입니다.</param>
        /// <param name="slotIndex">슬롯 인덱스입니다.</param>
        /// <param name="struckTableShop">상점 테이블 데이터입니다.</param>
        public void Initialize(UIWindowShop uiWindowShop, int slotIndex, StruckTableShop struckTableShop)
        {
            Initialize(uiWindowShop, slotIndex, new ShopDisplayItem(struckTableShop));
        }

        /// <summary>
        /// ShopDisplayItem 데이터를 기반으로 초기화합니다.
        /// </summary>
        /// <param name="uiWindowShop">부모 상점 윈도우입니다.</param>
        /// <param name="slotIndex">슬롯 인덱스입니다.</param>
        /// <param name="shopDisplayItem">표시할 상점 아이템 데이터입니다.</param>
        public void Initialize(UIWindowShop uiWindowShop, int slotIndex, ShopDisplayItem shopDisplayItem)
        {
            ShowImageDiscount(false);
            _sceneGame ??= SceneGame.Instance;
            _playerData = _sceneGame.saveDataManager.Player;
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
        /// 이 상점 요소가 표시하는 슬롯을 설정합니다.
        /// </summary>
        /// <param name="uiSlot">생성된 UI 슬롯입니다.</param>
        public void SetSlot(UISlot uiSlot)
        {
            _uiSlot = uiSlot;
            // 맨 뒤로 배치
            _uiSlot.gameObject.transform.SetSiblingIndex(0);
        }
        
        /// <summary>
        /// 이 상점 요소가 표시하는 아이콘을 설정합니다.
        /// </summary>
        /// <param name="uiIcon">슬롯에 생성된 UI 아이콘입니다.</param>
        public void SetIcon(UIIcon uiIcon)
        {
            _uiIcon = uiIcon;
            ApplyAvailabilityVisual();
        }

        /// <summary>
        /// 테이블 데이터를 기반으로 아이템 정보를 갱신합니다.
        /// </summary>
        /// <param name="struckTableShop">상점 테이블 데이터입니다.</param>
        public void UpdateInfos(StruckTableShop struckTableShop)
        {
            UpdateInfos(new ShopDisplayItem(struckTableShop));
        }

        /// <summary>
        /// 표시용 아이템 데이터를 기반으로 UI를 갱신합니다.
        /// </summary>
        /// <param name="shopDisplayItem">갱신할 아이템 데이터입니다.</param>
        public void UpdateInfos(ShopDisplayItem shopDisplayItem)
        {
            _shopDisplayItem = shopDisplayItem;

            if (_shopDisplayItem == null || _shopDisplayItem.Source == null || _shopDisplayItem.IsEmpty)
            {
                GcLogger.LogError("shop 테이블에 정보가 없습니다.");
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
                textName.text = ItemDisplayNameUtility.GetDisplayName(info);
            }

            if (textPrice != null)
            {
                textPrice.text = $"{_shopDisplayItem.CurrencyType} {_shopDisplayItem.CurrencyValue}";
            }

            RefreshAvailability();
        }

        /// <summary>
        /// 현재 아이템의 구매 가능 여부를 갱신하고 UI 상태를 반영합니다.
        /// </summary>
        public void RefreshAvailability()
        {
            if (_shopDisplayItem == null) return;

            if (_uiWindowShop != null)
            {
                bool isBuyable = _uiWindowShop.CanBuy(_shopDisplayItem, out var disabledReason);
                _shopDisplayItem.SetAvailability(isBuyable, disabledReason);
            }
            
            ShowImageDiscount(_shopDisplayItem.HasDiscount);

            ApplyAvailabilityVisual();

            RefreshPurchaseButton();
        }

        /// <summary>
        /// 현재 상품의 구매 제한 상태와 플레이어 재화 보유 여부를 구매 버튼에 반영합니다.
        /// </summary>
        public void RefreshPurchaseButton()
        {
            if (!buttonBuy)
            {
                return;
            }

            buttonBuy.gameObject.SetActive(true);
            buttonBuy.interactable = _shopDisplayItem != null &&
                                     _shopDisplayItem.IsBuyable &&
                                     _uiWindowShop != null &&
                                     _uiWindowShop.CanAfford(_shopDisplayItem);
        }

        /// <summary>
        /// 구매 가능 여부와 품절 표시 방식에 따라 아이콘 표시 상태를 갱신합니다.
        /// </summary>
        private void ApplyAvailabilityVisual()
        {
            if (_uiIcon == null || _shopDisplayItem == null) return;

            bool dimIcon = !_shopDisplayItem.IsBuyable &&
                           _shopDisplayItem.SoldOutDisplayType == ShopSoldOutDisplayType.Disable;

            _uiIcon.SetAlpha(dimIcon ? soldOutIconAlpha : normalIconAlpha);
        }

        /// <summary>
        /// 구매 버튼 클릭 시 호출되며, 아이템 구매 로직을 수행합니다.
        /// </summary>
        public void OnClickBuy()
        {
            if (_shopDisplayItem == null || _shopDisplayItem.Source == null) return;

            string disabledReason = String.Empty;
            if (!_shopDisplayItem.IsBuyable || !_uiWindowShop.CanBuy(_shopDisplayItem, out disabledReason))
            {
                if (!string.IsNullOrEmpty(disabledReason))
                    _sceneGame.systemMessageManager.ShowMessageWarning(disabledReason);
                else
                    _sceneGame.systemMessageManager.ShowMessageWarning("Shop_CannotBuyItem");

                return;
            }

            // 다중 구매 처리
            // 즉시 사용 정책은 구매 결과와 사용 결과를 1:1로 맞추기 위해 단일 구매로 처리합니다.
            if (_shopDisplayItem.MaxBuyCount > 1 && _shopDisplayItem.BuyUsePolicy != ShopBuyUsePolicy.UseImmediately)
            {
                int count = (int)_playerData.GetPossibleBuyCount(
                    _shopDisplayItem.CurrencyType,
                    _shopDisplayItem.CurrencyValue);

                if (count <= 0)
                {
                    _sceneGame.systemMessageManager.ShowWarningCurrency(_shopDisplayItem.CurrencyType);
                    return;
                }

                if (_shopDisplayItem.MaxBuyCount > 0 && count > _shopDisplayItem.MaxBuyCount)
                    count = _shopDisplayItem.MaxBuyCount;

                var info = _tableItem.GetDataByUid(_shopDisplayItem.ItemUid);
                if (info != null && count > info.MaxOverlayCount)
                    count = info.MaxOverlayCount;

                var shopPurchaseData = SceneGame.Instance?.saveDataManager?.ShopPurchase;
                if (shopPurchaseData != null)
                {
                    int remainingCount = shopPurchaseData.GetRemainingCount(_shopDisplayItem);
                    if (remainingCount <= 0)
                    {
                        _sceneGame.systemMessageManager.ShowMessageWarning("Shop_SoldOut");
                        RefreshAvailability();
                        return;
                    }

                    if (count > remainingCount)
                        count = remainingCount;
                }

                _uIWindowItemBuy?.SetPriceInfo(_shopDisplayItem);
                _sceneGame.uIWindowManager.RegisterIcon(
                    _uiWindowShop.uid,
                    _slotIndex,
                    UIWindowConstants.WindowUid.ItemBuy,
                    count);
            }
            // 단일 구매
            else
            {
                _sceneGame.BuyItem(_shopDisplayItem);
            }
        }

        /// <summary>
        /// 마우스 오버 시 아이템을 선택 상태로 변경합니다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!usePointerEnterEvent) return;
            SetSelected(true);
        }

        /// <summary>
        /// 사용자 왼쪽 포인터 클릭 시 아이템을 선택하고 수동 클릭 사운드를 요청합니다.
        /// 초기 자동 선택은 이 경로를 거치지 않으므로 클릭 사운드가 재생되지 않습니다.
        /// </summary>
        /// <param name="eventData">포인터 버튼 정보를 포함한 이벤트 데이터입니다.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!usePointerClickEvent ||
                eventData == null ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            SetSelected(true);
            _clickSoundEventBroadcaster?.TryDispatchManualClick();
        }

        /// <summary>
        /// 마우스 아웃 시 아이템 정보 창을 숨깁니다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!usePointerExitEvent) return;
            _uiWindowShop?.HideItemInfo();
        }

        /// <summary>
        /// 아이콘 위치를 반환합니다.
        /// </summary>
        /// <returns>로컬 좌표 기준 아이콘 위치입니다.</returns>
        public Vector3 GetIconPosition() => iconPosition;

        /// <summary>
        /// 선택 상태를 설정하고 관련 UI를 갱신합니다.
        /// </summary>
        /// <param name="value">선택 여부입니다.</param>
        public void SetSelected(bool value)
        {
            SetColorImageDiscount(colorNotSelected);
            if (value)
            {
                _uiWindowShop.SetSelectItem(this);

                EnsureWindows();

                if (_shopDisplayItem != null)
                {
                    _uiWindowShop?.TryShowItemInfo(this);
                    SetColorImageDiscount(colorSelected);
                }
            }

            if (_uiSlot)
            {
                _uiSlot.SetColor(value ? colorSelected : colorNotSelected);
            }
        }

        /// <summary>
        /// 현재 아이템의 가격 정보를 반환합니다.
        /// </summary>
        /// <returns>(재화 타입, 가격) 튜플입니다.</returns>
        public (CurrencyConstants.Type, int) GetPrice()
        {
            if (_shopDisplayItem == null) return (CurrencyConstants.Type.None, 0);
            return (_shopDisplayItem.CurrencyType, _shopDisplayItem.CurrencyValue);
        }

        /// <summary>
        /// 현재 표시 중인 아이템 데이터를 반환합니다.
        /// </summary>
        public ShopDisplayItem GetDisplayItem() => _shopDisplayItem;

        private void ShowImageDiscount(bool value)
        {
            if (!imageDiscount) return;
            imageDiscount.gameObject.SetActive(value);
        }

        private void SetColorImageDiscount(Color color)
        {
            if (!imageDiscount) return;
            imageDiscount.color = color;
        }
    }
}
