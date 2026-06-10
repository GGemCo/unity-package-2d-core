using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 인터랙션 대화 UI를 관리합니다.
    /// dialogue graph 재생과 기본 interaction 선택지 표시를 하나의 창에서 순차적으로 처리합니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue : UIWindow
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

        [Header("말풍선 월드 위치")]
        [Tooltip("SpeechBubble 모드에서 캐릭터 머리 위 기본 위치에 추가로 더할 월드 오프셋")]
        [SerializeField]
        private Vector3 speechBubbleWorldOffset = Vector3.zero;

        [Tooltip("SpeechBubble 월드 오프셋 X값을 화자 방향에 따라 보정하는 정책")]
        [SerializeField]
        private DialogueBalloonWorldOffsetXPolicy speechBubbleWorldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.KeepOriginal;

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

        [Header("말풍선 입력 안내 이미지")]
        [Tooltip("SpeechBubble 모드에서 프로젝트 공통 입력 안내 이미지 기본값을 사용할지 여부")]
        [SerializeField]
        private bool useProjectEnterIndicatorDefaultsInSpeechBubble;

        [Tooltip("입력 안내 이미지 컴포넌트입니다. 비어 있으면 이름(ImageEnter) 기준 자동 탐색을 시도합니다.")]
        [SerializeField]
        private Image imageEnter;

        [Tooltip("프로젝트 기본값 미사용 시 적용할 입력 안내 이미지입니다. 비어 있으면 프리팹 기존 이미지를 유지합니다.")]
        [SerializeField]
        private Sprite enterIndicatorSpriteOverride;

        [Tooltip("프로젝트 기본값 미사용 시 적용할 대사 마지막 글자와 입력 안내 이미지 사이 간격(px)")]
        [SerializeField]
        private float enterIndicatorGapPx = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorGapPx;

        [Tooltip("프로젝트 기본값 미사용 시 적용할 입력 안내 이미지 깜빡임 속도(Hz)")]
        [SerializeField]
        private float enterIndicatorBlinkHz = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorBlinkHz;

        [Range(0f, 1f)]
        [Tooltip("프로젝트 기본값 미사용 시 적용할 입력 안내 이미지 최소 알파값")]
        [SerializeField]
        private float enterIndicatorMinAlpha = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorMinAlpha;

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
        private CanvasGroup _panelDialogueCanvasGroup;
        private bool _isInitialRevealPending;
        private int _initialRevealRequestVersion = -1;
        private RectTransform _enterRectTransform;
        private bool _hasEnterBaseColor;
        private Color _enterBaseColor = Color.white;
        private bool _hasEnterBaseAnchoredPosition;
        private Vector2 _enterBaseAnchoredPosition;
        private float _resolvedEnterIndicatorGapPx = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorGapPx;
        private float _resolvedEnterIndicatorBlinkHz = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorBlinkHz;
        private float _resolvedEnterIndicatorMinAlpha = GGemCoDialogueBalloonSettings.DefaultEnterIndicatorMinAlpha;
        private int _lastKnownEnterIndicatorVisibleCharacters = -1;

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
        private GGemCoPlayerStatSettings _playerStatSettings;
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
            CacheInitialRevealCanvasGroupReference();
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
            _playerStatSettings = AddressableLoaderSettings.Instance.playerStatSettings;
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
    }
}
