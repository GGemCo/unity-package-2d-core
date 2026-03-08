using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우, 아이콘 기능 포함
    /// </summary>
    public class UIWindow : UIWindowBase, IDropHandler
    {
        // 공개(public) → 보호(protected) → 내부(internal) → 비공개(private) 
        // 상수(const), 정적(static) 필드 → 인스턴스 필드 → 속성(Properties) → 생성자(Constructors) → 메서드(Methods)
        [Tooltip("아이콘 타입")] 
        public IconConstants.Type iconType;
        [Tooltip("사용할 최대 아이콘 개수")]
        public int maxCountIcon;
        [Tooltip("슬롯 프리팹")]
        public GameObject slotPrefab;
        [Tooltip("아이콘 프리팹")]
        public GameObject iconPrefab;
        [Tooltip("윈도우 On/Off 시 fade in/Out 효과 사용 여부")]
        [HideInInspector] public GameObject[] slots;
        [HideInInspector] public GameObject[] icons;
        [Tooltip("slot 사이즈. 보통 icon size 보다 크게 설정")]
        public Vector2 slotSize;
        [Tooltip("icon 사이즈")]
        public Vector2 iconSize;
        
        [Tooltip("미리 만들어놓은 slot 이 있을 경우")]
        public GameObject[] preLoadSlots;
        [Tooltip("미리 만들어놓은 icon 이 있을 경우")]
        public GameObject[] preLoadIcons;
        [Tooltip("icon 이 들어갈 panel")]
        public GridLayoutGroup containerIcon;
        
        [Tooltip("아이콘 선택 시 이미지 표시 여부")]
        public bool showSelectedIconImage = true;
        [Tooltip("아이콘 마우스 오버 이미지 표시 여부")]
        public bool showOverIconImage = true;
        
        protected UIIcon selectedIcon;
        
        // 서브 매니저
        // 아이콘 생성 관리
        protected IconPoolManager IconPoolManager;
        // 아이콘 드래그 관리
        protected IconDragDropHandler DragDropHandler;

        protected override void Awake()
        {
            base.Awake();
            if (containerIcon != null && containerIcon.cellSize == Vector2.zero && slotSize != Vector2.zero)
            {
                containerIcon.cellSize = new Vector2(slotSize.x, slotSize.y);
            }

            // 기능 위임 객체 생성
            InitializeIconPoolManager();
            DragDropHandler = new IconDragDropHandler(this);
        }
        /// <summary>
        /// 아이콘 pool 초기화
        /// </summary>
        private void InitializeIconPoolManager()
        {
            IconPoolManager = new IconPoolManager();
            IconPoolManager.Initialize(this);
        }

        public virtual void SetSelectedIcon(int index)
        {
            if (selectedIcon != null)
            {
                selectedIcon.SetSelected(false);
            }
            if (icons.Length <= 0 || index >= icons.Length) return;
            var icon = icons[index];
            if (icon == null) return;
            selectedIcon = icon.GetComponent<UIIcon>();
            selectedIcon.SetSelected(true);
            OnSelectedIcon(selectedIcon);
        }

        public void RemoveSelectedIcon()
        {
            if (selectedIcon == null) return;
            selectedIcon.SetSelected(false);
        }
        public UIIcon GetSelectedIcon() => selectedIcon;

        /// <summary>
        /// 아이콘 선택시 후 처리
        /// </summary>
        /// <param name="icon"></param>
        protected virtual void OnSelectedIcon(UIIcon icon)
        {
        }

        public void OnDrop(PointerEventData eventData)
        {
        }
        /// <summary>
        /// 아이콘 위에서 드래그가 끝났을때 처리 
        /// </summary>
        /// <param name="droppedIcon">드랍한 한 아이콘</param>
        /// <param name="targetIcon">드랍되는 곳에 있는 아이콘</param>
        public void OnEndDragInIcon(GameObject droppedIcon, GameObject targetIcon) =>
            DragDropHandler?.HandleDragInIcon(droppedIcon, targetIcon);
        
        public void OnEndDragInWindow(GameObject droppedIcon) =>
            DragDropHandler?.HandleDragInWindow(droppedIcon);

        /// <summary>
        ///  window 밖에다 드래그앤 드랍 했을때 처리 
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="droppedIcon"></param>
        /// <param name="targetIcon"></param>
        /// <param name="originalPosition"></param>
        public void OnEndDragOutWindow(PointerEventData eventData, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition) =>
            DragDropHandler?.HandleDragOut(eventData, droppedIcon, targetIcon, originalPosition);
        /// <summary>
        /// 아이콘 지우기 
        /// </summary>
        /// <param name="slotIndex"></param>
        public void DetachIcon(int slotIndex) => IconPoolManager.DetachIcon(slotIndex);
        public virtual UIIcon GetIconByIndex(int index) => IconPoolManager.GetIcon(index);
        public virtual UISlot GetSlotByIndex(int index) => IconPoolManager.GetSlot(index);
        public UIIcon GetIconByUid(int iconUid) => IconPoolManager.GetIconByUid(iconUid);
        public UIIcon SetIconCount(int slotIndex, int itemUid, int count, int level = 0, bool learn = false, long instanceId = 0, IconConstants.Type type = IconConstants.Type.None) => IconPoolManager.SetIcon(slotIndex, itemUid, count, level, learn, instanceId, type);
        public virtual void SetIconCount(int iconUid, int iconCount, long instanceId = 0) => IconPoolManager.SetIconCount(iconUid, iconCount, instanceId);
        public UIIcon SetIconCountReturnIcon(int slotIndex, int iconUid, int iconCount, int iconLevel = 0, bool iconLearn = false, long instanceId = 0) => IconPoolManager.SetIcon(slotIndex, iconUid, iconCount, iconLevel, iconLearn, instanceId);
        public UIIcon SetIconCountReturnIcon(int iconUid, int iconCount, long instanceId = 0) => IconPoolManager.SetIconCountReturnIcon(iconUid, iconCount, instanceId);

        /// <summary>
        /// 아이콘 이동 후 슬롯별 uid, count 처리  
        /// </summary>
        /// <param name="result"></param>
        public void SetIcons(ResultCommon result) => IconPoolManager.SetIcons(result);

        /// <summary>
        /// 모든 아이콘 Un Register 처리 하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="toWindowUid"></param>
        protected void UnRegisterAllIcons(UIWindowConstants.WindowUid fromWindowUid,
            UIWindowConstants.WindowUid toWindowUid = UIWindowConstants.WindowUid.Inventory) =>
            IconPoolManager.UnRegisterAllIcons(fromWindowUid, toWindowUid);

        /// <summary>
        /// 해당 윈도우에 있던 아이콘은 Detach 하고, Register 되었던 인벤토리 아이템은 지운다.
        /// </summary>
        protected void RemoveAndDetachIcon() => IconPoolManager.RemoveAndDetachIcon();

        /// <summary>
        /// 모든 아이콘 detach 하기
        /// </summary>
        protected void DetachAllIcons() => IconPoolManager.DetachAllIcons();

    }
}
