namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 인터랙션 시작 직전에 진행 중인 플레이어 입력 액션을 정리할 수 있는 공용 인터페이스입니다.
    /// Core 패키지는 Control 패키지의 구체 구현을 알지 않고, 이 인터페이스를 통해 점프/대시/가드 등의 잔여 액션 취소를 요청합니다.
    /// </summary>
    public interface IInteractionActionCanceler
    {
        /// <summary>
        /// NPC 인터랙션 시작 전에 플레이어 조작 상태와 입력 버퍼를 정리합니다.
        /// </summary>
        void CancelActionsOnInteractionStart();
    }
}
