using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 공용 베이스 클래스입니다.
    /// </summary>
    public class UIWindowBase : MonoBehaviour
    {
        [HideInInspector] public UIWindowConstants.WindowUid uid;
        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("윈도우 닫기 버튼")]
        public Button buttonClose;
        [Tooltip("윈도우 On/Off 시 fade in/out 효과 사용 여부")]
        public bool useFade = true;
        [Tooltip("윈도우 열기 효과 프리셋")]
        public UIEffectPreset windowOpenPreset;
        [Tooltip("윈도우 닫기 효과 프리셋")]
        public UIEffectPreset windowClosePreset;

        private UIWindowFade _uiWindowFade;
        private StruckTableWindow _struckTableWindow;
        private InteractionManager _interactionManager;
        private UIEffectTarget _uiEffectTarget;

        [HideInInspector] public SceneGame SceneGame;

        protected virtual void Awake()
        {
            if (useFade)
            {
                _uiEffectTarget = UIEffectTarget.GetOrAdd(gameObject);
                UiFadeUtility.TryGetCanvasGroup(gameObject, true, out _);
                _uiWindowFade = gameObject.GetComponent<UIWindowFade>();
                if (_uiWindowFade == null)
                    _uiWindowFade = gameObject.AddComponent<UIWindowFade>();
                _uiWindowFade.Initialize(this, _uiEffectTarget, windowOpenPreset, windowClosePreset);
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

            ClickSoundEventBroadcaster clickSoundEventBroadcaster =
                buttonClose.gameObject.GetComponent<ClickSoundEventBroadcaster>();
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
                    uiWindow._uiWindowFade.ShowPanel();
                else
                    uiWindow._uiWindowFade.HidePanel();
            }
        }

        /// <summary>
        /// window 테이블에 있는 OpenWindowUid, CloseWindowUid 컬럼을 즉시/무음 모드로 처리합니다.
        /// </summary>
        private void SetVisibleByTableImmediate(int[] windowUids, bool show, bool invokeOnShow)
        {
            if (windowUids == null || SceneGame == null || SceneGame.uIWindowManager == null)
            {
                return;
            }

            foreach (var linkedWindowUid in windowUids)
            {
                UIWindowConstants.WindowUid windowUid = (UIWindowConstants.WindowUid)linkedWindowUid;
                UIWindow uiWindow = SceneGame.uIWindowManager.GetUIWindowByUid<UIWindow>(windowUid);
                if (uiWindow == null)
                {
                    continue;
                }

                uiWindow.SetVisibleImmediate(show, invokeOnShow, followLinkedWindows: false);
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
        /// 애니메이션과 OnShow 호출 없이 즉시 윈도우 표시 상태를 변경합니다.
        /// </summary>
        public virtual void SetVisibleImmediate(bool show, bool invokeOnShow = false, bool followLinkedWindows = false)
        {
            if (_uiWindowFade == null)
            {
                if (show)
                {
                    gameObject.SetActive(true);
                    UiFadeUtility.SetVisible(gameObject, true, useFade, true);

                    if (invokeOnShow)
                    {
                        OnShow(true);
                    }
                }
                else
                {
                    if (invokeOnShow)
                    {
                        OnShow(false);
                    }

                    UiFadeUtility.SetVisible(gameObject, false, useFade, true);
                    gameObject.SetActive(false);
                }
            }
            else
            {
                _uiWindowFade.SetVisibleImmediate(show, invokeOnShow);
            }

            if (!followLinkedWindows || _struckTableWindow == null)
            {
                return;
            }

            if (show)
            {
                SetVisibleByTableImmediate(_struckTableWindow.OpenWindowUid, true, invokeOnShow);
            }
            else
            {
                SetVisibleByTableImmediate(_struckTableWindow.CloseWindowUid, false, invokeOnShow);
            }
        }

        /// <summary>
        /// 윈도우가 show 된 후 처리
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
