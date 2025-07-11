using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 
    /// </summary>
    public class UIWindow : MonoBehaviour, IDropHandler
    {
        // 공개(public) → 보호(protected) → 내부(internal) → 비공개(private) 
        // 상수(const), 정적(static) 필드 → 인스턴스 필드 → 속성(Properties) → 생성자(Constructors) → 메서드(Methods)
        // 윈도우 고유번호
        [HideInInspector] public UIWindowConstants.WindowUid uid;
        [Header(UIWindowConstants.TitleHeaderCommon)] 
        [Tooltip("윈도우 닫기 버튼")] 
        public Button buttonClose;
        [Tooltip("아이콘 타입")] 
        public IconConstants.Type iconType;
        [Tooltip("사용할 최대 아이콘 개수")]
        public int maxCountIcon;
        [Tooltip("윈도우 On/Off 시 fade in/Out 효과 사용 여부")]
        public bool useFade = true;
        [HideInInspector] public GameObject[] slots;
        [HideInInspector] public GameObject[] icons;
        [Tooltip("slot 사이즈. 보통 icon size 보다 크게 설정")]
        public Vector2 slotSize;
        [Tooltip("icon 사이즈")]
        public Vector2 iconSize;
        
        [Tooltip("미리 만들어놓은 slot 이 있을 경우")]
        public GameObject[] preLoadSlots;
        [Tooltip("icon 이 들어갈 panel")]
        public GridLayoutGroup containerIcon;
        
        private UIIcon _selectedIcon;
        
        private UIWindowFade _uiWindowFade;
        private StruckTableWindow _struckTableWindow;
        private InteractionManager _interactionManager;
        protected SceneGame SceneGame;
        
        // 서브 매니저
        // 아이콘 생성 관리
        protected IconPoolManager IconPoolManager;
        // 아이콘 드래그 관리
        protected IconDragDropHandler DragDropHandler;

        protected virtual void Awake()
        {
            // 사용하지 않는 index 가 있을 수 있으므로 미리 만들어 두어야 건너 띄어도 문제가 없다.
            // maxCountIcon 이 0 일때, 예외처리
            if (maxCountIcon == 0 && preLoadSlots.Length > 0)
            {
                maxCountIcon = preLoadSlots.Length;
            }
            slots = new GameObject[maxCountIcon];
            icons = new GameObject[maxCountIcon];
            
            gameObject.AddComponent<CanvasGroup>();
            if (useFade)
            {
                _uiWindowFade = gameObject.AddComponent<UIWindowFade>();
            }

            if (containerIcon != null && containerIcon.cellSize == Vector2.zero && slotSize != Vector2.zero)
            {
                containerIcon.cellSize = new Vector2(slotSize.x, slotSize.y);
            }

            if (buttonClose != null)
            {
                buttonClose.onClick.RemoveAllListeners();
                buttonClose.onClick.AddListener(OnClickClose);
            }

            // 기능 위임 객체 생성
            IconPoolManager = new IconPoolManager(this);
            // 커스텀 전략 설정 지점
            var strategy = GetSlotIconBuildStrategy();
            if (strategy != null)
                IconPoolManager.SetBuildStrategy(strategy);

            IconPoolManager.Initialize();
            
            DragDropHandler = new IconDragDropHandler(this);
        }
        /// <summary>
        /// 커스텀 빌드 전략을 반환. 기본은 null → Default 사용
        /// </summary>
        private ISlotIconBuildStrategy GetSlotIconBuildStrategy()
        {
            // Pre Load Slots 를 사용하면 예외처리 
            if (preLoadSlots.Length > 0)
                return new SlotIconBuildStrategyPreLoad();
            
            return uid switch
            {
                UIWindowConstants.WindowUid.Skill => new SlotIconBuildStrategySkill(),
                UIWindowConstants.WindowUid.ItemSalvage => new SlotIconBuildStrategyItemSalvage(),
                // UIWindowConstants.WindowUid.QuestReward => new SlotIconBuildStrategyQuestReward(),
                _ => null,
            };
        }

        protected void SetSetIconHandler(ISetIconHandler handler)
        {
            if (IconPoolManager != null)
                IconPoolManager.SetSetIconHandler(handler);
        }
        protected virtual void Start()
        {
            if (_struckTableWindow is { DefaultActive: false })
            {
                gameObject.SetActive(false);
            }
            SceneGame = SceneGame.Instance;
            _interactionManager = SceneGame.InteractionManager;
        }
        /// <summary>
        /// 아이콘 지우기 
        /// </summary>
        /// <param name="slotIndex"></param>
        public void DetachIcon(int slotIndex)
        {
            IconPoolManager.DetachIcon(slotIndex);
        }
        public UIIcon GetIconByIndex(int index) => IconPoolManager.GetIcon(index);
        public UISlot GetSlotByIndex(int index) => IconPoolManager.GetSlot(index);
        public UIIcon GetIconByUid(int iconUid) => IconPoolManager.GetIconByUid(iconUid);
        public UIIcon SetIconCount(int slotIndex, int windowUid, int count, int level = 0, bool learn = false) => IconPoolManager.SetIcon(slotIndex, windowUid, count, level, learn);

        /// <summary>
        /// 빈 슬롯 찾기
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < maxCountIcon; i++)
            {
                var icon = icons[i];
                if (icon == null) continue;
                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                if (uiIcon.uid <= 0 || uiIcon.GetCount() <= 0)
                    return i;
            }
            return -1;
        }
        public virtual void SetIconCount(int iconUid, int iconCount)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                SceneGame.popupManager.ShowPopupError("There is no empty space in the window.");//"윈도우에 빈 공간이 없습니다."
                return;
            }
            SetIconCount(emptySlot, iconUid, iconCount);
        }

        public UIIcon SetIconCountReturnIcon(int slotIndex, int iconUid, int iconCount, int iconLevel = 0,
            bool iconLearn = false)
        {
            return SetIconCount(slotIndex, iconUid, iconCount, iconLevel, iconLearn);
        }
        public UIIcon SetIconCountReturnIcon(int iconUid, int iconCount)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                SceneGame.popupManager.ShowPopupError("There is no empty space in the window.");//"윈도우에 빈 공간이 없습니다."
                return null;
            }
            return SetIconCount(emptySlot, iconUid, iconCount);
        }
        /// <summary>
        /// 아이콘 이동 후 슬롯별 uid, count 처리  
        /// </summary>
        /// <param name="result"></param>
        public void SetIcons(ResultCommon result)
        {
            if (!result.IsSuccess())
            {
                GcLogger.LogError(result.Message);
                SceneGame.systemMessageManager.ShowMessageWarning(result.Message);
            }
            if (result.ResultIcons == null || result.ResultIcons.Count <= 0) return;
            foreach (var icon in result.ResultIcons)
            {
                SetIconCount(icon.SlotIndex, icon.Uid, icon.Count, icon.Level, icon.IsLearned);
            }
        }
        public void OnDrop(PointerEventData eventData)
        {
        }
        /// <summary>
        /// 아이콘 위에서 드래그가 끝났을때 처리 
        /// </summary>
        /// <param name="droppedIcon">드랍한 한 아이콘</param>
        /// <param name="targetIcon">드랍되는 곳에 있는 아이콘</param>
        public void OnEndDragInIcon(GameObject droppedIcon, GameObject targetIcon)
        {
            // GcLogger.Log("OnEndDragInIcon");
            DragDropHandler?.HandleDragInIcon(droppedIcon, targetIcon);
        }
        /// <summary>
        ///  window 밖에다 드래그앤 드랍 했을때 처리 
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="droppedIcon"></param>
        /// <param name="targetIcon"></param>
        /// <param name="originalPosition"></param>
        public void OnEndDragOutWindow(PointerEventData eventData, GameObject droppedIcon, GameObject targetIcon, Vector3 originalPosition)
        {
            // GcLogger.Log("OnEndDragInIcon");
            DragDropHandler?.HandleDragOut(eventData, droppedIcon, targetIcon, originalPosition);
        }
        /// <summary>
        /// window 테이블에 있는 OpenWindowUid, CloseWindowUid 컬럼 처리
        /// </summary>
        /// <param name="windowUids"></param>
        /// <param name="show"></param>
        private void ShowByTable(int[] windowUids, bool show)
        {
            foreach (var openWindowUid in windowUids)
            {
                UIWindowConstants.WindowUid windowUid = (UIWindowConstants.WindowUid)openWindowUid;
                UIWindow uiWindow = SceneGame.uIWindowManager.GetUIWindowByUid<UIWindow>(windowUid);
                if (uiWindow == null) continue;
                    
                if (uiWindow._uiWindowFade == null)
                {
                    if (uiWindow.gameObject == null) continue;
                    uiWindow.gameObject.SetActive(show);
                    uiWindow.OnShow(show); 
                    continue;
                }

                if (show)
                {
                    uiWindow._uiWindowFade.ShowPanel();
                }
                else
                {
                    uiWindow._uiWindowFade.HidePanel();
                }
            }
        }
        /// <summary>
        /// 윈도우 open/close
        /// </summary>
        /// <param name="show"></param>
        public virtual bool Show(bool show)
        {
            if (_uiWindowFade == null)
            {
                if (gameObject == null) return false;
                gameObject.SetActive(show);
                OnShow(show);
                return false;
            }
            if (show)
            {
                _uiWindowFade.ShowPanel();
                ShowByTable(_struckTableWindow.OpenWindowUid, true);
            }
            else
            {
                _uiWindowFade.HidePanel();
                ShowByTable(_struckTableWindow.CloseWindowUid, false);
            }

            return true;
        }
        /// <summary>
        /// 윈도우가 show 가 된 후 처리 
        /// </summary>
        public virtual void OnShow(bool show)
        {
            
        }

        public void OnClickClose()
        {
            if (_uiWindowFade == null) return;
            if (_struckTableWindow.IsInteraction && _interactionManager != null && _interactionManager.IsInteractioning())
            {
                _interactionManager.EndInteraction();
            }
            else
            {
                Show(false);
            }
        }
        /// <summary>
        /// 각 윈도우에 table 정보 연결하기
        /// </summary>
        /// <param name="pstruckTableWindow"></param>
        public void SetTableWindow(StruckTableWindow pstruckTableWindow)
        {
            _struckTableWindow = pstruckTableWindow;
        }

        public virtual void SetSelectedIcon(int index)
        {
            if (_selectedIcon != null)
            {
                _selectedIcon.SetSelected(false);
            }
            if (icons.Length <= 0) return;
            var icon = icons[index];
            if (icon == null) return;
            _selectedIcon = icon.GetComponent<UIIcon>();
            _selectedIcon.SetSelected(true);
        }
        public virtual void OnRightClick(UIIcon icon)
        {
            
        }
        public bool GetDefaultActive()
        {
            return _struckTableWindow.DefaultActive;
        }

        public bool IsOpen()
        {
            return gameObject.activeSelf;
        }

        public virtual void ShowItemInfo(UIIcon icon)
        {
        }
        /// <summary>
        /// 모든 아이콘 Un Register 처리 하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="toWindowUid"></param>
        protected void UnRegisterAllIcons(UIWindowConstants.WindowUid fromWindowUid, UIWindowConstants.WindowUid toWindowUid = UIWindowConstants.WindowUid.Inventory)
        {
            if (IconPoolManager == null) return;
            IconPoolManager.UnRegisterAllIcons(fromWindowUid, toWindowUid);
        }
        /// <summary>
        /// 해당 윈도우에 있던 아이콘은 Detach 하고, Register 되었던 인벤토리 아이템은 지운다.
        /// </summary>
        protected void RemoveAndDetachIcon()
        {
            foreach (var icon in icons)
            {
                UIIconItem uiIconItem = icon.GetComponent<UIIconItem>();
                if (uiIconItem == null || uiIconItem.uid <= 0 || uiIconItem.GetCount() <= 0) continue;
                var parentInfo = uiIconItem.GetParentInfo();
                // 등록 되었던것을 빼준다.
                DetachIcon(uiIconItem.slotIndex);
                // 인벤토리에서 지워준다.
                if (parentInfo.Item1 != UIWindowConstants.WindowUid.None)
                {
                    SceneGame.uIWindowManager.RemoveIcon(parentInfo.Item1, parentInfo.Item2);
                }
            }
        }
        /// <summary>
        /// 모든 아이콘 detach 하기
        /// </summary>
        protected void DetachAllIcons()
        {
            foreach (var icon in icons)
            {
                UIIconItem uiIconItem = icon.GetComponent<UIIconItem>();
                DetachIcon(uiIconItem.slotIndex);
            }
        }
    }
}
