namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터별 MP 증가 정책을 외부 컴포넌트가 대체하기 위한 포트입니다.
    /// </summary>
    /// <remarks>
    /// Core 기본 규칙은 <see cref="CharacterStat.AddMp"/>를 사용하지만, 게임별 MP 상한이나 UI 동기화 정책이
    /// 필요한 경우 이 인터페이스를 구현한 컴포넌트가 우선 처리합니다.
    /// </remarks>
    public interface ICharacterMpGainReceiver
    {
        /// <summary>
        /// 지정한 양만큼 MP 증가를 시도합니다.
        /// </summary>
        /// <param name="amount">증가할 MP 양입니다.</param>
        /// <returns>실제로 MP가 변경되었으면 <see langword="true"/>입니다.</returns>
        bool TryAddMp(int amount);
    }
}
