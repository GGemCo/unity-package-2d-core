namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 상주 캐릭터가 카메라 컬링에 의해 표시/숨김 처리되는 방식을 정의합니다.
    /// </summary>
    public enum MapCharacterVisibilityPolicy
    {
        /// <summary>
        /// 기존 맵 컬링 규칙을 따릅니다.
        /// 카메라 컬링 영역 안이면 Fade In, 밖이면 Fade Out 됩니다.
        /// </summary>
        DefaultCulling = 0,

        /// <summary>
        /// 카메라 컬링 영역과 관계없이 표시 상태를 유지합니다.
        /// 컷신 종료 후 플레이어를 따라다니거나 계속 대화해야 하는 NPC에 사용합니다.
        /// </summary>
        KeepVisible = 1,

        /// <summary>
        /// 카메라 컬링 영역과 관계없이 숨김 상태를 유지합니다.
        /// 맵에는 등록해 두되 별도 조건이 오기 전까지 노출하지 않을 캐릭터에 사용합니다.
        /// </summary>
        KeepHidden = 2,
    }
}
