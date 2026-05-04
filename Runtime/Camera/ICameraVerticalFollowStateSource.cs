namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라의 세로 추적 보정이 필요한 상태를 제공하는 인터페이스입니다.
    /// 현재는 점프 중일 때 세로 추적 비율을 낮추는 용도로 사용합니다.
    /// </summary>
    public interface ICameraVerticalFollowStateSource
    {
        /// <summary>
        /// 카메라가 세로 추적 비율 보정을 적용해야 하는 상태인지 반환합니다.
        /// </summary>
        bool IsVerticalFollowInfluenceActive { get; }
    }
}
