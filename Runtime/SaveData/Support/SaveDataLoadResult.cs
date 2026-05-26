namespace GGemCo2DCore
{
    /// <summary>
    /// 저장 데이터 로드와 복구 처리 결과입니다.
    /// </summary>
    public sealed class SaveDataLoadResult
    {
        /// <summary>
        /// 저장 데이터 로드와 복구 처리 상태입니다.
        /// </summary>
        public SaveDataLoadStatus Status { get; }

        /// <summary>
        /// 역직렬화에 사용할 평문 JSON입니다.
        /// </summary>
        public string Json { get; }

        /// <summary>
        /// 사용자에게 안내할 메시지 키입니다.
        /// </summary>
        public string UserMessageKey { get; }

        /// <summary>
        /// 사용자 안내 메시지를 표시해야 하는지 여부입니다.
        /// </summary>
        public bool ShouldShowUserMessage { get; }

        /// <summary>
        /// 새 저장 데이터를 생성해야 하는지 여부입니다.
        /// </summary>
        public bool RequiresNewData => Status == SaveDataLoadStatus.NoSaveFile || Status == SaveDataLoadStatus.NewDataRequired;

        /// <summary>
        /// 로드 또는 복구에 성공했는지 여부입니다.
        /// </summary>
        public bool HasJson => !string.IsNullOrEmpty(Json);

        /// <summary>
        /// 저장 데이터 로드 결과를 생성합니다.
        /// </summary>
        /// <param name="status">로드와 복구 처리 상태입니다.</param>
        /// <param name="json">역직렬화에 사용할 평문 JSON입니다.</param>
        /// <param name="userMessageKey">사용자에게 안내할 메시지 키입니다.</param>
        /// <param name="shouldShowUserMessage">사용자 안내 메시지 표시 여부입니다.</param>
        public SaveDataLoadResult(
            SaveDataLoadStatus status,
            string json = null,
            string userMessageKey = null,
            bool shouldShowUserMessage = false)
        {
            Status = status;
            Json = json;
            UserMessageKey = userMessageKey;
            ShouldShowUserMessage = shouldShowUserMessage;
        }
    }
}
