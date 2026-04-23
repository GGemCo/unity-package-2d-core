using System.Collections;
using System.Collections.Generic;
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
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("상점 Element 프리팹")]
        [SerializeField] private GameObject prefabUIElementShop;
        [Tooltip("구매하기 버튼")]
        [SerializeField] private Button buttonBuy;
        
        [Tooltip("가격 텍스트")]
        [SerializeField] private TextMeshProUGUI textPrice;
        [Tooltip("재화가 부족하지 않을 때 적용할 스타일 키")]
        [SerializeField] private string styleKeyPriceNormal;
        [Tooltip("재화가 부족할 때 적용할 스타일 키")]
        [SerializeField] private string styleKeyPriceLack;

        [Tooltip("아이템을 선택했을 때 보여줄 이펙트")]
        [SerializeField] private VfxEffectUI vfxEffectUISelected;
        
        private Coroutine _coRefreshSelectedVfx;

        
        private ShopResolver _shopResolver;
        private ShopAvailabilityService _shopAvailabilityService;
        private readonly Dictionary<int, UIElementShop> _uiElementShops = new Dictionary<int, UIElementShop>();
        private int _currentShopUid;

        private UIElementShop _selectedElementShop;
        
        protected override void Awake()
        {
            _selectedElementShop = null;
            _uiElementShops.Clear();
            uid = UIWindowConstants.WindowUid.Shop;
            if (TableLoaderManager.Instance == null) return;
            _shopAvailabilityService = ShopAvailabilityService.Instance;
            _shopResolver = new ShopResolver(TableLoaderManager.Instance.TableShop, _shopAvailabilityService);
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
        }

        private void OnDisable()
        {
            if (buttonBuy)
                buttonBuy.onClick.RemoveAllListeners();
            if (_shopAvailabilityService != null)
                _shopAvailabilityService.Changed -= OnShopAvailabilityChanged;
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
            // 같은 상점을 열었으면 업데이트 하지 않는다
            if (!forceRefresh && !reroll && _currentShopUid > 0 && _currentShopUid == shopUid)
            {
                RefreshVisibleAvailability();
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
                if (info.IsEmpty) continue;
                int index = info.SlotIndex;
                if (index < 0 || index >= maxCountIcon) continue;

                GameObject parent = gameObject;
                // UI Element 프리팹이 있으면 만든다.
                if (prefabUIElementShop != null)
                {
                    parent = Instantiate(prefabUIElementShop, containerIcon.gameObject.transform);
                    if (parent == null) continue;
                    UIElementShop uiElementShop = parent.GetComponent<UIElementShop>();
                    if (uiElementShop == null) continue;
                    uiElementShop.Initialize(this, index, info);
                    _uiElementShops.TryAdd(index, uiElementShop);
                }

                GameObject slotObject = Instantiate(slot, parent.transform);
                UISlot uiSlot = slotObject.GetComponent<UISlot>();
                if (uiSlot == null) continue;
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
        public void ShowByUid(int shopUid)
        {
            if (shopUid <= 0) return;
            SetInfoByShopUid(shopUid);
            Show(true);
        }

        public void SetSelectItem(UIElementShop uiElementShop)
        {
            if (_selectedElementShop == uiElementShop) return;
            if (_selectedElementShop != null)
            {
                _selectedElementShop.SetSelected(false);
            }
            _selectedElementShop = uiElementShop;
            
            UpdatePriceText();
            
            if (_coRefreshSelectedVfx != null)
            {
                StopCoroutine(_coRefreshSelectedVfx);
                _coRefreshSelectedVfx = null;
            }

            _coRefreshSelectedVfx = StartCoroutine(CoRefreshSelectedVfxPosition());
        }

        
        private IEnumerator CoRefreshSelectedVfxPosition()
        {
            if (_selectedElementShop == null || vfxEffectUISelected == null)
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

            if (_selectedElementShop == null || vfxEffectUISelected == null)
            {
                yield break;
            }

            vfxEffectUISelected.gameObject.SetActive(true);
            vfxEffectUISelected.PlayAnimation("start", forceReset: true);
            vfxEffectUISelected.transform.position = _selectedElementShop.transform.position;

            _coRefreshSelectedVfx = null;
        }

        private void UpdatePriceText()
        {
            if (_selectedElementShop == null)
            {
                if (buttonBuy)
                {
                    buttonBuy.interactable = false;
                }

                return;
            }

            var displayItem = _selectedElementShop.GetDisplayItem();
            if (buttonBuy)
            {
                buttonBuy.interactable = displayItem != null && displayItem.IsBuyable;
            }

            if (!textPrice) return;

            var playerGold = SceneGame.saveDataManager.Player.CurrentGold;
            var data = _selectedElementShop.GetPrice();
            var itemPrice = data.Item2;
            var key = playerGold < itemPrice ? styleKeyPriceLack : styleKeyPriceNormal;

            textPrice.text = string.Format("( <style={2}>{0}</style> / {1} )", playerGold, itemPrice, key);
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

        private void OnShopAvailabilityChanged()
        {
            RefreshVisibleAvailability();
        }

        private void RefreshVisibleAvailability()
        {
            foreach (var pair in _uiElementShops)
            {
                pair.Value?.RefreshAvailability();
            }

            UpdatePriceText();
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

            if (vfxEffectUISelected)
            {
                vfxEffectUISelected.gameObject.SetActive(false);
            }
        }

        public override void OnShow(bool show)
        {
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
                if (pair.Key < minSlotIndex)
                {
                    minSlotIndex = pair.Key;
                    element = pair.Value;
                }
            }

            if (element == null) return;
            element.SetSelected(true);
        }
    }
}
