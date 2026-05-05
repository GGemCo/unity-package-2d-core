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
    /// </summary>
    public class UIWindowInteractionDialogue : UIWindow
    {
        private enum ThumbnailPositionType
        {
            Left,
            Right,
        }

        /// <summary>
        /// 대화창 위치 타입입니다.
        /// </summary>
        private enum PositionType
        {
            None,
            CharacterTop,
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("썸네일 기준 위치")]
        [SerializeField]
        private PositionType positionType;

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
        private readonly InteractionDialogueMessagePlayer _messagePlayer = new();

        private int _currentCharacterUid;
        private CharacterBase _currentNpc;

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

        private enum ChoiceType
        {
            Interaction,
            Quest,
        }

        private struct InteractionData
        {
            public ChoiceType ChoiceType;
            public InteractionConstants.Type InteractionType;
            public string CustomTypeKey;
            public int Value;
            public NpcQuestData NpcQuestData;

            public bool HasBuiltInInteraction => InteractionType != InteractionConstants.Type.None;
            public bool HasCustomInteraction => string.IsNullOrWhiteSpace(CustomTypeKey) == false;
        }

        protected override void Awake()
        {
            _currentCharacterUid = 0;
            uid = UIWindowConstants.WindowUid.InteractionDialogue;
            base.Awake();
            InitializeButtonChoice();
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
        public void SetInfos(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<NpcQuestData> npcQuestDatas,
            GGemCoNpcInteractionSettings npcInteractionSettings = null)
        {
            _currentNpc = npc;
            _npcInteractionSettings = npcInteractionSettings != null ? npcInteractionSettings : ResolveNpcInteractionSettings();
            _currentCharacterUid = npcData.Uid;

            BindNpcThumbnail(npcData);
            SetNpcName(npcData.Name);

            List<NpcQuestData> questList = npcQuestDatas ?? new List<NpcQuestData>();
            ResetChoiceButtons();
            BuildChoiceButtons(questList, interactionData);

            string initialMessage = ResolveInitialMessage(interactionData, questList);
            ApplyDialogueMessage(initialMessage, revealImmediately: false);

            Show(true);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
            RefreshPosition();
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
        }

        /// <summary>
        /// 클릭 또는 터치 입력으로 대화 페이지를 진행합니다.
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
            if (result == InteractionDialogueAdvanceResult.None)
            {
                return;
            }

            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
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

            return _messagePlayer.HasMessage && !_messagePlayer.IsSequenceCompleted;
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
            if (string.IsNullOrEmpty(npcData.ImageThumbnailFileName))
            {
                return;
            }

            string key = ConfigAddressableKey.GetKeyThumbnailNpc(npcData.ImageThumbnailFileName);
            Sprite sprite = _addressableLoaderCharacterThumbnail.GetCharacterThumbnailByName(key);
            if (sprite != null)
            {
                imageThumbnail.sprite = sprite;
            }
        }

        /// <summary>
        /// 인터랙션 메시지와 선택지를 초기화하고 다시 구성합니다.
        /// </summary>
        /// <param name="questList">NPC 퀘스트 목록입니다.</param>
        /// <param name="interactionData">인터랙션 데이터입니다.</param>
        private void BuildChoiceButtons(List<NpcQuestData> questList, StruckTableInteraction interactionData)
        {
            int index = 0;
            if (questList != null)
            {
                foreach (NpcQuestData npcQuestData in questList)
                {
                    SetupChoiceButtonQuest(index++, npcQuestData);
                }
            }

            if (interactionData == null)
            {
                return;
            }

            index += SetupChoiceButton(index, interactionData.Type1, interactionData.Value1, interactionData.CustomTypeKey1) ? 1 : 0;
            index += SetupChoiceButton(index, interactionData.Type2, interactionData.Value2, interactionData.CustomTypeKey2) ? 1 : 0;
            index += SetupChoiceButton(index, interactionData.Type3, interactionData.Value3, interactionData.CustomTypeKey3) ? 1 : 0;
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
                return _localizationManager.GetInteractionByKey(interactionData.Message);
            }

            if (questList != null && questList.Count > 0)
            {
                return messageQuestSelect;
            }

            return string.Empty;
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

            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
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
            if (!_currentNpc || panelDialogue == null)
            {
                return;
            }

            panelDialogue.transform.SetParent(SceneGame.containerDialogueBalloon.transform, false);
            Vector3 worldPosition = _currentNpc.transform.position + new Vector3(0, _currentNpc.GetHeightByScale(), 0) + offsetPanelDialogue;
            panelDialogue.transform.position = worldPosition;
        }

        /// <summary>
        /// 썸네일 크기를 대화 내용에 맞게 정리합니다.
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            if (!panelMessage)
            {
                return;
            }

            if (!imageThumbnail || !imageThumbnail.gameObject.TryGetComponent(out RectTransform thumbnailRectTransform))
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);

            float panelHalfWidth = panelMessage.rect.width * 0.5f;
            float thumbnailHalfWidth = thumbnailRectTransform.rect.width * 0.5f;
            float side = 1f;

            float x = side * (panelHalfWidth + thumbnailHalfWidth) + offsetImageThumbnailCharacter.x;
            float y = offsetImageThumbnailCharacter.y;
            imageThumbnail.transform.localPosition = new Vector3(x, y, 0f);
        }

        /// <summary>
        /// NPC 이름 텍스트를 설정합니다.
        /// </summary>
        /// <param name="npcName">표시할 NPC 이름입니다.</param>
        private void SetNpcName(string npcName)
        {
            if (!textName)
            {
                return;
            }

            textName.text = npcName;
        }

        /// <summary>
        /// 퀘스트 선택 버튼 데이터를 구성합니다.
        /// </summary>
        /// <param name="index">버튼 인덱스입니다.</param>
        /// <param name="npcQuestData">퀘스트 데이터입니다.</param>
        private void SetupChoiceButtonQuest(int index, NpcQuestData npcQuestData)
        {
            if (index < 0 || index >= ButtonCount)
            {
                return;
            }

            Button button = _buttonChoices.GetValueOrDefault(index);
            if (button == null)
            {
                return;
            }

            _interactionData[index] = new InteractionData
            {
                ChoiceType = ChoiceType.Quest,
                NpcQuestData = npcQuestData,
            };

            StruckTableQuest info = _tableQuest.GetDataByUid(npcQuestData.QuestUid);
            SetChoiceButtonLabel(button, info != null ? info.Name : string.Empty);
        }

        /// <summary>
        /// 일반 interaction 버튼 데이터를 구성합니다.
        /// </summary>
        /// <param name="index">버튼 인덱스입니다.</param>
        /// <param name="interactionType">기본 interaction 타입입니다.</param>
        /// <param name="value">interaction 보조 값입니다.</param>
        /// <param name="customTypeKey">커스텀 interaction 키입니다.</param>
        /// <returns>버튼 데이터가 실제로 추가되었으면 true입니다.</returns>
        private bool SetupChoiceButton(int index, InteractionConstants.Type interactionType, int value, string customTypeKey)
        {
            if (index < 0 || index >= ButtonCount)
            {
                return false;
            }

            bool hasBuiltIn = interactionType != InteractionConstants.Type.None;
            bool hasCustom = string.IsNullOrWhiteSpace(customTypeKey) == false;
            if (!hasBuiltIn && !hasCustom)
            {
                return false;
            }

            Button button = _buttonChoices.GetValueOrDefault(index);
            if (button == null)
            {
                return false;
            }

            _interactionData[index] = new InteractionData
            {
                ChoiceType = ChoiceType.Interaction,
                InteractionType = interactionType,
                CustomTypeKey = hasBuiltIn ? string.Empty : customTypeKey,
                Value = value,
            };

            string buttonLabel = hasBuiltIn
                ? InteractionConstants.GetTypeName(interactionType)
                : ResolveCustomInteractionDisplayName(customTypeKey, value);
            SetChoiceButtonLabel(button, buttonLabel);
            return true;
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
            if (!_interactionData.TryGetValue(index, out InteractionData data))
            {
                return;
            }

            if (data.ChoiceType == ChoiceType.Quest)
            {
                await OnClickChoiceQuest(data.NpcQuestData);
            }
            else if (data.ChoiceType == ChoiceType.Interaction)
            {
                OnClickChoiceInteraction(data);
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
                    ApplyDialogueMessage(_localizationManager.GetSmartInteractionByKey("Text_Not_Enough_Gold"), revealImmediately: true);
                    return false;
                }
            }

            _uiWindowPlayerStatReset?.Show(true);
            return true;
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
            ResetPanelDialogue();
            ResetChoiceButtons();
            _messagePlayer.Clear(textMessage);
        }

        /// <summary>
        /// CharacterTop 모드에서 변경했던 부모를 원래 윈도우로 되돌립니다.
        /// </summary>
        private void ResetPanelDialogue()
        {
            if (positionType == PositionType.CharacterTop && panelDialogue != null)
            {
                panelDialogue.transform.SetParent(transform, false);
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
