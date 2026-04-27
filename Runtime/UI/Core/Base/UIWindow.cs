using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 단위로 아이콘/슬롯/드래그 기능을 공통 제공하는 기본 UI 창입니다.
    /// </summary>
    public class UIWindow : UIWindowBase, IDropHandler
    {
        [Tooltip("윈도우가 주로 다루는 아이콘 타입입니다.")]
        public IconConstants.Type iconType;

        [Tooltip("윈도우가 보유하는 최대 슬롯 수입니다.")]
        public int maxCountIcon;

        [Tooltip("슬롯 프리팹입니다.")]
        public GameObject slotPrefab;

        [Tooltip("아이콘 프리팹입니다.")]
        public GameObject iconPrefab;

        [HideInInspector] public GameObject[] slots;
        [HideInInspector] public GameObject[] icons;

        [Tooltip("슬롯 크기입니다. GridLayoutGroup 셀 크기로도 사용됩니다.")]
        public Vector2 slotSize;

        [Tooltip("아이콘 크기입니다.")]
        public Vector2 iconSize;

        [Tooltip("미리 만들어 둔 슬롯이 있을 때 연결합니다.")]
        public GameObject[] preLoadSlots;

        [Tooltip("미리 만들어 둔 아이콘이 있을 때 연결합니다.")]
        public GameObject[] preLoadIcons;

        [Tooltip("아이콘이 배치될 GridLayoutGroup 입니다.")]
        public GridLayoutGroup containerIcon;

        [Tooltip("아이콘 선택 이미지를 표시할지 여부입니다.")]
        public bool showSelectedIconImage = true;

        [Tooltip("아이콘 마우스 오버 이미지를 표시할지 여부입니다.")]
        public bool showOverIconImage = true;

        [Header("Slot Accept Rules")]
        [Tooltip("윈도우 전체에 적용할 기본 슬롯 수용 규칙입니다.")]
        [SerializeField] private UISlotAcceptRule defaultAcceptRule = new UISlotAcceptRule();

        [Tooltip("특정 슬롯에만 적용할 수용 규칙 오버라이드입니다.")]
        [SerializeField] private UISlotAcceptRuleOverride[] slotAcceptRules;

        [Header("Inactive Slots")]
        [Tooltip("아이콘 정보 없이 비활성 상태로 표시할 슬롯 목록입니다. WindowUid가 None이면 현재 윈도우로 처리합니다.")]
        [SerializeField] private UISlotInactiveState[] inactiveSlots;
        [Tooltip("비활성 슬롯에 아이콘을 배치하려고 할 때 출력할 메시지 키입니다.")]
        [SerializeField] private string inactiveSlotFailMessageKey = "Slot_Inactive";

        [Header("Default Active State")]
        [Tooltip("UIWindow가 생성하거나 초기화하는 슬롯 GameObject의 기본 활성 상태입니다.")]
        [SerializeField] private bool defaultSlotActive = true;
        [Tooltip("UIWindow가 생성하거나 초기화하는 아이콘 GameObject의 기본 활성 상태입니다.")]
        [SerializeField] private bool defaultIconActive = true;

        protected UIIcon selectedIcon;

        // 서브 매니저
        protected IconPoolManager IconPoolManager;
        protected IconDragDropHandler DragDropHandler;

        private Dictionary<int, UISlotAcceptRule> _slotAcceptRuleByIndex;
        private HashSet<int> _inactiveSlotIndexes;

        protected override void Awake()
        {
            base.Awake();
            BuildSlotAcceptRuleCache();
            BuildInactiveSlotCache();

            if (containerIcon != null && containerIcon.cellSize == Vector2.zero && slotSize != Vector2.zero)
            {
                containerIcon.cellSize = new Vector2(slotSize.x, slotSize.y);
            }

            InitializeIconPoolManager();
            DragDropHandler = new IconDragDropHandler(this);
        }

        /// <summary>
        /// Inspector 에서 입력한 슬롯별 규칙을 빠르게 조회할 수 있도록 캐시합니다.
        /// </summary>
        private void BuildSlotAcceptRuleCache()
        {
            _slotAcceptRuleByIndex = new Dictionary<int, UISlotAcceptRule>();
            if (slotAcceptRules == null || slotAcceptRules.Length == 0)
                return;

            for (int i = 0; i < slotAcceptRules.Length; i++)
            {
                var entry = slotAcceptRules[i];
                if (entry == null || entry.slotIndex < 0)
                    continue;

                _slotAcceptRuleByIndex[entry.slotIndex] = entry.rule;
            }
        }

        /// <summary>
        /// Inspector에 입력된 비활성 슬롯 정보를 현재 윈도우 기준 인덱스 집합으로 캐시합니다.
        /// 비활성 상태는 아이콘 데이터가 아니라 WindowUid와 SlotIndex만 가진 슬롯 메타 상태입니다.
        /// </summary>
        private void BuildInactiveSlotCache()
        {
            _inactiveSlotIndexes = new HashSet<int>();
            if (inactiveSlots == null || inactiveSlots.Length == 0)
                return;

            for (int i = 0; i < inactiveSlots.Length; i++)
            {
                var entry = inactiveSlots[i];
                if (entry == null || entry.slotIndex < 0)
                    continue;

                if (entry.windowUid != UIWindowConstants.WindowUid.None && entry.windowUid != uid)
                    continue;

                _inactiveSlotIndexes.Add(entry.slotIndex);
            }
        }

        /// <summary>
        /// 아이콘 pool 을 초기화합니다.
        /// </summary>
        private void InitializeIconPoolManager()
        {
            IconPoolManager = new IconPoolManager();
            IconPoolManager.Initialize(this);
        }

        /// <summary>
        /// 현재 윈도우의 선택 아이콘을 단일 진입점으로 관리합니다.
        /// </summary>
        public virtual void SetSelectedIcon(int index)
        {
            if (selectedIcon != null)
            {
                selectedIcon.SetSelected(false);
                selectedIcon = null;
            }

            if (icons.Length <= 0 || index >= icons.Length)
            {
                OnClearedSelectedIcon();
                return;
            }

            if (IsSlotInactive(index))
            {
                OnClearedSelectedIcon();
                return;
            }

            var icon = icons[index];
            if (icon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            selectedIcon = icon.GetComponent<UIIcon>();
            if (selectedIcon == null)
            {
                OnClearedSelectedIcon();
                return;
            }

            selectedIcon.SetSelected(true);
            OnSelectedIcon(selectedIcon);
        }

        public void RemoveSelectedIcon()
        {
            if (selectedIcon == null) return;

            selectedIcon.SetSelected(false);
            selectedIcon = null;
            OnClearedSelectedIcon();
        }

        public UIIcon GetSelectedIcon() => selectedIcon;

        /// <summary>
        /// 파생 윈도우가 선택 시 추가 동작을 붙일 수 있도록 열어 둔 훅입니다.
        /// </summary>
        protected virtual void OnSelectedIcon(UIIcon icon)
        {
        }

        /// <summary>
        /// 선택이 해제될 때 파생 윈도우가 정리 로직을 넣을 수 있는 훅입니다.
        /// </summary>
        protected virtual void OnClearedSelectedIcon()
        {
        }

        public void OnDrop(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 아이콘 위로 드래그가 끝났을 때 드래그 전략에 위임합니다.
        /// </summary>
        public void OnEndDragInIcon(GameObject droppedIcon, GameObject targetIcon) =>
            DragDropHandler?.HandleDragInIcon(droppedIcon, targetIcon);

        public void OnEndDragInWindow(GameObject droppedIcon) =>
            DragDropHandler?.HandleDragInWindow(droppedIcon);

        /// <summary>
        /// 윈도우 바깥으로 드래그가 끝났을 때 처리합니다.
        /// </summary>
        public void OnEndDragOutWindow(PointerEventData eventData, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition) =>
            DragDropHandler?.HandleDragOut(eventData, droppedIcon, targetIcon, originalPosition);

        /// <summary>
        /// UIWindow가 생성하거나 초기화하는 슬롯의 기본 활성 상태를 반환합니다.
        /// </summary>
        /// <returns>기본 활성 상태이면 true입니다.</returns>
        public bool GetDefaultSlotActive() => defaultSlotActive;

        /// <summary>
        /// UIWindow가 생성하거나 초기화하는 아이콘의 기본 활성 상태를 반환합니다.
        /// </summary>
        /// <returns>기본 활성 상태이면 true입니다.</returns>
        public bool GetDefaultIconActive() => defaultIconActive;

        /// <summary>
        /// 슬롯과 아이콘 GameObject에 UIWindow의 기본 활성 상태를 적용합니다.
        /// 이 설정은 비활성 슬롯 시스템과 별개로, 생성 직후 activeSelf 기본값만 결정합니다.
        /// </summary>
        /// <param name="slotObj">기본 활성 상태를 적용할 슬롯 GameObject입니다.</param>
        /// <param name="iconObj">기본 활성 상태를 적용할 아이콘 GameObject입니다.</param>
        public void ApplyDefaultSlotIconActiveState(GameObject slotObj, GameObject iconObj)
        {
            if (slotObj != null)
            {
                slotObj.SetActive(defaultSlotActive);
            }

            if (iconObj != null)
            {
                iconObj.SetActive(defaultIconActive);
            }
        }

        /// <summary>
        /// 지정한 슬롯이 비활성 상태인지 반환합니다.
        /// </summary>
        /// <param name="slotIndex">확인할 슬롯 인덱스입니다.</param>
        /// <returns>비활성 슬롯이면 true입니다.</returns>
        public bool IsSlotInactive(int slotIndex)
        {
            if (_inactiveSlotIndexes == null || !_inactiveSlotIndexes.Contains(slotIndex))
            {
                return false;
            }

            UIWindowManager windowManager = SceneGame?.uIWindowManager;
            return windowManager == null || !windowManager.IsWindowSlotActivated(uid, slotIndex);
        }

        /// <summary>
        /// 지정한 슬롯의 비활성 상태를 런타임에 변경합니다.
        /// 비활성으로 전환할 때 기존 아이콘 정보가 있으면 먼저 제거합니다.
        /// </summary>
        /// <param name="slotIndex">변경할 슬롯 인덱스입니다.</param>
        /// <param name="inactive">비활성 여부입니다.</param>
        public void SetSlotInactive(int slotIndex, bool inactive)
        {
            if (slotIndex < 0 || slotIndex >= maxCountIcon)
                return;

            _inactiveSlotIndexes ??= new HashSet<int>();
            if (inactive)
            {
                _inactiveSlotIndexes.Add(slotIndex);
                UIIcon icon = null;
                if (icons != null && slotIndex < icons.Length)
                {
                    icon = icons[slotIndex]?.GetComponent<UIIcon>();
                }

                if (icon != null && icon.uid > 0)
                {
                    DetachIcon(slotIndex);
                }
            }
            else
            {
                _inactiveSlotIndexes.Remove(slotIndex);
            }

            RefreshInactiveSlotState(slotIndex);
        }

        /// <summary>
        /// 모든 슬롯과 아이콘에 현재 비활성 슬롯 캐시를 반영합니다.
        /// 슬롯/아이콘 풀 생성 직후 또는 외부 설정 재적용 시 호출합니다.
        /// </summary>
        public void RefreshInactiveSlotStates()
        {
            if (maxCountIcon <= 0)
            {
                return;
            }

            for (int i = 0; i < maxCountIcon; i++)
            {
                RefreshInactiveSlotState(i);
            }
        }

        /// <summary>
        /// 지정 슬롯 하나의 비활성 시각 상태를 슬롯과 아이콘에 동시에 반영합니다.
        /// </summary>
        /// <param name="slotIndex">갱신할 슬롯 인덱스입니다.</param>
        public void RefreshInactiveSlotState(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return;
            }

            bool inactive = IsSlotInactive(slotIndex);
            if (slots != null && slotIndex < slots.Length)
            {
                GameObject slotObject = slots[slotIndex];
                if (slotObject != null)
                {
                    slotObject.GetComponent<UISlot>()?.SetInactiveState(inactive);
                }
            }

            if (icons != null && slotIndex < icons.Length)
            {
                GameObject iconObject = icons[slotIndex];
                if (iconObject != null)
                {
                    iconObject.GetComponent<UIIcon>()?.SetInactiveState(inactive);
                }
            }
        }

        /// <summary>
        /// 대상 슬롯이 주어진 아이콘을 받을 수 있는지 판단합니다.
        /// Drag & Drop 과 자동 장착/등록 모두 이 진입점을 사용합니다.
        /// </summary>
        public virtual bool CanAcceptIcon(UIIcon icon, int slotIndex, out string failMessageKey)
        {
            failMessageKey = null;

            if (slotIndex < 0 || slotIndex >= maxCountIcon)
                return false;

            if (IsSlotInactive(slotIndex))
            {
                failMessageKey = inactiveSlotFailMessageKey;
                return false;
            }

            var rule = GetAcceptRule(slotIndex);
            return UISlotAcceptRuleEvaluator.CanAccept(rule, icon, out failMessageKey);
        }

        /// <summary>
        /// 규칙을 만족하는 첫 번째 빈 슬롯을 찾습니다.
        /// 우클릭 자동 등록처럼 목표 슬롯이 정해지지 않은 흐름에서 사용합니다.
        /// </summary>
        public virtual int FindFirstAcceptableSlot(UIIcon icon, bool requireEmpty = false)
        {
            if (icon == null || icons == null)
                return -1;

            for (int slotIndex = 0; slotIndex < maxCountIcon; slotIndex++)
            {
                var targetIcon = GetIconByIndex(slotIndex);
                if (requireEmpty && targetIcon != null && targetIcon.uid > 0)
                    continue;

                if (CanAcceptIcon(icon, slotIndex, out _))
                    return slotIndex;
            }

            return -1;
        }

        public virtual int FindFirstAcceptableEmptySlot(UIIcon icon) => FindFirstAcceptableSlot(icon, true);

        /// <summary>
        /// 슬롯 수용 규칙 실패 메시지를 공통 방식으로 노출합니다.
        /// </summary>
        public virtual void ShowSlotAcceptFailure(string failMessageKey)
        {
            if (string.IsNullOrEmpty(failMessageKey))
                return;

            SceneGame?.systemMessageManager?.ShowMessageWarning(failMessageKey);
        }

        /// <summary>
        /// 아이콘 정보를 제거합니다.
        /// </summary>
        public void DetachIcon(int slotIndex) => IconPoolManager.DetachIcon(slotIndex);

        public virtual UIIcon GetIconByIndex(int index) => IconPoolManager.GetIcon(index);
        public virtual UISlot GetSlotByIndex(int index) => IconPoolManager.GetSlot(index);
        public UIIcon GetIconByUid(int iconUid) => IconPoolManager.GetIconByUid(iconUid);

        public UIIcon SetIconCount(int slotIndex, int itemUid, int count, int level = 0, bool learn = false,
            long instanceId = 0, IconConstants.Type type = IconConstants.Type.None) =>
            IconPoolManager.SetIcon(slotIndex, itemUid, count, level, learn, instanceId, type);

        public virtual void SetIconCount(int iconUid, int iconCount, long instanceId = 0) =>
            IconPoolManager.SetIconCount(iconUid, iconCount, instanceId);

        public UIIcon SetIconCountReturnIcon(int slotIndex, int iconUid, int iconCount, int iconLevel = 0,
            bool iconLearn = false, long instanceId = 0) =>
            IconPoolManager.SetIcon(slotIndex, iconUid, iconCount, iconLevel, iconLearn, instanceId);

        public UIIcon SetIconCountReturnIcon(int iconUid, int iconCount, long instanceId = 0) =>
            IconPoolManager.SetIconCountReturnIcon(iconUid, iconCount, instanceId);

        /// <summary>
        /// 이동 결과 아이콘 목록을 일괄 반영합니다.
        /// </summary>
        public void SetIcons(ResultCommon result) => IconPoolManager.SetIcons(result);

        /// <summary>
        /// 등록형 윈도우에 들어 있는 아이콘을 모두 해제합니다.
        /// </summary>
        protected void UnRegisterAllIcons(UIWindowConstants.WindowUid fromWindowUid,
            UIWindowConstants.WindowUid toWindowUid = UIWindowConstants.WindowUid.Inventory) =>
            IconPoolManager?.UnRegisterAllIcons(fromWindowUid, toWindowUid);

        /// <summary>
        /// 현재 윈도우에 등록된 아이콘을 지우고 연결도 해제합니다.
        /// </summary>
        protected void RemoveAndDetachIcon() => IconPoolManager.RemoveAndDetachIcon();

        /// <summary>
        /// 모든 아이콘을 detach 합니다.
        /// </summary>
        protected void DetachAllIcons() => IconPoolManager.DetachAllIcons();

        /// <summary>
        /// 슬롯 index 에 적용할 실제 규칙을 결정합니다.
        /// </summary>
        private UISlotAcceptRule GetAcceptRule(int slotIndex)
        {
            if (_slotAcceptRuleByIndex != null &&
                _slotAcceptRuleByIndex.TryGetValue(slotIndex, out var slotRule) &&
                slotRule != null &&
                slotRule.mode != UISlotAcceptMode.Inherit)
            {
                return slotRule;
            }

            if (defaultAcceptRule != null && defaultAcceptRule.mode != UISlotAcceptMode.Inherit)
                return defaultAcceptRule;

            return GetFallbackAcceptRule(slotIndex);
        }

        /// <summary>
        /// 아직 Inspector 마이그레이션이 끝나지 않은 윈도우를 위한 코드 fallback 입니다.
        /// </summary>
        protected virtual UISlotAcceptRule GetFallbackAcceptRule(int slotIndex)
        {
            return null;
        }
    }
}
