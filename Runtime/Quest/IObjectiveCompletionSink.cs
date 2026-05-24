namespace GGemCo2DCore
{
    /// <summary>
    /// 퀘스트 목표 처리기가 완료 요청을 소유 퀘스트 매니저로 전달하기 위한 계약입니다.
    /// </summary>
    public interface IObjectiveCompletionSink
    {
        /// <summary>
        /// 지정한 퀘스트의 현재 목표 완료를 요청합니다.
        /// </summary>
        /// <param name="questUid">완료할 퀘스트 UID입니다.</param>
        void CompleteObjective(int questUid);
    }
}
