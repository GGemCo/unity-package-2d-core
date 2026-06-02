namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 런타임 초기화 단계에서 명시적으로 호출되는 초기화 계약입니다.
    /// Unity의 Awake/Start 실행 순서에 의존하지 않고, 초기화 파이프라인에서 순서대로 호출하기 위해 사용합니다.
    /// </summary>
    public interface IGameInitializable
    {
        /// <summary>
        /// 같은 초기화 단계 안에서 실행 순서를 정렬하기 위한 값입니다.
        /// 낮은 값이 먼저 실행됩니다.
        /// </summary>
        int InitializeOrder { get; }

        /// <summary>
        /// 게임 실행에 필요한 의존성, 설정, 테이블 데이터를 주입받아 초기화합니다.
        /// </summary>
        /// <param name="context">초기화에 필요한 공통 런타임 컨텍스트입니다.</param>
        void Initialize(GameInitContext context);
    }

    /// <summary>
    /// 모든 초기화가 완료된 뒤 실제 런타임 동작을 시작하는 계약입니다.
    /// 입력, Tick, 이벤트 발행처럼 다른 시스템 준비가 필요한 처리를 이 단계에서 활성화합니다.
    /// </summary>
    public interface IGameActivatable
    {
        /// <summary>
        /// 초기화 완료 이후 런타임 동작을 활성화합니다.
        /// </summary>
        /// <param name="context">초기화가 완료된 공통 런타임 컨텍스트입니다.</param>
        void Activate(GameInitContext context);
    }

    /// <summary>
    /// 씬 종료 또는 게임 종료 시 명시적으로 정리되는 계약입니다.
    /// 이벤트 구독, 캐시, 임시 상태를 안전하게 해제하기 위해 사용합니다.
    /// </summary>
    public interface IGameDeinitializable
    {
        /// <summary>
        /// 초기화/활성화 단계에서 연결한 이벤트와 임시 상태를 정리합니다.
        /// </summary>
        void Deinitialize();
    }
}
