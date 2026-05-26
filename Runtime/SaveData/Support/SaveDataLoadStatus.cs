namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 로드와 복구 처리 결과 상태입니다.
    /// </summary>
    public enum SaveDataLoadStatus
    {
        /// <summary>
        /// 아직 로드 결과가 정해지지 않은 상태입니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 저장 파일이 없어 새 저장 데이터 생성이 필요한 상태입니다.
        /// </summary>
        NoSaveFile,

        /// <summary>
        /// 기본 저장 파일을 정상적으로 불러온 상태입니다.
        /// </summary>
        LoadedPrimary,

        /// <summary>
        /// 기본 저장 파일은 실패했지만 백업 파일 복구에 성공한 상태입니다.
        /// </summary>
        RestoredFromBackup,

        /// <summary>
        /// 기본 저장 파일과 백업 파일이 모두 실패하여 새 저장 데이터 생성이 필요한 상태입니다.
        /// </summary>
        NewDataRequired,

        /// <summary>
        /// 파일 입출력 등으로 저장 데이터를 불러올 수 없는 상태입니다.
        /// </summary>
        Failed,
    }
}
