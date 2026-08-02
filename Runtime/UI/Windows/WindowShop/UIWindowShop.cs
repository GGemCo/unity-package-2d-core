using System.Collections;
using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 구매 윈도우
    /// </summary>
    public class UIWindowShop : UIWindow
    {
        private const string ExternalItemInfoWindowKey = "Shop.ItemInfo";
        private const string NotEnoughStockTextKey = "Text_Not_Enough_Stock";
        private const string CannotBuyMoreTextKey = "Text_Cannot_Buy_More";

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("상점 Element 프리팹")]
        [SerializeField] private GameObject prefabUIElementShop;
        [Tooltip("상점에서 사용할 아이템 정보 표시 윈도우")]
        [SerializeField] private UIWindowItemInfo overrideUiWindowItemInfo;
        [Tooltip("상점 오브젝트를 기준으로 아이템 정보 윈도우 위치")] [SerializeField]
        private UIWindowManager.ExternalWindowInsertMode overrideUiWindowItemInfoInsertMode =
            UIWindowManager.ExternalWindowInsertMode.After;
        [Tooltip("구매하기 버튼")]
        [SerializeField] private Button buttonBuy;
        
        [Tooltip("가격 텍스트")]
        [SerializeField] private TextMeshProUGUI textPrice;
        [Tooltip("재화가 부족하지 않을 때 적용할 스타일 키")]
        [SerializeField] private string styleKeyPriceNormal;
        [Tooltip("재화가 부족할 때 적용할 스타일 키")]
        [SerializeField] private string styleKeyPriceLack;
        [Tooltip("할인 적용 시 기존 판매 금액에 적용할 TMP color 태그 값")]
        [SerializeField] private string styleKeyPriceDiscount;

        [Tooltip("아이템을 선택했을 때 보여줄 이펙트")]
        [SerializeField] private VfxEffectUI vfxEffectUISelected;
        
        [Header("할인/재고없음")]
        [Tooltip("보여질 말풍선")]
        [SerializeField] private GameObject panelDiscountTalkBubble;
        [Tooltip("보여질 말풍선의 텍스트")]
        [SerializeField] private TextMeshProUGUI textDiscountTalkBubble;
        [Tooltip("아이템 아이콘 기준으로 어느곳에 위치할 것인지")]
        [SerializeField] private Vector3 offsetDiscountTalkBubble;
        [Tooltip("캐릭터 썸네일 이미지")]
        [SerializeField] private GameObject imageThumbnailCharacter;
        [Tooltip("캐릭터 썸네일 이미지 위치")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacter;
        
        private Coroutine _coRefreshSelectedVfx;
        
        private ShopResolver _shopResolver;
        private ShopPromotionService _shopPromotionService;
        private ShopAvailabilityService _shopAvailabilityService;
        private readonly Dictionary<int, UIElementShop> _uiElementShops = new Dictionary<int, UIElementShop>();
        private int _currentShopUid;

        private UIElementShop _selectedElementShop;
        private UIWindowItemInfo _uiWindowItemInfo;
        
        protected override void Awake()
        {
            SetDiscountTalkBubble(false, string.Empty, Vector3.zero);
            _selectedElementShop = null;
            _uiElementShops.Clear();
            uid = UIWindowConstants.WindowUid.Shop;
            if (TableLoaderManager.Instance == null) return;
            _shopAvailabilityService = ShopAvailabilityService.Instance;
            _shopResolver = new ShopResolver(TableLoaderManager.Instance.TableShopItem, _shopAvailabilityService, TableLoaderManager.Instance.TableShop);
            _shopPromotionService = new ShopPromotionService(TableLoaderManager.Instance.TableShopPromotion);
            base.Awake();
            if (vfxEffectUISelected)
            {
                vfxEffectUISelected.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (buttonBuy)
                buttonBuy.onClick.AddListener(OnClickBuy);
            if (_shopAvailabilityService != null)
                _shopAvailabilityService.Changed += OnShopAvailabilityChanged;
            GameEventManager.ItemPurchasedEvent += OnItemPurchased;

            if (_currentShopUid > 0)
            {
                RefreshShopAvailabilityPresentation();
            }
        }

        /// <summary>
        /// 상점 비활성화 시 연결된 이벤트와 선택 상태를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            if (buttonBuy)
                buttonBuy.onClick.RemoveAllListeners();
            if (_shopAvailabilityService != null)
                _shopAvailabilityService.Changed -= OnShopAvailabilityChanged;
            GameEventManager.ItemPurchasedEvent -= OnItemPurchased;
            if (_selectedElementShop)
                _selectedElementShop.SetSelected(false);
            HideItemInfo();
        }

        /// <summary>
        /// 상점에서 사용하는 연계 윈도우와 플레이어 상태 구독을 초기화합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            ResolveItemInfoWindow();

            var playerData = SceneGame?.saveDataManager?.Player;
            if (playerData == null) return;

            playerData.OnCurrentGoldChanged()
                .CombineLatest(playerData.OnCurrentSilverChanged(), (_, _) => Unit.Default)
                .Subscribe(_ => RefreshCurrencyDependentUi())
                .AddTo(this);

            Player player = SceneGame?.player != null
                ? SceneGame.player.GetComponent<Player>()
                : null;
            if (player != null)
            {
                // 아이템 임시 HP가 피해로 감소하면 충전형 상품을 즉시 다시 구매할 수 있도록 상태를 갱신합니다.
                player.CurrentHpTemp
                    .DistinctUntilChanged()
                    .Skip(1)
                    .Subscribe(_ => RefreshShopAvailabilityPresentation())
                    .AddTo(this);
            }
        }

        /// <summary>
        /// 상점에서 사용할 아이템 정보창을 결정합니다.
        /// override 가 연결되어 있으면 해당 창을 우선 사용하고, 없으면 공용 창을 사용합니다.
        /// </summary>
        private void ResolveItemInfoWindow()
        {
            _uiWindowItemInfo =
                SceneGame?.uIWindowManager?.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid.ItemInfo);

            if (overrideUiWindowItemInfo == null)
            {
                return;
            }

            _uiWindowItemInfo = overrideUiWindowItemInfo;
            _uiWindowItemInfo.Show(false);
            RegisterOverrideItemInfoWindowOrder();
        }

        /// <summary>
        /// 상점 전용 아이템 정보창을 UIWindowManager 정렬 목록에 등록합니다.
        /// 같은 ItemInfo Uid 를 여러 창이 공유하므로 windowKeys 대신 외부 윈도우로 붙여 순서를 보존합니다.
        /// </summary>
        private void RegisterOverrideItemInfoWindowOrder()
        {
            if (SceneGame == null || SceneGame.uIWindowManager == null || overrideUiWindowItemInfo == null)
            {
                return;
            }

            SceneGame.uIWindowManager.RegisterExternalWindow(
                ExternalItemInfoWindowKey,
                overrideUiWindowItemInfo,
                UIWindowConstants.WindowUid.Shop,
                overrideUiWindowItemInfoInsertMode);
        }

        /// <summary>
        /// 상점 문맥에서 테이블 연동 윈도우 Uid 를 실제 윈도우 오브젝트로 해석합니다.
        /// ItemInfo 는 override 가 연결되어 있으면 해당 전용 창을 우선 반환합니다.
        /// </summary>
        /// <param name="windowUid">해석할 연결 윈도우 Uid 입니다.</param>
        /// <returns>상점 문맥에서 사용해야 하는 실제 윈도우 오브젝트입니다.</returns>
        protected override UIWindow ResolveLinkedWindow(UIWindowConstants.WindowUid windowUid)
        {
            if (windowUid != UIWindowConstants.WindowUid.ItemInfo)
            {
                return base.ResolveLinkedWindow(windowUid);
            }

            if (_uiWindowItemInfo == null)
            {
                ResolveItemInfoWindow();
            }

            return _uiWindowItemInfo != null ? _uiWindowItemInfo : base.ResolveLinkedWindow(windowUid);
        }

        /// <summary>
        /// 상점 uid 로 ui element shop 정보 셋팅하기
        /// </summary>
        public void SetInfoByShopUid(int shopUid)
        {
            SetInfoByShopUid(shopUid, false, false);
        }

        /// <summary>
        /// 상점 uid 로 ui element shop 정보 셋팅하기
        /// </summary>
        public void SetInfoByShopUid(int shopUid, bool forceRefresh, bool reroll)
        {
            SetInfoByShopUid(shopUid, forceRefresh, reroll, true);
        }

        private void SetInfoByShopUid(int shopUid, bool forceRefresh, bool reroll, bool countExposure)
        {
            // 같은 상점을 열었으면 업데이트 하지 않는다
            if (!forceRefresh && !reroll && _currentShopUid > 0 && _currentShopUid == shopUid)
            {
                RefreshShopAvailabilityPresentation();
                return;
            }

            ClearShopElements();
            _currentShopUid = shopUid;
            
            if (AddressableLoaderSettings.Instance == null || containerIcon == null) return;
            if (prefabUIElementShop == null)
            {
                GcLogger.LogError("UIElementShop 프리팹이 없습니다.");
                return;
            }
            if (shopUid <= 0) return;
            var datas = _shopResolver?.Resolve(shopUid, reroll);
            if (datas == null)
            {
                GcLogger.LogError("shop 테이블에 정보가 없습니다. shop Uid: " + shopUid);
                return;
            }
            _shopPromotionService?.ApplyPromotions(datas, countExposure);
            if (datas.Count <= 0) return;
            int maxSlotIndex = 0;
            foreach (var data in datas)
            {
                if (data != null && data.SlotIndex > maxSlotIndex)
                {
                    maxSlotIndex = data.SlotIndex;
                }
            }

            maxCountIcon = maxSlotIndex + 1;
            slots = new GameObject[maxCountIcon];
            icons = new GameObject[maxCountIcon];
            
            GameObject iconItem = iconPrefab != null ? iconPrefab : ConfigResources.IconItem.Load();
            GameObject slot = slotPrefab != null ? slotPrefab : ConfigResources.Slot.Load();
            
            if (iconItem == null) return;

            foreach (var info in datas)
            {
                if (info == null) continue;
                int index = info.SlotIndex;
                if (index < 0 || index >= maxCountIcon) continue;

                GameObject parent = gameObject;
                UIElementShop uiElementShop = null;
                // UI Element 프리팹이 있으면 만든다.
                if (prefabUIElementShop != null)
                {
                    parent = Instantiate(prefabUIElementShop, containerIcon.gameObject.transform);
                    if (parent == null) continue;
                    uiElementShop = parent.GetComponent<UIElementShop>();
                    if (uiElementShop == null) continue;
                    uiElementShop.Initialize(this, index, info);
                    _uiElementShops.TryAdd(index, uiElementShop);
                }

                // 빈 상품도 UIElementShop은 생성하여 전용 이미지를 표시합니다.
                // 실제 아이템이 없으므로 UISlot과 UIIcon 생성 및 아이템 테이블 조회는 생략합니다.
                if (info.IsEmpty)
                {
                    continue;
                }

                GameObject slotObject = Instantiate(slot, parent.transform);
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
                // 상점의 품절 Alpha는 UISlot.SetAlpha()로 적용하므로 상점 슬롯에만 CanvasGroup 사용을 활성화합니다.
                uiSlot.useCanvasGroup = true;
                uiSlot.Initialize(this, uid, index, slotSize);
                SetPositionUiSlot(uiSlot, index);
                slots[index] = slotObject;
                
                GameObject icon = Instantiate(iconItem, slotObject.transform);
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                // deactivate 상태에서는 awake 가 호출되지 않는다.
                uiIcon.Initialize(this, uid, index, index, iconSize, slotSize);
                // count  1로 초기화
                uiIcon.ChangeInfoByUid(info.ItemUid, 1);
                // element 에서 마우스 이벤트 처리
                uiIcon.SetRaycastTarget(false);
                uiIcon.RemoveLockImage();
                uiElementShop?.SetIcon(uiIcon);
                uiElementShop?.SetSlot(uiSlot);
                
                icons[index] = icon;
            }
            if (gameObject.activeSelf)
            {
                SelectFirstElement();
            }
            // GcLogger.Log($"풀 확장: {amount}개 아이템 추가 (총 {poolDropItem.Count}개)");
        }
        /// <summary>
        /// 슬롯 위치 정해주기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="index"></param>
        private void SetPositionUiSlot(UISlot slot, int index)
        {
            if (!_uiElementShops.TryGetValue(index, out var uiElementSkill))
            {
                return;
            }
            if (uiElementSkill == null) return;
            Vector3 position = uiElementSkill.GetIconPosition();
            if (position == Vector3.zero) return;
            slot.transform.localPosition = position;
        }
        /// <summary>
        /// npc uid 정보로 아이콘 셋팅하기
        /// 상점이 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            if (icons == null) return;
            foreach (var pair in _uiElementShops)
            {
                int index = pair.Key;
                if (index >= icons.Length) continue;
                var icon = icons[index];
                if (icon == null) continue;
                UIIconItem uiIcon = icon.GetComponent<UIIconItem>();
                if (uiIcon == null) continue;

                var data = pair.Value.GetDisplayItem();
                if (data == null) continue;
                
                var info = TableLoaderManager.Instance.GetItemData(data.ItemUid);
                if (info == null) continue;
                uiIcon.ChangeInfoByUid(info.Uid, 1);
                pair.Value.UpdateInfos(data);
            }
        }

        /// <summary>
        /// 상점 Uid 로 윈도우 오픈하기
        /// </summary>
        /// <param name="shopUid"></param>
        /// <param name="forceRefresh"></param>
        /// <param name="reroll"></param>
        public void ShowByUid(int shopUid, bool forceRefresh, bool reroll)
        {
            if (shopUid <= 0) return;
            SetInfoByShopUid(shopUid, forceRefresh, reroll);
            Show(true);
        }

        /// <summary>
        /// 지정한 상점 요소를 현재 선택 상품으로 설정하고 연계 UI를 갱신합니다.
        /// </summary>
        /// <param name="uiElementShop">선택할 상점 요소입니다.</param>
        public void SetSelectItem(UIElementShop uiElementShop)
        {
            ShopDisplayItem displayItem = uiElementShop != null
                ? uiElementShop.GetDisplayItem()
                : null;
            if (displayItem == null || displayItem.IsEmpty)
            {
                return;
            }

            if (_selectedElementShop == uiElementShop)
            {
                RefreshShopAvailabilityPresentation();
                return;
            }

            if (_selectedElementShop != null)
            {
                _selectedElementShop.SetSelected(false);
            }
            _selectedElementShop = uiElementShop;
            
            RefreshSelectedPurchaseUi();
            
            if (_coRefreshSelectedVfx != null)
            {
                StopCoroutine(_coRefreshSelectedVfx);
                _coRefreshSelectedVfx = null;
            }

            _coRefreshSelectedVfx = StartCoroutine(CoRefreshSelectedVfxPosition());
        }

        /// <summary>
        /// 지정한 상점 요소의 아이템 정보를 현재 선택된 정보창에 표시합니다.
        /// </summary>
        /// <param name="uiElementShop">아이템 정보를 표시할 상점 요소입니다.</param>
        /// <returns>표시에 성공하면 true, 아니면 false 입니다.</returns>
        public bool TryShowItemInfo(UIElementShop uiElementShop)
        {
            if (uiElementShop == null)
            {
                return false;
            }

            if (_uiWindowItemInfo == null)
            {
                ResolveItemInfoWindow();
            }

            if (_uiWindowItemInfo == null)
            {
                return false;
            }

            var displayItem = uiElementShop.GetDisplayItem();
            if (displayItem == null || displayItem.IsEmpty)
            {
                return false;
            }

            Vector2 iconSlotSize = containerIcon != null ? containerIcon.cellSize : slotSize;
            _uiWindowItemInfo.SetItemInfo(new UIWindowItemInfoRequest
            {
                ItemUid = displayItem.ItemUid,
                InstanceId = 0,
                AnchorObject = uiElementShop.gameObject,
                PositionType = UIWindowItemInfo.PositionType.Fixed,
                IconSlotSize = iconSlotSize,
            });
            return true;
        }

        /// <summary>
        /// 상점에서 사용 중인 아이템 정보창을 숨깁니다.
        /// </summary>
        public void HideItemInfo()
        {
            _uiWindowItemInfo?.Show(false);
        }

        private IEnumerator CoRefreshSelectedVfxPosition()
        {
            if (_selectedElementShop == null)
            {
                yield break;
            }

            // 레이아웃 즉시 갱신
            Canvas.ForceUpdateCanvases();

            if (containerIcon != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerIcon.GetComponent<RectTransform>());
            }

            // ScrollRect / Layout / Canvas 최종 반영 대기
            yield return null;
            yield return new WaitForEndOfFrame();

            UpdateDiscountTalkBubble();
            
            if (_selectedElementShop == null || vfxEffectUISelected == null)
            {
                yield break;
            }

            vfxEffectUISelected.gameObject.SetActive(true);
            vfxEffectUISelected.PlayAnimation("start", forceReset: true);
            vfxEffectUISelected.transform.position = _selectedElementShop.transform.position;

            _coRefreshSelectedVfx = null;
        }

        /// <summary>
        /// 현재 선택된 상품의 가격 표시와 상단 구매 버튼 상태를 함께 갱신합니다.
        /// </summary>
        private void RefreshSelectedPurchaseUi()
        {
            UpdatePriceText();
            UpdateButtonBuy();
        }

        /// <summary>
        /// 플레이어 재화 변경에 따라 가격 표시와 모든 구매 버튼 상태를 갱신합니다.
        /// 재고 및 외부 구매 제한 상태는 변경하지 않고 재화 보유 여부만 다시 반영합니다.
        /// </summary>
        private void RefreshCurrencyDependentUi()
        {
            RefreshSelectedPurchaseUi();

            foreach (var pair in _uiElementShops)
            {
                pair.Value?.RefreshPurchaseButton();
            }
        }

        /// <summary>
        /// 현재 선택된 상품의 구매 가능 여부와 재화 보유 여부를 상단 구매 버튼에 반영합니다.
        /// </summary>
        private void UpdateButtonBuy()
        {
            bool isBuyable = RefreshSelectedItemAvailability(out ShopDisplayItem displayItem);
            if (!buttonBuy)
            {
                return;
            }

            buttonBuy.interactable = isBuyable &&
                                     displayItem != null &&
                                     CanAfford(displayItem);
        }

        /// <summary>
        /// 현재 선택된 상품의 구매 가능 상태를 최신 런타임 조건으로 다시 계산하고 표시 데이터에 반영합니다.
        /// 버튼은 이 결과를 사용하여 캐시된 과거 상태로 활성화되는 문제를 방지합니다.
        /// </summary>
        /// <param name="displayItem">현재 선택된 상점 상품 표시 데이터입니다.</param>
        /// <returns>현재 시점에 상품을 구매할 수 있으면 true입니다.</returns>
        private bool RefreshSelectedItemAvailability(out ShopDisplayItem displayItem)
        {
            displayItem = _selectedElementShop != null
                ? _selectedElementShop.GetDisplayItem()
                : null;
            if (displayItem == null)
            {
                return false;
            }

            bool isBuyable = CanBuy(displayItem, out string disabledReason);
            displayItem.SetAvailability(isBuyable, disabledReason);
            return isBuyable;
        }

        private void UpdatePriceText()
        {
            if (!textPrice) return;

            var displayItem = _selectedElementShop != null
                ? _selectedElementShop.GetDisplayItem()
                : null;
            if (displayItem == null || displayItem.IsEmpty)
            {
                textPrice.text = string.Empty;
                return;
            }

            var playerCurrency = GetPlayerCurrencyValue(displayItem.CurrencyType);
            var itemPrice = displayItem.CurrencyValue;
            var key = playerCurrency < itemPrice ? styleKeyPriceLack : styleKeyPriceNormal;

            if (displayItem.HasDiscount)
            {
                textPrice.text = string.Format(
                    "( <style={3}>{0}</style> / <style={4}>{1}</style> {2} )",
                    playerCurrency,
                    displayItem.BaseCurrencyValue,
                    itemPrice,
                    key,
                    styleKeyPriceDiscount);

                return;
            }

            textPrice.text = string.Format("( <style={2}>{0}</style> / {1} )", playerCurrency, itemPrice, key);
        }

        private void UpdateDiscountTalkBubble()
        {
            SetDiscountTalkBubble(false, string.Empty, Vector3.zero);

            if (_selectedElementShop == null) return;
            
            var displayItem = _selectedElementShop.GetDisplayItem();
            if (displayItem == null) return;
            
            // 구매 불가 상태에서는 할인 안내보다 구매 제한 사유를 우선해서 표시합니다.
            if (!displayItem.IsBuyable)
            {
                string localizationKey = ResolveUnavailableTalkBubbleKey(displayItem);
                string text = LocalizationManager.Instance.GetUIWindowShopByKey(localizationKey);
                SetDiscountTalkBubble(true, text, _selectedElementShop.transform.position);
            }
            else if (displayItem.HasDiscount)
            {
                var itemPrice = displayItem.CurrencyValue;
                var text = string.Format(LocalizationManager.Instance.GetUIWindowShopByKey("Text_Discount"), itemPrice);
                SetDiscountTalkBubble(true, text, _selectedElementShop.transform.position);
            }
        }

        /// <summary>
        /// 상품의 구매 불가 사유에 맞는 상점 말풍선 Localization 키를 반환합니다.
        /// 실제 구매 재고가 소진된 경우에만 재고 부족 문구를 사용하고, 그 외 상태 제한은 추가 구매 불가로 표시합니다.
        /// </summary>
        /// <param name="displayItem">구매 불가 사유를 확인할 상점 상품입니다.</param>
        /// <returns>상점 말풍선에서 사용할 Localization 키입니다.</returns>
        private static string ResolveUnavailableTalkBubbleKey(ShopDisplayItem displayItem)
        {
            return displayItem != null && displayItem.DisabledReason == ShopAvailabilityReason.SoldOut
                ? NotEnoughStockTextKey
                : CannotBuyMoreTextKey;
        }

        /// <summary>
        /// 플레이어가 현재 보유한 지정 재화의 수량을 반환합니다.
        /// </summary>
        /// <param name="currencyType">조회할 재화 종류입니다.</param>
        /// <returns>현재 보유 수량이며, 지원하지 않는 재화이거나 플레이어 데이터가 없으면 0입니다.</returns>
        private long GetPlayerCurrencyValue(CurrencyConstants.Type currencyType)
        {
            var player = SceneGame?.saveDataManager?.Player;
            if (player == null) return 0;

            return currencyType switch
            {
                CurrencyConstants.Type.Gold => player.CurrentGold,
                CurrencyConstants.Type.Silver => player.CurrentSilver,
                _ => 0
            };
        }

        /// <summary>
        /// 플레이어가 지정 상품 한 개를 구매할 수 있는 재화를 보유하고 있는지 확인합니다.
        /// </summary>
        /// <param name="item">재화 보유 여부를 확인할 상점 상품입니다.</param>
        /// <returns>상품 가격 이상의 재화를 보유하고 있으면 true입니다.</returns>
        public bool CanAfford(ShopDisplayItem item)
        {
            if (item == null || item.IsEmpty)
            {
                return false;
            }

            // 가격이 0 이하인 무료 상품은 재화 종류와 관계없이 구매 가능한 것으로 처리합니다.
            if (item.CurrencyValue <= 0)
            {
                return true;
            }

            return GetPlayerCurrencyValue(item.CurrencyType) >= item.CurrencyValue;
        }

        private void OnClickBuy()
        {
            if (!_selectedElementShop) return;
            _selectedElementShop.OnClickBuy();
        }

        public bool CanBuy(ShopDisplayItem item, out string disabledReason)
        {
            disabledReason = null;
            return _shopAvailabilityService == null || _shopAvailabilityService.CanBuy(item, out disabledReason);
        }

        public void RefreshCurrentShop(bool reroll = false)
        {
            if (_currentShopUid <= 0) return;
            SetInfoByShopUid(_currentShopUid, true, reroll);
        }

        public void ClearShopRoll(int shopUid)
        {
            _shopResolver?.ClearRoll(shopUid);
        }

        public bool RestockShopItem(ShopDisplayItem item)
        {
            return SceneGame.saveDataManager?.ShopPurchase?.ClearBoughtCount(item) == true;
        }

        public bool RestockShopItem(int shopItemUid)
        {
            return SceneGame.saveDataManager?.ShopPurchase?.ClearBoughtCount(shopItemUid) == true;
        }

        public bool RestockShop(int shopUid)
        {
            return SceneGame.saveDataManager?.ShopPurchase?.ClearBoughtCountsByShopUid(shopUid) == true;
        }

        public bool RestockCurrentShop()
        {
            return RestockShop(_currentShopUid);
        }

        /// <summary>
        /// 재고 또는 외부 구매 제한이 변경되면 현재 상점 데이터와 구매 제한 표시를 다시 구성합니다.
        /// </summary>
        private void OnShopAvailabilityChanged()
        {
            if (_currentShopUid <= 0) return;
            SetInfoByShopUid(_currentShopUid, true, false, false);
            RefreshShopAvailabilityPresentation();
        }

        /// <summary>
        /// 현재 상점에서 구매가 완료되면 버튼과 선택 상품 말풍선을 최종 구매 상태로 즉시 갱신합니다.
        /// </summary>
        /// <param name="eventData">구매한 상품과 상점 식별 정보입니다.</param>
        private void OnItemPurchased(ItemPurchasedEventData eventData)
        {
            if (_currentShopUid <= 0)
            {
                return;
            }

            if (eventData.ShopUid > 0 && eventData.ShopUid != _currentShopUid)
            {
                return;
            }

            // 레거시 구매 경로는 ShopUid가 없을 수 있으므로 현재 선택 상품 UID로 한 번 더 범위를 제한합니다.
            if (eventData.ShopUid <= 0)
            {
                ShopDisplayItem selectedItem = _selectedElementShop != null
                    ? _selectedElementShop.GetDisplayItem()
                    : null;
                if (selectedItem == null || selectedItem.ItemUid != eventData.ItemUid)
                {
                    return;
                }
            }

            RefreshShopAvailabilityPresentation();
        }

        /// <summary>
        /// 현재 표시 중인 상품의 구매 가능 상태, 구매 버튼 및 선택 상품 말풍선을 함께 갱신합니다.
        /// </summary>
        private void RefreshShopAvailabilityPresentation()
        {
            RefreshVisibleAvailability();
            UpdateDiscountTalkBubble();
        }

        /// <summary>
        /// 현재 생성된 모든 상품 요소와 선택 상품의 구매 가능 상태를 다시 계산합니다.
        /// </summary>
        private void RefreshVisibleAvailability()
        {
            foreach (var pair in _uiElementShops)
            {
                pair.Value?.RefreshAvailability();
            }

            RefreshSelectedPurchaseUi();
        }

        private void ClearShopElements()
        {
            foreach (var data in _uiElementShops)
            {
                if (data.Value)
                {
                    Destroy(data.Value.gameObject);
                }
            }

            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    if (slot)
                    {
                        Destroy(slot.gameObject);
                    }
                }
            }

            if (icons != null)
            {
                foreach (var icon in icons)
                {
                    if (icon)
                    {
                        Destroy(icon.gameObject);
                    }
                }
            }

            slots = null;
            icons = null;
            maxCountIcon = 0;
            _selectedElementShop = null;
            _uiElementShops.Clear();
            RefreshSelectedPurchaseUi();

            if (vfxEffectUISelected)
            {
                vfxEffectUISelected.gameObject.SetActive(false);
            }
        }

        public override void OnShow(bool show)
        {
            base.OnShow(show);
            if (!show) return;
            SelectFirstElement();
        }

        private void SelectFirstElement()
        {
            _selectedElementShop = null;
            UIElementShop element = null;
            int minSlotIndex = int.MaxValue;
            foreach (var pair in _uiElementShops)
            {
                ShopDisplayItem displayItem = pair.Value != null
                    ? pair.Value.GetDisplayItem()
                    : null;
                if (displayItem != null &&
                    !displayItem.IsEmpty &&
                    pair.Key < minSlotIndex)
                {
                    minSlotIndex = pair.Key;
                    element = pair.Value;
                }
            }

            if (element == null)
            {
                RefreshSelectedPurchaseUi();
                return;
            }

            element.SetSelected(true);
        }

        /// <summary>
        /// shop_item 테이블의 Uid 찾기
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public int GetShopItemUid(int slotIndex)
        {
            var element = _uiElementShops.GetValueOrDefault(slotIndex);
            if (element == null) return 0;
            ShopDisplayItem displayItem = element.GetDisplayItem();
            return displayItem != null && !displayItem.IsEmpty ? displayItem.Uid : 0;
        }

        /// <summary>
        /// 할인 또는 재고 부족 말풍선의 표시 상태와 텍스트를 설정합니다.
        /// 표시할 때는 텍스트 레이아웃을 확정한 뒤 썸네일 위치를 다시 계산합니다.
        /// </summary>
        /// <param name="value">말풍선을 표시할지 여부입니다.</param>
        /// <param name="text">말풍선에 표시할 텍스트입니다.</param>
        /// <param name="position">선택된 상점 요소의 월드 좌표입니다.</param>
        private void SetDiscountTalkBubble(bool value, string text, Vector3 position)
        {
            if (textDiscountTalkBubble)
            {
                textDiscountTalkBubble.text = text;
            }

            if (panelDiscountTalkBubble)
            {
                panelDiscountTalkBubble.SetActive(value);
                if (!value)
                {
                    return;
                }

                panelDiscountTalkBubble.transform.position = position + offsetDiscountTalkBubble;

                if (panelDiscountTalkBubble.TryGetComponent<RectTransform>(out var bubbleRectTransform))
                {
                    // 대화 말풍선과 동일하게 TMP 및 레이아웃 갱신 후 최종 패널 너비를 사용합니다.
                    textDiscountTalkBubble?.ForceMeshUpdate();
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRectTransform);

                    if (imageThumbnailCharacter && imageThumbnailCharacter.TryGetComponent<RectTransform>(out var thumbnailRectTransform))
                    {
                        RefreshDiscountThumbnailPosition(bubbleRectTransform, thumbnailRectTransform);
                    }
                }
            }
        }

        /// <summary>
        /// 대화 말풍선과 동일한 기준으로 할인 말풍선 오른쪽에 썸네일을 배치합니다.
        /// 패널 반너비와 썸네일 반너비를 합산하고 Inspector 오프셋을 추가합니다.
        /// </summary>
        /// <param name="bubbleRectTransform">최종 텍스트 크기가 반영된 말풍선 패널입니다.</param>
        /// <param name="thumbnailRectTransform">위치를 갱신할 썸네일 RectTransform입니다.</param>
        private void RefreshDiscountThumbnailPosition(
            RectTransform bubbleRectTransform,
            RectTransform thumbnailRectTransform)
        {
            if (bubbleRectTransform == null || thumbnailRectTransform == null)
            {
                return;
            }

            float bubbleHalfWidth = bubbleRectTransform.rect.width * 0.5f;
            float thumbnailHalfWidth = thumbnailRectTransform.rect.width * 0.5f;
            float thumbnailCenterX =
                bubbleHalfWidth + thumbnailHalfWidth + offsetImageThumbnailCharacter.x;

            if (thumbnailRectTransform.parent != bubbleRectTransform)
            {
                RectTransform thumbnailParentRectTransform = thumbnailRectTransform.parent as RectTransform;
                thumbnailCenterX = ConvertBubbleSpaceXToParentLocalX(
                    bubbleRectTransform,
                    thumbnailParentRectTransform,
                    thumbnailCenterX);
            }

            // 매 갱신마다 기준값을 대입하여 오프셋이 누적되지 않도록 합니다.
            Vector2 anchoredPosition = thumbnailRectTransform.anchoredPosition;
            anchoredPosition.x = thumbnailCenterX;
            anchoredPosition.y = offsetImageThumbnailCharacter.y;
            thumbnailRectTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 말풍선 로컬 X 좌표를 썸네일 부모의 로컬 X 좌표로 변환합니다.
        /// 말풍선과 썸네일이 형제 또는 서로 다른 계층에 있어도 같은 배치 기준을 유지합니다.
        /// </summary>
        /// <param name="bubbleRectTransform">좌표 변환의 기준이 되는 말풍선 패널입니다.</param>
        /// <param name="thumbnailParentRectTransform">썸네일의 부모 RectTransform입니다.</param>
        /// <param name="bubbleSpaceX">말풍선 중심 기준 로컬 X 좌표입니다.</param>
        /// <returns>썸네일 부모 로컬 좌표계로 변환된 X 좌표입니다.</returns>
        private static float ConvertBubbleSpaceXToParentLocalX(
            RectTransform bubbleRectTransform,
            RectTransform thumbnailParentRectTransform,
            float bubbleSpaceX)
        {
            if (bubbleRectTransform == null || thumbnailParentRectTransform == null)
            {
                return bubbleSpaceX;
            }

            Vector3 worldPoint = bubbleRectTransform.TransformPoint(new Vector3(bubbleSpaceX, 0f, 0f));
            Vector3 parentLocalPoint = thumbnailParentRectTransform.InverseTransformPoint(worldPoint);
            return parentLocalPoint.x;
        }
    }
}
