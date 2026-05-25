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
        private bool _advanceRequestedWhileWaiting;
        private int _inputWaitStartFrame = -1;
        private float _inputWaitResumeTime;
        private DialogueBalloonAdvancePolicy _advancePolicy = DialogueBalloonAdvancePolicy.LegacyImmediate;

        private Transform _newTarget;
        private CharacterBase _newTargetCharacter;
        private readonly DialogueBalloonPool _dialogueBalloonPool;
        private GameObject _currentDialogueBalloon;
        private UIDialogueBalloon _currentDialogueBalloonUi;
        private CharacterBase _talkLoopAnimationCharacter;
        private float _talkLoopAnimationCapturedPlaybackTimeScale = 1f;
        private bool _restoreTalkLoopAnimationOnStop;

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
            _inputWaitResumeTime = evt.time + evt.duration;
            _advancePolicy = data.advancePolicy;
            _advanceRequestedWhileWaiting = false;

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
            _currentDialogueBalloon = _dialogueBalloonPool?.Get(this);
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

            _message = ResolveDialogueBalloonMessage(data);
            _currentDialogueBalloonUi.Initialize(_newTargetCharacter, data, _message);
            
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
            TryStartTalkLoopAnimation(data);

            if (data.waitForUserInput)
            {
                StartInputWait();
            }
        }


        /// <summary>
        /// 말풍선 데이터의 Localization table/key를 해석하여 최종 출력 메시지를 반환합니다.
        /// Localization 정보가 없거나 조회에 실패하면 데이터에 저장된 fallback 메시지를 사용합니다.
        /// </summary>
        /// <param name="data">현재 말풍선 이벤트 데이터입니다.</param>
        /// <returns>말풍선에 표시할 최종 메시지입니다.</returns>
        private static string ResolveDialogueBalloonMessage(DialogueBalloonData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return data.ResolveMessage();
        }

        /// <summary>
        /// 말풍선 표시 시간을 갱신하고, 입력 대기 상태이면 유저 입력을 받아 컷신 진행을 재개합니다.
        /// </summary>
        public void Update()
        {
            if (!_isBalloon) return;

            // 입력 대기 상태에서도 실제 경과 시간은 계속 누적해 최소 대기 정책 판단에 사용합니다.
            _timer += Time.deltaTime;

            if (_isWaitingForUserInput)
            {
                HandleUserInputWait();
                return;
            }

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

            if (_advanceRequestedWhileWaiting && CanCompleteInputWaitNow())
            {
                CompleteInputWait();
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

            if (!CanCompleteInputWaitNow())
            {
                _advanceRequestedWhileWaiting = true;
                return;
            }

            CompleteInputWait();
        }

        /// <summary>
        /// 현재 정책과 클립 경과 시간을 기준으로 다음 연출 진행이 가능한지 판단합니다.
        /// </summary>
        /// <returns>즉시 진행이 가능하면 <see langword="true"/>를 반환합니다.</returns>
        private bool CanCompleteInputWaitNow()
        {
            if (_advancePolicy != DialogueBalloonAdvancePolicy.WaitUntilClipDuration)
            {
                return true;
            }

            if (_duration <= 0f)
            {
                return true;
            }

            return _timer >= _duration;
        }

        /// <summary>
        /// 유저 입력 대기를 완료하고 말풍선 종료 시점까지 컷신 타임라인을 즉시 보정합니다.
        /// 말풍선을 먼저 회수한 뒤 도달 이벤트를 실행하여 다음 말풍선이 같은 프레임에 표시될 수 있게 합니다.
        /// </summary>
        private void CompleteInputWait()
        {
            float resumeTime = _inputWaitResumeTime;
            Stop();
            CutsceneManager.CompleteTimelineProgressWait(this, resumeTime);
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
            StopTalkLoopAnimation();
            _timer = 0f;
            _duration = 0f;
            _inputWaitResumeTime = 0f;
            _isBalloon = false;
            _advanceRequestedWhileWaiting = false;
            _advancePolicy = DialogueBalloonAdvancePolicy.LegacyImmediate;
            _dialogueBalloonPool?.Return(_currentDialogueBalloon, this);
            // 현재 참조가 유실된 말풍선까지 owner 기준으로 안전 회수합니다.
            _dialogueBalloonPool?.ReturnAllByOwner(this);
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

        /// <summary>
        /// 말풍선 데이터에 설정된 루프 애니메이션을 시작합니다.
        /// 캐릭터별 owner 소유권을 획득한 경우에만 재생하여, 다른 컨트롤러의 상태를 덮어쓰는 문제를 방지합니다.
        /// </summary>
        /// <param name="data">현재 말풍선 이벤트 데이터입니다.</param>
        private void TryStartTalkLoopAnimation(DialogueBalloonData data)
        {
            if (data == null || !data.useTalkLoopAnimation)
            {
                return;
            }

            string animationName = data.talkLoopAnimationName;
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return;
            }

            CharacterBase targetCharacter = ResolveTalkLoopAnimationTarget(data);
            if (targetCharacter == null)
            {
                GcLogger.LogError("말풍선 루프 애니메이션 대상 캐릭터를 찾을 수 없습니다.");
                return;
            }

            ICharacterAnimationController animationController = targetCharacter.CharacterAnimationController;
            if (animationController == null)
            {
                GcLogger.LogError(
                    "말풍선 루프 애니메이션 대상에 ICharacterAnimationController가 없습니다. type: " +
                    targetCharacter.type + "/ uid: " + targetCharacter.uid);
                return;
            }

            if (!animationController.HasAnimation(animationName))
            {
                GcLogger.LogError(
                    "말풍선 루프 애니메이션 클립을 찾을 수 없습니다. type: " +
                    targetCharacter.type + "/ uid: " + targetCharacter.uid + "/ clip: " + animationName);
                return;
            }

            if (!CutsceneDialogueLoopAnimationOwnershipService.TryAcquire(
                    targetCharacter,
                    this,
                    out float capturedPlaybackTimeScale))
            {
                GcLogger.Log(
                    "말풍선 루프 애니메이션 owner 획득에 실패했습니다. type: " +
                    targetCharacter.type + "/ uid: " + targetCharacter.uid);
                return;
            }

            _talkLoopAnimationCharacter = targetCharacter;
            _talkLoopAnimationCapturedPlaybackTimeScale = capturedPlaybackTimeScale;
            _restoreTalkLoopAnimationOnStop = data.restoreTalkLoopAnimationOnStop;

            animationController.PlayCharacterAnimation(
                animationName,
                loop: true,
                timeScale: data.GetSafeTalkLoopAnimationTimeScale());
        }

        /// <summary>
        /// 현재 컨트롤러가 보유한 말풍선 루프 애니메이션을 종료합니다.
        /// owner가 일치할 때만 복원 로직을 수행하여, 최신 owner의 애니메이션 상태를 보호합니다.
        /// </summary>
        private void StopTalkLoopAnimation()
        {
            CharacterBase targetCharacter = _talkLoopAnimationCharacter;
            if (targetCharacter == null)
            {
                CutsceneDialogueLoopAnimationOwnershipService.ReleaseAllByOwner(this);
                ClearTalkLoopAnimationRuntimeState();
                return;
            }

            bool isOwner = CutsceneDialogueLoopAnimationOwnershipService.IsOwnedBy(targetCharacter, this);
            if (isOwner && _restoreTalkLoopAnimationOnStop)
            {
                ICharacterAnimationController animationController = targetCharacter.CharacterAnimationController;
                if (animationController != null)
                {
                    animationController.PlayWaitAnimation();
                    animationController.SetPlaybackTimeScale(_talkLoopAnimationCapturedPlaybackTimeScale);
                }
            }

            CutsceneDialogueLoopAnimationOwnershipService.Release(targetCharacter, this);
            ClearTalkLoopAnimationRuntimeState();
        }

        /// <summary>
        /// 말풍선 루프 애니메이션 대상 캐릭터를 해석합니다.
        /// 별도 대상이 설정되어 있으면 해당 대상을 사용하고, 없으면 말풍선 화자를 기본 대상으로 사용합니다.
        /// </summary>
        /// <param name="data">말풍선 이벤트 데이터입니다.</param>
        /// <returns>해석된 대상 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveTalkLoopAnimationTarget(DialogueBalloonData data)
        {
            CutsceneCharacterReference targetReference = data?.talkLoopAnimationTarget;
            if (IsTalkLoopAnimationTargetConfigured(targetReference))
            {
                CharacterBase configuredTarget = ResolveCharacterReferenceTarget(targetReference);
                if (configuredTarget != null)
                {
                    return configuredTarget;
                }
            }

            return _newTargetCharacter;
        }

        /// <summary>
        /// 루프 애니메이션 대상 참조가 실제 대상을 해석할 수 있는 상태인지 확인합니다.
        /// </summary>
        /// <param name="targetReference">검사할 캐릭터 대상 참조입니다.</param>
        /// <returns>참조가 유효하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsTalkLoopAnimationTargetConfigured(CutsceneCharacterReference targetReference)
        {
            if (targetReference == null)
            {
                return false;
            }

            if (targetReference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                return targetReference.runtimeTargetKey != CutsceneKeyCharacterTarget.None;
            }

            return targetReference.characterType != CharacterConstants.Type.None;
        }

        /// <summary>
        /// <see cref="CutsceneCharacterReference"/>를 실제 캐릭터 인스턴스로 해석합니다.
        /// Fixed 모드는 type/uid를 사용하고, RuntimeOverride 모드는 CutsceneManager의 런타임 키를 사용합니다.
        /// </summary>
        /// <param name="targetReference">해석할 캐릭터 참조입니다.</param>
        /// <returns>해석된 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveCharacterReferenceTarget(CutsceneCharacterReference targetReference)
        {
            if (targetReference == null)
            {
                return null;
            }

            if (targetReference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                if (targetReference.runtimeTargetKey == CutsceneKeyCharacterTarget.None)
                {
                    return null;
                }

                if (CutsceneManager != null &&
                    CutsceneManager.TryGetCharacterTargetOverride(targetReference.runtimeTargetKey, out CharacterBase runtimeTarget))
                {
                    return runtimeTarget;
                }

                return null;
            }

            Transform target = GetTargetTransform(targetReference.characterType, targetReference.characterUid);
            if (target == null && CutsceneManager != null)
            {
                target = CutsceneManager.GetCharacter(targetReference.characterType, targetReference.characterUid);
            }

            return target != null ? target.GetComponent<CharacterBase>() : null;
        }

        /// <summary>
        /// 말풍선 루프 애니메이션의 런타임 캐시 상태를 초기화합니다.
        /// </summary>
        private void ClearTalkLoopAnimationRuntimeState()
        {
            _talkLoopAnimationCharacter = null;
            _talkLoopAnimationCapturedPlaybackTimeScale = 1f;
            _restoreTalkLoopAnimationOnStop = false;
        }
    }
}
