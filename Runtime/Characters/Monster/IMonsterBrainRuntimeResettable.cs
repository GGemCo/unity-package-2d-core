namespace GGemCo2DCore
{
    /// <summary>
    /// 컬링 정책에 따라 몬스터 Brain 런타임 상태를 초기화할 수 있는 기능을 정의합니다.
    /// </summary>
    public interface IMonsterBrainRuntimeResettable
    {
        /// <summary>
        /// 컬링 Fade In 복귀 시점에 Brain 런타임 상태를 초기화합니다.
        /// </summary>
        void ResetRuntimeForCulling();
    }
}
