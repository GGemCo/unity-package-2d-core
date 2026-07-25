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
    public partial class UIWindowDialogue : UIWindow
    {
        /// <summary>
        /// 대화창의 배치 방식을 정의합니다.
        /// </summary>
        private enum PositionType
        {
            None,
            CharacterTop,
        }

        /// <summary>
        /// 대화창의 시각 표현 방식을 정의합니다.
        /// </summary>
        private enum DialogueVisualMode
        {
            DialogueBox,
            SpeechBubble,
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("대화창의 화면 배치 방식")]
        [SerializeField] private PositionType positionType;

        [Tooltip("대화창의 시각 표현 방식")]
        [SerializeField] private DialogueVisualMode dialogueVisualMode = DialogueVisualMode.DialogueBox;

        [Tooltip("대화 박스 또는 말풍선의 루트 오브젝트")]
        [SerializeField] private GameObject panelDialogue;

        [Header("말풍선 월드 위치")]
        [Tooltip("SpeechBubble 모드에서 화자 머리 위 기준 위치에 추가할 월드 오프셋")]
        [SerializeField] private Vector3 speechBubbleWorldOffset = Vector3.zero;

        [Tooltip("SpeechBubble 월드 오프셋 X값을 화자 방향에 따라 보정하는 정책")]
        [SerializeField]
        private DialogueBalloonWorldOffsetXPolicy speechBubbleWorldOffsetXPolicy =
            DialogueBalloonWorldOffsetXPolicy.KeepOriginal;

        [Tooltip("말하는 캐릭터 썸네일 이미지 오브젝트")]
        [SerializeField] private Image imageThumbnail;

        [Tooltip("말하는 캐릭터 이름 텍스트 오브젝트")]
        [SerializeField] private TextMeshProUGUI textName;
        [Tooltip("대사 텍스트 오브젝트")]
        [SerializeField] private TextMeshProUGUI textMessage;
        [Tooltip("메시지와 입력 안내 이미지가 들어가는 패널")]
        [SerializeField] private RectTransform panelMessage;
        [Tooltip("썸네일 오른쪽 배치 기준 오프셋")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacter;
        [Tooltip("썸네일 왼쪽 배치 기준 오프셋")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacterLeft;
        [Tooltip("한번에 보여줄 대사 라인 수")]
        [SerializeField] private int maxLineCount = 3;

        [Header("말풍선 레이아웃")]
        [Tooltip("노드 썸네일 위치가 None일 때 기존 오른쪽 배치를 사용할지 여부")]
        [SerializeField] private bool useLegacyThumbnailFallbackForNone = true;
        [Tooltip("말풍선 패널과 썸네일 사이 간격(px)")]
        [SerializeField] private float thumbnailGapPx;
        [Tooltip("썸네일이 없는 쪽 텍스트 패딩(px)")]
        [SerializeField] private int textPaddingOnNonThumbnailSidePx = 7;
        [Tooltip("썸네일이 있는 쪽 텍스트 패딩(px)")]
        [SerializeField] private int textPaddingOnThumbnailSidePx = 3;
        [Tooltip("말꼬리를 기준으로 좌우 대칭 배치를 사용할지 여부")]
        [SerializeField] private bool useSymmetricLayoutByTail = true;
        [Tooltip("말꼬리를 화자 방향 앞으로 이동할 오프셋(px)")]
        [SerializeField] private float tailForwardOffsetPx = 3f;
        [Tooltip("말꼬리 중심 기준 최소 반너비(px). 0 이하면 강제하지 않습니다.")]
        [SerializeField] private float minHalfExtentByTailPx;
        [Tooltip("말풍선 말꼬리 이미지")]
        [SerializeField] private Image imageTail;

        [Header("말풍선 입력 안내 이미지")]
        [Tooltip("SpeechBubble 모드에서 프로젝트 공통 입력 안내 이미지 기본값을 사용할지 여부")]
        [SerializeField] private bool useProjectEnterIndicatorDefaultsInSpeechBubble;
        [Tooltip("입력 안내 이미지")]
        [SerializeField] private Image imageEnter;
        [Tooltip("프로젝트 기본값 대신 사용할 입력 안내 이미지")]
        [SerializeField] private Sprite enterIndicatorSpriteOverride;
        [Tooltip("대사 마지막 글자와 입력 안내 이미지 사이 간격(px)")]
        [SerializeField]
        private float enterIndicatorGapPx = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorGapPx;
        [Tooltip("입력 안내 이미지 깜빡임 속도(Hz)")]
        [SerializeField]
        private float enterIndicatorBlinkHz = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorBlinkHz;
        [Range(0f, 1f)]
        [Tooltip("입력 안내 이미지 최소 알파값")]
        [SerializeField]
        private float enterIndicatorMinAlpha = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorMinAlpha;

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
        private int _dialogueLoadVersion;
        private List<string> _messages;
        private Dictionary<string, DialogueNodeData> _dialogueNodeDatas;
        private int _currentNpcUid;
        private DialogueNodeData _currentDialogue;
        private SystemMessageManager _systemMessageManager;
        private ChoiceButtonHandler _choiceButtonHandler;
        private bool _isCurrentPageVisible;
        private RectTransform _panelDialogueRectTransform;
        private RectTransform _thumbnailRectTransform;
        private RectTransform _tailRectTransform;
        private VerticalLayoutGroup _panelMessageLayoutGroup;
        private LayoutElement _panelMessageLayoutElement;
        private Vector3 _thumbnailBaseScale = Vector3.one;
        private bool _hasThumbnailBaseScale;
        private Vector3 _tailBaseScale = Vector3.one;
        private bool _hasTailBaseScale;
        private bool _hasDefaultPanelMessagePadding;
        private int _defaultPanelMessagePaddingLeft;
        private int _defaultPanelMessagePaddingRight;
        private float _defaultPanelMessageMinWidth = -1f;
        private bool _hasDefaultPanelMessageMinWidth;
        private RectTransform _enterRectTransform;
        private bool _hasEnterBaseColor;
        private Color _enterBaseColor = Color.white;
        private bool _hasEnterBaseAnchoredPosition;
        private Vector2 _enterBaseAnchoredPosition;
        private float _resolvedEnterIndicatorGapPx = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorGapPx;
        private float _resolvedEnterIndicatorBlinkHz = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorBlinkHz;
        private float _resolvedEnterIndicatorMinAlpha = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorMinAlpha;
        private int _lastKnownEnterIndicatorVisibleCharacters = -1;
        private CanvasGroup _panelDialogueCanvasGroup;
        private bool _isInitialRevealPending;
        private int _initialRevealRequestVersion = -1;

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
            CacheSpeechBubbleLayoutReferences();
            CacheInitialRevealCanvasGroupReference();
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
            _dialogueLoadVersion++;
            _messages.Clear();
            _dialogueNodeDatas.Clear();
            _currentDialogue = null;
            _indexMessage = 0;
            _currentNpcUid = 0;
            _isCurrentPageVisible = false;
            _choiceButtonHandler.HideButtons();
            CancelDeferredInitialReveal();
            RestoreSpeechBubbleLayoutDefaults();
            ResetPanelDialogueParent();
        }

        /// <summary>
        /// 대사 json 을 불러와 대화창을 시작합니다.
        /// </summary>
        /// <param name="dialogueUid">dialogue 테이블 Uid 입니다.</param>
        /// <param name="npcUid">현재 대화 대상 NPC Uid 입니다.</param>
        /// <returns>대사 데이터를 정상적으로 불러와 시작했으면 true 입니다.</returns>
        public async Task<bool> LoadDialogue(int dialogueUid, int npcUid = 0)
        {
            DialogueData data = await DialogueLoader.LoadDialogueData(dialogueUid);
            if (data == null || data.nodes == null || data.nodes.Count == 0)
            {
                return false;
            }

            SetDialogue(data);
            _currentNpcUid = npcUid;
            DialogEventData eventData = new DialogEventData(npcUid: _currentNpcUid);
            GameEventManager.DialogStart(eventData);
            return true;
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

            PrepareSpeechBubbleEnterIndicatorForNewSession();
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
            int requestVersion = ++_dialogueLoadVersion;
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
                    BeginDeferredInitialReveal(requestVersion);
                    Sprite thumbnail = await DialogueCharacterHelper.GetThumbnail(_currentDialogue);
                    if (requestVersion != _dialogueLoadVersion || _currentDialogue == null)
                    {
                        return;
                    }

                    imageThumbnail.sprite = thumbnail;
                }

                if (textMessage != null)
                {
                    textMessage.fontSize = _currentDialogue.fontSize > 0 ? _currentDialogue.fontSize : _originalFontSize;
                }

                string resolvedDialogueText = DialogueLocalizationRuntimeResolver.ResolveNodeText(_currentDialogue);
                _messages = DialogueTextFormatter.SplitMessage(resolvedDialogueText, maxLineCount);
                if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
                {
                    ApplyThumbnailVisibilityAfterBinding();
                    RefreshThumbnailPosition();
                }

                DisplayNextMessage();
                TryCompleteDeferredInitialReveal(requestVersion);
            }
            catch (Exception e)
            {
                if (requestVersion == _dialogueLoadVersion)
                {
                    CancelDeferredInitialReveal();
                }

                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// 현재 페이지의 다음 메시지를 출력합니다.
        /// 마지막 페이지에 도달하면 선택지 또는 다음 노드로 진행합니다.
        /// </summary>
        private void DisplayNextMessage()
        {
            _isCurrentPageVisible = false;
            SetSpeechBubbleEnterIndicatorVisible(false, 1f);

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
                textMessage.maxVisibleCharacters = int.MaxValue;
            }

            if (_indexMessage == _messages.Count - 1 && HasCurrentOptions())
            {
                _choiceButtonHandler.SetupButtons(_currentDialogue.options, ResolveChoiceLabel);
            }

            _indexMessage++;
            _isCurrentPageVisible = true;
            if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                RefreshThumbnailPosition();
                RefreshSpeechBubbleEnterIndicatorPosition();
            }
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
        /// 활성 대화창의 화자 추적 위치와 말풍선 시각 상태를 프레임 단위로 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            RefreshPosition();
            RefreshSpeechBubbleRuntimeVisuals();
            UpdateSpeechBubbleEnterIndicatorRuntime();
        }

        /// <summary>
        /// 일반 대화 도중 종료
        /// </summary>
        public void OnClickCancel()
        {
            EndDialogue();
        }

        /// <summary>
        /// 맵 전환처럼 대화를 완료로 처리하면 안 되는 상황에서 대화창을 강제로 닫습니다.
        /// 대화 종료 이벤트는 발행하지 않고 UI 상태만 초기화합니다.
        /// </summary>
        public void CancelDialogue()
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
