namespace GGemCo2DCore
{
    public static class EffectConstants
    {
        /// <summary>
        /// 추가시 BuildEffectPath 여기에도 추가해야 함
        /// </summary>
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
    }
}
