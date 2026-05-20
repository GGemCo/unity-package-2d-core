using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에서 일반 대화창을 열고, 필요 시 대화 종료까지 타임라인 진행을 대기시키는 컨트롤러입니다.
    /// </summary>
    public sealed class DialogueWindowController : CutsceneDefaultController, ICutsceneController
    {
        private DialogueWindowData _data;
        private bool _isWaitingForDialogueEnd;
        private float _resumeTime;
        private int _waitingNpcUid;

        /// <summary>
        /// 대화창 컷신 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public DialogueWindowController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 대화창 로드는 비동기 Addressables/JSON 로딩이 포함될 수 있어 즉시 준비를 지원합니다.
        /// 실제 로딩은 Trigger 단계에서 수행합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 즉시 준비 경로에서 대화창 데이터 유효성만 캐시합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueWindow)
            {
                return;
            }

            _data = evt.dialogueWindow ?? new DialogueWindowData();
        }

        /// <summary>
        /// 대화창 이벤트 실행 전 준비를 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>준비 코루틴입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 대화창을 열고 대화 종료 대기 정책을 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueWindow)
            {
                return;
            }

            _data = evt.dialogueWindow ?? new DialogueWindowData();
            _resumeTime = evt.time + evt.duration;

            if (_data.dialogueUid <= 0)
            {
                GcLogger.LogWarning("DialogueWindow 이벤트의 dialogueUid가 비어 있습니다.");
                return;
            }

            if (_data.waitUntilEnd)
            {
                StartDialogueEndWait(_data.npcUid);
            }

            SceneGame scene = SceneGame.Instance;
            if (scene == null)
            {
                GcLogger.LogError("DialogueWindowController: SceneGame.Instance가 없습니다.");
                CompleteDialogueEndWait();
                return;
            }

            scene.StartCoroutine(CoOpenDialogueWindow(_data));
        }

        /// <summary>
        /// 대화창 로드 작업을 수행하고 실패 시 타임라인 대기를 복구합니다.
        /// </summary>
        /// <param name="data">대화창 실행 데이터입니다.</param>
        /// <returns>대화창 로드 완료까지 대기하는 코루틴입니다.</returns>
        private IEnumerator CoOpenDialogueWindow(DialogueWindowData data)
        {
            UIWindowDialogue dialogueWindow = ResolveDialogueWindow();
            if (dialogueWindow == null)
            {
                GcLogger.LogError("DialogueWindowController: UIWindowDialogue를 찾을 수 없습니다.");
                CompleteDialogueEndWait();
                yield break;
            }

            if (data.closeOtherWindows)
            {
                SceneGame.Instance?.uIWindowManager?.CloseAll(new List<UIWindowConstants.WindowUid>
                {
                    UIWindowConstants.WindowUid.Dialogue
                });
            }

            Task<bool> loadTask = dialogueWindow.LoadDialogue(data.dialogueUid, data.npcUid);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            bool loaded = !loadTask.IsFaulted && !loadTask.IsCanceled && loadTask.Result;
            if (!loaded && data.releaseWaitOnLoadFailed)
            {
                if (loadTask.Exception != null)
                {
                    GcLogger.LogError(loadTask.Exception.GetBaseException().Message);
                }

                CompleteDialogueEndWait();
            }
        }

        /// <summary>
        /// 매 프레임 갱신이 필요한 자체 로직은 없습니다.
        /// 대화 종료는 GameEventManager.DialogEndEvent 구독으로 처리합니다.
        /// </summary>
        public void Update()
        {
        }

        /// <summary>
        /// 진행 중인 대화 대기를 강제로 중단합니다.
        /// </summary>
        public void Stop()
        {
            CompleteDialogueEndWait();
        }

        /// <summary>
        /// 컷신 종료 시 이벤트 구독과 타임라인 대기를 정리합니다.
        /// </summary>
        public void End()
        {
            CompleteDialogueEndWait();
        }

        /// <summary>
        /// 대화 종료 이벤트를 기다리도록 타임라인 진행 대기를 등록합니다.
        /// </summary>
        /// <param name="npcUid">대기 대상 NPC UID입니다. 0이면 첫 대화 종료 이벤트를 허용합니다.</param>
        private void StartDialogueEndWait(int npcUid)
        {
            if (_isWaitingForDialogueEnd)
            {
                CompleteDialogueEndWait();
            }

            _isWaitingForDialogueEnd = true;
            _waitingNpcUid = npcUid;
            GameEventManager.DialogEndEvent += OnDialogEnd;
            CutsceneManager.RequestTimelineProgressWait(this);
        }

        /// <summary>
        /// 대화 종료 이벤트를 받아 현재 대기 중인 대화와 일치하면 타임라인을 재개합니다.
        /// </summary>
        /// <param name="eventData">대화 종료 이벤트 데이터입니다.</param>
        private void OnDialogEnd(DialogEventData eventData)
        {
            if (!_isWaitingForDialogueEnd)
            {
                return;
            }

            if (_waitingNpcUid > 0 && eventData.NpcUid != _waitingNpcUid)
            {
                return;
            }

            CompleteDialogueEndWait();
        }

        /// <summary>
        /// 대화 종료 대기를 해제하고 컷신 타임라인을 지정된 재개 시간까지 보정합니다.
        /// </summary>
        private void CompleteDialogueEndWait()
        {
            if (!_isWaitingForDialogueEnd)
            {
                return;
            }

            _isWaitingForDialogueEnd = false;
            _waitingNpcUid = 0;
            GameEventManager.DialogEndEvent -= OnDialogEnd;
            CutsceneManager.CompleteTimelineProgressWait(this, _resumeTime);
            _resumeTime = 0f;
        }

        /// <summary>
        /// SceneGame의 UIWindowManager에서 대화창을 조회합니다.
        /// </summary>
        /// <returns>대화창 인스턴스입니다. 없으면 <see langword="null"/>을 반환합니다.</returns>
        private static UIWindowDialogue ResolveDialogueWindow()
        {
            SceneGame scene = SceneGame.Instance;
            return scene != null
                ? scene.uIWindowManager?.GetUIWindowByUid<UIWindowDialogue>(UIWindowConstants.WindowUid.Dialogue)
                : null;
        }
    }
}
