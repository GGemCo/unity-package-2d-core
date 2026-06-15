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
        [Tooltip("윈도우 닫힐 때, 아이콘 선택 효과 삭제 여부")]
        [SerializeField] private bool removeIconSelectEffectOnClose = true;

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
        /// 기본 비활성 윈도우가 초기 레이아웃 계산을 통과할 수 있도록 GameObject를 유지한 채 시각적으로 숨깁니다.
        /// </summary>
        internal void PrepareDefaultInactiveBeforeInitialLayout()
        {
            if (gameObject == null)
            {
                return;
            }

            if (!useFade)
            {
                return;
            }

            gameObject.SetActive(true);
            UiFadeUtility.SetVisible(gameObject, false, ensureCanvasGroup: true, updateInput: true);
        }

        /// <summary>
        /// 초기 Transform/Layout 갱신이 끝난 기본 비활성 윈도우를 최종 비활성 상태로 전환합니다.
        /// </summary>
        internal void ApplyDefaultInactiveAfterInitialLayout()
        {
            SetVisibleImmediate(false, invokeOnShow: false, followLinkedWindows: false);
        }

        /// <summary>
        /// 테이블에 연결된 윈도우 Uid 를 현재 윈도우 문맥에 맞는 실제 오브젝트로 해석합니다.
        /// 기본 구현은 UIWindowManager 에 등록된 공용 윈도우를 반환합니다.
        /// </summary>
        /// <param name="windowUid">해석할 연결 윈도우 Uid 입니다.</param>
        /// <returns>현재 윈도우 문맥에서 사용해야 하는 실제 윈도우 오브젝트입니다.</returns>
        protected virtual UIWindow ResolveLinkedWindow(UIWindowConstants.WindowUid windowUid)
        {
            if (SceneGame == null || SceneGame.uIWindowManager == null)
            {
                return null;
            }

            return SceneGame.uIWindowManager.GetUIWindowByUid<UIWindow>(windowUid);
        }

        /// <summary>
        /// window 테이블에 있는 OpenWindowUid, CloseWindowUid 컬럼 처리
        /// </summary>
        private void ShowByTable(int[] windowUids, bool show)
        {
            if (windowUids == null)
            {
                return;
            }

            foreach (var openWindowUid in windowUids)
            {
                UIWindowConstants.WindowUid windowUid = (UIWindowConstants.WindowUid)openWindowUid;
                UIWindow uiWindow = ResolveLinkedWindow(windowUid);
                if (uiWindow == null) continue;
                if (show && uiWindow.IsVisibilitySuppressedByManager()) continue;

                if (uiWindow._uiWindowFade == null)
                {
                    if (uiWindow.gameObject == null) continue;
                    uiWindow.SetNonFadeVisible(show, true);
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
                UIWindow uiWindow = ResolveLinkedWindow(windowUid);
                if (uiWindow == null)
                {
                    continue;
                }

                uiWindow.SetVisibleImmediate(show, invokeOnShow, followLinkedWindows: false);
            }
            
            if (!show && removeIconSelectEffectOnClose)
                SceneGame?.uIWindowManager?.ShowSelectIconImage(false);
        }

        /// <summary>
        /// 윈도우 open/close
        /// </summary>
        public virtual bool Show(bool show)
        {
            if (show && IsVisibilitySuppressedByManager())
            {
                return false;
            }

            if (_uiWindowFade == null)
            {
                if (gameObject == null) return false;
                SetNonFadeVisible(show, true);
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
            if (show && IsVisibilitySuppressedByManager())
            {
                return;
            }

            if (_uiWindowFade == null)
            {
                if (show)
                {
                    SetNonFadeVisible(true, invokeOnShow);
                }
                else
                {
                    SetNonFadeVisible(false, invokeOnShow);
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
        /// 현재 윈도우가 UIWindowManager의 표시 억제 정책에 의해 열림 요청을 막아야 하는지 확인합니다.
        /// 컷신처럼 일정 구간 동안 UI를 숨기는 시스템이 직접 Show 호출까지 일관되게 제어할 때 사용됩니다.
        /// </summary>
        /// <returns>표시 요청을 무시해야 하면 true입니다.</returns>
        private bool IsVisibilitySuppressedByManager()
        {
            SceneGame sceneGame = SceneGame != null ? SceneGame : global::GGemCo2DCore.SceneGame.Instance;
            return sceneGame != null &&
                   sceneGame.uIWindowManager != null &&
                   sceneGame.uIWindowManager.IsWindowVisibilitySuppressed(uid);
        }

        /// <summary>
        /// Fade를 사용하지 않는 윈도우의 표시 상태를 즉시 변경합니다.
        /// </summary>
        /// <param name="show">true면 윈도우를 표시하고, false면 비활성화합니다.</param>
        /// <param name="invokeOnShow">표시 상태 변경 콜백을 호출할지 여부입니다.</param>
        private void SetNonFadeVisible(bool show, bool invokeOnShow)
        {
            if (gameObject == null)
            {
                return;
            }

            if (show)
            {
                gameObject.SetActive(true);
                RestoreExistingCanvasGroupVisibility();

                if (invokeOnShow)
                {
                    OnShow(true);
                }

                return;
            }

            if (invokeOnShow)
            {
                OnShow(false);
            }

            // Fade 미사용 윈도우는 CanvasGroup 숨김 상태를 남기지 않고 GameObject 비활성화만 사용합니다.
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 이미 존재하는 CanvasGroup이 숨김 상태로 남아 있을 때 표시 상태로 복구합니다.
        /// </summary>
        private void RestoreExistingCanvasGroupVisibility()
        {
            // 새 CanvasGroup은 만들지 않고, 프리팹 또는 이전 로직으로 남은 CanvasGroup만 정상화합니다.
            UiFadeUtility.SetVisible(gameObject, true, ensureCanvasGroup: false, updateInput: true);
        }

        /// <summary>
        /// 윈도우가 show 된 후 처리
        /// </summary>
        public virtual void OnShow(bool show)
        {
            if (!show && removeIconSelectEffectOnClose)
                SceneGame?.uIWindowManager?.ShowSelectIconImage(false);
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
