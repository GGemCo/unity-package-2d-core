using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
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

        private float originalFontSize;
        private int indexMessage;
        private List<string> messages;
        private Dictionary<string, DialogueNodeData> dialogueNodeDatas;
        
        private int currentDialogueUid;
        private int currentNpcUid;
        private DialogueNodeData currentDialogue;
        
        private SystemMessageManager systemMessageManager;
        // 필드 추가
        private ChoiceButtonHandler choiceButtonHandler;
        
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
                originalFontSize = textMessage.fontSize;
            }
            buttonNextMessage?.onClick.RemoveAllListeners();
            buttonNextMessage?.onClick.AddListener(OnClickNext);
            messages = new List<string>();
            dialogueNodeDatas = new Dictionary<string, DialogueNodeData>();

            // 선택지 버튼 관리
            choiceButtonHandler = new ChoiceButtonHandler(containerAnswer, paddingWidth, prefabButtonAnswer)
                {
                    OnChoiceSelected = OnClickAnswer
                };
            choiceButtonHandler.InitializeButtonChoice(); // 버튼 생성만
        }

        protected override void Start()
        {
            base.Start();
            systemMessageManager = SceneGame.Instance.systemMessageManager;
        }
        private void ResetDialogue()
        {
            messages.Clear();
            dialogueNodeDatas.Clear();
            currentDialogueUid = 0;
            currentDialogue = null;
            indexMessage = 0;
            currentNpcUid = 0;
            choiceButtonHandler.HideButtons();
        }
        /// <summary>
        /// 대사 json 불러오기
        /// </summary>
        /// <param name="dialogueUid"></param>
        /// <param name="npcUid"></param>
        public void LoadDialogue(int dialogueUid, int npcUid = 0)
        {
            var data = DialogueLoader.LoadDialogueData(dialogueUid);
            if (data != null)
            {
                SetDialogue(data);
                currentNpcUid = npcUid;
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
                dialogueNodeDatas.TryAdd(nodeData.guid, nodeData);
            }

            if (!gameObject.activeSelf)
            {
                Show(true);
            }
            
            indexMessage = 0;
            // 첫번째 대사 선택
            DialogueNodeData dialogue = data.nodes[0];

            ProcessNextDialogue(dialogue.guid);
        }
        /// <summary>
        /// 다음 대사 처리
        /// </summary>
        private void ProcessNextDialogue(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                EndDialogue();
                return;
            }

            indexMessage = 0;
            currentDialogue = dialogueNodeDatas.GetValueOrDefault(guid);

            if (textName != null)
            {
                textName.text = DialogueCharacterHelper.GetName(currentDialogue);
            }

            if (imageThumbnail != null)
            {
                imageThumbnail.sprite = DialogueCharacterHelper.GetThumbnail(currentDialogue);
            }

            if (textMessage != null)
            {
                textMessage.fontSize = currentDialogue.fontSize>0?currentDialogue.fontSize:originalFontSize;
            }

            messages = DialogueTextFormatter.SplitMessage(currentDialogue.dialogueText, maxLineCount);
            DisplayNextMessage();
        }

        /// <summary>
        /// 메시지 표시
        /// </summary>
        private void DisplayNextMessage()
        {
            if (indexMessage >= messages.Count)
            {
                if (currentDialogue.options.Count > 0)
                {
                    systemMessageManager.ShowMessageWarning("선택지를 선택해주세요.");
                    return;
                }
                ProcessNextDialogue(currentDialogue.nextNodeGuid);
                return;
            }

            textMessage.text = messages[indexMessage];

            if (indexMessage == messages.Count - 1 && currentDialogue.options.Count > 0)
            {
                choiceButtonHandler.SetupButtons(currentDialogue.options);
            }

            indexMessage++;
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
            var option = currentDialogue.options[buttonIndex];
            if (option == null) return;

            choiceButtonHandler.HideButtons();
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
            GameEventManager.DialogEnd(currentNpcUid);
            ResetDialogue();
            gameObject.SetActive(false);
        }
    }
}