namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 실행 중인 공격의 HitStop 설정을 제공하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Control 패키지의 공격 콤보 설정을 직접 참조하지 않기 때문에,
    /// 상위 패키지는 이 인터페이스를 구현하여 현재 공격 설정만 전달합니다.
    /// </remarks>
    public interface IAttackHitStopProvider
    {
        /// <summary>
        /// 현재 공격에 사용할 HitStop 설정을 조회합니다.
        /// </summary>
        /// <param name="settings">조회된 HitStop 설정입니다.</param>
        /// <returns>현재 공격에 유효한 HitStop 설정이 있으면 <see langword="true"/>를 반환합니다.</returns>
        bool TryGetCurrentAttackHitStopSettings(out AttackHitStopSettings settings);
    }
}
