namespace GGemCo2DCore
{
    public class StruckAnimationEventEffect
    {
        public int Uid { get; set; }
        public float Scale { get; set; } = 1.0f;
        public float Duration { get; set; } = 0;
        public string Color { get; set; } = "FFFFFF";
    }
    public class StruckAnimationEventCameraShake
    {
        public float Duration { get; set; }
        public float Magnitude { get; set; }
    }
    public class StruckAnimationEventSkill
    {
        public int Uid { get; set; }
        public int Level { get; set; }
    }
    public class StruckAnimationEventSound
    {
        public int Uid { get; set; }
    }
    public class StruckAnimationEventAttack
    {
        // 공격 받는 대상에게 affect 걸기
        public int TargetAffectUid { get; set; } = 0;
    }
    public static class AnimationConstants
    {
        private const string Prefix = "GGemCoAniEvent";
        public const string EventNameAttack = Prefix+"Attack";
        public const string EventNameSound = Prefix+"Sound";
        public const string EventNameCameraShake = Prefix+"CameraShake";
        public const string EventNameEffect = Prefix+"Effect";
        public const string EventNameProjectile = Prefix+"Projectile";
        public const string EventNameSkill = Prefix+"Skill";
        public const string EventNameJumpUp = Prefix+"JumpUp";
        public const string EventNameJumpFall = Prefix+"JumpFall";
        public const string EventNameJumpEnd = Prefix+"JumpEnd";
    }
}