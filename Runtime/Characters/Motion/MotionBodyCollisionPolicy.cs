namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterMotionController2D"/>가 적용하는 모션 이동 중 캐릭터 Body 충돌 처리 정책입니다.
    /// </summary>
    public enum MotionBodyCollisionPolicy
    {
        /// <summary>
        /// <see cref="CharacterCollisionSettings"/>에 정의된 채널별 기본 정책을 사용합니다.
        /// </summary>
        UseCharacterDefault = 0,

        /// <summary>
        /// 모션 이동 중 캐릭터 Body 충돌 차단과 겹침 해소를 모두 수행하지 않습니다.
        /// </summary>
        Ignore = 1,

        /// <summary>
        /// 모션 이동 적용 전에 캐릭터 Body와의 충돌을 검사하여 새 겹침 진입을 차단합니다.
        /// </summary>
        BlockBeforeMove = 2,

        /// <summary>
        /// 모션 이동 적용 후 이미 겹친 Body를 여러 FixedUpdate에 걸쳐 자연스럽게 분리합니다.
        /// </summary>
        SeparateAfterMove = 3,

        /// <summary>
        /// 모션 이동 전 차단과 이동 후 겹침 해소를 모두 수행합니다.
        /// </summary>
        BlockAndSeparate = 4,
    }
}
