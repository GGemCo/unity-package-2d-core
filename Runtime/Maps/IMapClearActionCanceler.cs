namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 클리어가 확정되었을 때 진행 중인 플레이어 조작 상태를 정리할 수 있는 공용 인터페이스입니다.
    /// Core 패키지는 Control 패키지의 구체 구현을 알지 않고, 이 인터페이스를 통해 자동 이동과 입력 액션 정리를 요청합니다.
    /// </summary>
    public interface IMapClearActionCanceler
    {
        /// <summary>
        /// 맵 클리어 종료 연출이 시작되기 전에 자동 이동 요청과 잔여 입력 액션을 취소합니다.
        /// </summary>
        void CancelActionsOnMapClear();
    }
}
