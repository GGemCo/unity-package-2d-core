namespace GGemCo2DCore
{
    /// <summary>
    /// HUD 리소스가 피격 기반 피드백을 받을 수 있음을 나타내는 인터페이스입니다.
    /// </summary>
    public interface IHudDamageFeedbackReceiver
    {
        /// <summary>
        /// HUD 피격 피드백을 재생합니다.
        /// </summary>
        void PlayDamageFeedback();
    }
}
