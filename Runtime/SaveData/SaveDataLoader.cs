using Newtonsoft.Json;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 json 파일 로드
    /// </summary>
    public class SaveDataLoader : SaveDataLoaderBase
    {
        public static SaveDataLoader Instance { get; private set; }
        
        private SaveDataContainer _saveDataContainer;
        
        protected override void Awake()
        {
            base.Awake();

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Core 저장 로더가 제거될 때 컨테이너와 싱글톤 참조를 정리합니다.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _saveDataContainer = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 로드된 JSON을 저장 데이터 컨테이너로 역직렬화합니다.
        /// </summary>
        /// <param name="json">복호화와 검증을 마친 평문 JSON입니다.</param>
        protected override void OnLoaded(string json) 
        {
            _saveDataContainer = JsonConvert.DeserializeObject<SaveDataContainer>(json);
        }

        /// <summary>
        /// 저장 데이터 로드 실패 시 이전에 메모리에 남아 있던 컨테이너를 제거합니다.
        /// </summary>
        /// <param name="result">로드와 복구 처리 결과입니다.</param>
        protected override void OnLoadFailed(SaveDataLoadResult result)
        {
            _saveDataContainer = null;
        }

        public SaveDataContainer GetSaveDataContainer()
        {
            return _saveDataContainer;
        }

        /// <summary>
        /// 메모리에 로드되어 있던 세이브 데이터를 초기화합니다.
        /// </summary>
        public void ClearLoadedData()
        {
            _saveDataContainer = null;
        }

        /// <summary>
        /// 로컬 데이터 초기화 시 Core 저장 컨테이너를 메모리에서 제거합니다.
        /// </summary>
        /// <param name="scope">요청된 로컬 데이터 초기화 범위입니다.</param>
        protected override void OnClearLoadedDataForReset(SaveDataResetScope scope)
        {
            ClearLoadedData();
        }
    }
}
