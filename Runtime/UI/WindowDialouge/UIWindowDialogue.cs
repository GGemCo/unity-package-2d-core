using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowDialogue : UIWindow
    {
        [Header("대사속성")]
        [Tooltip("말하는 캐릭터 썸네일")]
        public Image imageThumbnail;
        [Tooltip("말하는 캐릭터 이름")]
        public TextMeshProUGUI textName;
        [Tooltip("대사")]
        public TextMeshProUGUI textMessage;
        [Tooltip("선택지 버튼 프리팹")]
        public GameObject prefabButtonAnswer;
        [Tooltip("선택지 버튼이 들어가는 Panel")]
        public Transform containerAnswer;
        [Tooltip("한번에 보여줄 대사 라인 수")]
        public int maxLineCount = 3;

        [Header("버튼")]
        [Tooltip("다음 대사 보기")]
        public Button buttonNextMessage;
        [Tooltip("선택지 왼쪽, 오른쪽 여백 사이즈ㅂ")]
        public int paddingWidth = 20;

        private float _originalFontSize;
        private int _indexMessage;
        private List<string> _messages;
        private Dictionary<string, DialogueNodeData> _dialogueNodeDatas;
        
        private int _currentDialogueUid;
        private int _currentNpcUid;
        private DialogueNodeData _currentDialogue;
        
        private SystemMessageManager _systemMessageManager;
        // 필드 추가
        private ChoiceButtonHandler _choiceButtonHandler;
        
        protected override void Awake()
        {
            uid = UIWindowManager.WindowUid.Dialogue;
            base.Awake();
            Initialize();
        }
        private void Initialize()
        {
            if (textMessage != null)
            {
                _originalFontSize = textMessage.fontSize;
            }
            buttonNextMessage?.onClick.RemoveAllListeners();
            buttonNextMessage?.onClick.AddListener(OnClickNext);
            _messages = new List<string>();
            _dialogueNodeDatas = new Dictionary<string, DialogueNodeData>();

            // 선택지 버튼 관리
            _choiceButtonHandler = new ChoiceButtonHandler(containerAnswer, paddingWidth, prefabButtonAnswer)
                {
                    OnChoiceSelected = OnClickAnswer
                };
            _choiceButtonHandler.InitializeButtonChoice(); // 버튼 생성만
        }

        protected override void Start()
        {
            base.Start();
            _systemMessageManager = SceneGame.Instance.systemMessageManager;
        }
        private void ResetDialogue()
        {
            _messages.Clear();
            _dialogueNodeDatas.Clear();
            _currentDialogueUid = 0;
            _currentDialogue = null;
            _indexMessage = 0;
            _currentNpcUid = 0;
            _choiceButtonHandler.HideButtons();
        }
        /// <summary>
        /// 대사 json 불러오기
        /// </summary>
        /// <param name="dialogueUid"></param>
        /// <param name="npcUid"></param>
        public async Task LoadDialogue(int dialogueUid, int npcUid = 0)
        {
            var data = await DialogueLoader.LoadDialogueData(dialogueUid);
            if (data != null)
            {
                SetDialogue(data);
                _currentNpcUid = npcUid;
            }
        }
        /// <summary>
        /// 일반 대화 시작
        /// </summary>
        private void SetDialogue(DialogueData data)
        {
            if (data == null) return;

            ResetDialogue();
            foreach (var nodeData in data.nodes)
            {
                _dialogueNodeDatas.TryAdd(nodeData.guid, nodeData);
            }

            if (!gameObject.activeSelf)
            {
                Show(true);
            }
            
            _indexMessage = 0;
            // 첫번째 대사 선택
            DialogueNodeData dialogue = data.nodes[0];

            ProcessNextDialogue(dialogue.guid);
        }
        /// <summary>
        /// 다음 대사 처리
        /// </summary>
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
                    textMessage.fontSize = _currentDialogue.fontSize>0?_currentDialogue.fontSize:_originalFontSize;
                }

                _messages = DialogueTextFormatter.SplitMessage(_currentDialogue.dialogueText, maxLineCount);
                DisplayNextMessage();
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// 메시지 표시
        /// </summary>
        private void DisplayNextMessage()
        {
            if (_indexMessage >= _messages.Count)
            {
                if (_currentDialogue.options.Count > 0)
                {
                    _systemMessageManager.ShowMessageWarning("선택지를 선택해주세요.");
                    return;
                }
                ProcessNextDialogue(_currentDialogue.nextNodeGuid);
                return;
            }

            textMessage.text = _messages[_indexMessage];

            if (_indexMessage == _messages.Count - 1 && _currentDialogue.options.Count > 0)
            {
                _choiceButtonHandler.SetupButtons(_currentDialogue.options);
            }

            _indexMessage++;
        }
        /// <summary>
        /// maxLineCount 만큼 대사 보기
        /// </summary>
        private void OnClickNext()
        {
            DisplayNextMessage();
        }
        /// <summary>
        /// 선택지 버튼 클릭시 처리
        /// </summary>
        /// <param name="buttonIndex"></param>
        private void OnClickAnswer(int buttonIndex)
        {
            var option = _currentDialogue.options[buttonIndex];
            if (option == null) return;

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
        /// 일반 대화 종료
        /// </summary>
        private void EndDialogue()
        {
            GameEventManager.DialogEnd(_currentNpcUid);
            ResetDialogue();
            gameObject.SetActive(false);
        }
    }
}