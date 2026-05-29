using System.IO;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 메인 매니저
    /// </summary>
    public class SaveDataManagerBase : MonoBehaviour
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

        protected virtual void Awake()
        {
            tableLoaderManager = TableLoaderManager.Instance;
            if (tableLoaderManager == null) return;

            InitializeSaveDirectory();
            InitializeController();
        }

        /// <summary>
        /// 기본 정보를 GGemCo Settings 에서 불러온다.
        /// </summary>
        private void InitializeSaveDirectory()
        {
            GGemCoSaveSettings saveSettings = AddressableLoaderSettings.Instance.saveSettings;
            _useSaveData = saveSettings.UseSaveData;
            _saveDelay = saveSettings.saveDataDelay;
            _forceSaveInterval = saveSettings.saveDataForceSaveInterval;
            thumbnailWidth = saveSettings.saveDataThumbnailWidth;
            _maxSaveSlotCount = saveSettings.saveDataMaxSlotCount;

            _useGameTime = AddressableLoaderSettings.Instance.settings.useInGameTime;

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
            _lastSaveTime = Time.time;
            // 강제 저장 시작 
            InvokeRepeating(nameof(ForceSave), _forceSaveInterval, _forceSaveInterval);
        }

        /// <summary>
        /// 저장하기 시작
        /// </summary>
        public void StartSaveData()
        {
            if (_isResetInProgress)
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
            if (_isResetInProgress)
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
            if (_isResetInProgress)
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
            if (_isResetInProgress)
            {
                return false;
            }

            _isResetInProgress = true;
            try
            {
                StopScheduledSaveInvokes();

                if (!SaveDataResetUtility.ResetPersistentStorage(scope))
                {
                    return false;
                }

                InitializeController();
                return true;
            }
            finally
            {
                _isResetInProgress = false;
            }
        }

        /// <summary>
        /// 저장 관련 예약 호출을 모두 중단합니다.
        /// </summary>
        private void StopScheduledSaveInvokes()
        {
            CancelInvoke(nameof(SaveData));
            CancelInvoke(nameof(ForceSave));
        }

        private void OnDestroy()
        {
            
        }

        protected SaveEnvelope BuildEnvelopeForSave()
        {
            var env = new SaveEnvelope();
            var list = SaveRegistry.All;
            for (int i = 0; i < list.Count; i++) list[i].Capture(env);
            return env;
        }
    }
}
