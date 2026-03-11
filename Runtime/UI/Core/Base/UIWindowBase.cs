using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 공통 베이스 클래스입니다.
    /// 윈도우 표시/숨김과 테이블 기반 연계 처리를 담당합니다.
    /// </summary>
    public class UIWindowBase : MonoBehaviour
    {
        [HideInInspector] public UIWindowConstants.WindowUid uid;

        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("윈도우 닫기 버튼")]
        public Button buttonClose;

        [Tooltip("윈도우 On/Off 시 fade in/Out 효과 사용 여부")]
        public bool useFade = true;

        [Header("UI Effect Presets")]
        [SerializeField] private UIEffectPreset windowOpenPreset;
        [SerializeField] private UIEffectPreset windowClosePreset;

        private UIWindowFade _uiWindowFade;
        private StruckTableWindow _struckTableWindow;
        private InteractionManager _interactionManager;

        [HideInInspector] public SceneGame SceneGame;

        public UIEffectPreset WindowOpenPreset => windowOpenPreset;
        public UIEffectPreset WindowClosePreset => windowClosePreset;

        protected virtual void Awake()
        {
            UiFadeUtility.TryGetCanvasGroup(gameObject, true, out _);
            UIEffectTarget.GetOrAdd(gameObject);

            if (useFade)
            {
                _uiWindowFade = gameObject.GetComponent<UIWindowFade>();
                if (_uiWindowFade == null)
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

            var clickSoundEventBroadcaster = buttonClose.gameObject.GetComponent<ClickSoundEventBroadcaster>();
            if (!clickSoundEventBroadcaster)
            {
                clickSoundEventBroadcaster = buttonClose.gameObject.AddComponent<ClickSoundEventBroadcaster>();
            }

            clickSoundEventBroadcaster.type = SoundConstants.UIButtonType.CloseWindow;
        }

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
                    GcLogger.LogError("SceneGame 싱글톤이 없습니다.");
                return;
            }

            SceneGame = SceneGame.Instance;
            _interactionManager = SceneGame.InteractionManager;
        }

        /// <summary>
        /// window 테이블에 있는 OpenWindowUid, CloseWindowUid 컬럼 처리
        /// </summary>
        private void ShowByTable(int[] windowUids, bool show)
        {
            if (windowUids == null || SceneGame == null || SceneGame.uIWindowManager == null)
                return;

            foreach (var openWindowUid in windowUids)
            {
                var windowUid = (UIWindowConstants.WindowUid)openWindowUid;
                var uiWindow = SceneGame.uIWindowManager.GetUIWindowByUid<UIWindow>(windowUid);
                if (uiWindow == null) continue;
                uiWindow.Show(show);
            }
        }

        /// <summary>
        /// 윈도우 open/close
        /// </summary>
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
                    ShowByTable(_struckTableWindow.OpenWindowUid, true);
            }
            else
            {
                _uiWindowFade.HidePanel();
                if (_struckTableWindow != null)
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
        public void SetTableWindow(StruckTableWindow struckTableWindow)
        {
            _struckTableWindow = struckTableWindow;
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
