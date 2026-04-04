namespace GGemCo2DCore
{
    /// <summary>
    /// HUD 리소스가 피격 기반 피드백을 받을 수 있음을 나타내는 인터페이스입니다.
    /// </summary>
    public interface IHudDamageFeedbackReceiver
    {
        /// <summary>
        /// HUD 피격 피드백을 프리셋 기본 방향으로 재생합니다.
        /// </summary>
        void PlayDamageFeedback();

        /// <summary>
        /// HUD 피격 피드백을 지정한 흔들림 방향으로 재생합니다.
        /// </summary>
        /// <param name="directionMode">런타임에서 지정할 흔들림 방향입니다.</param>
        void PlayDamageFeedback(UIEffectShakeDirectionMode directionMode);
    }
}
