using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

using System.Collections;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리 윈도우
    /// </summary>
    public class UIWindowInventory : UIWindow
    {
        private const string ExternalItemInfoWindowKey = "Inventory.ItemInfo";

        /// <summary>
        /// 장착하기, 해제하기 버튼 활성화 정책
        /// </summary>
        private enum ButtonContextActionHidePolicy
        {
            // 보이는 상태에서 Interaction만 안되게 처리
            Disable,
            // 안보이게 처리
            Hide
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("아이콘 드래그 가능 여부")]
        [SerializeField] private bool useIconDrag = true;
        [Tooltip("모든 아이템 합치기 버튼")]
        public Button buttonMergeAllItems;
        [Tooltip("아이템 정보 표시 윈도우")]
        [SerializeField] private UIWindowItemInfo overrideUiWindowItemInfo;
        [Tooltip("인벤토리 오브젝트를 기준으로 아이템 정보 윈도우 위치")] [SerializeField]
        private UIWindowManager.ExternalWindowInsertMode overrideUiWindowItemInfoInsertMode =
            UIWindowManager.ExternalWindowInsertMode.After;

        [Tooltip("인벤토리 창이 열릴 때 첫 번째 아이템 슬롯을 자동 선택할지 여부")]
        [SerializeField] private bool selectFirstItemOnShow = true;
        [Tooltip("아이템 나누기 가능 여부")]
        [SerializeField] private bool useItemSplit = true;
        
        [Header("선택 문맥")]
        [Tooltip("장착하기, 해제하기 버튼 활성화 정책")]
        [SerializeField] private ButtonContextActionHidePolicy buttonContextActionHidePolicy;
        [Tooltip("선택 문맥이 있을 때 실행할 버튼")]
        [SerializeField] private Button buttonContextAction;
        [Tooltip("선택 문맥 실행 버튼의 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI textContextAction;
        [Tooltip("선택 문맥의 장착 해제를 실행할 버튼")]
        [SerializeField] private Button buttonContextUnequip;
        [Tooltip("선택 문맥 장착 해제 버튼의 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI textContextUnequip;
        [Tooltip("인벤토리 슬롯 페이지 컨트롤러")]
        [SerializeField] private UIPageController pageController;
        
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
        private IInventorySelectionContext _selectionContext;
        private TextMeshProUGUI _fallbackContextActionText;
        private string _fallbackContextActionTextDefault;
        private readonly List<int> _contextVisibleSlotOrder = new List<int>(128);
        private readonly List<IInventoryEquippedBadgeSource> _equippedBadgeSources = new List<IInventoryEquippedBadgeSource>(4);

        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.Inventory;
            if (TableLoaderManager.Instance == null) return;
            TableItem = TableLoaderManager.Instance.TableItem;
            buttonMergeAllItems?.onClick.AddListener(OnClickMergeAllItems);
            buttonContextAction?.onClick.AddListener(OnClickContextAction);
            buttonContextUnequip?.onClick.AddListener(OnClickContextUnequip);
            UpdateContextActionVisibility();
            base.Awake();
            
            IconPoolManager.SetSetIconHandler(new SetIconHandlerInventory());
            if (useIconDrag)
                DragDropHandler.SetStrategy(new DragDropStrategyInventory());

            if (pageController == null)
            {
                pageController = GetComponentInChildren<UIPageController>(true);
            }
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

            pageController?.InitializeBySlotObjects(slots);
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
            RegisterOverrideItemInfoWindowOrder();
        }

        /// <summary>
        /// 인벤토리 전용 아이템 정보창을 UIWindowManager 정렬 목록에 등록합니다.
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
                UIWindowConstants.WindowUid.Inventory,
                overrideUiWindowItemInfoInsertMode);
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
                ClearContext(false);
            }
        }

        /// <summary>
        /// 상위 기능이 인벤토리를 "아이템 선택 창"으로 열 때 호출합니다.
        /// 기존 아이콘 이동/등록 로직 대신 context가 표시 필터와 실행 동작을 맡습니다.
        /// </summary>
        public void OpenWithContext(IInventorySelectionContext context)
        {
            ClearContext(false);
            _selectionContext = context;
            UpdateContextActionVisibility();

            Show(true);

            // 이미 열려 있는 인벤토리에 새 문맥을 입히는 경우 OnShow가 다시 호출되지 않을 수 있어 즉시 갱신합니다.
            if (IsOpen())
            {
                LoadIcons();
                ScheduleSelectFirstOccupiedSlot();
            }
        }

        /// <summary>
        /// 선택 문맥을 정리하고 일반 인벤토리 모드로 되돌립니다.
        /// </summary>
        public void ClearContext()
        {
            ClearContext(true);
        }

        /// <summary>
        /// 장비창 데이터 외에 인벤토리 아이템을 장착처럼 참조하는 시스템을 장착 배지 표시 대상으로 등록합니다.
        /// 같은 소스가 중복 등록되면 한 번만 유지합니다.
        /// </summary>
        public void RegisterEquippedBadgeSource(IInventoryEquippedBadgeSource source)
        {
            if (source == null || _equippedBadgeSources.Contains(source))
            {
                return;
            }

            _equippedBadgeSources.Add(source);

            if (IsOpen())
            {
                LoadIcons();
            }
        }

        /// <summary>
        /// 더 이상 사용하지 않는 외부 장착 배지 소스를 인벤토리 표시 대상에서 제거합니다.
        /// </summary>
        public void UnregisterEquippedBadgeSource(IInventoryEquippedBadgeSource source)
        {
            if (source == null)
            {
                return;
            }

            _equippedBadgeSources.Remove(source);

            if (IsOpen())
            {
                LoadIcons();
            }
        }

        /// <summary>
        /// context 정리 후 화면 갱신 여부를 선택할 수 있는 내부 정리 루틴입니다.
        /// 창이 닫히는 중에는 불필요한 아이콘 갱신을 피합니다.
        /// </summary>
        private void ClearContext(bool reloadIcons)
        {
            if (_selectionContext == null)
            {
                UpdateContextActionVisibility();
                return;
            }

            _selectionContext.OnClosed();
            _selectionContext = null;
            UpdateContextActionVisibility();

            if (reloadIcons && IsOpen())
            {
                LoadIcons();
            }
        }

        /// <summary>
        /// 문맥 실행 버튼은 선택 context가 살아 있을 때만 표시합니다.
        /// 프리팹에 버튼이 연결되지 않은 기존 인벤토리는 그대로 동작합니다.
        /// </summary>
        private void UpdateContextActionVisibility()
        {
            bool show = _selectionContext is { IsActive: true };

            if (buttonContextAction != null)
            {
                buttonContextAction.gameObject.SetActive(show);
            }

            if (buttonContextUnequip != null)
            {
                buttonContextUnequip.gameObject.SetActive(show);
            }

            if (textContextAction != null)
            {
                textContextAction.text = show ? _selectionContext.ActionMessageKey : string.Empty;
            }
            else if (buttonContextAction == null && buttonMergeAllItems != null)
            {
                _fallbackContextActionText ??= buttonMergeAllItems.GetComponentInChildren<TextMeshProUGUI>(true);
                if (_fallbackContextActionText != null)
                {
                    _fallbackContextActionTextDefault ??= _fallbackContextActionText.text;
                    _fallbackContextActionText.text = show
                        ? _selectionContext.ActionMessageKey
                        : _fallbackContextActionTextDefault;
                }
            }

            if (textContextUnequip != null)
            {
                textContextUnequip.text = show ? _selectionContext.UnequipMessageKey : string.Empty;
            }

            // 별도 context 버튼이 있는 프리팹에서는 일반 합치기 버튼과 역할이 겹치지 않게 숨깁니다.
            if (buttonMergeAllItems != null && buttonContextAction != null)
            {
                buttonMergeAllItems.gameObject.SetActive(!show);
            }

            RefreshContextActionButtons();
        }

        /// <summary>
        /// 저장되어있는 아이템 정보로 아이콘 셋팅하기
        /// 인벤토리가 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        public void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.saveDataManager.Inventory.GetAllItemCounts();
            bool contextActive = _selectionContext is { IsActive: true };
            int defaultItemUid = 0;
            long defaultItemInstanceId = 0;
            bool hasDefaultSelection = contextActive &&
                                       _selectionContext.TryGetDefaultSelection(
                                           out defaultItemUid,
                                           out defaultItemInstanceId);
            bool defaultSelectionAdded = false;

            _contextVisibleSlotOrder.Clear();

            if (datas == null)
            {
                SetAllSlotFilteringState(true);
                RefreshInventorySlotPage(contextActive);
                return;
            }

            for (int index = 0; index < maxCountIcon; index++)
            {
                if (index >= icons.Length)
                {
                    SetSlotFilteringState(index, true);
                    continue;
                }

                var icon = icons[index];
                if (icon == null)
                {
                    SetSlotFilteringState(index, true);
                    continue;
                }

                UIIconItem uiIcon = icon.GetComponent<UIIconItem>();
                if (uiIcon == null)
                {
                    SetSlotFilteringState(index, true);
                    continue;
                }

                if (!datas.TryGetValue(index, out var saveDataIcon))
                {
                    SetSlotFilteringState(index, true);
                    ClearInventoryIconAndSlot(uiIcon, index);
                    continue;
                }

                int itemUid = saveDataIcon.Uid;
                int itemCount = saveDataIcon.Count;
                if (itemUid <= 0)
                {
                    SetSlotFilteringState(index, true);
                    ClearInventoryIconAndSlot(uiIcon, index);
                    continue;
                }
                var table = TableItem.GetDataByUid(itemUid);
                if (table == null || table.Uid <= 0)
                {
                    SetSlotFilteringState(index, true);
                    ClearInventoryIconAndSlot(uiIcon, index);
                    continue;
                }

                bool isZeroCountItem = itemCount <= 0;
                bool displayZeroCountItem = ShouldDisplayZeroCountItem(saveDataIcon, table);
                if (isZeroCountItem && !displayZeroCountItem)
                {
                    SetSlotFilteringState(index, true);
                    ClearInventoryIconAndSlot(uiIcon, index);
                    continue;
                }

                // 선택 문맥이 있으면 해당 문맥에서 허용한 아이템만 후보로 보여줍니다.
                if (contextActive &&
                    !_selectionContext.CanDisplay(saveDataIcon, table))
                {
                    SetSlotFilteringState(index, true);
                    ClearInventoryIconAndSlot(uiIcon, index);
                    continue;
                }

                SetSlotFilteringState(index, true);

                if (contextActive)
                {
                    defaultSelectionAdded = AddContextVisibleSlotOrder(
                        index,
                        saveDataIcon,
                        hasDefaultSelection,
                        defaultItemUid,
                        defaultItemInstanceId,
                        defaultSelectionAdded);
                }

                // 0개 아이템을 보여주는 문맥에서만 개수 텍스트에 0을 표시할 수 있습니다.
                uiIcon.SetShowZeroCountText(isZeroCountItem && ShouldShowZeroCountText(saveDataIcon, table));
                uiIcon.ChangeInfoByUid(table.Uid, itemCount, iconInstanceId: saveDataIcon.InstanceId);
                bool equipped = ShouldShowEquippedBadge(uiIcon);
                uiIcon.SetEquippedState(equipped);
                uiIcon.SetDrag(useIconDrag);
                SetSlotEquippedState(index, equipped);
            }

            RefreshInventorySlotPage(contextActive);
            RefreshContextActionButtons();
        }

        /// <summary>
        /// 선택 문맥에서 화면에 보여줄 슬롯 순서를 구성합니다.
        /// 기본 선택 아이템은 사용자가 클릭한 스킬 슬롯에 이미 장착된 아이템이므로 목록 맨 앞으로 배치합니다.
        /// </summary>
        private bool AddContextVisibleSlotOrder(
            int slotIndex,
            SaveDataIcon saveDataIcon,
            bool hasDefaultSelection,
            int defaultItemUid,
            long defaultItemInstanceId,
            bool defaultSelectionAdded)
        {
            bool isDefaultSelection =
                hasDefaultSelection &&
                !defaultSelectionAdded &&
                saveDataIcon != null &&
                saveDataIcon.Uid == defaultItemUid &&
                saveDataIcon.InstanceId == defaultItemInstanceId;

            if (isDefaultSelection)
            {
                _contextVisibleSlotOrder.Insert(0, slotIndex);
                return true;
            }

            _contextVisibleSlotOrder.Add(slotIndex);
            return defaultSelectionAdded;
        }

        /// <summary>
        /// 현재 인벤토리 모드에 맞춰 페이지 컨트롤러의 슬롯 표시 순서를 갱신합니다.
        /// 일반 모드에서는 원래 슬롯 순서를 사용하고, 선택 문맥에서는 후보 슬롯만 앞에서부터 채워 보이게 합니다.
        /// </summary>
        private void RefreshInventorySlotPage(bool contextActive)
        {
            if (pageController == null)
            {
                return;
            }

            if (contextActive)
            {
                pageController.SetSlotDisplayOrder(_contextVisibleSlotOrder);
                return;
            }

            pageController.ClearSlotDisplayOrder();
        }

        /// <summary>
        /// 전체 슬롯의 필터 표시 상태를 한 번에 설정합니다.
        /// 선택 문맥이 없을 때는 빈 슬롯까지 일반 인벤토리처럼 보이도록 복구할 때 사용합니다.
        /// </summary>
        private void SetAllSlotFilteringState(bool visible)
        {
            for (int slotIndex = 0; slotIndex < maxCountIcon; slotIndex++)
            {
                SetSlotFilteringState(slotIndex, visible);
            }
        }

        /// <summary>
        /// 특정 슬롯이 페이지 컨트롤러에서 표시 대상으로 취급될지 설정합니다.
        /// 선택 후보가 아닌 아이템은 아이콘만 비우고 슬롯은 남겨 후보 목록 뒤의 빈 슬롯처럼 보이게 합니다.
        /// </summary>
        private void SetSlotFilteringState(int slotIndex, bool visible)
        {
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            UISlot slot = slots[slotIndex] != null ? slots[slotIndex].GetComponent<UISlot>() : null;
            if (slot != null)
            {
                slot.isFiltering = visible;
            }
        }

        /// <summary>
        /// 인벤토리 아이콘을 비울 때 슬롯의 장착 표시도 함께 정리합니다.
        /// 후보 필터링으로 아이템을 숨기는 경우에도 이전 표시가 남지 않게 합니다.
        /// </summary>
        private void ClearInventoryIconAndSlot(UIIconItem uiIcon, int slotIndex)
        {
            uiIcon.SetShowZeroCountText(false);
            uiIcon.ClearIconInfos();
            SetSlotEquippedState(slotIndex, false);
        }

        /// <summary>
        /// 선택 문맥이 0개 아이템 표시를 허용할 때만 인벤토리 후보에 남겨둡니다.
        /// 일반 인벤토리 모드와 기존 문맥은 계속 0개 아이템을 숨깁니다.
        /// </summary>
        private bool ShouldDisplayZeroCountItem(SaveDataIcon itemData, StruckTableItem itemTableData)
        {
            if (itemData == null || itemData.Count > 0)
            {
                return false;
            }

            return _selectionContext is IInventorySelectionZeroCountDisplayPolicy displayPolicy &&
                   displayPolicy.ShouldDisplayZeroCountItem(itemData, itemTableData);
        }

        /// <summary>
        /// 0개 아이템을 보여주는 경우에도 텍스트 표시 여부는 문맥 정책을 따로 확인합니다.
        /// </summary>
        private bool ShouldShowZeroCountText(SaveDataIcon itemData, StruckTableItem itemTableData)
        {
            if (itemData == null || itemData.Count > 0)
            {
                return false;
            }

            return _selectionContext is IInventorySelectionZeroCountDisplayPolicy displayPolicy &&
                   displayPolicy.ShouldShowZeroCountText(itemData, itemTableData);
        }

        /// <summary>
        /// 현재 인벤토리 모드와 선택 문맥 정책에 따라 아이콘/슬롯의 장착 배지 표시 여부를 결정합니다.
        /// 선택 문맥이 있으면 기본적으로 해당 문맥의 장착 상태만 보여주고, 일반 모드에서는 전체 장착 상태를 보여줍니다.
        /// </summary>
        private bool ShouldShowEquippedBadge(UIIconItem icon)
        {
            if (icon == null || icon.uid <= 0)
            {
                return false;
            }

            if (_selectionContext is { IsActive: true })
            {
                InventoryEquippedBadgePolicy policy = GetSelectionContextEquippedBadgePolicy();
                if (policy == InventoryEquippedBadgePolicy.SelectionContextOnly)
                {
                    return _selectionContext.IsEquipped(icon);
                }
            }

            return IsEquippedByAnySource(icon);
        }

        /// <summary>
        /// 선택 문맥이 별도 정책을 제공하지 않으면 문맥과 일치하는 아이템만 장착 배지로 표시합니다.
        /// </summary>
        private InventoryEquippedBadgePolicy GetSelectionContextEquippedBadgePolicy()
        {
            return _selectionContext is IInventoryEquippedBadgePolicyProvider policyProvider
                ? policyProvider.EquippedBadgePolicy
                : InventoryEquippedBadgePolicy.SelectionContextOnly;
        }

        /// <summary>
        /// 장비창 저장 데이터와 등록된 외부 소스를 모두 확인해서 전체 장착 상태를 계산합니다.
        /// </summary>
        private bool IsEquippedByAnySource(UIIconItem icon)
        {
            if (IsEquippedInEquipData(icon))
            {
                return true;
            }

            for (int index = 0; index < _equippedBadgeSources.Count; index++)
            {
                IInventoryEquippedBadgeSource source = _equippedBadgeSources[index];
                if (source != null && source.IsEquipped(icon))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 장비창에 들어간 아이템과 현재 인벤토리 아이콘이 같은 참조인지 확인합니다.
        /// 인스턴스 아이템은 instanceId까지 같아야 하며, 일반 아이템은 uid와 instanceId 0 기준으로 비교합니다.
        /// </summary>
        private bool IsEquippedInEquipData(UIIconItem icon)
        {
            if (icon == null || icon.uid <= 0)
            {
                return false;
            }

            var equippedItems = EquipData != null
                ? EquipData.GetAllItemCounts()
                : SceneGame?.saveDataManager?.Equip?.GetAllItemCounts();
            if (equippedItems == null)
            {
                return false;
            }

            foreach (var pair in equippedItems)
            {
                SaveDataIcon equippedItem = pair.Value;
                if (IsSameItemReference(icon.uid, icon.instanceId, equippedItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 저장 데이터의 아이템 참조가 현재 아이콘과 같은 아이템을 가리키는지 비교합니다.
        /// </summary>
        private static bool IsSameItemReference(int itemUid, long itemInstanceId, SaveDataIcon saveDataIcon)
        {
            return saveDataIcon != null &&
                   saveDataIcon.Uid == itemUid &&
                   saveDataIcon.Count > 0 &&
                   saveDataIcon.InstanceId == itemInstanceId;
        }

        /// <summary>
        /// 슬롯 컴포넌트가 장착 표시 오브젝트를 가지고 있으면 현재 정책으로 계산된 장착 상태를 반영합니다.
        /// </summary>
        private void SetSlotEquippedState(int slotIndex, bool equipped)
        {
            UISlot slot = GetSlotByIndex(slotIndex);
            slot?.SetEquippedState(equipped);
        }

        /// <summary>
        /// 선택 문맥의 실행 버튼 처리입니다.
        /// 인벤토리 아이콘은 이동하지 않고 context가 제공한 저장/등록 로직만 실행합니다.
        /// </summary>
        private void OnClickContextAction()
        {
            if (_selectionContext is not { IsActive: true }) return;

            UIIconItem icon = GetSelectedIcon() as UIIconItem;
            if (icon == null || icon.uid <= 0)
            {
                SceneGame.systemMessageManager.ShowMessageWarning("Inventory_SelectItem");
                return;
            }

            if (!_selectionContext.CanExecute(icon, out string failMessageKey))
            {
                ShowSlotAcceptFailure(failMessageKey);
                icon.HandleInvalidEffect();
                return;
            }

            ResultCommon result = _selectionContext.Execute(icon);
            if (result == null || result.Result != ResultCommon.ResultType.Success)
            {
                ShowSlotAcceptFailure("Inventory_ContextActionFailed");
                icon.HandleInvalidEffect();
                return;
            }

            icon.HandleEquipEffect();
            LoadIcons();
            RefreshContextActionButtons();
        }

        /// <summary>
        /// 선택 문맥의 장착 해제 버튼 처리입니다.
        /// 인벤토리를 열었던 슬롯에 장착된 아이템과 현재 선택 아이템이 같을 때만 해제를 실행합니다.
        /// </summary>
        private void OnClickContextUnequip()
        {
            if (_selectionContext is not { IsActive: true }) return;

            UIIconItem icon = GetSelectedIcon() as UIIconItem;

            if (!_selectionContext.CanUnequip(icon, out string failMessageKey))
            {
                ShowSlotAcceptFailure(failMessageKey);
                icon?.HandleInvalidEffect();
                return;
            }

            ResultCommon result = _selectionContext.Unequip(icon);
            if (result == null || result.Result != ResultCommon.ResultType.Success)
            {
                ShowSlotAcceptFailure("Inventory_ContextUnequipFailed");
                icon?.HandleInvalidEffect();
                return;
            }

            LoadIcons();
            RefreshContextActionButtons();
        }

        /// <summary>
        /// 선택 문맥 버튼들의 활성 상태를 현재 선택/장착 상태에 맞춰 갱신합니다.
        /// 장착해제 버튼은 현재 선택 아이템이 인벤토리를 열었던 슬롯의 장착 아이템과 같을 때만 누를 수 있습니다.
        /// </summary>
        private void RefreshContextActionButtons()
        {
            bool contextActive = _selectionContext is { IsActive: true };
            UIIconItem selectedItem = GetSelectedIcon() as UIIconItem;
            bool canExecuteSelectedItem =
                contextActive &&
                selectedItem != null &&
                selectedItem.uid > 0 &&
                _selectionContext.CanExecute(selectedItem, out _);
            bool canUnequipSelectedItem =
                contextActive && 
                (selectedItem != null && selectedItem.uid > 0) &&
                _selectionContext.CanUnequip(selectedItem, out _);

            if (buttonContextAction != null)
            {
                // 현재 열었던 슬롯에 이미 장착된 아이템을 다시 선택한 경우에는 중복 장착을 막기 위해 장착 버튼을 끕니다.
                if (buttonContextActionHidePolicy == ButtonContextActionHidePolicy.Disable)
                    buttonContextAction.interactable = canExecuteSelectedItem && !canUnequipSelectedItem;
                else if (buttonContextActionHidePolicy == ButtonContextActionHidePolicy.Hide)
                    buttonContextAction.gameObject.SetActive(canExecuteSelectedItem && !canUnequipSelectedItem);
            }
            else if (buttonMergeAllItems != null && contextActive)
            {
                // 별도 장착 버튼이 없는 프리팹에서는 합치기 버튼을 장착 버튼으로 재사용하므로 같은 조건을 적용합니다.
                buttonMergeAllItems.interactable = canExecuteSelectedItem && !canUnequipSelectedItem;
            }
            else if (buttonMergeAllItems != null)
            {
                buttonMergeAllItems.interactable = true;
            }

            if (buttonContextUnequip != null)
            {
                if (buttonContextActionHidePolicy == ButtonContextActionHidePolicy.Disable)
                    buttonContextUnequip.interactable = canUnequipSelectedItem;
                else if (buttonContextActionHidePolicy == ButtonContextActionHidePolicy.Hide)
                    buttonContextUnequip.gameObject.SetActive(canUnequipSelectedItem);
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
            if (TrySelectContextDefaultSlotImmediate())
            {
                return;
            }

            int firstOccupiedIndex = FindFirstOccupiedSlotIndex();
            if (firstOccupiedIndex < 0)
            {
                return;
            }

            base.SetSelectedIcon(firstOccupiedIndex);
            ShowItemInfo(true, GetIconByIndex(firstOccupiedIndex));
        }

        /// <summary>
        /// 선택 문맥이 기본 선택 아이템을 제공하면 해당 인벤토리 슬롯을 우선 선택합니다.
        /// 장착된 스킬 슬롯을 클릭해 인벤토리를 열었을 때, 이미 장착 중인 아이템을 바로 선택하기 위한 처리입니다.
        /// </summary>
        private bool TrySelectContextDefaultSlotImmediate()
        {
            if (_selectionContext is not { IsActive: true } ||
                !_selectionContext.TryGetDefaultSelection(out int itemUid, out long itemInstanceId))
            {
                return false;
            }

            int defaultSlotIndex = FindIconSlotIndex(itemUid, itemInstanceId);
            if (defaultSlotIndex < 0)
            {
                return false;
            }

            pageController?.ShowPageContainingSlot(defaultSlotIndex);
            base.SetSelectedIcon(defaultSlotIndex);
            ShowItemInfo(true, GetIconByIndex(defaultSlotIndex));
            return true;
        }

        /// <summary>
        /// 현재 로드된 인벤토리 아이콘 중 지정한 아이템 참조와 같은 슬롯을 찾습니다.
        /// 인스턴스 아이템은 instanceId까지 같아야 하며, 일반 아이템은 instanceId 0과 uid가 같아야 합니다.
        /// </summary>
        private int FindIconSlotIndex(int itemUid, long itemInstanceId)
        {
            if (itemUid <= 0 || icons == null)
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

                UIIconItem icon = iconObject.GetComponent<UIIconItem>();
                if (icon == null || icon.uid != itemUid)
                {
                    continue;
                }

                if (icon.instanceId != itemInstanceId)
                {
                    continue;
                }

                return icon.index;
            }

            return -1;
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
                if (uiIcon == null || uiIcon.uid <= 0)
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
            // 별도 context 버튼이 없는 프리팹에서는 합치기 버튼을 문맥 실행 버튼으로 재사용합니다.
            if (_selectionContext is { IsActive: true })
            {
                OnClickContextAction();
                return;
            }

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

            // 선택 문맥에서는 우클릭으로 기존 이동/사용 로직이 실행되지 않게 막고, 현재 아이템 선택만 반영합니다.
            if (_selectionContext is { IsActive: true })
            {
                SetSelectedIcon(icon.index);
                return;
            }

            // Simulation 퀵슬롯이 있으면
            if (_uiWindowQuickSlotSimulation != null && _uiWindowQuickSlotSimulation.IsOpen())
            {
                if (icon.IsToolType() || icon.IsSeedType())
                {
                    int targetSlotIndex = _uiWindowQuickSlotSimulation.FindFirstAcceptableEmptySlot(icon);
                    if (targetSlotIndex < 0)
                    {
                        _uiWindowQuickSlotSimulation.ShowSlotAcceptFailure("Slot_ItemNotAllowed");
                        return;
                    }

                    SceneGame.uIWindowManager.MoveIcon(uid, icon.slotIndex,
                        UIWindowConstants.WindowUid.QuickSlotSimulation, icon.GetCount(), targetSlotIndex);
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
                    var uiWindowEquip =
                        SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowEquip>(UIWindowConstants.WindowUid.Equip);
                    if (uiWindowEquip == null) return;

                    // 자동 장착은 더 이상 PartsType enum index 에 고정하지 않고
                    // 현재 장비창 규칙을 만족하는 "빈 슬롯 우선"으로 배치합니다.
                    int partSlotIndex = uiWindowEquip.FindFirstAcceptableEmptySlot(icon);
                    if (partSlotIndex < 0)
                    {
                        partSlotIndex = uiWindowEquip.FindFirstAcceptableSlot(icon);
                    }

                    if (partSlotIndex < 0)
                    {
                        uiWindowEquip.ShowSlotAcceptFailure("Equip_InvalidSlot");
                        return;
                    }

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

        /// <summary>
        /// 아이콘 선택이 바뀔 때 context 버튼 상태를 다시 계산합니다.
        /// 자동 선택처럼 base.SetSelectedIcon을 직접 호출하는 경로도 이 훅을 거칩니다.
        /// </summary>
        protected override void OnSelectedIcon(UIIcon icon)
        {
            base.OnSelectedIcon(icon);
            RefreshContextActionButtons();
        }

        /// <summary>
        /// 선택 아이콘이 사라졌을 때 context 버튼 상태를 다시 계산합니다.
        /// </summary>
        protected override void OnClearedSelectedIcon()
        {
            base.OnClearedSelectedIcon();
            RefreshContextActionButtons();
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
