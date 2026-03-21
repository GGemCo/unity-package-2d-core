namespace GGemCo2DCore
{
    public static class VfxConstants
    {
        public enum AssetKind
        {
            None,
            Effect,
            Particle,
        }

        public enum Category
        {
            None,
            Common,
            Skill,
            Player,
            Monster,
            UI,
            Etc
        }

        public enum Type
        {
            None,
            Default,
            Laser
        }

        public enum PlaybackType
        {
            Auto,
            SpriteSequence,
            SpineSequence,
            ParticleSystem,
            Laser,
        }

        public enum LifecycleType
        {
            AutoRelease,
            Duration,
            ManualRelease,
        }

        public enum AttachType
        {
            World,
            Owner,
            Target,
            UI,
        }

        public enum FollowMode
        {
            None,
            Position,
            PositionAndFlip,
        }
    }
}
