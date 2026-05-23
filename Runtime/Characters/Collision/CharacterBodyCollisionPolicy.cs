namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 Body Collider끼리 만났을 때 적용할 겹침 방지 정책입니다.
    /// </summary>
    public enum CharacterBodyCollisionPolicy
    {
        /// <summary>
        /// 해당 캐릭터 관계에서는 이동 차단을 수행하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 이동 전 Capsule Cast로 상대 Body Collider를 탐지하고, 충돌 직전까지만 이동합니다.
        /// </summary>
        BlockMovement = 1,
    }
}
