using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 
    /// </summary>
    public class UIWindowBase : MonoBehaviour
    {
        // 공개(public) → 보호(protected) → 내부(internal) → 비공개(private) 
        // 상수(const), 정적(static) 필드 → 인스턴스 필드 → 속성(Properties) → 생성자(Constructors) → 메서드(Methods)
        // 윈도우 고유번호
        [HideInInspector] public UIWindowConstants.WindowUid uid;
        [Header(UIWindowConstants.TitleHeaderCommon)] 
        [Tooltip("윈도우 닫기 버튼")] 
        public Button buttonClose;
        [Tooltip("윈도우 On/Off 시 fade in/Out 효과 사용 여부")]
        public bool useFade = true;
        
        private UIWindowFade _uiWindowFade;
        private StruckTableWindow _struckTableWindow;
        private InteractionManager _interactionManager;
        [HideInInspector] public SceneGame SceneGame;

        protected virtual void Awake()
        {
            gameObject.AddComponent<CanvasGroup>();
            if (useFade)
            {
                _uiWindowFade = gameObject.AddComponent<UIWindowFade>();
            }
            InitializeButtonClose();
        }
        /// <summary>
        /// 닫기 버튼 초기화
        /// </summary>
        private void InitializeButtonClose()
        {
            if (buttonClose == null) return;
            buttonClose.onClick.AddListener(OnClickClose);
            // 사운드 설정
            ClickSoundEventBroadcaster clickSoundEventBroadcaster =
                buttonClose.gameObject.GetComponent<ClickSoundEventBroadcaster>();
            if (!clickSoundEventBroadcaster)
            {
                clickSoundEventBroadcaster = buttonClose.gameObject.AddComponent<ClickSoundEventBroadcaster>();
            }
            clickSoundEventBroadcaster.type = SoundConstants.UIButtonType.CloseWindow; }
        protected virtual void Start()
        {
            if (_struckTableWindow is { DefaultActive: false })
            {
                gameObject.SetActive(false);
            }

            if (!SceneGame.Instance)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.name == ConfigDefine.SceneNameGame) 
                    GcLogger.LogError($"SceneGame 싱글톤이 없습니다.");
                return;
            }
            SceneGame = SceneGame.Instance;
            _interactionManager = SceneGame.InteractionManager;
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
                if (_struckTableWindow != null)
                {
                    ShowByTable(_struckTableWindow.OpenWindowUid, true);
                }
            }
            else
            {
                _uiWindowFade.HidePanel();
                if (_struckTableWindow != null)
                {
                    ShowByTable(_struckTableWindow.CloseWindowUid, false);
                }
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
            if (_struckTableWindow is { IsInteraction: true } && _interactionManager != null && _interactionManager.IsInteractioning())
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
        public virtual void OnRightClick(UIIcon icon)
        {
            
        }
        public bool GetDefaultActive()
        {
            return _struckTableWindow is { DefaultActive: true };
        }

        public bool IsOpen()
        {
            return gameObject.activeSelf;
        }

        public virtual void ShowItemInfo(bool show, UIIcon icon = null)
        {
        }
    }
}
