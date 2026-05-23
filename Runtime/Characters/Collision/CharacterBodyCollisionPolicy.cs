namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 Body Collider끼리 만났을 때 적용할 이동 차단과 겹침 해소 정책입니다.
    /// </summary>
    public enum CharacterBodyCollisionPolicy
    {
        /// <summary>
        /// 해당 캐릭터 관계에서는 Body 충돌 처리를 수행하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 이동 전 Capsule Cast로 상대 Body Collider를 탐지하고, 충돌 직전까지만 이동합니다.
        /// </summary>
        BlockMovement = 1,

        /// <summary>
        /// 이미 겹친 Body Collider를 감지하면 여러 프레임에 걸쳐 부드럽게 분리합니다.
        /// </summary>
        SeparateWhenOverlapped = 2,

        /// <summary>
        /// 이동 전 차단과 겹침 해소를 모두 수행합니다.
        /// </summary>
        BlockAndSeparate = 3,
    }
}
