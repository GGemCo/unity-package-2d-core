using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
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
        
        [Header("선택 시 색상")]
        [SerializeField] private Color colorSelected = Color.white;
        [SerializeField] private Color colorNotSelected = Color.white;
        
        [Header("할인 이미지 아이콘")]
        [SerializeField] private Image imageDiscount;

        [Header("상품 상태 이미지")]
        [Tooltip("실제 구매 재고가 모두 소진되었을 때 아이템 위에 표시할 이미지")]
        [SerializeField] private Image imageSoldOut;

        [Tooltip("품절 이미지와 슬롯에 적용할 투명도")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("soldOutImageAlpha")]
        [SerializeField] private float soldOutAlpha = 1f;

        [Tooltip("추첨 결과 판매할 아이템이 없는 슬롯에 표시할 이미지")]
        [SerializeField] private Image imageEmpty;

        [Tooltip("빈 상품 이미지 투명도")]
        [Range(0f, 1f)]
        [SerializeField] private float emptyImageAlpha = 1f;

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
            // SetIcon이 SetSlot보다 먼저 호출되므로 슬롯 연결 직후 현재 품절 상태를 다시 반영합니다.
            ApplyAvailabilityVisual();
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

            if (_shopDisplayItem == null || _shopDisplayItem.Source == null)
            {
                GcLogger.LogError("shop 테이블에 정보가 없습니다.");
                ClearProductPresentation();
                return;
            }

            // ItemUid가 0 이하인 행은 확률 추첨에 참여하는 정상적인 빈 상품입니다.
            // 아이템 테이블을 조회하지 않고 빈 슬롯 전용 표시로 갱신합니다.
            if (_shopDisplayItem.IsEmpty)
            {
                ClearProductTexts();
                ShowImageDiscount(false);
                ApplyAvailabilityVisual();
                ApplyPurchaseButtonState(false);
                return;
            }

            var info = _tableItem.GetDataByUid(_shopDisplayItem.ItemUid);
            if (info == null)
            {
                GcLogger.LogError($"item 테이블에 정보가 없습니다. item uid: {_shopDisplayItem.ItemUid}");
                ClearProductPresentation();
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

            bool isBuyable = RefreshAvailabilityState();
            
            ShowImageDiscount(_shopDisplayItem.HasDiscount);

            ApplyAvailabilityVisual();

            ApplyPurchaseButtonState(isBuyable);
        }

        /// <summary>
        /// 현재 상품의 구매 제한 상태와 플레이어 재화 보유 여부를 구매 버튼에 반영합니다.
        /// </summary>
        public void RefreshPurchaseButton()
        {
            bool isBuyable = RefreshAvailabilityState();
            ApplyPurchaseButtonState(isBuyable);
        }

        /// <summary>
        /// 현재 상품의 구매 가능 상태를 최신 런타임 조건으로 다시 계산하고 표시 데이터에 반영합니다.
        /// </summary>
        /// <returns>현재 시점에 상품을 구매할 수 있으면 true입니다.</returns>
        private bool RefreshAvailabilityState()
        {
            if (_shopDisplayItem == null || _uiWindowShop == null)
            {
                return false;
            }

            bool isBuyable = _uiWindowShop.CanBuy(_shopDisplayItem, out string disabledReason);
            _shopDisplayItem.SetAvailability(isBuyable, disabledReason);
            return isBuyable;
        }

        /// <summary>
        /// 계산된 구매 가능 상태와 재화 보유 여부를 현재 상품의 구매 버튼에 반영합니다.
        /// </summary>
        /// <param name="isBuyable">최신 런타임 조건으로 계산한 구매 가능 여부입니다.</param>
        private void ApplyPurchaseButtonState(bool isBuyable)
        {
            if (!buttonBuy)
            {
                return;
            }

            bool hasProduct = _shopDisplayItem != null && !_shopDisplayItem.IsEmpty;
            buttonBuy.gameObject.SetActive(hasProduct);
            buttonBuy.interactable = _shopDisplayItem != null &&
                                     hasProduct &&
                                     isBuyable &&
                                     _uiWindowShop != null &&
                                     _uiWindowShop.CanAfford(_shopDisplayItem);
        }

        /// <summary>
        /// 구매 가능 여부와 품절 표시 방식에 따라 상태 이미지와 슬롯 투명도를 갱신합니다.
        /// UIIcon에는 Alpha를 직접 적용하지 않고 슬롯의 CanvasGroup을 통해 일관된 표시 상태를 유지합니다.
        /// </summary>
        private void ApplyAvailabilityVisual()
        {
            bool isEmpty = _shopDisplayItem == null || _shopDisplayItem.IsEmpty;
            bool isSoldOut = !isEmpty &&
                             _shopDisplayItem.IsSoldOut &&
                             _shopDisplayItem.SoldOutDisplayType == ShopSoldOutDisplayType.Disable;

            SetStateImage(imageEmpty, isEmpty, emptyImageAlpha);
            SetStateImage(imageSoldOut, isSoldOut, soldOutAlpha);

            if (_uiSlot != null)
            {
                _uiSlot.SetAlpha(isSoldOut ? soldOutAlpha : 1f);
            }

            if (_uiIcon != null)
            {
                _uiIcon.gameObject.SetActive(!isEmpty);
            }
        }

        /// <summary>
        /// 구매 버튼 클릭 시 호출되며, 아이템 구매 로직을 수행합니다.
        /// </summary>
        public void OnClickBuy()
        {
            if (_shopDisplayItem == null || _shopDisplayItem.Source == null || _shopDisplayItem.IsEmpty) return;

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
                eventData.button != PointerEventData.InputButton.Left ||
                _shopDisplayItem == null ||
                _shopDisplayItem.IsEmpty)
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
                if (_shopDisplayItem == null || _shopDisplayItem.IsEmpty)
                {
                    _uiWindowShop?.HideItemInfo();
                    return;
                }

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

        /// <summary>
        /// 상품 정보 조회에 실패했을 때 이전 텍스트와 상태 이미지가 남지 않도록 표시를 초기화합니다.
        /// </summary>
        private void ClearProductPresentation()
        {
            ClearProductTexts();
            ShowImageDiscount(false);
            SetStateImage(imageEmpty, false, emptyImageAlpha);
            SetStateImage(imageSoldOut, false, soldOutAlpha);
            if (_uiSlot != null)
            {
                _uiSlot.SetAlpha(1f);
            }
            ApplyPurchaseButtonState(false);
        }

        /// <summary>
        /// 현재 상품의 이름과 가격 텍스트를 비웁니다.
        /// </summary>
        private void ClearProductTexts()
        {
            if (textName)
            {
                textName.text = string.Empty;
            }

            if (textPrice)
            {
                textPrice.text = string.Empty;
            }
        }

        /// <summary>
        /// 상태 이미지의 표시 여부와 투명도를 함께 적용합니다.
        /// 프리팹이 재사용되더라도 이전 상품의 표시 상태가 남지 않도록 항상 활성 상태를 갱신합니다.
        /// </summary>
        /// <param name="image">표시 상태를 변경할 이미지입니다.</param>
        /// <param name="show">이미지를 표시할지 여부입니다.</param>
        /// <param name="alpha">이미지에 적용할 투명도입니다.</param>
        private static void SetStateImage(Image image, bool show, float alpha)
        {
            if (!image)
            {
                return;
            }

            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
            // 상태 오버레이가 부모 요소의 선택 및 구매 포인터 이벤트를 가로채지 않도록 합니다.
            image.raycastTarget = false;
            image.gameObject.SetActive(show);
        }
    }
}
