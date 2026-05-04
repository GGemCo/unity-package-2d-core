namespace GGemCo2DCore
{
    /// <summary>
    /// 로컬 데이터 초기화 범위를 정의합니다.
    /// </summary>
    public enum SaveDataResetScope
    {
        /// <summary>
        /// 저장 슬롯, 저장 파일, 저장 썸네일 등 게임 진행 데이터만 삭제합니다.
        /// </summary>
        GameProgressOnly,

        /// <summary>
        /// 게임 진행 데이터와 PlayerPrefs 기반 로컬 설정을 모두 초기화합니다.
        /// </summary>
        AllLocalData,
    }
}