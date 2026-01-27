using System;
using System.Threading.Tasks;

namespace GGemCo2DCore
{
    /// <summary>
    /// MapManager의 로딩 파이프라인에 외부 패키지가 후처리를 연결할 수 있는 Hook 모음.
    /// </summary>
    /// <remarks>
    /// - Core는 외부 패키지(AI/BT 등)를 참조하지 않기 위해, 정적 델리게이트로만 확장 지점을 제공한다.
    /// - Hook 구현은 예외를 내부에서 처리하고, 실패 시에는 호출자가 적절히 로드 실패 처리를 하도록 한다.
    /// </remarks>
    public static class MapLoadHooks
    {
        /// <summary>
        /// MapConstants.State.CreateMonster 단계 이후, 다음 단계로 넘어가기 전에 호출된다.
        /// </summary>
        /// <remarks>
        /// - 몬스터 스폰 직후, AI/BT 에셋 로드, 런타임 컴포넌트 초기화 등 추가 작업이 필요할 때 사용한다.
        /// - 반환된 Task가 완료될 때까지 MapManager는 다음 단계로 진행하지 않는다.
        /// </remarks>
        public static Func<MapTileCommon, Task> AwaitAfterCreateMonsterAsync;
    }
}
