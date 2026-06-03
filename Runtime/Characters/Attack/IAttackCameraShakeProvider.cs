namespace GGemCo2DCore
{
    /// <summary>
    /// 현재 실행 중인 기본 공격의 카메라 Shake 설정을 제공하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Control 패키지의 기본 공격 콤보 설정을 직접 참조하지 않으므로,
    /// 상위 입력/액션 계층이 현재 콤보 단계의 카메라 Shake 설정만 전달합니다.
    /// </remarks>
    public interface IAttackCameraShakeProvider
    {
        /// <summary>
        /// 현재 기본 공격에 사용할 카메라 Shake 설정을 조회합니다.
        /// </summary>
        /// <param name="settings">조회된 카메라 Shake 설정입니다.</param>
        /// <returns>현재 공격에 유효한 카메라 Shake 설정이 있으면 <see langword="true"/>를 반환합니다.</returns>
        bool TryGetCurrentAttackCameraShakeSettings(out AttackCameraShakeSettings settings);
    }
}
