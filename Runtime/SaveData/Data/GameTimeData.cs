
namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 - 인게임 시간 정보
    /// </summary>
    public class GameTimeData : DefaultData, ISaveData
    {
        public double CurrentGameTime { get; set; }
        
        /// <summary>
        /// 초기화 (저장된 데이터를 불러오거나 새로운 데이터 생성)
        /// </summary>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            // 사용 안함이면 처리하지 않음.
            if (!AddressableLoaderSettings.Instance.settings.useInGameTime) return;
            
            if (saveDataContainer?.GameTimeData != null)
            {
                CurrentGameTime = saveDataContainer.GameTimeData.CurrentGameTime;
            }
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }
}