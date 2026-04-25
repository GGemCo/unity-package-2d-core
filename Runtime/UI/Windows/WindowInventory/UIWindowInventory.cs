using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

using System.Collections;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리 윈도우
    /// </summary>
    public class UIWindowInventory : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("모든 아이템 합치기 버튼")]
        public Button buttonMergeAllItems;
        [Tooltip("아이템 정보 표시 윈도우")]
        [SerializeField] private UIWindowItemInfo overrideUiWindowItemInfo;
        [Tooltip("인벤토리 창이 열릴 때 첫 번째 아이템 슬롯을 자동 선택할지 여부")]
        [SerializeField] private bool selectFirstItemOnShow = true;
        [Tooltip("아이템 나누기 가능 여부")]
        [SerializeField] private bool useItemSplit = true;
        
        public TableItem TableItem;
        public InventoryData InventoryData;
        public EquipData EquipData;
        private QuickSlotSimulationData _quickSlotSimulationData;
        
        private GameObject _iconItem;
        private PopupManager _popupManager;
        
        private UIWindowItemInfo _uiWindowItemInfo;
        private UIWindowItemSplit _uiWindowItemSplit;
        private UIWindowStash _uiWindowStash;
        private UIWindowShopSale _uiWindowShopSale;
        private UIWindowItemUpgrade _uiWindowItemUpgrade;
        private UIWindowItemSalvage _uiWindowItemSalvage;
        private UIWindowQuickSlotSimulation _uiWindowQuickSlotSimulation;
        private Coroutine _coSelectFirstItemOnShow;

        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.Inventory;
            if (TableLoaderManager.Instance == null) return;
            TableItem = TableLoaderManager.Instance.TableItem;
            buttonMergeAllItems?.onClick.AddListener(OnClickMergeAllItems);
            base.Awake();
            
            IconPoolManager.SetSetIconHandler(new SetIconHandlerInventory());
            DragDropHandler.SetStrategy(new DragDropStrategyInventory());
        }

        /// <summary>
        /// 인벤토리에서 사용하는 연계 윈도우와 데이터를 초기화합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _popupManager = SceneGame.popupManager;
            if (SceneGame != null && SceneGame.saveDataManager != null)
            {
                InventoryData = SceneGame.saveDataManager.Inventory;
                EquipData = SceneGame.saveDataManager.Equip;
                _quickSlotSimulationData = SceneGame.saveDataManager.QuickSlotSimulation;
            }
            ResolveItemInfoWindow();
            _uiWindowItemSplit =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemSplit>(UIWindowConstants.WindowUid
                    .ItemSplit);
            _uiWindowStash =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowStash>(UIWindowConstants.WindowUid
                    .Stash);
            _uiWindowShopSale =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowShopSale>(UIWindowConstants.WindowUid
                    .ShopSale);
            _uiWindowItemUpgrade =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemUpgrade>(UIWindowConstants.WindowUid
                    .ItemUpgrade);
            _uiWindowItemSalvage =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemSalvage>(UIWindowConstants.WindowUid
                    .ItemSalvage);
            _uiWindowQuickSlotSimulation =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowQuickSlotSimulation>(UIWindowConstants.WindowUid
                    .QuickSlotSimulation);
        }

        /// <summary>
        /// 인벤토리에서 사용할 아이템 정보창을 결정합니다.
        /// override 가 연결되어 있으면 해당 창을 우선 사용하고, 없으면 공용 창을 사용합니다.
        /// </summary>
        private void ResolveItemInfoWindow()
        {
            _uiWindowItemInfo =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowItemInfo>(UIWindowConstants.WindowUid.ItemInfo);

            if (overrideUiWindowItemInfo == null)
            {
                return;
            }

            _uiWindowItemInfo = overrideUiWindowItemInfo;
            _uiWindowItemInfo.Show(false);
        }

        /// <summary>
        /// 인벤토리 문맥에서 테이블 연동 윈도우 Uid 를 실제 윈도우 오브젝트로 해석합니다.
        /// ItemInfo 는 override 가 연결되어 있으면 해당 전용 창을 우선 반환합니다.
        /// </summary>
        /// <param name="windowUid">해석할 연결 윈도우 Uid 입니다.</param>
        /// <returns>인벤토리 문맥에서 사용해야 하는 실제 윈도우 오브젝트입니다.</returns>
        protected override UIWindow ResolveLinkedWindow(UIWindowConstants.WindowUid windowUid)
        {
            if (windowUid != UIWindowConstants.WindowUid.ItemInfo)
            {
                return base.ResolveLinkedWindow(windowUid);
            }

            if (_uiWindowItemInfo == null && SceneGame != null)
            {
                ResolveItemInfoWindow();
            }

            return _uiWindowItemInfo != null ? _uiWindowItemInfo : base.ResolveLinkedWindow(windowUid);
        }

        /// <summary>
        /// 인벤토리 표시 상태가 바뀔 때 아이콘을 갱신하고 필요 시 첫 아이템 슬롯을 자동 선택합니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            base.OnShow(show);
            if (show)
            {
                LoadIcons();
                ScheduleSelectFirstOccupiedSlot();
            }
            else
            {
                StopSelectFirstItemCoroutine();
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
                uiIcon.ChangeInfoByUid(table.Uid, itemCount, iconInstanceId: structInventoryIcon.InstanceId);
            }
        }

        /// <summary>
        /// 자동 선택 옵션이 활성화되어 있을 때 첫 번째 점유 슬롯 선택을 다음 프레임으로 예약합니다.
        /// 레이아웃이 모두 반영된 뒤 선택 이펙트 위치가 계산되도록 한 프레임 지연시킵니다.
        /// </summary>
        private void ScheduleSelectFirstOccupiedSlot()
        {
            if (!selectFirstItemOnShow || icons == null || icons.Length <= 0)
            {
                return;
            }

            StopSelectFirstItemCoroutine();
            _coSelectFirstItemOnShow = StartCoroutine(CoSelectFirstOccupiedSlotOnShow());
        }

        /// <summary>
        /// 인벤토리 레이아웃이 최종 반영된 이후 첫 번째 점유 슬롯을 선택합니다.
        /// </summary>
        private IEnumerator CoSelectFirstOccupiedSlotOnShow()
        {
            Canvas.ForceUpdateCanvases();

            if (containerIcon != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerIcon.GetComponent<RectTransform>());
            }

            yield return null;
            yield return new WaitForEndOfFrame();

            TrySelectFirstOccupiedSlotImmediate();
            _coSelectFirstItemOnShow = null;
        }

        /// <summary>
        /// 예약된 첫 아이템 자동 선택 코루틴이 있으면 중지합니다.
        /// </summary>
        private void StopSelectFirstItemCoroutine()
        {
            if (_coSelectFirstItemOnShow == null)
            {
                return;
            }

            StopCoroutine(_coSelectFirstItemOnShow);
            _coSelectFirstItemOnShow = null;
        }

        /// <summary>
        /// 자동 선택 옵션이 활성화되어 있을 때 첫 번째 점유 슬롯을 즉시 선택합니다.
        /// 비어 있는 슬롯만 있으면 아무 작업도 하지 않습니다.
        /// </summary>
        private void TrySelectFirstOccupiedSlotImmediate()
        {
            int firstOccupiedIndex = FindFirstOccupiedSlotIndex();
            if (firstOccupiedIndex < 0)
            {
                return;
            }

            base.SetSelectedIcon(firstOccupiedIndex);
            ShowItemInfo(true, GetIconByIndex(firstOccupiedIndex));
        }

        /// <summary>
        /// 현재 인벤토리 아이콘 배열에서 가장 앞에 있는 점유 슬롯 인덱스를 찾습니다.
        /// </summary>
        /// <returns>점유 슬롯 인덱스, 없으면 -1 입니다.</returns>
        private int FindFirstOccupiedSlotIndex()
        {
            if (icons == null)
            {
                return -1;
            }

            for (int index = 0; index < icons.Length; index++)
            {
                GameObject iconObject = icons[index];
                if (iconObject == null)
                {
                    continue;
                }

                UIIcon uiIcon = iconObject.GetComponent<UIIcon>();
                if (uiIcon == null || uiIcon.uid <= 0 || uiIcon.GetCount() <= 0)
                {
                    continue;
                }

                return index;
            }

            return -1;
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
            // Simulation 퀵슬롯이 있으면
            if (_uiWindowQuickSlotSimulation != null && _uiWindowQuickSlotSimulation.IsOpen())
            {
                if (icon.IsToolType() || icon.IsSeedType())
                {
                    SceneGame.uIWindowManager.MoveIcon(uid, icon.slotIndex,
                        UIWindowConstants.WindowUid.QuickSlotSimulation, icon.GetCount());
                    return;
                }
            }
            
            // 상점 판매창이 열려있으면
            if (_uiWindowShopSale != null && _uiWindowShopSale.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.ShopSale))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Item_CannotSell");//"판매할 수 없는 아이템 입니다."
                    return;
                }

                SceneGame.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowConstants.WindowUid.ShopSale,
                    icon.GetCount());
            }
            // 창고가 열려 있으면 창고로 이동
            else if (_uiWindowStash != null && _uiWindowStash.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.Stash))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Item_CannotStore");//"보관할 수 없는 아이템 입니다."
                    return;
                }
                SceneGame.uIWindowManager.MoveIcon(uid, icon.slotIndex, UIWindowConstants.WindowUid.Stash, icon.GetCount());
            }
            // 아이템 강화
            else if (_uiWindowItemUpgrade != null && _uiWindowItemUpgrade.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.Upgrade))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Item_CannotUpgrade");//"강화할 수 없는 아이템 입니다."
                    return;
                }
                // 기존 register 된 아이콘이 있으면 un register 해주기
                var registerIcon = _uiWindowItemUpgrade.GetIconByIndex(_uiWindowItemUpgrade.GetSourceIconSlotIndex());
                if (registerIcon != null && registerIcon.uid > 0)
                {
                    SceneGame.uIWindowManager.UnRegisterIcon(UIWindowConstants.WindowUid.ItemUpgrade, 0);
                }

                _uiWindowItemUpgrade.ShowTextResult(false);
                SceneGame.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowConstants.WindowUid.ItemUpgrade, 1);
            }
            // 아이템 분해
            else if (_uiWindowItemSalvage != null && _uiWindowItemSalvage.IsOpen())
            {
                if (icon.IsAntiFlag(ItemConstants.AntiFlag.Salvage))
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Item_CannotSalvage");//"분해할 수 없는 아이템 입니다."
                    return;
                }
                // 분해 할 수 있는 개수가 넘어가지 않았는지 체크
                if (_uiWindowItemSalvage.CheckSalvagePossibleCount() == false)
                {
                    SceneGame.systemMessageManager.ShowMessageWarning("Item_CannotRegisterMore");//"더 이상 아이템을 등록할 수 없습니다."
                    return;
                }
                SceneGame.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowConstants.WindowUid.ItemSalvage,
                    icon.GetCount());
            }
            else
            {
                // 장비일때
                if (icon.IsEquipType())
                {
                    int partSlotIndex = icon.GetPartsSlotIndex();
                    if (partSlotIndex < 0) return;
                    SceneGame.uIWindowManager.MoveIcon(uid, icon.index, UIWindowConstants.WindowUid.Equip, 1, partSlotIndex);
                }
                // item_use 테이블에 정의된 "사용형 아이템" 처리
                else if (TableLoaderManager.Instance != null && TableLoaderManager.Instance.TableItemUse != null
                         && TableLoaderManager.Instance.TableItemUse.TryGetByItemUid(icon.uid, out _))
                {
                    // 쿨타임 선 체크(사용 실패 시 쿨타임이 시작되면 안 되므로, StartHandler 호출 없이 현재 값만 확인)
                    float currentCd = SceneGame.uIIconCoolTimeManager.GetCurrentCoolTime(uid, icon.uid);
                    if (currentCd > 0)
                    {
                        SceneGame.systemMessageManager.ShowMessageWarning("Action_CannotUseDuringCooldown");
                        return;
                    }

                    var useResult = ItemUseService.TryUseInventoryItem(SceneGame, InventoryData, icon.slotIndex,
                        out var cooldown);
                    SetIcons(useResult);
                    if (useResult is not { Result: ResultCommon.ResultType.Success }) return;

                    if (cooldown > 0)
                    {
                        // 성공 시에만 쿨타임 시작
                        icon.PlayCoolTime(cooldown);
                    }
                }
            }
        }
        
        /// <summary>
        /// index 가 없을때는, 같은 uid 는 중첩 가능여부를 확인하고 합치고, 나머지는 추가
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        public override void SetIconCount(int iconUid, int iconCount, long instanceId = 0)
        {
            ResultCommon result = InventoryData.AddItem(new IconPayload(iconUid, iconCount, instanceId));
            SetIcons(result);
        }
        
        /// <summary>
        /// 아이템 나누기 단축키 : shift + 좌클릭 적용 
        /// </summary>
        /// <param name="index"></param>
        public override void SetSelectedIcon(int index)
        {
            base.SetSelectedIcon(index);

            OnItemSplit(index);
        }

        private void OnItemSplit(int index)
        {
            if (!useItemSplit) return;
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKey(KeyCode.LeftShift))
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.leftShiftKey.isPressed)
#endif
            {
                UIIcon icon = GetIconByIndex(index);
                if (icon == null || icon.uid <= 0)
                {
                    _popupManager.ShowPopupError("Inventory_SelectItemToSplit");//"나누기를 할 아이템을 선택해주세요."
                    return;
                }

                if (icon.GetCount() <= 1)
                {
                    _popupManager.ShowPopupError("Inventory_Split_MinimumTwo");
                    return;
                }
                // 팝업창 띄우기
                if (_uiWindowItemSplit == null) return;
                SceneGame.Instance.uIWindowManager.RegisterIcon(uid, icon.slotIndex, UIWindowConstants.WindowUid.ItemSplit, icon.GetCount());
            }
        }

        /// <summary>
        /// 인벤토리 아이콘의 아이템 정보창을 표시하거나 숨깁니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        /// <param name="icon">정보를 표시할 아이콘입니다.</param>
        public override void ShowItemInfo(bool show, UIIcon icon = null)
        {
            if (show)
            {
                TryShowItemInfo(icon);
            }
            else
            {
                HideItemInfoWindow();
            }
        }

        /// <summary>
        /// 지정한 인벤토리 아이콘 정보를 현재 선택된 아이템 정보창에 표시합니다.
        /// </summary>
        /// <param name="icon">정보를 표시할 아이콘입니다.</param>
        /// <returns>표시에 성공하면 true, 아니면 false 입니다.</returns>
        private bool TryShowItemInfo(UIIcon icon)
        {
            if (icon == null || _uiWindowItemInfo == null)
            {
                return false;
            }

            _uiWindowItemInfo.SetItemInfo(new UIWindowItemInfoRequest
            {
                ItemUid = icon.uid,
                InstanceId = icon.instanceId,
                AnchorObject = icon.gameObject,
                PositionType = UIWindowItemInfo.PositionType.Fixed,
                IconSlotSize = slotSize,
            });
            return true;
        }

        /// <summary>
        /// 인벤토리에서 사용 중인 아이템 정보창을 숨깁니다.
        /// </summary>
        private void HideItemInfoWindow()
        {
            _uiWindowItemInfo?.Show(false);
        }

        public void AddToQuickSlotSimulation(UIIcon icon)
        {
            float time = SceneGame.uIIconCoolTimeManager.GetCurrentCoolTime(uid, icon.uid);
            if (time > 0)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("Skill_CannotChangeDuringCooldown");//"쿨타임 중에는 바꿀 수 없습니다."
                return;
            }
            if (_uiWindowQuickSlotSimulation == null) return;
            // 퀵슬롯에 하나 넣기
            var result = _quickSlotSimulationData.AddItem(icon.uid, icon.GetCount(), icon.GetLevel());
            _uiWindowQuickSlotSimulation.SetIcons(result);
        }
    }
}
