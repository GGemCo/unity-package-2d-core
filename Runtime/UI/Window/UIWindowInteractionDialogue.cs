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
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("캐릭터 썸네일")]
        public Image imageThumbnail;
        [Tooltip("캐릭터 이름")]
        public TextMeshProUGUI textName;
        [Tooltip("메시지")]
        public TextMeshProUGUI textMessage;
        [Tooltip("선택지 버튼 프리팹")]
        public GameObject prefabButtonChoice;
        [Tooltip("선택지 버튼이 들어갈 Panel")]
        public Transform containerButton;
        [Tooltip("퀘스트 선택 요청 메시지")]
        public string messageQuestSelect;

        // 최대 interaction 버튼 개수
        private const int ButtonCount = 10;
        private readonly Dictionary<int, Button> _buttonChoices = new Dictionary<int, Button>();
        private int _currentCharacterUid;
        
        private UIWindowShop _uiWindowShop;
        private UIWindowShopSale _uiWindowShopSale;
        private UIWindowStash _uiWindowStash;
        private UIWindowItemUpgrade _uiWindowItemUpgrade;
        private UIWindowItemSalvage _uiWindowItemSalvage;
        private UIWindowItemCraft _uiWindowItemCraft;
        
        private TableQuest _tableQuest;
        private QuestManager _questManager;
        
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
            _uiWindowShop =
                SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowShop>(UIWindowConstants.WindowUid.Shop);
            _uiWindowStash =
                SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowStash>(UIWindowConstants.WindowUid.Stash);
            _uiWindowShopSale =
                SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowShopSale>(UIWindowConstants.WindowUid.ShopSale);
            _uiWindowItemUpgrade =
                SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowItemUpgrade>(UIWindowConstants.WindowUid.ItemUpgrade);
            _uiWindowItemSalvage =
                SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowItemSalvage>(UIWindowConstants.WindowUid.ItemSalvage);
            _uiWindowItemCraft =
                SceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowItemCraft>(UIWindowConstants.WindowUid.ItemCraft);
            _tableQuest = TableLoaderManager.Instance.TableQuest;
            _questManager = SceneGame.Instance.QuestManager;
        }

        /// <summary>
        /// interaction 버튼 초기화
        /// </summary>
        private void InitializeButtonChoice()
        {
            if (prefabButtonChoice == null)
            {
                GcLogger.LogError("선택 버튼 프리팹이 없습니다.");
                return;
            }
            if (containerButton == null)
            {
                GcLogger.LogError("선택 버튼 container 가 없습니다.");
                return;
            }
            _buttonChoices.Clear();

            for (int i = 0; i < ButtonCount; i++)
            {
                GameObject buttonObj = Instantiate(prefabButtonChoice, containerButton);
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    _buttonChoices.TryAdd(i, button);
                    button.gameObject.SetActive(false); // 초기 상태 비활성화
                }
            }
        }
        /// <summary>
        /// interaction 정보 셋티
        /// </summary>
        /// <param name="npcData"></param>
        /// <param name="interactionData"></param>
        /// <param name="npcQuestDatas"></param>
        public async Task SetInfos(StruckTableNpc npcData, StruckTableInteraction interactionData, List<NpcQuestData> npcQuestDatas)
        {
            string key = $"{ConfigAddressables.KeyCharacterThumbnailNpc}_{npcData.ImageThumbnailFileName}";
            Sprite sprite = await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
            if (sprite != null)
            {
                imageThumbnail.sprite = sprite;
            }

            textName.text = npcData.Name;
            _currentCharacterUid = npcData.Uid;
            
            if (interactionData != null)
            {
                textMessage.text = interactionData.Message;
            }
            // 퀘스트가 있을 경우 퀘스트 선택 메시지
            else if (npcQuestDatas.Count > 0)
            {
                textMessage.text = messageQuestSelect;
            }

            for (int i = 0; i < ButtonCount; i++)
            {
                Button button = _buttonChoices.GetValueOrDefault(i);
                if (button == null) continue;
                button.gameObject.SetActive(false);
            }

            int index = 0;
            if (npcQuestDatas.Count > 0)
            {
                foreach (var npcQuestData in npcQuestDatas)
                {
                    SetupChoiceButtonQuest(index++, npcQuestData);
                }
            }

            if (interactionData != null)
            {
                SetupChoiceButton(index++, interactionData.Type1, interactionData.Value1);
                SetupChoiceButton(index++, interactionData.Type2, interactionData.Value2);
                SetupChoiceButton(index, interactionData.Type3, interactionData.Value3);
            }
        }
        /// <summary>
        /// 퀘스트 버튼 셋팅
        /// </summary>
        /// <param name="index"></param>
        /// <param name="npcQuestData"></param>
        private void SetupChoiceButtonQuest(int index, NpcQuestData npcQuestData)
        {
            if (index < 0 || index >= ButtonCount)
                return;
            Button button = _buttonChoices.GetValueOrDefault(index);
            if (button == null) return;
            button.gameObject.SetActive(true);
            
            // 중복 호출을 막기 위해 기존에 연결된 리스너를 모두 지워준다.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClickChoiceQuest(npcQuestData));
            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                var info = _tableQuest.GetDataByUid(npcQuestData.QuestUid);
                textComponent.text = $"{info.Name}";
            }
        }
        /// <summary>
        /// 퀘스트 버튼 클릭 처리 
        /// </summary>
        /// <param name="npcQuestData"></param>
        private async void OnClickChoiceQuest(NpcQuestData npcQuestData)
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
                    GameEventManager.DialogStart(_currentCharacterUid);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// interaction 버튼 셋팅
        /// </summary>
        /// <param name="index"></param>
        /// <param name="interactionType"></param>
        /// <param name="value"></param>
        private void SetupChoiceButton(int index, InteractionConstants.Type interactionType, int value)
        {
            if (index < 0 || index >= ButtonCount)
                return;

            Button button = _buttonChoices.GetValueOrDefault(index);
            if (button == null) return;
            bool isActive = interactionType != InteractionConstants.Type.None;
            button.gameObject.SetActive(isActive);

            if (!isActive) return;
            // 중복 호출을 막기 위해 기존에 연결된 리스너를 모두 지워준다.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClickChoice(interactionType, value));
            
            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = InteractionConstants.GetTypeName(interactionType);
            }
        }
        /// <summary>
        /// interaction 버튼 처리 
        /// </summary>
        /// <param name="interactionType"></param>
        /// <param name="value"></param>
        private void OnClickChoice(InteractionConstants.Type interactionType, int value)
        {
            if (interactionType == InteractionConstants.Type.None) return;
            if (interactionType == InteractionConstants.Type.Shop)
            {
                _uiWindowShop.Show(true);
                _uiWindowShop.SetInfoByShopUid(value);
            }
            else if (interactionType == InteractionConstants.Type.Stash)
            {
                _uiWindowStash?.Show(true);
            }
            else if (interactionType == InteractionConstants.Type.ShopSale)
            {
                _uiWindowShopSale?.Show(true);
            }
            else if (interactionType == InteractionConstants.Type.ItemUpgrade)
            {
                _uiWindowItemUpgrade?.Show(true);
            }
            else if (interactionType == InteractionConstants.Type.ItemSalvage)
            {
                _uiWindowItemSalvage?.Show(true);
            }
            else if (interactionType == InteractionConstants.Type.ItemCraft)
            {
                _uiWindowItemCraft?.Show(true);
            }

            Show(false);
        }
        /// <summary>
        /// 플레이어가 npc 에서 멀어져서 interaction 이 끝났을때 처리 
        /// </summary>
        public void OnEndInteraction()
        {
            Show(false);
        }
    }
}