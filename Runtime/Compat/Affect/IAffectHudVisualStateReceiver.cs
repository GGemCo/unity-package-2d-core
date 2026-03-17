namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 상태를 HUD 시각 상태 키로 받아 표현하는 수신자 인터페이스입니다.
    /// </summary>
    public interface IAffectHudVisualStateReceiver
    {
        /// <summary>
        /// 지정한 상태 키에 대응하는 HUD 시각 상태를 적용합니다.
        /// </summary>
        void SetAffectVisualState(string stateKey);

        /// <summary>
        /// HUD 시각 상태를 기본값으로 되돌립니다.
        /// </summary>
        void ResetAffectVisualState();
    }
}
