using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 대사창을 관리합니다.
    /// export 된 Dialogue json 을 읽고 페이지 단위로 대사를 재생합니다.
    /// </summary>
    public class UIWindowDialogue : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("말하는 캐릭터 썸네일 이미지 오브젝트")]
        [SerializeField] private Image imageThumbnail;

        [Tooltip("말하는 캐릭터 이름 텍스트 오브젝트")]
        [SerializeField] private TextMeshProUGUI textName;
        [Tooltip("대사 텍스트 오브젝트")]
        [SerializeField] private TextMeshProUGUI textMessage;
        [Tooltip("한번에 보여줄 대사 라인 수")]
        [SerializeField] private int maxLineCount = 3;

        [Header("선택지")]
        [Tooltip("선택지 버튼 프리팹")]
        [SerializeField] private GameObject prefabButtonAnswer;
        [Tooltip("선택지 버튼이 들어가는 Panel")]
        [SerializeField] private Transform containerAnswer;
        [Tooltip("선택지 버튼 왼쪽, 오른쪽 여백 사이즈")]
        [SerializeField] private int paddingWidth = 20;
        [Tooltip("다음 대사 보기 버튼")]
        [SerializeField] private Button buttonNextMessage;

        private float _originalFontSize;
        private int _indexMessage;
        private List<string> _messages;
        private Dictionary<string, DialogueNodeData> _dialogueNodeDatas;
        private int _currentNpcUid;
        private DialogueNodeData _currentDialogue;
        private SystemMessageManager _systemMessageManager;
        private ChoiceButtonHandler _choiceButtonHandler;

        /// <summary>
        /// 윈도우 초기화를 수행합니다.
        /// </summary>
        protected override void Awake()
        {
            uid = UIWindowConstants.WindowUid.Dialogue;
            base.Awake();
            Initialize();
        }

        /// <summary>
        /// 내부 상태와 선택지 버튼 핸들러를 초기화합니다.
        /// </summary>
        private void Initialize()
        {
            if (textMessage != null)
            {
                _originalFontSize = textMessage.fontSize;
            }

            buttonNextMessage?.onClick.AddListener(OnClickNext);
            _messages = new List<string>();
            _dialogueNodeDatas = new Dictionary<string, DialogueNodeData>();

            _choiceButtonHandler = new ChoiceButtonHandler(containerAnswer, paddingWidth, prefabButtonAnswer)
            {
                OnChoiceSelected = OnClickAnswer
            };
            _choiceButtonHandler.InitializeButtonChoice();
        }

        /// <summary>
        /// 씬 진입 후 시스템 메시지 매니저를 연결합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            _systemMessageManager = SceneGame.Instance.systemMessageManager;
        }

        /// <summary>
        /// 현재 대화 상태를 초기화합니다.
        /// </summary>
        private void ResetDialogue()
        {
            _messages.Clear();
            _dialogueNodeDatas.Clear();
            _currentDialogue = null;
            _indexMessage = 0;
            _currentNpcUid = 0;
            _choiceButtonHandler.HideButtons();
        }

        /// <summary>
        /// 대사 json 을 불러옵니다.
        /// </summary>
        /// <param name="dialogueUid">dialogue 테이블 Uid 입니다.</param>
        /// <param name="npcUid">현재 대화 대상 NPC Uid 입니다.</param>
        public async Task LoadDialogue(int dialogueUid, int npcUid = 0)
        {
            DialogueData data = await DialogueLoader.LoadDialogueData(dialogueUid);
            if (data != null)
            {
                SetDialogue(data);
                _currentNpcUid = npcUid;
            }
        }

        /// <summary>
        /// 일반 대화 시작 데이터를 바인딩합니다.
        /// </summary>
        /// <param name="data">대화 데이터입니다.</param>
        private void SetDialogue(DialogueData data)
        {
            if (data?.nodes == null || data.nodes.Count == 0)
            {
                return;
            }

            ResetDialogue();
            foreach (DialogueNodeData nodeData in data.nodes)
            {
                if (nodeData == null || string.IsNullOrWhiteSpace(nodeData.guid))
                {
                    continue;
                }

                _dialogueNodeDatas[nodeData.guid] = nodeData;
            }

            if (!gameObject.activeSelf)
            {
                Show(true);
            }

            _indexMessage = 0;
            DialogueNodeData dialogue = data.nodes[0];
            ProcessNextDialogue(dialogue.guid);
        }

        /// <summary>
        /// 지정한 guid 의 다음 대사 노드를 화면에 반영합니다.
        /// </summary>
        /// <param name="guid">표시할 노드 guid 입니다.</param>
        private async void ProcessNextDialogue(string guid)
        {
            try
            {
                if (string.IsNullOrEmpty(guid))
                {
                    EndDialogue();
                    return;
                }

                _indexMessage = 0;
                _currentDialogue = _dialogueNodeDatas.GetValueOrDefault(guid);
                if (_currentDialogue == null)
                {
                    EndDialogue();
                    return;
                }

                if (textName != null)
                {
                    textName.text = DialogueCharacterHelper.GetName(_currentDialogue);
                }

                if (imageThumbnail != null)
                {
                    imageThumbnail.sprite = await DialogueCharacterHelper.GetThumbnail(_currentDialogue);
                }

                if (textMessage != null)
                {
                    textMessage.fontSize = _currentDialogue.fontSize > 0 ? _currentDialogue.fontSize : _originalFontSize;
                }

                string resolvedDialogueText = DialogueLocalizationRuntimeResolver.ResolveNodeText(_currentDialogue);
                _messages = DialogueTextFormatter.SplitMessage(resolvedDialogueText, maxLineCount);
                DisplayNextMessage();
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// 현재 페이지의 다음 메시지를 출력합니다.
        /// 마지막 페이지에 도달하면 선택지 또는 다음 노드로 진행합니다.
        /// </summary>
        private void DisplayNextMessage()
        {
            if (_indexMessage >= _messages.Count)
            {
                if (HasCurrentOptions())
                {
                    _systemMessageManager.ShowMessageWarning("Dialogue_SelectChoice");
                    return;
                }

                ProcessNextDialogue(_currentDialogue.nextNodeGuid);
                return;
            }

            if (textMessage != null)
            {
                textMessage.text = _messages[_indexMessage];
            }

            if (_indexMessage == _messages.Count - 1 && HasCurrentOptions())
            {
                _choiceButtonHandler.SetupButtons(_currentDialogue.options, ResolveChoiceLabel);
            }

            _indexMessage++;
        }

        /// <summary>
        /// 현재 노드에 선택지가 있는지 확인합니다.
        /// </summary>
        /// <returns>선택지가 있으면 true 입니다.</returns>
        private bool HasCurrentOptions()
        {
            return _currentDialogue?.options != null && _currentDialogue.options.Count > 0;
        }

        /// <summary>
        /// 선택지 표시 문자열을 결정합니다.
        /// </summary>
        /// <param name="option">대상 선택지입니다.</param>
        /// <returns>표시할 문자열입니다.</returns>
        private static string ResolveChoiceLabel(DialogueOption option)
        {
            return DialogueLocalizationRuntimeResolver.ResolveOptionText(option);
        }

        /// <summary>
        /// maxLineCount 만큼 대사를 다음 페이지로 넘깁니다.
        /// </summary>
        private void OnClickNext()
        {
            DisplayNextMessage();
        }

        /// <summary>
        /// 선택지 버튼 클릭 시 다음 노드로 이동합니다.
        /// </summary>
        /// <param name="buttonIndex">선택한 버튼 인덱스입니다.</param>
        private void OnClickAnswer(int buttonIndex)
        {
            if (!HasCurrentOptions() || buttonIndex < 0 || buttonIndex >= _currentDialogue.options.Count)
            {
                return;
            }

            DialogueOption option = _currentDialogue.options[buttonIndex];
            if (option == null)
            {
                return;
            }

            _choiceButtonHandler.HideButtons();
            ProcessNextDialogue(option.nextNodeGuid);
        }

        /// <summary>
        /// 일반 대화 도중 종료
        /// </summary>
        public void OnClickCancel()
        {
            ResetDialogue();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 일반 대화 종료 후 종료 이벤트를 발행합니다.
        /// </summary>
        private void EndDialogue()
        {
            DialogEventData data = new DialogEventData(npcUid: _currentNpcUid);
            GameEventManager.DialogEnd(data);
            ResetDialogue();
            gameObject.SetActive(false);
        }
    }
}
