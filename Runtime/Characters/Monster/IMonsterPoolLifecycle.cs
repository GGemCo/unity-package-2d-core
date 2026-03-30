
namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 풀 대여/반납 시점에 상위 패키지와 Core 런타임 컴포넌트가
    /// 상태를 정리하거나 재초기화할 수 있도록 제공하는 공용 포트입니다.
    /// </summary>
    public interface IMonsterPoolLifecycle
    {
        /// <summary>
        /// 풀에서 대여된 직후 호출됩니다.
        /// </summary>
        /// <param name="owner">현재 대여된 몬스터 인스턴스입니다.</param>
        void OnPoolRent(Monster owner);

        /// <summary>
        /// 풀로 반납되기 직전에 호출됩니다.
        /// </summary>
        /// <param name="owner">현재 반납되는 몬스터 인스턴스입니다.</param>
        void OnPoolReturn(Monster owner);
    }
}
