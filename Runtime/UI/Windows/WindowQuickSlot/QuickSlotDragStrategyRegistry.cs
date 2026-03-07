using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class QuickSlotDragStrategyRegistry
    {
        private static readonly Dictionary<UIWindowConstants.WindowUid, IDragDropStrategy> Strategies = new();

        public static void Register(UIWindowConstants.WindowUid sourceUid, IDragDropStrategy strategy)
        {
            Strategies[sourceUid] = strategy;
        }

        public static bool TryGet(UIWindowConstants.WindowUid sourceUid, out IDragDropStrategy strategy)
        {
            return Strategies.TryGetValue(sourceUid, out strategy);
        }
    }
}