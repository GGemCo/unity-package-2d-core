using System;
using System.IO;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 메인 매니저
    /// </summary>
    public class SaveDataManagerBase : MonoBehaviour, IGameInitializable, ISaveDataResetParticipant
    {
        protected TableLoaderManager tableLoaderManager;
        protected SlotMetaDatController slotMetaDatController;
        protected SaveFileController saveFileController;
        protected ThumbnailController thumbnailController;
        
        // 썸네일 width 
        protected int thumbnailWidth;
        // 현재 진행중인 slot index
        protected int currentSaveSlot;
        
        // 최대 저장 슬롯 개수
        private int _maxSaveSlotCount;
        // 게임 데이터 저장 경로
        private string _saveDirectory;
        // 썸네일 저장 경로
        private string _thumbnailDirectory;

        // 이 시간안에 저장 요청이 오면 기존 요청은 취소된다.
        private float _saveDelay;
        // 강제로 저장할 시간
        private float _forceSaveInterval;
        // 마지막 저장된 시간
        private float _lastSaveTime;
        private bool _useSaveData;
        private bool _useGameTime;
        private bool _isResetInProgress;
        private bool _isInitialized;
        private int _saveSuppressionCount;

        /// <summary>
        /// 저장 데이터 매니저 초기화 순서입니다.
        /// SceneGame의 Core 매니저 생성 단계에서 명시적으로 호출됩니다.
        /// </summary>
        public int InitializeOrder => 200;

        /// <summary>
        /// 저장 데이터 매니저가 초기화되었는지 확인합니다.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 외부 시스템의 저장 억제 요청으로 현재 저장이 차단되어 있는지 확인합니다.
        /// </summary>
        public bool IsSaveSuppressed => _saveSuppressionCount > 0;

        /// <summary>
        /// 로더 캐시를 정리하기 전에 모든 저장 예약을 중단하도록 초기화 순서를 반환합니다.
        /// </summary>
        public virtual int LocalDataResetOrder => 100;

        protected virtual void Awake()
        {
            // Awake에서는 Unity 오브젝트 생성만 허용합니다.
            // 테이블/설정/저장 경로 초기화는 Initialize 단계에서 명시적으로 처리합니다.
        }

        /// <summary>
        /// 저장 데이터 매니저에 필요한 테이블과 설정을 주입받아 초기화합니다.
        /// Awake/Start 순서에 의존하지 않도록 SceneGame 초기화 파이프라인에서 호출합니다.
        /// </summary>
        /// <param name="context">Core 초기화 컨텍스트입니다.</param>
        public void Initialize(GameInitContext context)
        {
            if (_isInitialized)
            {
                return;
            }

            if (context == null || context.TableLoader == null || context.SettingsLoader == null || context.SettingsLoader.saveSettings == null)
            {
                GcLogger.LogError("[SaveDataManagerBase] 저장 데이터 초기화에 필요한 테이블 또는 설정이 준비되지 않았습니다.");
                return;
            }

            tableLoaderManager = context.TableLoader;
            InitializeSaveDirectory(context.SettingsLoader);
            InitializeController();
            SaveDataResetParticipantRegistry.Register(this);
            _isInitialized = true;
        }

        /// <summary>
        /// 레거시 테스트 씬처럼 명시적 초기화 파이프라인을 거치지 않은 경우 싱글톤 상태를 이용해 초기화를 시도합니다.
        /// 신규 런타임 경로에서는 SceneGame.Initialize 단계에서 Initialize(GameInitContext)가 먼저 호출됩니다.
        /// </summary>
        /// <returns>초기화에 성공하면 true입니다.</returns>
        private bool TryInitializeFromSingletons()
        {
            if (_isInitialized)
            {
                return true;
            }

            var context = new GameInitContext(SceneGame.Instance, TableLoaderManager.Instance, AddressableLoaderSettings.Instance);
            Initialize(context);
            return _isInitialized;
        }

        /// <summary>
        /// 기본 정보를 GGemCo Settings 에서 불러온다.
        /// </summary>
        private void InitializeSaveDirectory(AddressableLoaderSettings settingsLoader)
        {
            GGemCoSaveSettings saveSettings = settingsLoader.saveSettings;
            _useSaveData = saveSettings.UseSaveData;
            _saveDelay = saveSettings.saveDataDelay;
            _forceSaveInterval = saveSettings.saveDataForceSaveInterval;
            thumbnailWidth = saveSettings.saveDataThumbnailWidth;
            _maxSaveSlotCount = saveSettings.saveDataMaxSlotCount;

            _useGameTime = settingsLoader.settings.useInGameTime;

            _saveDirectory = saveSettings.SaveDataFolderName;
            _thumbnailDirectory = saveSettings.SaveDataThumnailFolderName;
            Directory.CreateDirectory(_saveDirectory);
            Directory.CreateDirectory(_thumbnailDirectory);
        }

        /// <summary>
        /// 슬롯 관리, 파일 관리, 썸네일 관리 매니저 초기화
        /// </summary>
        private void InitializeController()
        {
            slotMetaDatController = new SlotMetaDatController(_saveDirectory, _maxSaveSlotCount);
            saveFileController = new SaveFileController(_saveDirectory, _maxSaveSlotCount);
            thumbnailController = new ThumbnailController(_thumbnailDirectory, thumbnailWidth);

            currentSaveSlot = PlayerPrefsManager.LoadSaveDataSlotIndex();
            
            InitializeData();
        }

        /// <summary>
        /// PlayerPrefs에 저장된 현재 슬롯 선택값을 런타임 슬롯 상태와 동기화합니다.
        /// Intro에서 새 게임/불러오기로 슬롯이 바뀐 직후, 이미 생성된 SaveDataManager 인스턴스가
        /// 이전 슬롯 값을 유지하는 문제를 방지하기 위해 저장 진입 시마다 호출합니다.
        /// </summary>
        protected void SyncCurrentSaveSlotFromPlayerPrefs()
        {
            // 슬롯 선택은 Intro/UI에서 PlayerPrefs로 갱신되므로,
            // 저장 직전에는 항상 최신 선택값을 다시 읽어 런타임 상태와 맞춥니다.
            currentSaveSlot = PlayerPrefsManager.LoadSaveDataSlotIndex();
        }

        protected virtual void InitializeData()
        {
            
        }

        protected virtual void Start()
        {
            if (!TryInitializeFromSingletons())
            {
                return;
            }

            _lastSaveTime = Time.time;

            // 강제 저장 시작. 저장 기능이 비활성화되었거나 간격이 0 이하이면 예약하지 않습니다.
            ScheduleForceSaveInvoke();
        }

        /// <summary>
        /// 외부 시스템이 사용자 선택이나 초기 연출을 완료할 때까지 저장을 억제합니다.
        /// 반환된 토큰을 해제하기 전에는 예약 저장과 직접 저장을 모두 수행하지 않습니다.
        /// </summary>
        /// <returns>저장 억제 상태를 해제하는 토큰입니다.</returns>
        public IDisposable AcquireSaveSuppression()
        {
            _saveSuppressionCount++;

            // 이미 예약된 지연 저장과 강제 저장이 억제 구간 중 실행되지 않도록 함께 취소합니다.
            StopScheduledSaveInvokes();
            return new SaveSuppressionToken(this);
        }

        /// <summary>
        /// 저장 억제 토큰 하나를 해제하고, 모든 억제가 해제되면 주기 저장 예약을 복원합니다.
        /// 억제 해제 자체로 즉시 저장하지 않으며, 저장 시점은 호출자가 명시적으로 결정합니다.
        /// </summary>
        private void ReleaseSaveSuppression()
        {
            if (_saveSuppressionCount <= 0)
            {
                return;
            }

            _saveSuppressionCount--;
            if (_saveSuppressionCount == 0)
            {
                ScheduleForceSaveInvoke();
            }
        }

        /// <summary>
        /// 저장 변경 요청을 받아 설정된 지연 정책에 따라 저장을 시작합니다.
        /// </summary>
        public void StartSaveData()
        {
            if (IsSaveBlocked())
            {
                return;
            }

            // 런타임 중 슬롯 선택이 변경될 수 있으므로 저장 시작 전에 최신 슬롯을 반영합니다.
            SyncCurrentSaveSlotFromPlayerPrefs();

            // 로더 초기화 순서가 늦은 환경(커스텀 로더/테스트 씬)에서는 null일 수 있으므로 방어합니다.
            SaveDataLoader saveDataLoader = SaveDataLoader.Instance;
            SaveDataContainer saveDataContainer = saveDataLoader != null
                ? saveDataLoader.GetSaveDataContainer()
                : null;
            if (saveDataContainer == null)
            {
                SaveData();
            }
            else
            {
                CancelInvoke(nameof(SaveData));
                Invoke(nameof(SaveData), _saveDelay);
            }
        }

        /// <summary>
        /// 강제 저장하기
        /// </summary>
        private void ForceSave()
        {
            if (IsSaveBlocked())
            {
                return;
            }

            if (Time.time - _lastSaveTime >= _forceSaveInterval)
            {
                // GcLogger.Log("강제 저장");
                SaveData();
            }
        }

        /// <summary>
        /// 현재 데이터를 선택한 슬롯에 저장 + 메타파일 업데이트
        /// </summary>
        public virtual bool SaveData()
        {
            if (IsSaveSuppressed)
            {
                return false;
            }

            if (_isResetInProgress || SaveDataResetParticipantRegistry.IsResetInProgress)
            {
                GcLogger.LogWarning("저장 데이터 초기화 중에는 저장할 수 없습니다.");
                return false;
            }

            // 실제 저장 직전에도 슬롯을 한 번 더 동기화해 저장 파일/메타데이터가
            // 사용자가 마지막으로 선택한 슬롯으로 기록되도록 보장합니다.
            SyncCurrentSaveSlotFromPlayerPrefs();

            if (!_useSaveData)
            {
                GcLogger.LogWarning($"저장 하기가 비활성화 상태 입니다. {ConfigDefine.NameSDK}SaveSettings 에서 활성화 시켜주세요.");
                return false;
            }

            if (currentSaveSlot < 1 || currentSaveSlot > _maxSaveSlotCount)
            {
                GcLogger.LogError("잘못된 슬롯 번호입니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 슬롯 삭제 + 메타파일 업데이트
        /// </summary>
        public void DeleteData(int slot)
        {
            string thumbnailPath = thumbnailController.GetThumbnailPath(slot);

            saveFileController.DeleteData(slot);
            if (File.Exists(thumbnailPath)) File.Delete(thumbnailPath);
            
            slotMetaDatController.DeleteSlot(slot);
        }

        /// <summary>
        /// 로컬 저장 데이터를 지정한 범위 기준으로 초기화합니다.
        /// </summary>
        /// <param name="scope">초기화 범위입니다.</param>
        /// <returns>초기화 성공 여부입니다.</returns>
        public bool ResetLocalData(SaveDataResetScope scope)
        {
            if (_isResetInProgress || SaveDataResetParticipantRegistry.IsResetInProgress)
            {
                return false;
            }

            // 초기화 성공 후에는 현재 장면의 메모리 데이터를 다시 만들지 않습니다.
            // 옵션 UI가 Intro 장면으로 전환하면 새 저장 매니저가 빈 저장소를 기준으로 초기화합니다.
            return SaveDataResetUtility.ResetPersistentStorage(scope);
        }

        /// <summary>
        /// 로컬 데이터 삭제 전에 현재 매니저의 예약 저장을 모두 중단하고 저장 차단 상태로 전환합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        public virtual void PrepareLocalDataReset(SaveDataResetScope scope)
        {
            _isResetInProgress = true;
            StopScheduledSaveInvokes();
        }

        /// <summary>
        /// 매니저가 보유한 런타임 저장 상태를 정리합니다.
        /// 파생 매니저에서 추가 캐시 정리가 필요한 경우 재정의할 수 있습니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        public virtual void ClearLocalDataRuntimeState(SaveDataResetScope scope)
        {
        }

        /// <summary>
        /// 로컬 데이터 초기화 결과를 반영합니다.
        /// 성공한 현재 장면의 매니저는 이전 메모리가 재저장되지 않도록 차단 상태를 유지합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        /// <param name="success">영구 저장소 삭제까지 성공했으면 true입니다.</param>
        public virtual void CompleteLocalDataReset(SaveDataResetScope scope, bool success)
        {
            if (success)
            {
                return;
            }

            _isResetInProgress = false;
            ScheduleForceSaveInvoke();
        }

        /// <summary>
        /// 저장 관련 예약 호출을 모두 중단합니다.
        /// </summary>
        private void StopScheduledSaveInvokes()
        {
            CancelInvoke(nameof(SaveData));
            CancelInvoke(nameof(ForceSave));
        }

        /// <summary>
        /// 저장 억제 또는 로컬 데이터 초기화로 현재 저장 요청을 처리할 수 없는지 확인합니다.
        /// </summary>
        /// <returns>저장을 차단해야 하면 true입니다.</returns>
        private bool IsSaveBlocked()
        {
            return IsSaveSuppressed ||
                   _isResetInProgress ||
                   SaveDataResetParticipantRegistry.IsResetInProgress;
        }

        /// <summary>
        /// 저장 매니저가 파괴될 때 전역 초기화 참여자 등록을 해제합니다.
        /// </summary>
        protected virtual void OnDestroy()
        {
            SaveDataResetParticipantRegistry.Unregister(this);
        }

        /// <summary>
        /// 저장 기능과 강제 저장 간격이 유효할 때 주기 저장 호출을 예약합니다.
        /// </summary>
        private void ScheduleForceSaveInvoke()
        {
            if (!IsSaveSuppressed &&
                !_isResetInProgress &&
                !SaveDataResetParticipantRegistry.IsResetInProgress &&
                isActiveAndEnabled &&
                _useSaveData &&
                _forceSaveInterval > 0f &&
                !IsInvoking(nameof(ForceSave)))
            {
                InvokeRepeating(nameof(ForceSave), _forceSaveInterval, _forceSaveInterval);
            }
        }

        protected SaveEnvelope BuildEnvelopeForSave()
        {
            var env = new SaveEnvelope();
            var list = SaveRegistry.All;
            for (int i = 0; i < list.Count; i++) list[i].Capture(env);
            return env;
        }

        /// <summary>
        /// 저장 억제 요청의 수명을 관리하고 중복 해제를 방지합니다.
        /// </summary>
        private sealed class SaveSuppressionToken : IDisposable
        {
            private SaveDataManagerBase _owner;

            /// <summary>
            /// 지정한 저장 매니저에 연결된 억제 토큰을 생성합니다.
            /// </summary>
            /// <param name="owner">저장 억제를 소유한 저장 매니저입니다.</param>
            public SaveSuppressionToken(SaveDataManagerBase owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// 저장 억제를 한 번만 해제합니다.
            /// </summary>
            public void Dispose()
            {
                SaveDataManagerBase owner = _owner;
                _owner = null;

                if (owner != null)
                {
                    owner.ReleaseSaveSuppression();
                }
            }
        }
    }
}
