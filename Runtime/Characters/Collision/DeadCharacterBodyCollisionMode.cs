namespace GGemCo2DCore
{
    /// <summary>
    /// 사망한 캐릭터의 Body Collider를 캐릭터 충돌 시스템에서 처리하는 방식을 정의합니다.
    /// </summary>
    public enum DeadCharacterBodyCollisionMode
    {
        /// <summary>
        /// 사망 후에도 기존 Body 충돌 정책을 그대로 유지합니다.
        /// </summary>
        Keep = 0,

        /// <summary>
        /// 사망 캐릭터를 이동 차단과 겹침 해소 검사에서만 제외합니다.
        /// Body Collider 컴포넌트 자체는 유지합니다.
        /// </summary>
        IgnoreInCharacterCollision = 1,

        /// <summary>
        /// 사망 시 Body Collider를 비활성화하여 물리 검사 대상에서도 제외합니다.
        /// 풀 재사용 또는 부활 시 기존 활성 상태로 복원됩니다.
        /// </summary>
        DisableBodyCollider = 2,
    }
}
