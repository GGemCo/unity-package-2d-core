using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 게임 씬 관리 클래스
    /// </summary>
    public class SceneGame : DefaultScene
    {
        public static SceneGame Instance { get; private set; }

        public enum GameState { Begin, Combat, End, DirectionStart, DirectionEnd, QuestDialogueStart, QuestDialogueEnd }
        public enum GameSubState { Normal, BossChallenge, DialogueStart, DialogueEnd }

        private GameState State { get; set; }
        private GameSubState SubState { get; set; }
        private bool isStateDirty;

        [HideInInspector] public GameObject player;
        [Header("기본오브젝트")]
        [Tooltip("메인으로 사용되는 Camera")]
        public Camera mainCamera;
        [Tooltip("UI 에 사용되는 메인 Canvas")]
        public Canvas canvasUI;
        [Tooltip("드랍 아이템의 이름 text, Npc 이름 text, Npc 퀘스트 마크 오브젝트가 들어갈 오브젝트 입니다.")]
        public GameObject containerDropItemName;
        [Tooltip("워프로 맵 이동시 화면을 가려줄 검정화면")]
        public GameObject bgBlackForMapLoading;
        [Tooltip("몬스터 Hp Bar 오브젝트가 들어갈 오브젝트 입니다.")]
        public GameObject containerMonsterHpBar;
        [Tooltip("연출 말풍선이 들어갈 오브젝트 입니다.")]
        public GameObject containerDialogueBalloon;
        
        [Header("매니저")]
        [Tooltip("윈도우 매니저")]
        public UIWindowManager uIWindowManager;
        [Tooltip("시스템 메시지 매니저")]
        public SystemMessageManager systemMessageManager;
        [Tooltip("카메라 매니저")]
        public CameraManager cameraManager;
        [Tooltip("팝업 매니저")]
        public PopupManager popupManager;

        [HideInInspector] public SaveDataManager saveDataManager;
        [HideInInspector] public CalculateManager calculateManager;
        [HideInInspector] public MapManager mapManager;
        [HideInInspector] public DamageTextManager damageTextManager;
        [HideInInspector] public UIIconCoolTimeManager uIIconCoolTimeManager;
        public ItemManager ItemManager;
        public CharacterManager CharacterManager;
        public KeyboardManager KeyboardManager;
        public InteractionManager InteractionManager;
        public CutsceneManager CutsceneManager;
        public QuestManager QuestManager;
        
        private UIWindowInventory uiWindowInventory;

        private void Awake()
        {
            if (TableLoaderManager.Instance == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Intro");
                return;
            }

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeManagers();

            isStateDirty = false;
            SetState(GameState.Begin);
        }
        /// <summary>
        /// 매니저 스크립트 오브젝트 생성하기
        /// </summary>
        private void InitializeManagers()
        {
            GameObject managerContainer = new GameObject("Managers");

            calculateManager = CreateManager<CalculateManager>(managerContainer);
            mapManager = CreateManager<MapManager>(managerContainer);
            saveDataManager = CreateManager<SaveDataManager>(managerContainer);
            damageTextManager = CreateManager<DamageTextManager>(managerContainer);
            uIIconCoolTimeManager = CreateManager<UIIconCoolTimeManager>(managerContainer);
            
            ItemManager = new ItemManager();
            ItemManager.Initialize(this);
            CharacterManager = new CharacterManager();
            CharacterManager.Initialize(TableLoaderManager.Instance.TableNpc, TableLoaderManager.Instance.TableMonster,
                TableLoaderManager.Instance.TableAnimation);
            KeyboardManager = new KeyboardManager();
            KeyboardManager.Initialize(this);
            InteractionManager = new InteractionManager();
            InteractionManager.Initialize(this);
            CutsceneManager = new CutsceneManager();
            CutsceneManager.Initialize(this);
            QuestManager = new QuestManager();
            QuestManager.Initialize(this);
        }

        private T CreateManager<T>(GameObject parent) where T : Component
        {
            GameObject obj = new GameObject(typeof(T).Name);
            obj.transform.SetParent(parent.transform);
            return obj.AddComponent<T>();
        }

        private void Start()
        {
            if (TableLoaderManager.Instance == null) return;
            
            uiWindowInventory = uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid
                .Inventory);
            
            StartCoroutine(UpdateStateRoutine());
        }
        private IEnumerator UpdateStateRoutine()
        {
            while (true)
            {
                if (isStateDirty)
                {
                    OnStateChanged();
                    isStateDirty = false;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnStateChanged()
        {
            switch (State)
            {
                case GameState.QuestDialogueStart:
                case GameState.QuestDialogueEnd:
                case GameState.Begin:
                case GameState.Combat:
                case GameState.DirectionStart:
                case GameState.DirectionEnd:
                default:
                    break;
                case GameState.End:
                    PopupMetadata popupMetadata = new PopupMetadata
                    {
                        PopupType = PopupManager.Type.Default,
                        Title = "게임 종료",
                        Message = "플레이어가 사망하였습니다.\n마을로 이동합니다.",
                        MessageColor = Color.red,
                        ShowCancelButton = false,
                        OnConfirm = OnDeadPlayer,
                        IsClosableByClick = false
                    };
                    popupManager.ShowPopup(popupMetadata);
                    break;
            }
        }
        /// <summary>
        /// 플레이어가 죽었을 때 처리 
        /// </summary>
        private void OnDeadPlayer()
        {
            mapManager.LoadMapByPlayerDead();
        }

        public void SetState(GameState newState)
        {
            State = newState;
            isStateDirty = true;
        }

        public void SetSubState(GameSubState newSubState)
        {
            SubState = newSubState;
            isStateDirty = true;
        }

        public bool IsSubStateDialogueStart => SubState == GameSubState.DialogueStart;
        public bool IsStateDirectionStart => State == GameState.DirectionStart;

        private void Update()
        {
            if (KeyboardManager != null)
            {
                KeyboardManager.Update();
            }

            if (CutsceneManager != null)
            {
                CutsceneManager.Update();
            }
        }
        /// <summary>
        /// 아이템 구매하기
        /// </summary>
        /// <param name="itemUid"></param>
        /// <param name="currencyType"></param>
        /// <param name="price"></param>
        /// <param name="itemCount"></param>
        public void BuyItem(int itemUid, CurrencyConstants.Type currencyType, int price, int itemCount = 1)
        {
            int totalPrice = price * itemCount;
            // 가지고 있는 재화가 충분하지 체크
            var checkNeedCurrency = saveDataManager.Player.CheckNeedCurrency(currencyType, totalPrice);
            if (checkNeedCurrency.Code == ResultCommon.Type.Fail)
            {
                systemMessageManager.ShowMessageWarning(checkNeedCurrency.Message);
                return;
            }
            // 재화 빼주기
            var minusCurrency = saveDataManager.Player.MinusCurrency(currencyType, totalPrice);
            if (minusCurrency.Code == ResultCommon.Type.Fail)
            {
                systemMessageManager.ShowMessageWarning(minusCurrency.Message);
                return;
            }
            // 인벤토리에 아이템 넣을 수 있는지 체크
            var addItem = saveDataManager.Inventory.AddItem(itemUid, itemCount);
            if (addItem.Code == ResultCommon.Type.Fail)
            {
                systemMessageManager.ShowMessageWarning(addItem.Message);
                return;
            }
            if (uiWindowInventory != null)
            {
                uiWindowInventory.SetIcons(addItem);
            }
        }

        private void OnDestroy()
        {
            ItemManager?.OnDestroy();
            CharacterManager?.OnDestroy();
            KeyboardManager?.OnDestroy();
            InteractionManager?.OnDestroy();
            CutsceneManager?.OnDestroy();
            GameEventManager.OnDestroy();
        }
    }
}
