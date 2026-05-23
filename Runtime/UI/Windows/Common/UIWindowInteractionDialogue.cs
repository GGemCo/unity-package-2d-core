using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 인터랙션 대화 UI를 관리합니다.
    /// dialogue graph 재생과 기본 interaction 선택지 표시를 하나의 창에서 순차적으로 처리합니다.
    /// </summary>
    public class UIWindowInteractionDialogue : UIWindow
    {
        /// <summary>
        /// 대화창 위치 타입입니다.
        /// </summary>
        private enum PositionType
        {
            None,
            CharacterTop,
        }

        /// <summary>
        /// 인터랙션 대화 UI의 시각 표현 모드를 정의합니다.
        /// </summary>
        private enum DialogueVisualMode
        {
            /// <summary>
            /// 기존 대화 박스 형태를 사용합니다.
            /// </summary>
            DialogueBox,

            /// <summary>
            /// 캐릭터 상단 말풍선 형태를 사용합니다.
            /// </summary>
            SpeechBubble,
        }

        private enum ChoiceType
        {
            Interaction,
            Quest,
            Dialogue,
        }

        private struct InteractionData
        {
            public ChoiceType ChoiceType;
            public InteractionConstants.Type InteractionType;
            public string CustomTypeKey;
            public int Value;
            public NpcQuestData NpcQuestData;
            public DialogueOption DialogueOption;
            public string Label;

            public bool HasBuiltInInteraction => InteractionType != InteractionConstants.Type.None;
            public bool HasCustomInteraction => string.IsNullOrWhiteSpace(CustomTypeKey) == false;
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("썸네일 기준 위치")]
        [SerializeField]
        private PositionType positionType;

        [Tooltip("대화 표현 모드")]
        [SerializeField]
        private DialogueVisualMode dialogueVisualMode = DialogueVisualMode.DialogueBox;

        [Tooltip("텍스트 박스, 썸네일, 박스 꼬리가 들어가있는 오브젝트")]
        [SerializeField]
        private GameObject panelDialogue;

        [Tooltip("말풍선 위치")]
        [SerializeField]
        private Vector3 offsetPanelDialogue;

        [Tooltip("캐릭터 썸네일")]
        [SerializeField]
        private Image imageThumbnail;

        [Tooltip("캐릭터 이름")]
        [SerializeField]
        private TextMeshProUGUI textName;

        [Tooltip("메시지")]
        [SerializeField]
        private TextMeshProUGUI textMessage;

        [Tooltip("메시지가 들어가는 Panel")]
        [SerializeField]
        private RectTransform panelMessage;

        [Tooltip("캐릭터 썸네일 이미지 위치. 오른쪽 기준")]
        [SerializeField]
        private Vector3 offsetImageThumbnailCharacter;

        [Tooltip("캐릭터 썸네일 이미지 위치. 왼쪽 기준")]
        [SerializeField]
        private Vector3 offsetImageThumbnailCharacterLeft;

        [Header("말풍선 레이아웃")]
        [Tooltip("노드 썸네일 위치가 None일 때 기존처럼 오른쪽 배치를 강제할지 여부")]
        [SerializeField]
        private bool useLegacyThumbnailFallbackForNone = true;

        [Tooltip("패널과 썸네일 사이 간격(px)")]
        [SerializeField]
        private float thumbnailGapPx = 0f;

        [Tooltip("썸네일이 없는 쪽 텍스트 패딩(px)")]
        [SerializeField]
        private int textPaddingOnNonThumbnailSidePx = 7;

        [Tooltip("썸네일이 있는 쪽 텍스트 패딩(px)")]
        [SerializeField]
        private int textPaddingOnThumbnailSidePx = 3;

        [Tooltip("말꼬리 기준 좌우 대칭 배치 사용 여부")]
        [SerializeField]
        private bool useSymmetricLayoutByTail = true;

        [Tooltip("말꼬리를 화자 방향 앞쪽으로 이동할 픽셀 오프셋")]
        [SerializeField]
        private float tailForwardOffsetPx = 3f;

        [Tooltip("말꼬리 중심 기준 최소 반너비(px). 0 이하면 강제를 사용하지 않습니다.")]
        [SerializeField]
        private float minHalfExtentByTailPx = 0f;

        [Tooltip("말풍선 말꼬리 이미지")]
        [SerializeField]
        private Image imageTail;

        [Tooltip("썸네일 Flip 적용 정책")]
        [SerializeField]
        private DialogueBalloonThumbnailFlipPolicy thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition;

        [Tooltip("원본 썸네일 이미지의 기본 바라보기 방향")]
        [SerializeField]
        private DialogueBalloonThumbnailSourceFacing thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;

        [Header("선택지 버튼")]
        [Tooltip("선택지 버튼 프리팹")]
        [SerializeField]
        private GameObject prefabButtonChoice;

        [Tooltip("선택지 버튼이 들어갈 Panel")]
        [SerializeField]
        private Transform containerButton;

        [Tooltip("퀘스트 선택 요청 메시지")]
        [SerializeField]
        private string messageQuestSelect;

        private const int ButtonCount = 10;
        private readonly Dictionary<int, Button> _buttonChoices = new();
        private readonly Dictionary<int, InteractionData> _interactionData = new();
        private readonly List<InteractionData> _defaultChoices = new();
        private readonly InteractionDialogueMessagePlayer _messagePlayer = new();
        private readonly InteractionDialogueRuntimeSession _dialogueSession = new();

        private int _currentCharacterUid;
        private int _dialogueLoadVersion;
        private CharacterBase _currentNpc;
        private StruckTableNpc _currentNpcData;
        private StruckTableInteraction _currentInteractionData;
        private InteractionDialogueSelectionResult _currentDialogueSelection;
        /// <summary>
        /// 현재 UI에 바인딩된 런타임 대화 노드입니다.
        /// 썸네일 위치는 세션의 최신 노드가 아니라 실제 출력 중인 이 노드를 기준으로 해석합니다.
        /// </summary>
        private DialogueNodeData _currentDialogueNode;
        private InteractionDialogueTextContext _currentTextContext = InteractionDialogueTextContext.Empty;
        private List<NpcQuestData> _currentQuestDatas = new();
        private float _defaultMessageFontSize;
        private bool _isLoadingDialogue;
        private bool _isExecutingChoice;
        private bool _hasAutoStartedCurrentChoiceSet;
        private int _pendingAutoStartChoiceIndex = -1;
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
        private int _lastKnownVisibleCharacters = -1;

        private UIWindowShop _uiWindowShop;
        private UIWindowShopSale _uiWindowShopSale;
        private UIWindowStash _uiWindowStash;
        private UIWindowItemUpgrade _uiWindowItemUpgrade;
        private UIWindowItemSalvage _uiWindowItemSalvage;
        private UIWindowItemCraft _uiWindowItemCraft;
        private UIWindowPlayerStatReset _uiWindowPlayerStatReset;
        private UIWindowWorldMap _uiWindowWorldMap;

        private TableQuest _tableQuest;
        private QuestManager _questManager;
        private LocalizationManager _localizationManager;
        private AddressableLoaderCharacterThumbnail _addressableLoaderCharacterThumbnail;
        private GGemCoPlayerSettings _playerSettings;
        private GGemCoNpcInteractionSettings _npcInteractionSettings;
        private PlayerData _playerData;
        private PopupManager _popupManager;

        protected override void Awake()
        {
            _currentCharacterUid = 0;
            uid = UIWindowConstants.WindowUid.InteractionDialogue;
            base.Awake();
            InitializeButtonChoice();

            if (textMessage != null)
            {
                _defaultMessageFontSize = textMessage.fontSize;
            }

            CacheSpeechBubbleLayoutReferences();
        }

        protected override void Start()
        {
            base.Start();
            _uiWindowShop = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowShop>(UIWindowConstants.WindowUid.Shop);
            _uiWindowStash = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowStash>(UIWindowConstants.WindowUid.Stash);
            _uiWindowShopSale = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowShopSale>(UIWindowConstants.WindowUid.ShopSale);
            _uiWindowItemUpgrade = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowItemUpgrade>(UIWindowConstants.WindowUid.ItemUpgrade);
            _uiWindowItemSalvage = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowItemSalvage>(UIWindowConstants.WindowUid.ItemSalvage);
            _uiWindowItemCraft = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowItemCraft>(UIWindowConstants.WindowUid.ItemCraft);
            _uiWindowPlayerStatReset = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerStatReset>(UIWindowConstants.WindowUid.PlayerStatReset);
            _uiWindowWorldMap = SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowWorldMap>(UIWindowConstants.WindowUid.WorldMap);

            _tableQuest = TableLoaderManager.Instance.TableQuest;
            _questManager = SceneGame.QuestManager;
            _localizationManager = LocalizationManager.Instance;
            _addressableLoaderCharacterThumbnail = AddressableLoaderCharacterThumbnail.Instance;
            _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            _npcInteractionSettings = ResolveNpcInteractionSettings();
            _playerData = SceneGame.saveDataManager.Player;
            _popupManager = SceneGame.popupManager;
        }

        /// <summary>
        /// 대화창이 열려 있는 동안 위치, 타자 효과, 입력 진행을 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            RefreshPosition();
            RefreshSpeechBubbleRuntimeVisuals();
            UpdateDialogueMessageReveal();
            TryHandleAdvancePointerInput();
        }

        /// <summary>
        /// 선택지 버튼 풀을 초기화합니다.
        /// </summary>
        private void InitializeButtonChoice()
        {
            if (GcLogger.IsNull(prefabButtonChoice, "선택 버튼 프리팹이 없습니다."))
            {
                return;
            }

            if (GcLogger.IsNull(containerButton, "선택 버튼 container 가 없습니다."))
            {
                return;
            }

            _buttonChoices.Clear();
            _interactionData.Clear();

            for (int i = 0; i < ButtonCount; i++)
            {
                GameObject buttonObj = Instantiate(prefabButtonChoice, containerButton);
                Button button = buttonObj.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                int capturedIndex = i;
                button.onClick.AddListener(() => OnClickChoice(capturedIndex));
                _buttonChoices[i] = button;
                button.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// NPC 인터랙션 정보를 UI에 바인딩하고 대화 세션을 시작합니다.
        /// </summary>
        /// <param name="npc">대상 NPC입니다.</param>
        /// <param name="npcData">NPC 테이블 데이터입니다.</param>
        /// <param name="interactionData">인터랙션 테이블 데이터입니다.</param>
        /// <param name="npcQuestDatas">퀘스트 목록입니다.</param>
        /// <param name="npcInteractionSettings">NPC 인터랙션 설정입니다.</param>
        /// <param name="dialogueSelection">이번 인터랙션에서 선택된 dialogue 정보입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        public void SetInfos(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<NpcQuestData> npcQuestDatas,
            GGemCoNpcInteractionSettings npcInteractionSettings = null,
            InteractionDialogueSelectionResult dialogueSelection = default(InteractionDialogueSelectionResult),
            InteractionDialogueTextContext textContext = null)
        {
            _dialogueLoadVersion++;
            _currentNpc = npc;
            _currentNpcData = npcData;
            _currentInteractionData = interactionData;
            _currentDialogueSelection = dialogueSelection.HasDialogue
                ? dialogueSelection
                : InteractionDialogueSelector.Select(interactionData);
            _currentTextContext = textContext ?? InteractionDialogueTextContext.Empty;
            _currentQuestDatas = npcQuestDatas != null ? new List<NpcQuestData>(npcQuestDatas) : new List<NpcQuestData>();
            _npcInteractionSettings = npcInteractionSettings != null ? npcInteractionSettings : ResolveNpcInteractionSettings();
            _currentCharacterUid = npcData != null ? npcData.Uid : 0;
            _isLoadingDialogue = false;

            ResetRuntimeStateForNewInteraction();
            CacheDefaultChoices(_currentQuestDatas, interactionData);
            RestoreNpcPresentation();

            Show(true);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
            RefreshPosition();

            if (_currentDialogueSelection.HasDialogue)
            {
                StartInteractionDialogueAsync(_dialogueLoadVersion, _currentDialogueSelection);
                return;
            }

            BeginDefaultChoiceFlow();
        }

        /// <summary>
        /// 새 인터랙션 세션 시작 전에 이전 런타임 상태를 초기화합니다.
        /// </summary>
        private void ResetRuntimeStateForNewInteraction()
        {
            ResetChoiceButtons();
            _messagePlayer.Clear(textMessage);
            _dialogueSession.Clear();
            ClearCurrentDialogueNode();
            _isExecutingChoice = false;
            ClearPendingAutoStartChoice();
            ApplyMessageFontSize(0f);
            if (imageThumbnail != null)
            {
                imageThumbnail.sprite = null;
                ApplyThumbnailVisibilityAfterBinding();
            }
            _lastKnownVisibleCharacters = -1;
            RestoreSpeechBubbleLayoutDefaults();
        }

        /// <summary>
        /// 현재 UI에 출력할 런타임 대화 노드를 지정합니다.
        /// </summary>
        /// <param name="node">UI 표시와 썸네일 위치 계산 기준이 되는 대화 노드입니다.</param>
        private void SetCurrentDialogueNode(DialogueNodeData node)
        {
            _currentDialogueNode = node;
        }

        /// <summary>
        /// 런타임 대화 노드 표시 상태를 해제해 기본 NPC 표시 규칙을 사용하도록 되돌립니다.
        /// </summary>
        private void ClearCurrentDialogueNode()
        {
            _currentDialogueNode = null;
        }

        /// <summary>
        /// DialogueData를 비동기로 로드하고 interaction 전용 대화 세션을 시작합니다.
        /// </summary>
        /// <param name="requestVersion">요청 시점 버전입니다.</param>
        /// <param name="dialogueSelection">이번 세션에서 선택된 dialogue 정보입니다.</param>
        private async void StartInteractionDialogueAsync(int requestVersion, InteractionDialogueSelectionResult dialogueSelection)
        {
            _isLoadingDialogue = true;
            DialogueData data = await DialogueLoader.LoadDialogueData(dialogueSelection.DialogueUid);

            if (requestVersion != _dialogueLoadVersion)
            {
                return;
            }

            _isLoadingDialogue = false;
            if (data == null)
            {
                BeginDefaultChoiceFlow();
                return;
            }

            _dialogueSession.Start(data, dialogueSelection.StartNodeGuid);
            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
                return;
            }

            await ApplyCurrentDialogueNodeAsync(requestVersion);
        }

        /// <summary>
        /// 기본 interaction/quest 선택지 흐름으로 진입합니다.
        /// 대화 그래프가 없거나 로드 실패했을 때의 fallback 진입점입니다.
        /// </summary>
        private void BeginDefaultChoiceFlow()
        {
            RestoreNpcPresentation();
            BindVisibleChoices(_defaultChoices);
            ApplyDialogueMessage(ResolveInitialMessage(_currentInteractionData, _currentQuestDatas), revealImmediately: false);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// 현재 대사 노드를 UI에 반영합니다.
        /// </summary>
        /// <param name="requestVersion">요청 시점 버전입니다.</param>
        /// <returns>표시 적용 작업 완료를 기다리는 Task입니다.</returns>
        private async Task ApplyCurrentDialogueNodeAsync(int requestVersion)
        {
            DialogueNodeData node = _dialogueSession.CurrentNode;
            if (node == null)
            {
                HandleDialogueSequenceCompleted();
                return;
            }

            SetCurrentDialogueNode(node);
            ApplyMessageFontSize(node.fontSize);
            SetNpcName(ResolveDialogueSpeakerName(node));
            await BindDialogueThumbnailAsync(node, requestVersion);
            if (requestVersion != _dialogueLoadVersion || _currentDialogueNode != node)
            {
                return;
            }

            BindVisibleChoices(BuildDialogueChoiceEntries(node));
            ApplyDialogueMessage(ResolveDialogueNodeText(node), revealImmediately: false);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// 대화 노드 전용 선택지 목록을 생성합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <returns>노드 선택지 목록입니다.</returns>
        private List<InteractionData> BuildDialogueChoiceEntries(DialogueNodeData node)
        {
            List<InteractionData> result = new List<InteractionData>();
            if (node?.options == null)
            {
                return result;
            }

            foreach (DialogueOption option in node.options)
            {
                if (option == null)
                {
                    continue;
                }

                result.Add(new InteractionData
                {
                    ChoiceType = ChoiceType.Dialogue,
                    DialogueOption = option,
                    Label = ResolveDialogueOptionText(option),
                });
            }

            return result;
        }

        /// <summary>
        /// 대화 그래프가 종료되었을 때 정책에 맞춰 후속 UI를 구성합니다.
        /// </summary>
        private void HandleDialogueSequenceCompleted()
        {
            _dialogueSession.Clear();
            ClearCurrentDialogueNode();
            BindVisibleChoices(_defaultChoices);

            if (_currentInteractionData != null && _currentInteractionData.DialogueEndPolicy == InteractionDialogueEndPolicy.Close)
            {
                CloseInteractionWindow();
                return;
            }

            if (_defaultChoices.Count > 0)
            {
                RestoreNpcPresentation();
                string followupMessage = ResolveFollowupMessageAfterDialogue();
                if (!string.IsNullOrEmpty(followupMessage))
                {
                    ApplyDialogueMessage(followupMessage, revealImmediately: false);
                }
                else
                {
                    RefreshChoiceButtonsVisibility();
                    RefreshThumbnailPosition();
                }

                return;
            }

            RefreshChoiceButtonsVisibility();
        }

        /// <summary>
        /// dialogue 종료 후 기본 선택지를 다시 표시할 때 사용할 후속 메시지를 계산합니다.
        /// </summary>
        /// <returns>후속 메시지입니다. 비어 있으면 기존 마지막 대사를 유지합니다.</returns>
        private string ResolveFollowupMessageAfterDialogue()
        {
            if (_currentInteractionData != null && !string.IsNullOrEmpty(_currentInteractionData.Message))
            {
                return ResolveInteractionLocalizedMessage(_currentInteractionData.Message);
            }

            if (_currentQuestDatas != null && _currentQuestDatas.Count > 0)
            {
                return FormatInteractionText(messageQuestSelect);
            }

            return string.Empty;
        }

        /// <summary>
        /// 기본 interaction/quest 선택지 목록을 캐시합니다.
        /// dialogue 종료 후 같은 데이터를 다시 바인딩할 수 있도록 UI 상태와 분리해 저장합니다.
        /// </summary>
        /// <param name="questList">NPC 퀘스트 목록입니다.</param>
        /// <param name="interactionData">interaction 테이블 데이터입니다.</param>
        private void CacheDefaultChoices(List<NpcQuestData> questList, StruckTableInteraction interactionData)
        {
            _defaultChoices.Clear();

            if (questList != null)
            {
                foreach (NpcQuestData npcQuestData in questList)
                {
                    if (npcQuestData == null)
                    {
                        continue;
                    }

                    StruckTableQuest info = _tableQuest.GetDataByUid(npcQuestData.QuestUid);
                    _defaultChoices.Add(new InteractionData
                    {
                        ChoiceType = ChoiceType.Quest,
                        NpcQuestData = npcQuestData,
                        Label = info != null ? info.Name : string.Empty,
                    });
                }
            }

            if (interactionData == null)
            {
                return;
            }

            TryAddDefaultInteractionChoice(interactionData.Type1, interactionData.Value1, interactionData.CustomTypeKey1);
            TryAddDefaultInteractionChoice(interactionData.Type2, interactionData.Value2, interactionData.CustomTypeKey2);
            TryAddDefaultInteractionChoice(interactionData.Type3, interactionData.Value3, interactionData.CustomTypeKey3);
        }

        /// <summary>
        /// 기본 interaction 선택지를 캐시 목록에 추가합니다.
        /// </summary>
        /// <param name="interactionType">기본 interaction 타입입니다.</param>
        /// <param name="value">보조 값입니다.</param>
        /// <param name="customTypeKey">커스텀 interaction 키입니다.</param>
        private void TryAddDefaultInteractionChoice(
            InteractionConstants.Type interactionType,
            int value,
            string customTypeKey)
        {
            bool hasBuiltIn = interactionType != InteractionConstants.Type.None;
            bool hasCustom = string.IsNullOrWhiteSpace(customTypeKey) == false;
            if (!hasBuiltIn && !hasCustom)
            {
                return;
            }

            _defaultChoices.Add(new InteractionData
            {
                ChoiceType = ChoiceType.Interaction,
                InteractionType = interactionType,
                CustomTypeKey = hasBuiltIn ? string.Empty : customTypeKey,
                Value = value,
                Label = hasBuiltIn
                    ? InteractionConstants.GetTypeName(interactionType)
                    : ResolveCustomInteractionDisplayName(customTypeKey, value),
            });
        }

        /// <summary>
        /// 현재 표시할 선택지 목록을 버튼 UI에 다시 바인딩합니다.
        /// </summary>
        /// <param name="choices">현재 단계에서 표시할 선택지 목록입니다.</param>
        private void BindVisibleChoices(IReadOnlyList<InteractionData> choices)
        {
            _interactionData.Clear();
            _isExecutingChoice = false;

            foreach (KeyValuePair<int, Button> pair in _buttonChoices)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.gameObject.SetActive(false);
                SetChoiceButtonLabel(pair.Value, string.Empty);
            }

            if (choices == null)
            {
                ClearPendingAutoStartChoice();
                return;
            }

            int count = Mathf.Min(ButtonCount, choices.Count);
            for (int i = 0; i < count; i++)
            {
                Button button = _buttonChoices.GetValueOrDefault(i);
                if (button == null)
                {
                    continue;
                }

                _interactionData[i] = choices[i];
                SetChoiceButtonLabel(button, choices[i].Label);
            }

            ConfigureAutoStartChoice(choices, count);
        }

        /// <summary>
        /// 매 프레임 타자 효과를 진행하고 마지막 페이지 도달 시 선택지 표시 상태를 갱신합니다.
        /// </summary>
        private void UpdateDialogueMessageReveal()
        {
            if (textMessage == null)
            {
                return;
            }

            _messagePlayer.Tick(textMessage, GetRevealDeltaTime());
            RefreshChoiceButtonsVisibility();

            if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                int currentVisibleCharacters = textMessage.maxVisibleCharacters;
                if (_lastKnownVisibleCharacters != currentVisibleCharacters)
                {
                    _lastKnownVisibleCharacters = currentVisibleCharacters;
                    RefreshThumbnailPosition();
                }
            }
        }

        /// <summary>
        /// 클릭 또는 터치 입력으로 대화 페이지 또는 다음 대사 노드를 진행합니다.
        /// </summary>
        private void TryHandleAdvancePointerInput()
        {
            if (!CanAdvanceDialogue())
            {
                return;
            }

            if (!TryGetAdvancePointerPosition(out Vector2 screenPoint))
            {
                return;
            }

            if (IsAdvancePointerBlocked(screenPoint))
            {
                return;
            }

            InteractionDialogueAdvanceResult result = _messagePlayer.Advance(textMessage);
            if (result != InteractionDialogueAdvanceResult.None)
            {
                RefreshChoiceButtonsVisibility();
                RefreshThumbnailPosition();
                return;
            }

            if (_messagePlayer.IsSequenceCompleted)
            {
                TryAdvanceDialogueNodeAfterMessageEnd();
            }
        }

        /// <summary>
        /// 현재 메시지 시퀀스가 끝난 뒤, 다음 대사 노드로 자동 진행할 수 있으면 진행합니다.
        /// </summary>
        private void TryAdvanceDialogueNodeAfterMessageEnd()
        {
            if (_isLoadingDialogue || !_dialogueSession.IsActive)
            {
                return;
            }

            if (_dialogueSession.HasCurrentOptions)
            {
                RefreshChoiceButtonsVisibility();
                return;
            }

            if (_dialogueSession.TryMoveNext())
            {
                int requestVersion = _dialogueLoadVersion;
                _ = ApplyCurrentDialogueNodeAsync(requestVersion);
                return;
            }

            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
            }
        }

        /// <summary>
        /// 현재 상태에서 대화 진행 입력을 받을 수 있는지 확인합니다.
        /// </summary>
        /// <returns>대화 진행 입력을 받을 수 있으면 true입니다.</returns>
        private bool CanAdvanceDialogue()
        {
            GGemCoNpcInteractionSettings settings = ResolveNpcInteractionSettings();
            if (settings.page.advanceInputPolicy != InteractionDialogueAdvanceInputPolicy.PointerClickOrTap)
            {
                return false;
            }

            if (!_messagePlayer.HasMessage)
            {
                return false;
            }

            return !_messagePlayer.IsSequenceCompleted || CanAdvanceAfterSequenceCompleted();
        }

        /// <summary>
        /// 현재 페이지 시퀀스가 끝난 뒤에도 다음 노드로 넘어갈 수 있는지 확인합니다.
        /// dialogue graph가 활성 상태이고 현재 노드에 선택지가 없을 때만 true를 반환합니다.
        /// </summary>
        /// <returns>다음 노드 자동 진행이 가능하면 true입니다.</returns>
        private bool CanAdvanceAfterSequenceCompleted()
        {
            if (_isLoadingDialogue || !_dialogueSession.IsActive)
            {
                return false;
            }

            return !_dialogueSession.HasCurrentOptions;
        }

        /// <summary>
        /// 현재 프로젝트의 입력 시스템 정의 심볼에 맞춰 대화 진행 입력 좌표를 가져옵니다.
        /// </summary>
        /// <param name="screenPoint">입력이 발생한 화면 좌표입니다.</param>
        /// <returns>현재 프레임에 대화 진행 입력이 발생했으면 true입니다.</returns>
        private bool TryGetAdvancePointerPosition(out Vector2 screenPoint)
        {
#if GGEMCO_USE_OLD_INPUT
            return TryGetAdvancePointerPositionOldInput(out screenPoint);
#elif GGEMCO_USE_NEW_INPUT
            return TryGetAdvancePointerPositionNewInput(out screenPoint);
#else
            screenPoint = default;
            return false;
#endif
        }

        /// <summary>
        /// Legacy Input Manager 기준으로 클릭 또는 터치 시작 좌표를 가져옵니다.
        /// </summary>
        /// <param name="screenPoint">입력이 발생한 화면 좌표입니다.</param>
        /// <returns>현재 프레임에 입력이 감지되었으면 true입니다.</returns>
        private bool TryGetAdvancePointerPositionOldInput(out Vector2 screenPoint)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == UnityEngine.TouchPhase.Began)
                {
                    screenPoint = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePosition = Input.mousePosition;
                screenPoint = new Vector2(mousePosition.x, mousePosition.y);
                return true;
            }

            screenPoint = default;
            return false;
        }

        /// <summary>
        /// New Input System 기준으로 클릭 또는 터치 시작 좌표를 가져옵니다.
        /// </summary>
        /// <param name="screenPoint">입력이 발생한 화면 좌표입니다.</param>
        /// <returns>현재 프레임에 입력이 감지되었으면 true입니다.</returns>
        private bool TryGetAdvancePointerPositionNewInput(out Vector2 screenPoint)
        {
#if GGEMCO_USE_NEW_INPUT
            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame)
                {
                    screenPoint = primaryTouch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPoint = Mouse.current.position.ReadValue();
                return true;
            }
#endif

            screenPoint = default;
            return false;
        }

        /// <summary>
        /// 클릭 또는 터치가 선택지/닫기 버튼 위에서 발생했는지 확인합니다.
        /// </summary>
        /// <param name="screenPoint">화면 좌표입니다.</param>
        /// <returns>기존 버튼 입력과 충돌하면 true입니다.</returns>
        private bool IsAdvancePointerBlocked(Vector2 screenPoint)
        {
            if (IsPointerInsideButton(buttonClose, screenPoint))
            {
                return true;
            }

            foreach (Button button in _buttonChoices.Values)
            {
                if (IsPointerInsideButton(button, screenPoint))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 화면 좌표가 버튼 Rect 내부인지 확인합니다.
        /// </summary>
        /// <param name="button">확인할 버튼입니다.</param>
        /// <param name="screenPoint">화면 좌표입니다.</param>
        /// <returns>버튼 내부 좌표이면 true입니다.</returns>
        private bool IsPointerInsideButton(Button button, Vector2 screenPoint)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!button.TryGetComponent(out RectTransform rectTransform))
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, GetUiEventCamera());
        }

        /// <summary>
        /// 현재 UI가 속한 Canvas의 이벤트 카메라를 반환합니다.
        /// </summary>
        /// <returns>Screen Space Overlay가 아니면 Canvas 카메라를 반환합니다.</returns>
        private Camera GetUiEventCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        /// <summary>
        /// 현재 타자 효과 갱신에 사용할 deltaTime을 반환합니다.
        /// </summary>
        /// <returns>설정에 따라 보정된 deltaTime입니다.</returns>
        private float GetRevealDeltaTime()
        {
            GGemCoNpcInteractionSettings settings = ResolveNpcInteractionSettings();
            return settings.reveal.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        /// <summary>
        /// 현재 NPC 기본 정보를 기준으로 이름과 썸네일을 복원합니다.
        /// </summary>
        private void RestoreNpcPresentation()
        {
            ClearCurrentDialogueNode();
            if (_currentNpcData == null)
            {
                return;
            }

            SetNpcName(_currentNpcData.Name);
            ApplyMessageFontSize(0f);
            BindNpcThumbnail(_currentNpcData);
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// NPC 썸네일을 로드해 바인딩합니다.
        /// </summary>
        /// <param name="npcData">NPC 테이블 데이터입니다.</param>
        private void BindNpcThumbnail(StruckTableNpc npcData)
        {
            if (imageThumbnail == null)
            {
                return;
            }

            imageThumbnail.sprite = null;
            if (npcData == null || string.IsNullOrEmpty(npcData.ImageThumbnailFileName))
            {
                ApplyThumbnailVisibilityAfterBinding();
                return;
            }

            string key = ConfigAddressableKey.GetKeyThumbnailNpc(npcData.ImageThumbnailFileName);
            Sprite sprite = _addressableLoaderCharacterThumbnail.GetCharacterThumbnailByName(key);
            if (sprite != null)
            {
                imageThumbnail.sprite = sprite;
            }

            ApplyThumbnailVisibilityAfterBinding();
        }

        /// <summary>
        /// 대사 노드 기준 썸네일을 비동기로 바인딩합니다.
        /// 노드에 전용 썸네일이 없으면 현재 NPC 기본 썸네일을 유지합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <param name="requestVersion">요청 시점 버전입니다.</param>
        /// <returns>썸네일 로드 완료를 기다리는 Task입니다.</returns>
        private async Task BindDialogueThumbnailAsync(DialogueNodeData node, int requestVersion)
        {
            if (imageThumbnail == null)
            {
                return;
            }

            BindNpcThumbnail(_currentNpcData);
            Sprite sprite = await DialogueCharacterHelper.GetThumbnail(node);
            if (requestVersion != _dialogueLoadVersion || _currentDialogueNode != node)
            {
                return;
            }

            if (sprite != null)
            {
                imageThumbnail.sprite = sprite;
            }

            ApplyThumbnailVisibilityAfterBinding();
        }

        /// <summary>
        /// 인터랙션 시작 시 표시할 첫 메시지를 계산합니다.
        /// </summary>
        /// <param name="interactionData">인터랙션 데이터입니다.</param>
        /// <param name="questList">퀘스트 목록입니다.</param>
        /// <returns>초기 표시 메시지입니다.</returns>
        private string ResolveInitialMessage(StruckTableInteraction interactionData, List<NpcQuestData> questList)
        {
            if (interactionData != null && !string.IsNullOrEmpty(interactionData.Message))
            {
                return ResolveInteractionLocalizedMessage(interactionData.Message);
            }

            if (questList != null && questList.Count > 0)
            {
                return FormatInteractionText(messageQuestSelect);
            }

            return string.Empty;
        }

        /// <summary>
        /// 현재 대화 노드의 발화자 이름을 해석합니다.
        /// 이름을 찾지 못하면 현재 NPC 이름을 유지합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <returns>표시할 발화자 이름입니다.</returns>
        private string ResolveDialogueSpeakerName(DialogueNodeData node)
        {
            string speakerName = DialogueCharacterHelper.GetName(node);
            if (!string.IsNullOrEmpty(speakerName))
            {
                return speakerName;
            }

            return _currentNpcData != null ? _currentNpcData.Name : string.Empty;
        }


        /// <summary>
        /// 현재 대화 노드 본문을 localization table/key 기준으로 해석합니다.
        /// localization 정보가 없거나 실패하면 기존 raw 문자열 포맷 결과를 fallback 으로 사용합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <returns>표시할 본문 문자열입니다.</returns>
        private string ResolveDialogueNodeText(DialogueNodeData node)
        {
            string fallback = node != null ? FormatInteractionText(node.dialogueText) : string.Empty;
            object[] arguments = _currentTextContext?.PositionalArgs ?? Array.Empty<object>();
            return DialogueLocalizationRuntimeResolver.Resolve(node?.dialogueTable, node?.dialogueKey, fallback, arguments);
        }

        /// <summary>
        /// 현재 대화 선택지 문자열을 localization table/key 기준으로 해석합니다.
        /// localization 정보가 없거나 실패하면 기존 raw 문자열 포맷 결과를 fallback 으로 사용합니다.
        /// </summary>
        /// <param name="option">현재 선택지입니다.</param>
        /// <returns>표시할 선택지 문자열입니다.</returns>
        private string ResolveDialogueOptionText(DialogueOption option)
        {
            string fallback = option != null ? FormatInteractionText(option.optionText) : string.Empty;
            object[] arguments = _currentTextContext?.PositionalArgs ?? Array.Empty<object>();
            return DialogueLocalizationRuntimeResolver.Resolve(option?.optionTable, option?.optionKey, fallback, arguments);
        }

        /// <summary>
        /// 현재 인터랙션 텍스트 컨텍스트를 사용해 원본 문자열을 포맷합니다.
        /// </summary>
        /// <param name="template">원본 문자열입니다.</param>
        /// <returns>포맷이 적용된 문자열입니다.</returns>
        private string FormatInteractionText(string template)
        {
            return InteractionDialogueFormatter.FormatRaw(template, _currentTextContext);
        }

        /// <summary>
        /// 인터랙션 로컬라이즈 키를 현재 텍스트 컨텍스트와 함께 평가합니다.
        /// </summary>
        /// <param name="localizationKey">평가할 로컬라이즈 키입니다.</param>
        /// <returns>치환이 적용된 로컬라이즈 문자열입니다.</returns>
        private string ResolveInteractionLocalizedMessage(string localizationKey)
        {
            if (_localizationManager == null || string.IsNullOrWhiteSpace(localizationKey))
            {
                return string.Empty;
            }

            object[] arguments = _currentTextContext?.PositionalArgs ?? Array.Empty<object>();
            return _localizationManager.GetSmartInteractionByKey(localizationKey, arguments);
        }

        /// <summary>
        /// 설정된 정책에 따라 메시지를 새로 바인딩합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        /// <param name="revealImmediately">true이면 현재 페이지를 즉시 모두 노출합니다.</param>
        private void ApplyDialogueMessage(string message, bool revealImmediately)
        {
            if (textMessage == null)
            {
                return;
            }

            _messagePlayer.Configure(textMessage, message, ResolveNpcInteractionSettings());
            if (revealImmediately)
            {
                _messagePlayer.RevealCurrentPage(textMessage);
            }

            _lastKnownVisibleCharacters = textMessage.maxVisibleCharacters;

            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// 메시지 텍스트 폰트 크기를 기본값 또는 지정값으로 적용합니다.
        /// </summary>
        /// <param name="fontSize">적용할 폰트 크기입니다. 0 이하이면 기본 크기를 사용합니다.</param>
        private void ApplyMessageFontSize(float fontSize)
        {
            if (textMessage == null)
            {
                return;
            }

            textMessage.fontSize = fontSize > 0f ? fontSize : _defaultMessageFontSize;
        }

        /// <summary>
        /// 선택지 버튼 상태를 초기화합니다.
        /// </summary>
        private void ResetChoiceButtons()
        {
            _interactionData.Clear();

            foreach (KeyValuePair<int, Button> pair in _buttonChoices)
            {
                Button button = pair.Value;
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(false);
                SetChoiceButtonLabel(button, string.Empty);
            }

            if (containerButton != null)
            {
                containerButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 현재 메시지 완료 여부에 따라 선택지 버튼 표시 상태를 갱신합니다.
        /// </summary>
        private void RefreshChoiceButtonsVisibility()
        {
            bool shouldShowChoices = _interactionData.Count > 0 && _messagePlayer.IsSequenceCompleted;
            if (containerButton != null)
            {
                containerButton.gameObject.SetActive(shouldShowChoices);
            }

            foreach (KeyValuePair<int, Button> pair in _buttonChoices)
            {
                Button button = pair.Value;
                bool show = shouldShowChoices && _interactionData.ContainsKey(pair.Key);
                if (button != null)
                {
                    button.gameObject.SetActive(show);
                }
            }

            TryAutoStartSingleChoice();
        }

        /// <summary>
        /// 현재 선택지 상태를 기준으로 단일 선택 자동 시작 예약을 갱신합니다.
        /// </summary>
        /// <param name="choices">현재 바인딩한 선택지 목록입니다.</param>
        /// <param name="count">실제로 바인딩된 선택지 수입니다.</param>
        private void ConfigureAutoStartChoice(IReadOnlyList<InteractionData> choices, int count)
        {
            ClearPendingAutoStartChoice();

            if (!CanAutoStartWhenOneChoice())
            {
                return;
            }

            if (choices == null || count != 1)
            {
                return;
            }

            _pendingAutoStartChoiceIndex = 0;
            _hasAutoStartedCurrentChoiceSet = false;
        }

        /// <summary>
        /// 현재 선택지 목록이 단일 선택 자동 시작 정책을 만족하면 한 번만 자동 실행합니다.
        /// </summary>
        private void TryAutoStartSingleChoice()
        {
            if (_pendingAutoStartChoiceIndex < 0)
            {
                return;
            }

            if (_hasAutoStartedCurrentChoiceSet || _isExecutingChoice)
            {
                return;
            }

            if (!_messagePlayer.IsSequenceCompleted)
            {
                return;
            }

            if (!_interactionData.ContainsKey(_pendingAutoStartChoiceIndex))
            {
                ClearPendingAutoStartChoice();
                return;
            }

            _hasAutoStartedCurrentChoiceSet = true;
            OnClickChoice(_pendingAutoStartChoiceIndex);
        }

        /// <summary>
        /// 현재 인터랙션 상태에서 단일 선택 자동 시작 정책을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>정책 사용 가능 시 true입니다.</returns>
        private bool CanAutoStartWhenOneChoice()
        {
            GGemCoNpcInteractionSettings settings = ResolveNpcInteractionSettings();
            return settings != null && settings.autoStartWhenOneChoice;
        }

        /// <summary>
        /// 현재 선택지 집합에 대한 자동 시작 예약 상태를 초기화합니다.
        /// </summary>
        private void ClearPendingAutoStartChoice()
        {
            _pendingAutoStartChoiceIndex = -1;
            _hasAutoStartedCurrentChoiceSet = false;
        }

        /// <summary>
        /// PositionType 별 위치 조정을 수행합니다.
        /// </summary>
        private void RefreshPosition()
        {
            switch (positionType)
            {
                case PositionType.CharacterTop:
                    RefreshPositionCharacterTop();
                    break;
            }
        }

        /// <summary>
        /// NPC 머리 위에 대화창을 배치합니다.
        /// </summary>
        private void RefreshPositionCharacterTop()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            if (!_currentNpc || panelDialogue == null || SceneGame.containerDialogueBalloon == null)
            {
                return;
            }

            panelDialogue.transform.SetParent(SceneGame.containerDialogueBalloon.transform, false);
            Vector3 worldPosition = _currentNpc.transform.position + new Vector3(0, _currentNpc.GetHeightByScale(), 0) + offsetPanelDialogue;
            panelDialogue.transform.position = worldPosition;
        }

        /// <summary>
        /// 썸네일 위치를 대화 내용에 맞게 정리합니다.
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                RefreshSpeechBubbleLayout();
                return;
            }

            RefreshDialogueBoxThumbnailPosition();
        }

        /// <summary>
        /// 기존 대화 박스 모드의 썸네일 위치 계산을 수행합니다.
        /// </summary>
        private void RefreshDialogueBoxThumbnailPosition()
        {
            if (panelMessage == null)
            {
                return;
            }

            if (imageThumbnail == null || !imageThumbnail.gameObject.TryGetComponent(out RectTransform thumbnailRectTransform))
            {
                return;
            }

            ConfigCommon.ThumbnailPositionType thumbnailPositionType = ResolveEffectiveThumbnailPositionType();
            if (thumbnailPositionType == ConfigCommon.ThumbnailPositionType.None)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);

            float panelHalfWidth = panelMessage.rect.width * 0.5f;
            float thumbnailHalfWidth = thumbnailRectTransform.rect.width * 0.5f;
            Vector3 offset = ResolveThumbnailOffset(thumbnailPositionType);
            float side = ResolveThumbnailSideSign(thumbnailPositionType);

            float x = side * (panelHalfWidth + thumbnailHalfWidth) + offset.x;
            float y = offset.y;
            imageThumbnail.transform.localPosition = new Vector3(x, y, 0f);
        }

        /// <summary>
        /// 현재 UI에 바인딩된 대화 노드 기준 썸네일 배치 방향을 해석합니다.
        /// </summary>
        /// <remarks>
        /// 대화 노드가 없거나 <see cref="ConfigCommon.ThumbnailPositionType.None"/>이면 기존 인터랙션 UI와 동일하게 오른쪽 배치를 사용합니다.
        /// </remarks>
        /// <returns>썸네일 위치 타입입니다.</returns>
        private ConfigCommon.ThumbnailPositionType ResolveEffectiveThumbnailPositionType()
        {
            if (_currentDialogueNode == null)
            {
                return ConfigCommon.ThumbnailPositionType.Right;
            }

            if (_currentDialogueNode.thumbnailPositionType != ConfigCommon.ThumbnailPositionType.None)
            {
                return _currentDialogueNode.thumbnailPositionType;
            }

            if (useLegacyThumbnailFallbackForNone || dialogueVisualMode == DialogueVisualMode.DialogueBox)
            {
                return ConfigCommon.ThumbnailPositionType.Right;
            }

            return ConfigCommon.ThumbnailPositionType.None;
        }

        /// <summary>
        /// 말풍선 모드에서 패널/썸네일/말꼬리 정렬을 함께 갱신합니다.
        /// </summary>
        private void RefreshSpeechBubbleLayout()
        {
            if (!TryEnsureSpeechBubbleLayoutReferences())
            {
                return;
            }

            ApplyThumbnailVisibilityAfterBinding();
            ConfigCommon.ThumbnailPositionType thumbnailPositionType = ResolveEffectiveThumbnailPositionType();
            bool hasThumbnail = HasVisibleThumbnail();
            float side = ResolveThumbnailSideSign(thumbnailPositionType);
            Vector3 thumbnailOffset = ResolveThumbnailOffset(thumbnailPositionType);

            ApplySpeechBubblePanelPaddingByThumbnailSide(hasThumbnail, thumbnailPositionType);
            UpdatePanelMinimumWidthByTailSymmetry(hasThumbnail, side, thumbnailOffset.x);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);

            float panelHalfWidth = panelMessage.rect.width * 0.5f;
            float panelCenterX = 0f;
            if (TryEnsureTailReferences())
            {
                float tailX = _tailRectTransform.anchoredPosition.x;
                panelCenterX = tailX;
                if (useSymmetricLayoutByTail && hasThumbnail)
                {
                    float thumbnailWidth = _thumbnailRectTransform != null ? _thumbnailRectTransform.rect.width : 0f;
                    float thumbnailSpan = Mathf.Max(0f, thumbnailGapPx) + thumbnailWidth;
                    panelCenterX = tailX - (side * (thumbnailSpan * 0.5f)) - (thumbnailOffset.x * 0.5f);
                }
            }

            SetPanelMessageCenterX(panelCenterX);

            if (hasThumbnail && _thumbnailRectTransform != null)
            {
                float thumbnailHalfWidth = _thumbnailRectTransform.rect.width * 0.5f;
                Vector2 thumbnailAnchoredPosition = _thumbnailRectTransform.anchoredPosition;
                thumbnailAnchoredPosition.x = side * (panelHalfWidth + Mathf.Max(0f, thumbnailGapPx) + thumbnailHalfWidth) + thumbnailOffset.x;
                thumbnailAnchoredPosition.y = thumbnailOffset.y;
                _thumbnailRectTransform.anchoredPosition = thumbnailAnchoredPosition;
            }

            ApplyThumbnailFlip();
        }

        /// <summary>
        /// 말풍선 레이아웃 참조를 캐시합니다.
        /// 프리팹 참조가 비어 있어도 이름 기반으로 자동 탐색해 보정합니다.
        /// </summary>
        private void CacheSpeechBubbleLayoutReferences()
        {
            if (panelDialogue == null)
            {
                panelDialogue = transform.Find("Panel")?.gameObject;
            }

            if (_panelDialogueRectTransform == null)
            {
                _panelDialogueRectTransform = panelDialogue != null
                    ? panelDialogue.transform as RectTransform
                    : transform.Find("Panel") as RectTransform;
            }

            if (panelMessage == null)
            {
                panelMessage = _panelDialogueRectTransform?.Find("Panel") as RectTransform;
                panelMessage ??= transform.Find("Panel/Panel") as RectTransform;
                panelMessage ??= textMessage != null ? textMessage.transform.parent as RectTransform : null;
            }

            if (imageThumbnail == null)
            {
                Transform thumbnailTransform = panelMessage?.Find("ImageThumbnail");
                thumbnailTransform ??= _panelDialogueRectTransform?.Find("Panel/ImageThumbnail");
                thumbnailTransform ??= transform.Find("Panel/Panel/ImageThumbnail");
                thumbnailTransform ??= transform.Find("ImageThumbnail");
                if (thumbnailTransform != null)
                {
                    imageThumbnail = thumbnailTransform.GetComponent<Image>();
                }
            }

            if (imageTail == null)
            {
                Transform tailTransform = _panelDialogueRectTransform?.Find("IconTail");
                tailTransform ??= panelMessage?.Find("IconTail");
                tailTransform ??= transform.Find("Panel/IconTail");
                tailTransform ??= transform.Find("IconTail");
                if (tailTransform != null)
                {
                    imageTail = tailTransform.GetComponent<Image>();
                }
            }

            _thumbnailRectTransform = imageThumbnail != null
                ? imageThumbnail.GetComponent<RectTransform>()
                : null;

            if (_thumbnailRectTransform != null && !_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            _tailRectTransform = imageTail != null
                ? imageTail.GetComponent<RectTransform>()
                : _tailRectTransform;

            if (_tailRectTransform != null && !_hasTailBaseScale)
            {
                _tailBaseScale = _tailRectTransform.localScale;
                _hasTailBaseScale = true;
            }

            if (panelMessage != null)
            {
                _panelMessageLayoutGroup = panelMessage.GetComponent<VerticalLayoutGroup>();
                _panelMessageLayoutElement = panelMessage.GetComponent<LayoutElement>();
                if (_panelMessageLayoutElement == null)
                {
                    _panelMessageLayoutElement = panelMessage.gameObject.AddComponent<LayoutElement>();
                }

                if (_panelMessageLayoutGroup != null && !_hasDefaultPanelMessagePadding)
                {
                    _defaultPanelMessagePaddingLeft = _panelMessageLayoutGroup.padding.left;
                    _defaultPanelMessagePaddingRight = _panelMessageLayoutGroup.padding.right;
                    _hasDefaultPanelMessagePadding = true;
                }

                if (_panelMessageLayoutElement != null && !_hasDefaultPanelMessageMinWidth)
                {
                    _defaultPanelMessageMinWidth = _panelMessageLayoutElement.minWidth;
                    _hasDefaultPanelMessageMinWidth = true;
                }
            }
        }

        /// <summary>
        /// 말풍선 레이아웃 계산에 필요한 참조가 준비되어 있는지 확인합니다.
        /// </summary>
        /// <returns>참조가 준비되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureSpeechBubbleLayoutReferences()
        {
            if (panelMessage == null || _thumbnailRectTransform == null || _panelDialogueRectTransform == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            return panelMessage != null &&
                   _panelDialogueRectTransform != null &&
                   _thumbnailRectTransform != null;
        }

        /// <summary>
        /// 말꼬리 처리에 필요한 참조가 준비되었는지 확인합니다.
        /// </summary>
        /// <returns>말꼬리 참조가 준비되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureTailReferences()
        {
            if (imageTail == null || _tailRectTransform == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            return imageTail != null && _tailRectTransform != null;
        }

        /// <summary>
        /// 썸네일 스프라이트 바인딩 결과를 바탕으로 표시 상태를 갱신합니다.
        /// </summary>
        private void ApplyThumbnailVisibilityAfterBinding()
        {
            if (imageThumbnail == null)
            {
                return;
            }

            bool shouldShow = imageThumbnail.sprite != null;
            if (ResolveEffectiveThumbnailPositionType() == ConfigCommon.ThumbnailPositionType.None)
            {
                shouldShow = false;
            }

            if (shouldShow && dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                imageThumbnail.SetNativeSize();
            }

            imageThumbnail.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                RestoreThumbnailScaleToBase();
            }
        }

        /// <summary>
        /// 썸네일 배치 방향을 좌(-1) / 우(+1) 부호로 변환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        /// <returns>왼쪽이면 -1, 오른쪽이면 +1을 반환합니다.</returns>
        private static float ResolveThumbnailSideSign(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left ? -1f : 1f;
        }

        /// <summary>
        /// 썸네일 배치 방향에 맞는 오프셋 값을 반환합니다.
        /// </summary>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        /// <returns>좌우 방향별 오프셋입니다.</returns>
        private Vector3 ResolveThumbnailOffset(ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            return thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left
                ? offsetImageThumbnailCharacterLeft
                : offsetImageThumbnailCharacter;
        }

        /// <summary>
        /// 현재 프레임에서 썸네일이 실제로 보이는 상태인지 반환합니다.
        /// </summary>
        /// <returns>썸네일이 활성화되고 스프라이트가 유효하면 <see langword="true"/>를 반환합니다.</returns>
        private bool HasVisibleThumbnail()
        {
            return imageThumbnail != null &&
                   imageThumbnail.gameObject.activeSelf &&
                   imageThumbnail.sprite != null &&
                   ResolveEffectiveThumbnailPositionType() != ConfigCommon.ThumbnailPositionType.None;
        }

        /// <summary>
        /// 말풍선 모드에서 텍스트 패널 좌우 패딩을 썸네일 배치 방향에 맞게 조정합니다.
        /// </summary>
        /// <param name="hasThumbnail">썸네일 표시 여부입니다.</param>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        private void ApplySpeechBubblePanelPaddingByThumbnailSide(bool hasThumbnail, ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            if (_panelMessageLayoutGroup == null)
            {
                return;
            }

            if (!hasThumbnail)
            {
                RestoreSpeechBubblePanelPadding();
                return;
            }

            bool isThumbnailLeft = thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
            _panelMessageLayoutGroup.padding.left = isThumbnailLeft
                ? Mathf.Max(0, textPaddingOnThumbnailSidePx)
                : Mathf.Max(0, textPaddingOnNonThumbnailSidePx);
            _panelMessageLayoutGroup.padding.right = isThumbnailLeft
                ? Mathf.Max(0, textPaddingOnNonThumbnailSidePx)
                : Mathf.Max(0, textPaddingOnThumbnailSidePx);
        }

        /// <summary>
        /// 말풍선 텍스트 패널 패딩을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreSpeechBubblePanelPadding()
        {
            if (_panelMessageLayoutGroup == null || !_hasDefaultPanelMessagePadding)
            {
                return;
            }

            _panelMessageLayoutGroup.padding.left = _defaultPanelMessagePaddingLeft;
            _panelMessageLayoutGroup.padding.right = _defaultPanelMessagePaddingRight;
        }

        /// <summary>
        /// 말꼬리 중심 대칭 규칙을 기반으로 패널 최소 너비를 갱신합니다.
        /// </summary>
        /// <param name="hasThumbnail">썸네일 표시 여부입니다.</param>
        /// <param name="side">썸네일 배치 방향 부호입니다.</param>
        /// <param name="thumbnailOffsetX">썸네일 X 오프셋입니다.</param>
        private void UpdatePanelMinimumWidthByTailSymmetry(bool hasThumbnail, float side, float thumbnailOffsetX)
        {
            if (_panelMessageLayoutElement == null)
            {
                return;
            }

            if (!useSymmetricLayoutByTail || minHalfExtentByTailPx <= 0f)
            {
                if (_hasDefaultPanelMessageMinWidth)
                {
                    _panelMessageLayoutElement.minWidth = _defaultPanelMessageMinWidth;
                }

                return;
            }

            float thumbnailWidth = hasThumbnail && _thumbnailRectTransform != null ? _thumbnailRectTransform.rect.width : 0f;
            float thumbnailSpan = hasThumbnail ? Mathf.Max(0f, thumbnailGapPx) + thumbnailWidth : 0f;
            float requiredPanelWidth = (2f * Mathf.Max(0f, minHalfExtentByTailPx)) - thumbnailSpan - (side * thumbnailOffsetX);
            _panelMessageLayoutElement.minWidth = Mathf.Max(0f, requiredPanelWidth);
        }

        /// <summary>
        /// 말풍선 텍스트 패널의 중심 X를 설정합니다.
        /// </summary>
        /// <param name="panelCenterX">설정할 중심 X 값입니다.</param>
        private void SetPanelMessageCenterX(float panelCenterX)
        {
            if (panelMessage == null)
            {
                return;
            }

            Vector2 anchoredPosition = panelMessage.anchoredPosition;
            anchoredPosition.x = panelCenterX;
            panelMessage.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 화자 방향을 반영한 말꼬리 X 오프셋을 계산합니다.
        /// </summary>
        /// <returns>말꼬리의 목표 anchoredPosition.x 입니다.</returns>
        private float ResolveTailAnchorX()
        {
            if (tailForwardOffsetPx <= 0f)
            {
                return 0f;
            }

            if (TryResolveSpeakerFacingRight(out bool isFacingRight))
            {
                return isFacingRight ? tailForwardOffsetPx : -tailForwardOffsetPx;
            }

            return 0f;
        }

        /// <summary>
        /// 말꼬리 X 위치를 갱신하고 변경 여부를 반환합니다.
        /// </summary>
        /// <returns>말꼬리 X 값이 변경되었으면 <see langword="true"/>를 반환합니다.</returns>
        private bool RefreshTailAnchorPosition()
        {
            if (!TryEnsureTailReferences())
            {
                return false;
            }

            float targetTailX = ResolveTailAnchorX();
            Vector2 anchoredPosition = _tailRectTransform.anchoredPosition;
            if (Mathf.Abs(anchoredPosition.x - targetTailX) <= 0.01f)
            {
                return false;
            }

            anchoredPosition.x = targetTailX;
            _tailRectTransform.anchoredPosition = anchoredPosition;
            return true;
        }

        /// <summary>
        /// 말꼬리 이미지를 화자 방향에 맞게 좌우 반전합니다.
        /// </summary>
        private void ApplyTailFlip()
        {
            if (!TryEnsureTailReferences())
            {
                return;
            }

            if (!_hasTailBaseScale)
            {
                _tailBaseScale = _tailRectTransform.localScale;
                _hasTailBaseScale = true;
            }

            bool isFacingRight = true;
            if (TryResolveSpeakerFacingRight(out bool speakerFacingRight))
            {
                isFacingRight = speakerFacingRight;
            }

            float baseAbsX = Mathf.Abs(_tailBaseScale.x);
            if (baseAbsX <= Mathf.Epsilon)
            {
                baseAbsX = 1f;
            }

            float x = isFacingRight ? baseAbsX : -baseAbsX;
            _tailRectTransform.localScale = new Vector3(
                x,
                _tailBaseScale.y,
                _tailBaseScale.z);
        }

        /// <summary>
        /// 썸네일 이미지에 Flip 정책을 적용합니다.
        /// </summary>
        private void ApplyThumbnailFlip()
        {
            if (_thumbnailRectTransform == null || !HasVisibleThumbnail())
            {
                return;
            }

            if (!_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            bool shouldFlip = ResolveShouldFlipThumbnail();
            float baseAbsX = Mathf.Abs(_thumbnailBaseScale.x);
            if (baseAbsX <= Mathf.Epsilon)
            {
                baseAbsX = 1f;
            }

            float x = shouldFlip ? -baseAbsX : baseAbsX;
            _thumbnailRectTransform.localScale = new Vector3(
                x,
                _thumbnailBaseScale.y,
                _thumbnailBaseScale.z);
        }

        /// <summary>
        /// 썸네일 Flip 정책과 배치 방향을 조합해 좌우 반전 필요 여부를 계산합니다.
        /// </summary>
        /// <returns>좌우 반전이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ResolveShouldFlipThumbnail()
        {
            switch (thumbnailFlipPolicy)
            {
                case DialogueBalloonThumbnailFlipPolicy.KeepOriginal:
                    return false;

                case DialogueBalloonThumbnailFlipPolicy.ForceFlip:
                    return true;

                case DialogueBalloonThumbnailFlipPolicy.AutoBySpeakerFacing:
                    if (TryResolveSpeakerFacingRight(out bool speakerFacingRight))
                    {
                        return ShouldFlipToDesiredFacing(speakerFacingRight);
                    }

                    return ShouldFlipToDesiredFacing(ResolveDesiredFacingRightByThumbnailPosition());

                case DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition:
                default:
                    return ShouldFlipToDesiredFacing(ResolveDesiredFacingRightByThumbnailPosition());
            }
        }

        /// <summary>
        /// 썸네일 배치 기준으로 목표 바라보기 방향을 계산합니다.
        /// </summary>
        /// <returns>오른쪽을 바라봐야 하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ResolveDesiredFacingRightByThumbnailPosition()
        {
            return ResolveEffectiveThumbnailPositionType() == ConfigCommon.ThumbnailPositionType.Left;
        }

        /// <summary>
        /// 원본 썸네일 방향과 목표 방향을 비교해 Flip 필요 여부를 반환합니다.
        /// </summary>
        /// <param name="desiredFacingRight">목표 방향이 오른쪽이면 <see langword="true"/>입니다.</param>
        /// <returns>Flip 이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldFlipToDesiredFacing(bool desiredFacingRight)
        {
            bool sourceFacingRight = thumbnailSourceFacing == DialogueBalloonThumbnailSourceFacing.Right;
            return sourceFacingRight != desiredFacingRight;
        }

        /// <summary>
        /// 화자의 현재 방향에서 수평(좌/우) 방향을 추출합니다.
        /// </summary>
        /// <param name="isFacingRight">오른쪽을 바라보면 <see langword="true"/>를 반환합니다.</param>
        /// <returns>좌우 방향을 판별할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryResolveSpeakerFacingRight(out bool isFacingRight)
        {
            isFacingRight = false;
            if (_currentNpc == null)
            {
                return false;
            }

            CharacterConstants.FacingDirection8 facing = _currentNpc.CurrentFacing;
            switch (facing)
            {
                case CharacterConstants.FacingDirection8.Right:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.DownRight:
                    isFacingRight = true;
                    return true;

                case CharacterConstants.FacingDirection8.Left:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
                    isFacingRight = false;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 썸네일 스케일을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreThumbnailScaleToBase()
        {
            if (_thumbnailRectTransform == null || !_hasThumbnailBaseScale)
            {
                return;
            }

            _thumbnailRectTransform.localScale = _thumbnailBaseScale;
        }

        /// <summary>
        /// 말꼬리 스케일을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreTailScaleToBase()
        {
            if (_tailRectTransform == null || !_hasTailBaseScale)
            {
                return;
            }

            _tailRectTransform.localScale = _tailBaseScale;
        }

        /// <summary>
        /// 말풍선 레이아웃 커스텀 값을 기본 상태로 복원합니다.
        /// </summary>
        private void RestoreSpeechBubbleLayoutDefaults()
        {
            RestoreSpeechBubblePanelPadding();

            if (_panelMessageLayoutElement != null && _hasDefaultPanelMessageMinWidth)
            {
                _panelMessageLayoutElement.minWidth = _defaultPanelMessageMinWidth;
            }

            RestoreThumbnailScaleToBase();
            RestoreTailScaleToBase();

            if (_tailRectTransform != null)
            {
                Vector2 anchoredPosition = _tailRectTransform.anchoredPosition;
                anchoredPosition.x = 0f;
                _tailRectTransform.anchoredPosition = anchoredPosition;
            }
        }

        /// <summary>
        /// 말풍선 모드에서 프레임 단위로 필요한 시각 상태를 갱신합니다.
        /// </summary>
        private void RefreshSpeechBubbleRuntimeVisuals()
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble)
            {
                return;
            }

            bool didTailAnchorChange = false;
            if (!(imageTail == null && _tailRectTransform == null))
            {
                didTailAnchorChange = RefreshTailAnchorPosition();
                ApplyTailFlip();
            }

            ApplyThumbnailFlip();

            if (didTailAnchorChange)
            {
                RefreshThumbnailPosition();
            }
        }

        /// <summary>
        /// NPC 이름 텍스트를 설정합니다.
        /// </summary>
        /// <param name="npcName">표시할 NPC 이름입니다.</param>
        private void SetNpcName(string npcName)
        {
            if (textName == null)
            {
                return;
            }

            textName.text = npcName;
        }

        /// <summary>
        /// 선택지 버튼에 표시할 텍스트를 설정합니다.
        /// </summary>
        /// <param name="button">대상 버튼입니다.</param>
        /// <param name="label">표시할 문구입니다.</param>
        private void SetChoiceButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = label;
            }
        }

        /// <summary>
        /// 커스텀 interaction 표시 이름을 해석합니다.
        /// </summary>
        /// <param name="customTypeKey">커스텀 interaction 키입니다.</param>
        /// <param name="value">interaction 값입니다.</param>
        /// <returns>표시 이름입니다.</returns>
        private string ResolveCustomInteractionDisplayName(string customTypeKey, int value)
        {
            if (InteractionCustomHandlerRegistry.TryGetDisplayName(customTypeKey, value, out string displayName))
            {
                return displayName;
            }

            return customTypeKey;
        }

        /// <summary>
        /// 선택지 버튼 클릭을 처리합니다.
        /// </summary>
        /// <param name="index">클릭한 버튼 인덱스입니다.</param>
        private async void OnClickChoice(int index)
        {
            if (_isExecutingChoice)
            {
                return;
            }

            if (!_interactionData.TryGetValue(index, out InteractionData data))
            {
                return;
            }

            _isExecutingChoice = true;
            try
            {
                _hasAutoStartedCurrentChoiceSet = true;

                switch (data.ChoiceType)
                {
                    case ChoiceType.Quest:
                        await OnClickChoiceQuest(data.NpcQuestData);
                        break;
                    case ChoiceType.Interaction:
                        OnClickChoiceInteraction(data);
                        break;
                    case ChoiceType.Dialogue:
                        await OnClickChoiceDialogue(index);
                        break;
                }
            }
            finally
            {
                _isExecutingChoice = false;
            }
        }

        /// <summary>
        /// dialogue 노드 선택지를 처리합니다.
        /// </summary>
        /// <param name="optionIndex">선택한 dialogue option 인덱스입니다.</param>
        /// <returns>처리 완료를 기다리는 Task입니다.</returns>
        private async Task OnClickChoiceDialogue(int optionIndex)
        {
            if (!_dialogueSession.IsActive)
            {
                return;
            }

            if (_dialogueSession.TrySelectOption(optionIndex))
            {
                int requestVersion = _dialogueLoadVersion;
                await ApplyCurrentDialogueNodeAsync(requestVersion);
                return;
            }

            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
            }
        }

        /// <summary>
        /// 퀘스트 버튼 클릭 처리를 수행합니다.
        /// </summary>
        /// <param name="npcQuestData">선택한 퀘스트 데이터입니다.</param>
        private async Task OnClickChoiceQuest(NpcQuestData npcQuestData)
        {
            try
            {
                CloseDialogueByChoice();
                if (npcQuestData.Status == QuestConstants.Status.Ready)
                {
                    if (await _questManager.StartQuest(npcQuestData.QuestUid, _currentCharacterUid) == false)
                    {
                        return;
                    }
                }
                else if (npcQuestData.Status == QuestConstants.Status.InProgress)
                {
                    DialogEventData data = new DialogEventData(
                        npcUid: _currentCharacterUid);
                    GameEventManager.DialogStart(data);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// interaction 버튼 클릭 처리를 수행합니다.
        /// </summary>
        /// <param name="data">버튼에 연결된 interaction 데이터입니다.</param>
        private void OnClickChoiceInteraction(InteractionData data)
        {
            bool handled = false;

            if (data.HasBuiltInInteraction)
            {
                handled = ExecuteBuiltInInteraction(data.InteractionType, data.Value);
            }
            else if (data.HasCustomInteraction)
            {
                handled = InteractionCustomHandlerRegistry.TryExecute(data.CustomTypeKey, SceneGame, _currentNpc, data.Value);
                if (!handled)
                {
                    GcLogger.LogError($"커스텀 interaction 처리기가 등록되지 않았습니다. key: {data.CustomTypeKey}");
                }
            }

            if (handled)
            {
                CloseDialogueByChoice();
            }
        }

        /// <summary>
        /// 기본 제공 interaction 타입을 실행합니다.
        /// </summary>
        /// <param name="interactionType">실행할 interaction 타입입니다.</param>
        /// <param name="value">보조 값입니다.</param>
        /// <returns>실행 성공 시 true입니다.</returns>
        private bool ExecuteBuiltInInteraction(InteractionConstants.Type interactionType, int value)
        {
            if (interactionType == InteractionConstants.Type.None)
            {
                return false;
            }

            switch (interactionType)
            {
                case InteractionConstants.Type.Shop:
                    _uiWindowShop?.Show(true);
                    _uiWindowShop?.SetInfoByShopUid(value);
                    return true;
                case InteractionConstants.Type.Stash:
                    _uiWindowStash?.Show(true);
                    return true;
                case InteractionConstants.Type.ShopSale:
                    _uiWindowShopSale?.Show(true);
                    return true;
                case InteractionConstants.Type.ItemUpgrade:
                    _uiWindowItemUpgrade?.Show(true);
                    return true;
                case InteractionConstants.Type.ItemSalvage:
                    _uiWindowItemSalvage?.Show(true);
                    return true;
                case InteractionConstants.Type.ItemCraft:
                    _uiWindowItemCraft?.Show(true);
                    _uiWindowItemCraft?.SetInfoByItemCraftUid(value);
                    return true;
                case InteractionConstants.Type.SaveGame:
                    SaveGameBySleep();
                    return true;
                case InteractionConstants.Type.StatReset:
                    return OpenPlayerStatReset();
                case InteractionConstants.Type.WorldMap:
                    _uiWindowWorldMap?.Show(true);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 스탯 초기화 창을 열기 전에 비용 조건을 검사합니다.
        /// </summary>
        /// <returns>창을 열었으면 true입니다.</returns>
        private bool OpenPlayerStatReset()
        {
            if (_playerSettings.statPointResetCost > 0)
            {
                long playerGold = _playerData.CurrentGold;
                if (playerGold < _playerSettings.statPointResetCost)
                {
                    ShowLocalizedInteractionFeedbackMessage("Text_Not_Enough_Gold", _playerSettings.statPointResetCost);
                    return false;
                }
            }

            _uiWindowPlayerStatReset?.Show(true);
            return true;
        }

        /// <summary>
        /// 로컬라이즈 키를 사용해 인터랙션 피드백 메시지를 표시합니다.
        /// GGemCoNpcInteractionSettings 의 대사 연출 정책을 그대로 따르도록 즉시 노출은 사용하지 않습니다.
        /// </summary>
        /// <param name="localizationKey">출력할 로컬라이즈 키입니다.</param>
        /// <param name="arguments">Smart String 치환에 사용할 인자 목록입니다.</param>
        private void ShowLocalizedInteractionFeedbackMessage(string localizationKey, params object[] arguments)
        {
            if (_localizationManager == null || string.IsNullOrWhiteSpace(localizationKey))
            {
                return;
            }

            string message = _localizationManager.GetSmartInteractionByKey(localizationKey, arguments);
            ShowInteractionFeedbackMessage(message);
        }

        /// <summary>
        /// 인터랙션 실행 실패 또는 안내용 피드백 메시지를 표시합니다.
        /// 선택지는 메시지 출력이 끝난 뒤 다시 표시되도록 일반 대사와 동일한 타자 효과 파이프라인을 사용합니다.
        /// </summary>
        /// <param name="message">표시할 피드백 메시지입니다.</param>
        private void ShowInteractionFeedbackMessage(string message)
        {
            ApplyDialogueMessage(message, revealImmediately: false);
        }

        /// <summary>
        /// 선택지 실행으로 대화창이 닫힐 때 세션 상태를 정리합니다.
        /// </summary>
        private void CloseDialogueByChoice()
        {
            _currentNpc = null;
            SceneGame?.InteractionManager?.RemoveCurrentNpc();
            Show(false);
        }

        /// <summary>
        /// 대화 종료 정책 또는 닫기 버튼으로 창을 종료할 때 공통 정리를 수행합니다.
        /// </summary>
        private void CloseInteractionWindow()
        {
            _currentNpc = null;
            SceneGame?.InteractionManager?.RemoveCurrentNpc();
            Show(false);
        }

        /// <summary>
        /// 플레이어가 NPC 범위를 벗어나 인터랙션이 종료될 때 처리합니다.
        /// </summary>
        public void OnEndInteraction()
        {
            _currentNpc = null;
            Show(false);
        }

        /// <summary>
        /// 윈도우 표시 상태가 바뀔 때 대화 세션 관련 UI 상태를 정리합니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        public override void OnShow(bool show)
        {
            base.OnShow(show);

            if (!show)
            {
                HandleDialogueHidden();
            }
        }

        /// <summary>
        /// 대화창이 숨겨질 때 페이지 상태와 선택지 표시를 정리합니다.
        /// </summary>
        private void HandleDialogueHidden()
        {
            _dialogueLoadVersion++;
            _isLoadingDialogue = false;
            ResetPanelDialogue();
            ResetChoiceButtons();
            _defaultChoices.Clear();
            _currentNpcData = null;
            _currentInteractionData = null;
            _currentDialogueSelection = InteractionDialogueSelectionResult.None;
            ClearCurrentDialogueNode();
            _currentTextContext = InteractionDialogueTextContext.Empty;
            _currentQuestDatas.Clear();
            if (imageThumbnail != null)
            {
                imageThumbnail.sprite = null;
                ApplyThumbnailVisibilityAfterBinding();
            }
            _messagePlayer.Clear(textMessage);
            _dialogueSession.Clear();
            _isExecutingChoice = false;
            ClearPendingAutoStartChoice();
            ApplyMessageFontSize(0f);
            _lastKnownVisibleCharacters = -1;
            RestoreSpeechBubbleLayoutDefaults();
        }

        /// <summary>
        /// CharacterTop 모드에서 변경했던 부모를 원래 윈도우로 되돌립니다.
        /// </summary>
        private void ResetPanelDialogue()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            if (positionType == PositionType.CharacterTop && panelDialogue != null)
            {
                panelDialogue.transform.SetParent(transform, false);
            }

            if (panelMessage != null)
            {
                Vector2 anchoredPosition = panelMessage.anchoredPosition;
                anchoredPosition.x = 0f;
                panelMessage.anchoredPosition = anchoredPosition;
            }
        }

        /// <summary>
        /// 잠자기 상호작용을 통해 저장 후 다음 날로 넘깁니다.
        /// </summary>
        private void SaveGameBySleep()
        {
            SceneGame.saveDataManager.SaveData();
            SceneGame.systemMessageManager.ShowMessageInfo("System_Save_Game_By_Sleep");

            int startMapUid = SceneGame.saveDataManager.Player.CurrentMapUid;
            SceneGame.mapManager.LoadMap(startMapUid);
            SceneGame.gameTimeManager.SetNextDay();
        }

        /// <summary>
        /// Addressables 설정에서 NPC 인터랙션 설정을 가져오고,
        /// 없으면 런타임 기본값을 사용합니다.
        /// </summary>
        /// <returns>사용 가능한 NPC 인터랙션 설정입니다.</returns>
        private GGemCoNpcInteractionSettings ResolveNpcInteractionSettings()
        {
            if (AddressableLoaderSettings.Instance != null && AddressableLoaderSettings.Instance.npcInteractionSettings != null)
            {
                _npcInteractionSettings = AddressableLoaderSettings.Instance.npcInteractionSettings;
            }

            if (_npcInteractionSettings == null)
            {
                _npcInteractionSettings = GGemCoNpcInteractionSettings.CreateRuntimeDefault();
            }

            return _npcInteractionSettings;
        }
    }
}
