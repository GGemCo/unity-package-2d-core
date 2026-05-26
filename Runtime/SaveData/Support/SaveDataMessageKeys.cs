namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 로드/복구 과정에서 사용하는 사용자 안내 메시지 키입니다.
    /// </summary>
    public static class SaveDataMessageKeys
    {
        /// <summary>
        /// 백업 저장 데이터로 복구했을 때 표시할 메시지 키입니다.
        /// </summary>
        public const string RestoredFromBackup = "save_data_restored_from_backup";

        /// <summary>
        /// 저장 데이터를 불러올 수 없어 새 데이터로 시작할 때 표시할 메시지 키입니다.
        /// </summary>
        public const string CannotLoadSaveData = "save_data_cannot_load";
    }
}
