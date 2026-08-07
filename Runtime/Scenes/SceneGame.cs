using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 씬 관리 클래스
    /// </summary>
    public class SceneGame : DefaultScene, IGameInitializable, IGameActivatable, IGameDeinitializable
    {
        public static SceneGame Instance { get; private set; }

        public enum GameState
        {
            Begin,
            Combat,
            End,
            DirectionStart,
            DirectionEnd,
            QuestDialogueStart,
            QuestDialogueEnd
        }

        public enum GameSubState
        {
            Normal,
            BossChallenge,
            DialogueStart,
            DialogueEnd
        }

        /// <summary>
        /// 게임 씬 초기화 순서입니다.
        /// SceneGame의 Core 매니저 생성 단계에서 명시적으로 호출됩니다.
        /// </summary>
        public int InitializeOrder => 0;
        
        private GameState State { get; set; }
        private GameSubState SubState { get; set; }
        private bool _isStateDirty;

        [HideInInspector] public GameObject player;

        [Header(ConfigCommon.TitleHeaderRequired)] [Tooltip("메인으로 사용되는 Camera")]
        public Camera mainCamera;

        public void SetMainCamera(Camera value) => mainCamera = value;

        [Tooltip("UI 에 사용되는 메인 Canvas")] public Canvas canvasUI;
        public void SetCanvasUI(Canvas value) => canvasUI = value;

        [Tooltip("드랍 아이템의 이름 text, Npc 이름 text, Npc 퀘스트 마크 오브젝트가 들어갈 오브젝트 입니다.")]
        public GameObject containerDropItemName;

        public void SetContainerDropItemName(GameObject value) => containerDropItemName = value;
        [Tooltip("워프로 맵 이동시 화면을 가려줄 검정화면")] public GameObject bgBlackForMapLoading;
        public void SetBgBlackForMapLoading(GameObject value) => bgBlackForMapLoading = value;

        [Tooltip("몬스터 Hp Bar 오브젝트가 들어갈 오브젝트 입니다.")]
        public GameObject containerMonsterHpBar;

        public void SetContainerMonsterHpBar(GameObject value) => containerMonsterHpBar = value;
        [Tooltip("연출 말풍선이 들어갈 오브젝트 입니다.")] public GameObject containerDialogueBalloon;
        public void SetContainerDialogueBalloon(GameObject value) => containerDialogueBalloon = value;

        [Tooltip("플레이어 보다 밑에 나와야 나와야 하는 UI를 처리하는 Canvas")]
        public GameObject canvasFromWorldCharacterBottom;

        public void SetCanvasFromWorldCharacterBottom(GameObject value) => canvasFromWorldCharacterBottom = value;

        [Header("매니저")] [Tooltip("윈도우 매니저")] public UIWindowManager uIWindowManager;
        public void SetUIWindowManager(UIWindowManager value) => uIWindowManager = value;
        [Tooltip("시스템 메시지 매니저")] public SystemMessageManager systemMessageManager;
        public void SetSystemMessageManager(SystemMessageManager value) => systemMessageManager = value;
        [Tooltip("카메라 매니저")] public CameraManager cameraManager;
        public void SetCameraManager(CameraManager value) => cameraManager = value;
        [Tooltip("팝업 매니저")] public PopupManager popupManager;
        public void SetPopupManager(PopupManager value) => popupManager = value;

        [HideInInspector] public SaveDataManager saveDataManager;
        [HideInInspector] public CalculateManager calculateManager;
        [HideInInspector] public MapManager mapManager;
        [HideInInspector] public MapClearExitPolicyController mapClearExitPolicyController;
        [HideInInspector] public DamageTextManager damageTextManager;
        [HideInInspector] public UIIconCoolTimeManager uIIconCoolTimeManager;
        [HideInInspector] public GameTimeManager gameTimeManager;
        public SoundManager soundManager;
        public void SetSoundManager(SoundManager value) => soundManager = value;

        public ItemManager ItemManager;
        public CharacterManager CharacterManager;
        public KeyboardManager KeyboardManager;
        public InteractionManager InteractionManager;
        public CutsceneManager CutsceneManager;
        public VfxManager VfxManager;
        public ProjectileManager ProjectileManager;
        public LaserManager LaserManager;
        public AddressableLoaderPrefabCharacter AddressableLoaderPrefabCharacter;

        private UIWindowInventory _uiWindowInventory;
        public event Action OnSceneGameDestroyed;

        private GameInitializationRunner _initializationRunner;
        private GameInitContext _initContext;
        private Coroutine _updateStateCoroutine;
        private bool _isInitialized;
        private bool _isActivated;
        private bool _isMonsterKilledSubscribed;

        private void Awake()
        {
            // 테이블이 로드 되지 않았다면, PreIntro 씬으로 이동합니다.
            // 실제 게임 초기화는 GameInitializationRunner가 명시적인 Initialize/Activate 단계로 처리합니다.
            if (TableLoaderManager.Instance == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNamePreIntro);
                return;
            }

            if (!TryRegisterSingleton())
            {
                return;
            }

            _initializationRunner = gameObject.GetComponent<GameInitializationRunner>();
            if (_initializationRunner == null)
            {
                _initializationRunner = gameObject.AddComponent<GameInitializationRunner>();
            }

            _initializationRunner.RunCoreScene(this);
        }

        /// <summary>
        /// 게임 씬 싱글톤을 등록합니다.
        /// Awake에서는 다른 시스템을 초기화하지 않고, 자기 오브젝트의 중복 여부만 판단합니다.
        /// </summary>
        /// <returns>현재 인스턴스가 유효한 게임 씬 싱글톤이면 true입니다.</returns>
        private bool TryRegisterSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return true;
            }

            Destroy(gameObject);
            return false;
        }

        /// <summary>
        /// Core 게임 씬에 필요한 매니저와 런타임 서비스를 초기화합니다.
        /// Awake/Start에 흩어져 있던 게임 필수 초기화를 명시적인 Initialize 단계로 모으기 위한 진입점입니다.
        /// </summary>
        /// <param name="context">Core 초기화 컨텍스트입니다.</param>
        public void Initialize(GameInitContext context)
        {
            if (_isInitialized)
            {
                return;
            }

            if (context == null || !context.ValidateCoreDependencies(nameof(SceneGame)))
            {
                return;
            }

            _initContext = context;

            // 로딩 중 보여주는 이미지를 활성화합니다.
            if (bgBlackForMapLoading)
            {
                bgBlackForMapLoading.SetActive(true);
            }

            InitializeManagers(context);
            RegisterCutsceneEvents();
            RegisterGameTimeProvider(context);

            if (context.SettingsLoader.settings.defaultFps > 0)
                Application.targetFrameRate = context.SettingsLoader.settings.defaultFps;
            _isStateDirty = false;
            SetState(GameState.Begin);

            _isInitialized = true;
        }

        /// <summary>
        /// 초기화가 완료된 뒤 실제 게임 동작을 활성화합니다.
        /// 이벤트 구독, 상태 갱신 코루틴, 사운드 풀 초기화처럼 다른 시스템 준비가 필요한 처리를 이 단계에서 실행합니다.
        /// </summary>
        /// <param name="context">Core 초기화 컨텍스트입니다.</param>
        public void Activate(GameInitContext context)
        {
            if (_isActivated || !_isInitialized)
            {
                return;
            }

            ItemManager?.OnStartBySceneGame();
            VfxManager?.OnStartBySceneGame();

            _uiWindowInventory = uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);

            soundManager?.InitializeSoundSfxPool();
            SubscribeMonsterKilledEvent();

            if (_updateStateCoroutine == null)
            {
                _updateStateCoroutine = StartCoroutine(UpdateStateRoutine());
            }

            _isActivated = true;
        }

        /// <summary>
        /// 컷신 시작/종료 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterCutsceneEvents()
        {
            if (CutsceneManager == null)
            {
                return;
            }

            CutsceneManager.CutsceneStarted -= OnCutsceneStarted;
            CutsceneManager.CutsceneEnded -= OnCutsceneEnded;
            CutsceneManager.CutsceneStarted += OnCutsceneStarted;
            CutsceneManager.CutsceneEnded += OnCutsceneEnded;
        }

        /// <summary>
        /// 인게임 시간 설정이 활성화되어 있을 때 시간 제공자 어댑터를 등록합니다.
        /// </summary>
        /// <param name="context">Core 초기화 컨텍스트입니다.</param>
        private void RegisterGameTimeProvider(GameInitContext context)
        {
            if (gameTimeManager == null || context.SettingsLoader == null || !context.SettingsLoader.settings.useInGameTime)
            {
                return;
            }

            var timeProvider = new GameTimeProviderAdapter(gameTimeManager);
            ServiceLocator.Register<IGameTimeProvider>(timeProvider);
        }

        /// <summary>
        /// <para>매니저 초기화 하기</para>
        /// </summary>
        private void InitializeManagers(GameInitContext context)
        {
            GameObject managerContainer = new GameObject("Managers");

            calculateManager = CreateManager<CalculateManager>(managerContainer);
            calculateManager.Initialize(context.SettingsLoader != null ? context.SettingsLoader.settings : null);

            var useMap = context.SettingsLoader.mapSettings.useMap;
            if (useMap)
            {
                mapManager = CreateManager<MapManager>(managerContainer);
            }
            else
            {
                if (bgBlackForMapLoading)
                {
                    bgBlackForMapLoading.SetActive(false);
                }
            }

            saveDataManager = CreateManager<SaveDataManager>(managerContainer);
            context.SetSaveDataManager(saveDataManager);
            saveDataManager.Initialize(context);
            uIWindowManager?.RefreshWindowSlotActivationStates();
            damageTextManager = CreateManager<DamageTextManager>(managerContainer);
            uIIconCoolTimeManager = CreateManager<UIIconCoolTimeManager>(managerContainer);
            if (context.SettingsLoader != null && context.SettingsLoader.settings.useInGameTime)
                gameTimeManager = CreateManager<GameTimeManager>(managerContainer);

            AddressableLoaderPrefabCharacter = new AddressableLoaderPrefabCharacter();
            AddressableLoaderPrefabCharacter.Initialize(this);
            ItemManager = new ItemManager();
            ItemManager.Initialize(this);
            CharacterManager = new CharacterManager();
            CharacterManager.Initialize(context.TableLoader.TableNpc, context.TableLoader.TableMonster,
                context.TableLoader.TableAnimation, AddressableLoaderPrefabCharacter);
            AnimationEventMediator animationEventMediator = new AnimationEventMediator();
            CharacterManager.SetAnimationEventMediator(animationEventMediator);

            KeyboardManager = new KeyboardManager();
            KeyboardManager.Initialize(this);
            InteractionManager = new InteractionManager();
            InteractionManager.Initialize(this);
            CutsceneManager = new CutsceneManager();
            CutsceneManager.Initialize(this);
            VfxManager = new VfxManager();
            VfxManager.Initialize(this);
            VfxManager.SetAnimationEventMediator(animationEventMediator);
            ProjectileManager = new ProjectileManager();
            ProjectileManager.Initialize(this);
            LaserManager = new LaserManager();
            LaserManager.Initialize(this);

            if (mapManager != null)
            {
                mapClearExitPolicyController = CreateManager<MapClearExitPolicyController>(managerContainer);
                mapClearExitPolicyController.Initialize(this);
            }

            // AnimationEventMediator 클래스에서 다른 매니저를 사용하고 있기때문에,
            // 매니저가 생성된 후 Initialize를 해야 한다.
            animationEventMediator.Initialize(this);
        }

        public T CreateManager<T>(GameObject parent) where T : Component
        {
            GameObject obj = new GameObject(typeof(T).Name);
            obj.transform.SetParent(parent.transform);
            return obj.AddComponent<T>();
        }

        private void Start()
        {
            // 레거시/테스트 씬에서 Awake 시점 초기화가 지연된 경우를 위한 호환 처리입니다.
            // 신규 코드는 GameInitializationRunner의 Initialize/Activate 경로를 사용합니다.
            if (_isActivated)
            {
                return;
            }

            if (_initContext == null)
            {
                _initContext = new GameInitContext(this, TableLoaderManager.Instance, AddressableLoaderSettings.Instance);
            }

            Initialize(_initContext);
            Activate(_initContext);
        }

        private IEnumerator UpdateStateRoutine()
        {
            while (true)
            {
                if (_isStateDirty)
                {
                    OnStateChanged();
                    _isStateDirty = false;
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
                        Title = "System_PlayerDied_MoveToTown_Title", // 게임 종료
                        Message = "System_PlayerDied_MoveToTown", //플레이어가 사망하였습니다.\n마을로 이동합니다.
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
            Destroy(player);
            SetState(GameState.Begin);
            mapManager.LoadMapByPlayerDead();
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            _isStateDirty = true;
        }

        public void SetSubState(GameSubState newSubState)
        {
            SubState = newSubState;
            _isStateDirty = true;
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
        /// 상점 표시 데이터를 기준으로 아이템 구매를 처리합니다.
        /// shop_item 테이블의 구매 후 사용 정책까지 함께 적용하기 위해 상점 UI에서는 이 오버로드를 우선 사용합니다.
        /// </summary>
        /// <param name="shopDisplayItem">구매할 상점 아이템 표시 데이터입니다.</param>
        /// <param name="itemCount">구매 수량입니다.</param>
        /// <returns>구매 처리 결과입니다.</returns>
        public ResultCommon BuyItem(ShopDisplayItem shopDisplayItem, int itemCount = 1)
        {
            if (shopDisplayItem == null ||
                shopDisplayItem.ItemUid <= 0)
            {
                return ResultCommon.Fail("Shop_InvalidItem");
            }

            if (itemCount <= 0)
            {
                return ResultCommon.Fail(
                    "Slot_InvalidItemCount",
                    $"itemUid: {shopDisplayItem.ItemUid}, itemCount: {itemCount}");
            }

            ResultCommon result = ExecuteItemPurchase(
                shopDisplayItem.ItemUid,
                shopDisplayItem.CurrencyType,
                shopDisplayItem.CurrencyValue,
                itemCount,
                shopDisplayItem.BuyUsePolicy);
            if (result == null ||
                result.Result == ResultCommon.ResultType.Fail)
            {
                return result;
            }

            // 구매 제한 기록까지 반영된 뒤 완료 이벤트를 발행해야 구독자가 확정된 구매 상태를 조회할 수 있습니다.
            saveDataManager?.ShopPurchase?.AddBoughtCount(
                shopDisplayItem,
                itemCount);
            GameEventManager.ItemPurchased(new ItemPurchasedEventData(
                shopDisplayItem.ItemUid,
                itemCount,
                shopDisplayItem.Uid,
                shopDisplayItem.ShopUid,
                shopDisplayItem.BuyUsePolicy,
                shopDisplayItem.CurrencyType,
                shopDisplayItem.CurrencyValue));
            return result;
        }

        /// <summary>
        /// 아이템 구매를 처리합니다.
        /// 구매 후 처리 정책에 따라 인벤토리에 추가하거나 즉시 사용합니다.
        /// </summary>
        /// <param name="itemUid">구매할 아이템 UID입니다.</param>
        /// <param name="currencyType">구매에 사용할 재화 타입입니다.</param>
        /// <param name="price">아이템 1개 가격입니다.</param>
        /// <param name="itemCount">구매 수량입니다.</param>
        /// <param name="buyUsePolicy">구매 성공 후 처리 정책입니다.</param>
        /// <returns>구매 처리 결과입니다.</returns>
        public ResultCommon BuyItem(
            int itemUid,
            CurrencyConstants.Type currencyType,
            int price,
            int itemCount = 1,
            ShopBuyUsePolicy buyUsePolicy = ShopBuyUsePolicy.AddToInventory)
        {
            if (itemCount <= 0)
            {
                return ResultCommon.Fail("Slot_InvalidItemCount", $"itemUid: {itemUid}, itemCount: {itemCount}");
            }

            ResultCommon result = ExecuteItemPurchase(
                itemUid,
                currencyType,
                price,
                itemCount,
                buyUsePolicy);
            if (result != null &&
                result.Result == ResultCommon.ResultType.Success)
            {
                GameEventManager.ItemPurchased(new ItemPurchasedEventData(
                    itemUid,
                    itemCount,
                    buyUsePolicy: buyUsePolicy,
                    currencyType: currencyType,
                    unitPrice: price));
            }

            return result;
        }

        /// <summary>
        /// 재화 차감과 구매 후 처리 정책에 따른 아이템 지급 또는 즉시 사용을 실행합니다.
        /// 구매 출처 기록과 완료 이벤트 발행은 호출자가 성공 결과를 확인한 뒤 처리합니다.
        /// </summary>
        /// <param name="itemUid">구매할 아이템 UID입니다.</param>
        /// <param name="currencyType">구매에 사용할 재화 타입입니다.</param>
        /// <param name="price">아이템 한 개의 가격입니다.</param>
        /// <param name="itemCount">구매할 아이템 수량입니다.</param>
        /// <param name="buyUsePolicy">구매 후 아이템 처리 정책입니다.</param>
        /// <returns>구매 거래 처리 결과입니다.</returns>
        private ResultCommon ExecuteItemPurchase(
            int itemUid,
            CurrencyConstants.Type currencyType,
            int price,
            int itemCount,
            ShopBuyUsePolicy buyUsePolicy)
        {
            return buyUsePolicy switch
            {
                ShopBuyUsePolicy.UseImmediately => BuyAndUseItemImmediately(itemUid, currencyType, price, itemCount),
                _ => BuyItemToInventory(itemUid, currencyType, price, itemCount),
            };
        }

        /// <summary>
        /// 구매한 아이템을 인벤토리에 추가합니다.
        /// 구매 실패 시 차감된 재화를 되돌립니다.
        /// </summary>
        /// <param name="itemUid">구매할 아이템 UID입니다.</param>
        /// <param name="currencyType">구매에 사용할 재화 타입입니다.</param>
        /// <param name="price">아이템 1개 가격입니다.</param>
        /// <param name="itemCount">구매 수량입니다.</param>
        /// <returns>구매 처리 결과입니다.</returns>
        private ResultCommon BuyItemToInventory(int itemUid, CurrencyConstants.Type currencyType, int price, int itemCount)
        {
            int totalPrice = price * itemCount;
            var checkNeedCurrency = saveDataManager.Player.CheckNeedCurrency(currencyType, totalPrice);
            if (checkNeedCurrency.Result == ResultCommon.ResultType.Fail) return checkNeedCurrency;

            var minusCurrency = saveDataManager.Player.MinusCurrency(currencyType, totalPrice);
            if (minusCurrency.Result == ResultCommon.ResultType.Fail) return minusCurrency;

            var addItem = saveDataManager.Inventory.AddItem(itemUid, itemCount);
            if (addItem.Result == ResultCommon.ResultType.Fail)
            {
                saveDataManager.Player.AddCurrency(currencyType, totalPrice);
                return addItem;
            }

            if (_uiWindowInventory != null)
            {
                _uiWindowInventory.SetIcons(addItem);
            }

            return addItem;
        }

        /// <summary>
        /// 구매한 아이템을 인벤토리에 넣지 않고 즉시 사용합니다.
        /// 즉시 사용은 사용 결과와 구매 기록을 명확하게 맞추기 위해 단일 구매만 허용합니다.
        /// </summary>
        /// <param name="itemUid">구매 후 즉시 사용할 아이템 UID입니다.</param>
        /// <param name="currencyType">구매에 사용할 재화 타입입니다.</param>
        /// <param name="price">아이템 1개 가격입니다.</param>
        /// <param name="itemCount">구매 수량입니다.</param>
        /// <returns>구매 및 즉시 사용 처리 결과입니다.</returns>
        private ResultCommon BuyAndUseItemImmediately(
            int itemUid,
            CurrencyConstants.Type currencyType,
            int price,
            int itemCount)
        {
            if (itemCount != 1)
            {
                return ResultCommon.Fail("Shop_ImmediateUseSingleOnly", $"itemUid: {itemUid}, itemCount: {itemCount}");
            }

            // 효과를 적용할 수 없는 상품은 재화를 차감하기 전에 구매를 중단합니다.
            // 재화 차감 후에도 실제 사용 단계에서 다시 검사하여 거래 시점의 데이터 무결성을 보장합니다.
            ResultCommon canUseResult = ItemUseService.CanUseItemDirect(this, itemUid);
            if (canUseResult == null || canUseResult.Result == ResultCommon.ResultType.Fail)
            {
                return canUseResult ?? ResultCommon.Fail("ItemUse_CannotExecute");
            }

            var checkNeedCurrency = saveDataManager.Player.CheckNeedCurrency(currencyType, price);
            if (checkNeedCurrency.Result == ResultCommon.ResultType.Fail) return checkNeedCurrency;

            var minusCurrency = saveDataManager.Player.MinusCurrency(currencyType, price);
            if (minusCurrency.Result == ResultCommon.ResultType.Fail) return minusCurrency;

            ResultCommon useResult = ItemUseService.TryUseItemDirect(this, itemUid, out _);
            if (useResult == null || useResult.Result == ResultCommon.ResultType.Fail)
            {
                saveDataManager.Player.AddCurrency(currencyType, price);
                return useResult ?? ResultCommon.Fail("ItemUse_Execute_Fail");
            }

            return useResult;
        }

        private void OnEnable()
        {
            if (_isActivated)
            {
                SubscribeMonsterKilledEvent();
            }
        }

        /// <summary>
        /// 몬스터 처치 이벤트를 중복 없이 구독합니다.
        /// 이벤트 구독은 초기화 완료 이후 Activate 단계에서 수행합니다.
        /// </summary>
        private void SubscribeMonsterKilledEvent()
        {
            if (_isMonsterKilledSubscribed)
            {
                return;
            }

            GameEventManager.MonsterKilledEvent += OnMonsterKilled;
            _isMonsterKilledSubscribed = true;
        }

        /// <summary>
        /// 몬스터 처치 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeMonsterKilledEvent()
        {
            if (!_isMonsterKilledSubscribed)
            {
                return;
            }

            GameEventManager.MonsterKilledEvent -= OnMonsterKilled;
            _isMonsterKilledSubscribed = false;
        }

        /// <summary>
        /// 게임 씬이 비활성화될 때 매니저 구독과 전역 서비스를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            Deinitialize();
        }

        /// <summary>
        /// 게임 씬 비활성화 또는 종료 시 Core 매니저와 전역 서비스를 정리합니다.
        /// </summary>
        public void Deinitialize()
        {
            UnsubscribeMonsterKilledEvent();

            if (_updateStateCoroutine != null)
            {
                StopCoroutine(_updateStateCoroutine);
                _updateStateCoroutine = null;
            }

            if (CutsceneManager != null)
            {
                CutsceneManager.CutsceneStarted -= OnCutsceneStarted;
                CutsceneManager.CutsceneEnded -= OnCutsceneEnded;
            }

            ItemManager?.OnDestroy();
            CharacterManager?.OnDestroy();
            KeyboardManager?.OnDestroy();
            InteractionManager?.OnDestroy();
            CutsceneManager?.OnDestroy();

            ServiceLocator.UnregisterAll();

            _isActivated = false;
            _isInitialized = false;

            OnSceneGameDestroyed?.Invoke();
        }

        private void OnMonsterKilled(MonsterKilledEventData e)
        {
            // 플레이어에게 사망했을 때 처리
            if (e.dieReasonType == CharacterConstants.DieReasonType.Battle)
            {
                saveDataManager.Player.AddExpByMonster(e.monsterUid);
                ItemManager.OnMonsterDead(e.monsterUid, e.monster);
            }

            mapManager.OnDeadMonster(e.monsterVid);
        }

        /// <summary>
        /// 컷신 세션 시작 시 모바일 HUD를 숨김 사유에 추가합니다.
        /// </summary>
        private void OnCutsceneStarted()
        {
            if (containerMonsterHpBar)
                containerMonsterHpBar.SetActive(false);
            if (containerDropItemName)
                containerDropItemName.SetActive(false);
            if (canvasFromWorldCharacterBottom)
                canvasFromWorldCharacterBottom.SetActive(false);
        }

        /// <summary>
        /// 컷신 세션 종료 시 모바일 HUD의 컷신 숨김 사유를 제거합니다.
        /// </summary>
        private void OnCutsceneEnded()
        {
            if (containerMonsterHpBar)
                containerMonsterHpBar.SetActive(true);
            if (containerDropItemName)
                containerDropItemName.SetActive(true);
            if (canvasFromWorldCharacterBottom)
                canvasFromWorldCharacterBottom.SetActive(true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeLogging()
        {
            if (GGemCoBuildFlags.AllowDebugFeatures)
            {
                GcLogger.ConfigureStackTraceLogging(
                    log: StackTraceLogType.ScriptOnly,
                    warning: StackTraceLogType.ScriptOnly,
                    error: StackTraceLogType.Full,
                    exception: StackTraceLogType.Full,
                    assert: StackTraceLogType.Full);
            }
            else
            {
                GcLogger.ConfigureStackTraceLogging(
                    log: StackTraceLogType.None,
                    warning: StackTraceLogType.None,
                    error: StackTraceLogType.ScriptOnly,
                    exception: StackTraceLogType.ScriptOnly,
                    assert: StackTraceLogType.ScriptOnly);
            }
        }
    }
}
