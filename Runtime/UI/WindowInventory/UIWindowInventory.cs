using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리 윈도우
    /// </summary>
    public class UIWindowInventory : UIWindow
    {
        [Header("모든 아이템 합치기 버튼")]
        public Button buttonMergeAllItems;
        public TableItem TableItem;
        public InventoryData InventoryData;
        public EquipData EquipData;
        
        private GameObject _iconItem;
        private PopupManager _popupManager;
        
        private UIWindowItemInfo _uiWindowItemInfo;
        private UIWindowItemSplit _uiWindowItemSplit;
        private UIWindowStash _uiWindowStash;
        private UIWindowShopSale _uiWindowShopSale;
        private UIWindowItemUpgrade _uiWindowItemUpgrade;
        private UIWindowItemSalvage _uiWindowItemSalvage;

        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.Inventory;
            if (TableLoaderManager.Instance == null) return;
            TableItem = TableLoaderManager.Instance.TableItem;
            buttonMergeAllItems?.onClick.RemoveAllListeners();
            buttonMergeAllItems?.onClick.AddListener(OnClickMergeAllItems);
            base.Awake();
            
            SetSetIconHandler(new SetIconHandlerInventory());
            DragDropHandler.SetStrategy(new DragDropStrategyInventory());
        }

        protected override void Start()
        {
            base.Start();
            _popupManager = SceneGame.popupManager;
            if (SceneGame != null && SceneGame.saveDataManager != null)
            {
                InventoryData = SceneGame.saveDataManager.Inventory;
                EquipData = SceneGame.saveDataManager.Equip;
            }
            _uiWindowItemInfo = 
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowManager.WindowUid
                    .ItemInfo);
            _uiWindowItemSplit =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemSplit>(UIWindowManager.WindowUid
                    .ItemSplit);
            _uiWindowStash =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowStash>(UIWindowManager.WindowUid
                    .Stash);
            _uiWindowShopSale =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowShopSale>(UIWindowManager.WindowUid
                    .ShopSale);
            _uiWindowItemUpgrade =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemUpgrade>(UIWindowManager.WindowUid
                    .ItemUpgrade);
            _uiWindowItemSalvage =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemSalvage>(UIWindowManager.WindowUid
                    .ItemSalvage);
        }
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            if (show)
            {
                LoadIcons();
            }
        }
        /// <summary>
        /// 저장되어있는 아이템 정보로 아이콘 셋팅하기
        /// 인벤토리가 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        public void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.saveDataManager.Inventory.GetAllItemCounts();
            if (datas == null) return;
            for (int index = 0; index < maxCountIcon; index++)
            {
                if (index >= icons.Length) continue;
                var icon = icons[index];
                if (icon == null) continue;
                UIIconItem uiIcon = icon.GetComponent<UIIconItem>();
                if (uiIcon == null) continue;
                if (!datas.TryGetValue(index, out var info))
                {
                    uiIcon.ClearIconInfos();
                    continue;
                }

                SaveDataIcon structInventoryIcon = info;
                int itemUid = structInventoryIcon.Uid;
                int itemCount = structInventoryIcon.Count;
                if (itemUid <= 0 || itemCount <= 0)
                {
                    uiIcon.ClearIconInfos();
                    continue;
                }
                var table = TableItem.GetDataByUid(itemUid);
                if (table == null || table.Uid <= 0) continue;
                uiIcon.ChangeInfoByUid(table.Uid);
                uiIcon.SetCount(itemCount);
            }
        }
        /// <summary>
        /// 모든 아이템 합치기
        /// </summary>
        private void OnClickMergeAllItems()
        {
            InventoryData.MergeAllItems();
            LoadIcons();
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            // 상점 판매창이 열려있으면
            if (_uiWindowShopSale != null && _uiWindowShopSale.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.ShopSale))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("판매할 수 없는 아이템 입니다.");
                    return;
                }

                SceneGame.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowManager.WindowUid.ShopSale,
                    icon.GetCount());
            }
            // 창고가 열려 있으면 창고로 이동
            else if (_uiWindowStash != null && _uiWindowStash.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.Stash))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("보관할 수 없는 아이템 입니다.");
                    return;
                }
                SceneGame.uIWindowManager.MoveIcon(uid, icon.slotIndex, UIWindowManager.WindowUid.Stash, icon.GetCount());
            }
            // 아이템 강화
            else if (_uiWindowItemUpgrade != null && _uiWindowItemUpgrade.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.Upgrade))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("강화할 수 없는 아이템 입니다.");
                    return;
                }
                // 기존 register 된 아이콘이 있으면 un register 해주기
                var registerIcon = _uiWindowItemUpgrade.GetIconByIndex(_uiWindowItemUpgrade.GetSourceIconSlotIndex());
                if (registerIcon != null && registerIcon.uid > 0)
                {
                    SceneGame.uIWindowManager.UnRegisterIcon(UIWindowManager.WindowUid.ItemUpgrade, 0);
                }

                _uiWindowItemUpgrade.ShowTextResult(false);
                SceneGame.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowManager.WindowUid.ItemUpgrade, 1);
            }
            // 아이템 분해
            else if (_uiWindowItemSalvage != null && _uiWindowItemSalvage.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.Salvage))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("분해할 수 없는 아이템 입니다.");
                    return;
                }
                // 분해 할 수 있는 개수가 넘어가지 않았는지 체크
                if (_uiWindowItemSalvage.CheckSalvagePossibleCount() == false)
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("더 이상 아이템을 등록할 수 없습니다.");
                    return;
                }
                SceneGame.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowManager.WindowUid.ItemSalvage,
                    icon.GetCount());
            }
            else
            {
                // 장비일때
                if (icon.IsEquipType())
                {
                    var partSlotIndex = (int)icon.GetPartsType();
                    SceneGame.uIWindowManager.MoveIcon(uid, icon.index, UIWindowManager.WindowUid.Equip, 1, partSlotIndex);
                }
                // 물약 일때
                else if (icon.IsPotionType())
                {
                    float coolTime = icon.GetCoolTime();
                    if (coolTime > 0)
                    {
                        if (!icon.PlayCoolTime(coolTime)) return;
                    }

                    // hp 물약일 때 
                    if (icon.IsHpPotionType() || icon.IsMpPotionType())
                    {
                        if (icon.uid <= 0 || icon.GetCount() <= 0)
                        {
                            _popupManager.ShowPopupError("사용할 수 있는 아이템 개수가 없습니다.");
                            return;
                        }

                        // mp 물약일 때 
                        if (icon.IsMpPotionType())
                        {
                            if (SceneGame.player.GetComponent<Player>().IsMaxMp())
                            {
                                SceneGame.systemMessageManager.ShowMessageWarning("현재 마력이 가득하여 사용할 수 없습니다.");
                                return;
                            }
                        }
                        else
                        {
                            if (SceneGame.player.GetComponent<Player>().IsMaxHp())
                            {
                                SceneGame.systemMessageManager.ShowMessageWarning("현재 생명력이 가득하여 사용할 수 없습니다.");
                                return;
                            }
                        }
                        var result = InventoryData.MinusItem(icon.slotIndex, icon.uid, 1);
                        SetIcons(result);
                        if (result is { Code: ResultCommon.Type.Success })
                        {
                            if (icon.IsMpPotionType())
                                SceneGame.player.GetComponent<Player>().AddMp(icon.GetStatusValue1());
                            else
                                SceneGame.player.GetComponent<Player>().AddHp(icon.GetStatusValue1());
                            
                        }
                    }
                    // affect 가 있을 때 
                    icon.CheckStatusAffect();
                }
            }
        }
        /// <summary>
        /// index 가 없을때는, 같은 uid 는 중첩 가능여부를 확인하고 합치고, 나머지는 추가
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        public override void SetIconCount(int iconUid, int iconCount)
        {
            ResultCommon result = InventoryData.AddItem(iconUid, iconCount);
            SetIcons(result);
        }
        /// <summary>
        /// 아이템 나누기 단축키 : shift + 좌클릭 적용 
        /// </summary>
        /// <param name="index"></param>
        public override void SetSelectedIcon(int index)
        {
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKey(KeyCode.LeftShift))
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.leftShiftKey.isPressed)
#endif
            {
                UIIcon icon = GetIconByIndex(index);
                if (icon == null || icon.uid <= 0)
                {
                    _popupManager.ShowPopupError("나누기를 할 아이템을 선택해주세요.");
                    return;
                }

                if (icon.GetCount() <= 1)
                {
                    _popupManager.ShowPopupError("아이템 개수가 2개 이상일때만 나눌 수 있습니다.");
                    return;
                }
                // 팝업창 띄우기
                if (_uiWindowItemSplit == null) return;
                SceneGame.Instance.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowManager.WindowUid.ItemSplit, icon.GetCount());
            }
        }
        /// <summary>
        /// 아이템 정보 보기
        /// </summary>
        /// <param name="icon"></param>
        public override void ShowItemInfo(UIIcon icon)
        {
            _uiWindowItemInfo.SetItemUid(icon.uid, icon.gameObject, UIWindowItemInfo.PositionType.Right, slotSize);
        }
    }
}