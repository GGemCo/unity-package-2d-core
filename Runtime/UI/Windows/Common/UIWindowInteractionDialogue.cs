using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowInteractionDialogue : UIWindow
    {
        private enum ThumbnailPositionType { Left, Right }
        /// <summary>
        /// 대화창 위치
        /// </summary>
        private enum PositionType { None, CharacterTop }
        
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("썸네일 기준 위치")]
        [SerializeField] private PositionType positionType;
        [Tooltip("텍스트 박스, 썸네일, 박스 꼬리가 들어가있는 오브젝트")]
        [SerializeField] private GameObject panelDialogue;
        [Tooltip("말풍선 위치")]
        [SerializeField] private Vector3 offsetPanelDialogue;
        
        [Tooltip("캐릭터 썸네일")] 
        [SerializeField] private Image imageThumbnail;
        [Tooltip("캐릭터 이름")] 
        [SerializeField] private TextMeshProUGUI textName;
        [Tooltip("메시지")] 
        [SerializeField] private TextMeshProUGUI textMessage;
        [Tooltip("메시지가 들어가는 Panel")]
        [SerializeField] private  RectTransform panelMessage;
        [Tooltip("캐릭터 썸네일 이미지 위치. 오른쪽 기준")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacter;
        [Tooltip("캐릭터 썸네일 이미지 위치. 왼쪽 기준")]
        [SerializeField] private Vector3 offsetImageThumbnailCharacterLeft;
        
        [Header("선택지 버튼")]
        [Tooltip("선택지 버튼 프리팹")] 
        [SerializeField] private GameObject prefabButtonChoice;
        [Tooltip("선택지 버튼이 들어갈 Panel")] 
        [SerializeField] private Transform containerButton;
        [Tooltip("퀘스트 선택 요청 메시지")] 
        [SerializeField] private string messageQuestSelect;

        private const int ButtonCount = 10;
        private readonly Dictionary<int, Button> _buttonChoices = new();
        private int _currentCharacterUid;
        private CharacterBase _currentNpc;

        private readonly Dictionary<int, InteractionData> _interactionData = new();

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
        private PlayerData _playerData;
        private PopupManager _popupManager;

        private enum ChoiceType { Interaction, Quest }

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
            _playerData = SceneGame.saveDataManager.Player;
            _popupManager = SceneGame.popupManager;
        }

        /// <summary>
        /// interaction 버튼 초기화
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
                if (button != null)
                {
                    int capturedIndex = i;  // Closure 캡처 주의
                    button.onClick.AddListener(() => OnClickChoice(capturedIndex));
                    _buttonChoices[i] = button;
                    button.gameObject.SetActive(false);
                }
            }
        }
        /// <summary>
        /// interaction 정보 셋티
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="npcData"></param>
        /// <param name="interactionData"></param>
        /// <param name="npcQuestDatas"></param>
        public void SetInfos(CharacterBase npc, StruckTableNpc npcData, StruckTableInteraction interactionData, List<NpcQuestData> npcQuestDatas)
        {
            _currentNpc = npc;

            if (!string.IsNullOrEmpty(npcData.ImageThumbnailFileName))
            {
                string key = ConfigAddressableKey.GetKeyThumbnailNpc(npcData.ImageThumbnailFileName);
                Sprite sprite = _addressableLoaderCharacterThumbnail.GetCharacterThumbnailByName(key);
                if (sprite != null)
                {
                    imageThumbnail.sprite = sprite;
                }
            }

            SetNpcName(npcData.Name);
            
            _currentCharacterUid = npcData.Uid;

            var questList = npcQuestDatas ?? new List<NpcQuestData>();

            if (textMessage)
            {
                if (interactionData != null && !string.IsNullOrEmpty(interactionData.Message))
                {
                    textMessage.text = _localizationManager.GetInteractionByKey(interactionData.Message);
                }
                else if (questList.Count > 0)
                {
                    textMessage.text = messageQuestSelect;
                }
                else
                {
                    textMessage.text = string.Empty;
                }
            }

            for (int i = 0; i < ButtonCount; i++)
            {
                if (!_buttonChoices.TryGetValue(i, out var button)) continue;
                button.gameObject.SetActive(false);
            }

            _interactionData.Clear();
            int index = 0;

            if (questList.Count > 0)
            {
                foreach (var npcQuestData in questList)
                {
                    SetupChoiceButtonQuest(index++, npcQuestData);
                }
            }

            if (interactionData != null)
            {
                index += SetupChoiceButton(index, interactionData.Type1, interactionData.Value1, interactionData.CustomTypeKey1) ? 1 : 0;
                index += SetupChoiceButton(index, interactionData.Type2, interactionData.Value2, interactionData.CustomTypeKey2) ? 1 : 0;
                index += SetupChoiceButton(index, interactionData.Type3, interactionData.Value3, interactionData.CustomTypeKey3) ? 1 : 0;
            }

            Show(true);
            RefreshThumbnailPosition();
            RefreshPosition();
        }

        /// <summary>
        /// PositionType 별 위치 조정
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
        /// NPC 머리 위에 배치
        /// </summary>
        private void RefreshPositionCharacterTop()
        {
            if (!_currentNpc) return;
            panelDialogue.transform.SetParent(SceneGame.containerDialogueBalloon.transform, false);
            var worldPosition = _currentNpc.transform.position + new Vector3(0, _currentNpc.GetHeightByScale(), 0) + offsetPanelDialogue;
            panelDialogue.transform.position = worldPosition;
        }

        /// <summary>
        /// 썸네일 크기를 대화 내용에 맞게 정리
        /// </summary>
        private void RefreshThumbnailPosition()
        {
            if (!panelMessage) return;
            if (!imageThumbnail ||
                !imageThumbnail.gameObject.TryGetComponent<RectTransform>(out var thumbnailRectTransform)) return;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);
            
            var panelHalfWidth = panelMessage.rect.width * 0.5f;
            var thumbnailHalfWidth = thumbnailRectTransform.rect.width * 0.5f;
            // todo. 정리 필요
            // var side = _currentEntityPlayerInfo.thumbnailPositionType == ThumbnailPositionType.Left ? -1f : 1f;
            var side = 1f;

            var x = side * (panelHalfWidth + thumbnailHalfWidth) + offsetImageThumbnailCharacter.x;
            // if (_currentEntityPlayerInfo.thumbnailPositionType == ThumbnailPositionType.Left)
            //     x = side * (panelHalfWidth + thumbnailHalfWidth) + offsetImageThumbnailCharacterLeft.x;
                
            var y = offsetImageThumbnailCharacter.y;
            imageThumbnail.transform.localPosition = new Vector3(x, y, 0);
        }

        private void SetNpcName(string npcName)
        {
            if (!textName) return;
            textName.text = npcName;
        }

        /// <summary>
        /// 퀘스트 버튼 셋팅
        /// </summary>
        /// <param name="index"></param>
        /// <param name="npcQuestData"></param>
        private void SetupChoiceButtonQuest(int index, NpcQuestData npcQuestData)
        {
            if (index < 0 || index >= ButtonCount) return;

            var button = _buttonChoices.GetValueOrDefault(index);
            if (button == null) return;

            button.gameObject.SetActive(true);

            _interactionData[index] = new InteractionData
            {
                ChoiceType = ChoiceType.Quest,
                NpcQuestData = npcQuestData
            };

            var info = _tableQuest.GetDataByUid(npcQuestData.QuestUid);
            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"{info.Name}";
            }
        }
        /// <summary>
        /// interaction 버튼 셋팅
        /// </summary>
        /// <param name="index"></param>
        /// <param name="interactionType"></param>
        /// <param name="value"></param>
        /// <param name="customTypeKey"></param>
        private bool SetupChoiceButton(int index, InteractionConstants.Type interactionType, int value, string customTypeKey)
        {
            if (index < 0 || index >= ButtonCount) return false;

            bool hasBuiltIn = interactionType != InteractionConstants.Type.None;
            bool hasCustom = string.IsNullOrWhiteSpace(customTypeKey) == false;
            if (!hasBuiltIn && !hasCustom) return false;

            var button = _buttonChoices.GetValueOrDefault(index);
            if (button == null) return false;

            button.gameObject.SetActive(true);

            _interactionData[index] = new InteractionData
            {
                ChoiceType = ChoiceType.Interaction,
                InteractionType = interactionType,
                CustomTypeKey = hasBuiltIn ? string.Empty : customTypeKey,
                Value = value
            };

            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = hasBuiltIn
                    ? InteractionConstants.GetTypeName(interactionType)
                    : ResolveCustomInteractionDisplayName(customTypeKey, value);
            }

            return true;
        }

        private string ResolveCustomInteractionDisplayName(string customTypeKey, int value)
        {
            if (InteractionCustomHandlerRegistry.TryGetDisplayName(customTypeKey, value, out var displayName))
            {
                return displayName;
            }

            return customTypeKey;
        }

        private async void OnClickChoice(int index)
        {
            if (!_interactionData.TryGetValue(index, out var data)) return;

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
        /// 퀘스트 버튼 클릭 처리 
        /// </summary>
        /// <param name="npcQuestData"></param>
        private async Task OnClickChoiceQuest(NpcQuestData npcQuestData)
        {
            try
            {
                Show(false);
                if (npcQuestData.Status == QuestConstants.Status.Ready)
                {
                    if (await _questManager.StartQuest(npcQuestData.QuestUid, _currentCharacterUid) == false) return;
                }
                else if (npcQuestData.Status == QuestConstants.Status.InProgress)
                {
                    var data = new DialogEventData(
                        npcUid: _currentCharacterUid
                    );
                    GameEventManager.DialogStart(data);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// interaction 버튼 처리 
        /// </summary>
        /// <param name="data"></param>
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
                Show(false);
            }
        }

        private bool ExecuteBuiltInInteraction(InteractionConstants.Type interactionType, int value)
        {
            if (interactionType == InteractionConstants.Type.None) return false;

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
                    return OpenPlayerStatReset();;
                case InteractionConstants.Type.WorldMap:
                    _uiWindowWorldMap?.Show(true);
                    return true;
                default:
                    return false;
            }
        }

        private bool OpenPlayerStatReset()
        {
            // 초기화 비용 체크
            if (_playerSettings.statPointResetCost > 0)
            {
                var playerGold = _playerData.CurrentGold;
                if (playerGold < _playerSettings.statPointResetCost)
                {
                    if (textMessage)
                    {
                        textMessage.text = _localizationManager.GetSmartInteractionByKey("Text_Not_Enough_Gold");
                        RefreshThumbnailPosition();
                    }
                    return false;
                }
            }
            
            _uiWindowPlayerStatReset?.Show(true);
            return true;
        }

        /// <summary>
        /// 플레이어가 npc 에서 멀어져서 interaction 이 끝났을때 처리 
        /// </summary>
        public void OnEndInteraction()
        {
            _currentNpc = null;
            ResetPanelDialogue();
            Show(false);
        }

        private void ResetPanelDialogue()
        {
            // 캐릭터 머리위에 표시할 때, 다시 이 윈도우로 되돌려야 함
            if (positionType == PositionType.CharacterTop)
            {
                panelDialogue.transform.SetParent(transform, false);
            }
        }

        private void SaveGameBySleep()
        {
            SceneGame.saveDataManager.SaveData();
            SceneGame.systemMessageManager.ShowMessageInfo("System_Save_Game_By_Sleep");
            // 맵 새로고침. 페이드 인 아웃을 잠자는 연출로 사용
            int startMapUid = SceneGame.saveDataManager.Player.CurrentMapUid;
            SceneGame.mapManager.LoadMap(startMapUid);
            SceneGame.gameTimeManager.SetNextDay();
        }
    }
}
