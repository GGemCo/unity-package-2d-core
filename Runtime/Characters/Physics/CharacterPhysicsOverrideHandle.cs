namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterPhysicsOverrideController"/>가 발급하는 물리 오버라이드 핸들입니다.
    /// </summary>
    public readonly struct CharacterPhysicsOverrideHandle
    {
        public int Id { get; }

        public bool IsValid => Id > 0;

        public CharacterPhysicsOverrideHandle(int id)
        {
            Id = id;
        }
    }
}
