using System.Collections;

namespace GGemCo2DCore
{
    public abstract class GameLoadStepBase : IGameLoadStep
    {
        public string Id { get; }
        public int Order { get; }
        public string LocalizedKey { get; }

        protected float progress; // 0~1

        protected GameLoadStepBase(string id, int order, string localizedKey)
        {
            Id = id;
            Order = order;
            LocalizedKey = localizedKey;
            progress = 0f;
        }

        public float GetProgress() => progress;

        public abstract IEnumerator Run();
    }
}