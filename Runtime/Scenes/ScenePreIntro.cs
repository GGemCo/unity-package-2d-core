using System.Collections;
using TMPro;
using UnityEngine;
#if ENABLE_LEGACY_INPUT_MANAGER
using TouchPhase = UnityEngine.TouchPhase;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System
#endif


namespace GGemCo2DCore
{
    /// <summary>
    /// Pre 인트로 씬
    /// </summary>
    public class ScenePreIntro : MonoBehaviour
    {
        public string GetFieldNameSceneIntro() => nameof(ScenePreIntro);
        
        [Header(ConfigCommon.TitleHeaderRequired)]
        [Tooltip("로딩 진행률(%)을 표시할 UI 텍스트")]
        [SerializeField] private TextMeshProUGUI textLoadingPercent;
        public void SetTextLoadingPercent(TextMeshProUGUI value) => textLoadingPercent = value;

        [Header("Press Any Key UI")]
        [Tooltip("체크 시 자동 로딩 시작")]
        [SerializeField] private bool autoStart = true;

        [Tooltip("'Press Any Key To Start' 메시지를 표시할 UI 텍스트")]
        [SerializeField] private TextMeshProUGUI textPressAnyKey;

        [Header("입력 모드 (로컬 오버라이드)")]
        [Tooltip("전역 설정(GGemCoSettings)이 있으면 전역 값을 사용하고,\n없으면 이 값을 적용")]
        [SerializeField] private InputSystemType inputModeOverride = InputSystemType.Both;

        private GameLoaderManager _gameLoaderManager;
        private bool _waitingForInput;
        private InputSystemType _resolvedMode = InputSystemType.Both;

        private void Awake()
        {
            _waitingForInput = true;
            _gameLoaderManager = GameLoaderManager.Instance;
            if (_gameLoaderManager == null) 
            {
                _gameLoaderManager = new GameObject("GameLoaderManager").AddComponent<GameLoaderManager>();
            }

            if (textLoadingPercent != null) {
                _gameLoaderManager.SetTextLoadingPercent(textLoadingPercent);
                textLoadingPercent.gameObject.SetActive(false);
            }

            if (textPressAnyKey != null)
                textPressAnyKey.gameObject.SetActive(true);
            _resolvedMode = ResolveInputMode();

            if (autoStart)
            {
                ChangeSceneToIntro();
            }
        }
        /// <summary>
        /// GameLoaderManager의 진행률 100% 도달을 기다림
        /// </summary>
        private IEnumerator WaitForLoadingComplete()
        {
            while (_gameLoaderManager != null && !_gameLoaderManager.IsCompleted())
            {
                yield return null;
            }

            OnIntroLoadComplete();
        }

        private void OnIntroLoadComplete()
        {
            SceneManager.ChangeScene(ConfigDefine.SceneNameIntro);
        }

        private void Update()
        {
            if (autoStart) return;
            if (!_waitingForInput) return;

            // 입력 모드별 체크
            bool pressed = _resolvedMode switch
            {
                InputSystemType.NewInputSystem => CheckNewInputSystemPressed(),
                InputSystemType.OldInputManager => CheckOldInputManagerPressed(),
                InputSystemType.Both => CheckBothPressed(),
                _ => CheckBothPressed()
            };

            if (!pressed) return;
            _waitingForInput = false;
            ChangeSceneToIntro();
        }

        private void ChangeSceneToIntro()
        {
            _gameLoaderManager.StartLoadingInScenePreIntro();
            
            if (textLoadingPercent != null) {
                textLoadingPercent.gameObject.SetActive(true);
            }

            if (textPressAnyKey != null)
            {
                textPressAnyKey.gameObject.SetActive(false);
            }
            // 로딩 완료 후 콜백 등록 (GameLoaderManager에서 OnLoadComplete 호출 시 연결)
            StartCoroutine(WaitForLoadingComplete());
        }
        /// <summary>
        /// 전역 설정이 있으면 우선 적용, 없으면 로컬 오버라이드 사용
        /// </summary>
        private InputSystemType ResolveInputMode()
        {
            try
            {
                // 전역 설정이 프로젝트에 존재하는 경우 사용
                var settings = Resources.Load<GGemCoSettings>(ConfigScriptableObject.Main.FileName);
                if (settings != null) return settings.inputSystemType;
            }
            catch
            {
                // Resources 경로나 설정이 없으면 무시하고 로컬 값 사용
            }
            return inputModeOverride;
        }
        
        /// <summary>
        /// 둘 다 허용일 때는 New → Old 순으로 체크
        /// </summary>
        private bool CheckBothPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (CheckNewInputSystemPressed()) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (CheckOldInputManagerPressed()) return true;
#endif
            // 어느 쪽도 컴파일/활성화 되어있지 않다면 false
            return false;
        }

        /// <summary>
        /// New Input System: 키보드/마우스/패드/터치
        /// </summary>
        private bool CheckNewInputSystemPressed()
        {
#if ENABLE_INPUT_SYSTEM
            // Keyboard
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;

            // Mouse
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame ||
                    Mouse.current.middleButton.wasPressedThisFrame)
                    return true;
            }

            // Gamepad (모든 패드 순회)
            if (Gamepad.all.Count > 0 && Gamepad.current != null)
            {
                foreach (var gp in Gamepad.all)
                {
                    if (gp == null) continue;
                    if (gp.startButton.wasPressedThisFrame ||
                        gp.selectButton.wasPressedThisFrame ||
                        gp.buttonSouth.wasPressedThisFrame ||
                        gp.buttonNorth.wasPressedThisFrame ||
                        gp.buttonWest.wasPressedThisFrame ||
                        gp.buttonEast.wasPressedThisFrame ||
                        gp.leftShoulder.wasPressedThisFrame ||
                        gp.rightShoulder.wasPressedThisFrame ||
                        gp.leftStickButton.wasPressedThisFrame ||
                        gp.rightStickButton.wasPressedThisFrame ||
                        gp.dpad.up.wasPressedThisFrame ||
                        gp.dpad.down.wasPressedThisFrame ||
                        gp.dpad.left.wasPressedThisFrame ||
                        gp.dpad.right.wasPressedThisFrame)
                        return true;
                }
            }

            // Touch
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
#else
            return false;
#endif
        }

        /// <summary>
        /// Old Input Manager: 키보드/마우스/터치(모바일 빌드)
        /// </summary>
        private bool CheckOldInputManagerPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // 키보드/마우스/조이스틱 모두 포함
            if (Input.anyKeyDown) return true;

            // 마우스 클릭
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                return true;

            // 터치(모바일)
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase == TouchPhase.Began)
                        return true;
                }
            }

            return false;
#else
            return false;
#endif
        }
    }
}
