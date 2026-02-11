namespace GGemCo2DCore
{
    /// <summary>
    /// 자동 이동이 활성화된 동안, "실제 이동 실행(Run/Move)"을 어떤 시스템이 담당하는지 Core에 알리는 계약입니다.
    /// 
    /// - Control 패키지(InputManager)가 이동을 실행하는 경우: true
    /// - Core 단독(Old/New Input 또는 별도 이동 시스템)으로 AutoMoveController가 직접 Run()을 실행해야 하는 경우: false
    /// 
    /// 이 인터페이스는 컴포넌트 존재/속성만으로 판정할 수 있도록 설계되어,
    /// PlayerInput의 부착 위치(루트/자식)나 입력 시스템 구성에 따라 오판정되는 문제를 방지합니다.
    /// </summary>
    public interface IAutoMoveMovementDriver
    {
        /// <summary>
        /// true이면, AutoMoveController는 Run()을 직접 호출하지 않고 완료 조건(거리/시간)만 판정합니다.
        /// </summary>
        bool DrivesAutoMoveMovement { get; }
    }
}
