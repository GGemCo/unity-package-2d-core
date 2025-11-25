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
        /// 바로 해제를 위해 추가
        /// </summary>
        private void OnDestroy()
        {
            _saveDataContainer = null;
        }

        protected override void OnLoaded(string json) 
        {
            _saveDataContainer = JsonConvert.DeserializeObject<SaveDataContainer>(json);
        }
        public SaveDataContainer GetSaveDataContainer()
        {
            return _saveDataContainer;
        }
    }
}