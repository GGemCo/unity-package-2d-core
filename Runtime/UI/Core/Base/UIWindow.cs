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

        protected UIIcon selectedIcon;

        // 서브 매니저
        protected IconPoolManager IconPoolManager;
        protected IconDragDropHandler DragDropHandler;

        private Dictionary<int, UISlotAcceptRule> _slotAcceptRuleByIndex;

        protected override void Awake()
        {
            base.Awake();
            BuildSlotAcceptRuleCache();

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
        /// 대상 슬롯이 주어진 아이콘을 받을 수 있는지 판단합니다.
        /// Drag & Drop 과 자동 장착/등록 모두 이 진입점을 사용합니다.
        /// </summary>
        public virtual bool CanAcceptIcon(UIIcon icon, int slotIndex, out string failMessageKey)
        {
            failMessageKey = null;

            if (slotIndex < 0 || slotIndex >= maxCountIcon)
                return false;

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
