using System.Collections;
using UnityEngine;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 특정 캐릭터에 대사 말풍선을 표시하는 컨트롤러입니다.
    /// 필요 시 카메라 추적 대상을 함께 변경하며, 지정 시간이 지나면 말풍선을 회수합니다.
    /// </summary>
    public class DialogueBalloonController : CutsceneDefaultController, ICutsceneController
    {
        private Camera _cam;
        private string _message;
        private float _timer;
        private float _duration;
        private bool _isFollowTarget;
        private bool _isBalloon;
        private bool _isWaitingForUserInput;
        private int _inputWaitStartFrame = -1;

        private Transform _newTarget;
        private CharacterBase _newTargetCharacter;
        private readonly DialogueBalloonPool _dialogueBalloonPool;
        private GameObject _currentDialogueBalloon;
        private UIDialogueBalloon _currentDialogueBalloonUi;

        /// <summary>
        /// 대사 말풍선 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        /// <param name="dialogueBalloonPool">말풍선 오브젝트를 재사용하기 위한 풀입니다.</param>
        public DialogueBalloonController(CutsceneManager manager, DialogueBalloonPool dialogueBalloonPool)
        {
            CutsceneManager = manager;
            _dialogueBalloonPool = dialogueBalloonPool;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;


        /// <summary>
        /// 말풍선 이벤트 실행 전 필요한 사전 준비를 수행합니다.
        /// 현재는 별도의 준비 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정을 비동기적으로 진행하기 위한 열거자입니다.</returns>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueBalloon)
            {
                return;
            }

            // TODO: 필요 시 대상 캐릭터 유효성 검사나 말풍선 사전 할당을 이 단계에서 수행합니다.
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 대상 캐릭터에 말풍선을 생성하고 대사 내용을 설정합니다.
        /// 필요 시 카메라 추적 대상을 해당 캐릭터로 변경합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueBalloon)
                return;

            // 이전 말풍선이 비정상적으로 종료되지 않은 경우 정리
            if (_isBalloon)
            {
                Stop();
            }

            var data = evt.dialogueBalloon ?? new DialogueBalloonData();
            _isFollowTarget = data.isFollowTarget;
            _duration = evt.duration;

            // 말풍선을 표시할 대상 캐릭터 탐색
            _newTarget = GetTargetTransform(data.characterType, data.characterUid);
            if (_newTarget == null)
            {
                _newTarget = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
            }

            if (_newTarget == null)
            {
                GcLogger.LogError("대사를 하는 캐릭터가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                return;
            }

            _newTargetCharacter = _newTarget.GetComponent<CharacterBase>();
            if (_newTargetCharacter == null)
            {
                GcLogger.LogError("CharacterBase 가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                return;
            }

            // 말풍선 오브젝트 확보
            _currentDialogueBalloon = _dialogueBalloonPool?.Get();
            if (_currentDialogueBalloon == null)
            {
                GcLogger.LogError("말풍선이 만들어지지 않았습니다.");
                return;
            }

            // 말풍선 초기화 및 텍스트 설정
            _currentDialogueBalloonUi = _currentDialogueBalloon.GetComponent<UIDialogueBalloon>();
            if (_currentDialogueBalloonUi == null)
            {
                GcLogger.LogError("UIDialogueBalloon 컴포넌트가 없습니다.");
                Stop();
                return;
            }

            _currentDialogueBalloonUi.Initialize(_newTargetCharacter, data);
            
            // 카메라 추적 대상 설정
            if (_isFollowTarget)
            {
                SceneGame.Instance.cameraManager.SetFollowTarget(_newTarget);
            }

            if (_newTarget.gameObject.activeSelf == false)
            {
                _newTarget.gameObject.SetActive(true);
            }

            _timer = 0f;
            _isBalloon = true;

            if (data.waitForUserInput)
            {
                StartInputWait();
            }
        }

        /// <summary>
        /// 말풍선 표시 시간을 갱신하고, 입력 대기 상태이면 유저 입력을 받아 컷신 진행을 재개합니다.
        /// </summary>
        public void Update()
        {
            if (!_isBalloon) return;

            if (_isWaitingForUserInput)
            {
                HandleUserInputWait();
                return;
            }
            
            _timer += Time.deltaTime;

            if (_timer >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 유저 입력을 받을 때까지 컷신 타임라인 진행을 대기 상태로 전환합니다.
        /// 대기 시작 프레임의 입력은 컷신을 시작한 입력과 겹칠 수 있으므로 다음 프레임부터 처리합니다.
        /// </summary>
        private void StartInputWait()
        {
            _isWaitingForUserInput = true;
            _inputWaitStartFrame = Time.frameCount;
            CutsceneManager.RequestTimelineProgressWait(this);
        }

        /// <summary>
        /// 유저 입력 대기 중의 입력 처리 규칙을 수행합니다.
        /// 타자 효과가 남아 있으면 먼저 전체 메시지를 표시하고, 이미 모두 표시된 상태이면 컷신 진행을 재개합니다.
        /// </summary>
        private void HandleUserInputWait()
        {
            if (Time.frameCount <= _inputWaitStartFrame)
            {
                return;
            }

            if (!TryConsumeAdvanceInput())
            {
                return;
            }

            if (_currentDialogueBalloonUi != null && !_currentDialogueBalloonUi.IsFullyRevealed)
            {
                _currentDialogueBalloonUi.RevealAll();
                return;
            }

            Stop();
        }

        /// <summary>
        /// 현재 프레임에 말풍선 진행용 클릭 또는 터치 입력이 발생했는지 확인합니다.
        /// 프로젝트 입력 방식 정의에 맞춰 Legacy Input Manager 또는 New Input System을 사용합니다.
        /// </summary>
        /// <returns>진행 입력이 발생했으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryConsumeAdvanceInput()
        {
#if GGEMCO_USE_OLD_INPUT
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }

            return Input.GetMouseButtonDown(0);
#elif GGEMCO_USE_NEW_INPUT
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return false;
#endif
        }

        /// <summary>
        /// 말풍선 입력 대기를 해제하고 컷신 타임라인 진행을 다시 허용합니다.
        /// </summary>
        private void ReleaseInputWait()
        {
            if (!_isWaitingForUserInput)
            {
                return;
            }

            _isWaitingForUserInput = false;
            _inputWaitStartFrame = -1;
            CutsceneManager.ReleaseTimelineProgressWait(this);
        }

        /// <summary>
        /// 현재 표시 중인 말풍선을 회수하고 상태를 초기화합니다.
        /// </summary>
        public void Stop()
        {
            ReleaseInputWait();
            _timer = 0f;
            _isBalloon = false;
            _dialogueBalloonPool?.Return(_currentDialogueBalloon);
            _currentDialogueBalloon = null;
            _currentDialogueBalloonUi = null;
        }

        /// <summary>
        /// 컷신 종료 시 표시 중인 말풍선과 입력 대기 상태를 정리합니다.
        /// </summary>
        public void End()
        {
            Stop();
        }
    }
}
