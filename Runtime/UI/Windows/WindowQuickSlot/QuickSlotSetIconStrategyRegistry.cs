using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class QuickSlotSetIconStrategyRegistry
    {
        private static readonly Dictionary<IconConstants.Type, ISetIconHandler> Strategies = new();

        public static void Register(IconConstants.Type iconType, ISetIconHandler strategy)
        {
            Strategies[iconType] = strategy;
        }

        public static bool TryGet(IconConstants.Type iconType, out ISetIconHandler strategy)
        {
            return Strategies.TryGetValue(iconType, out strategy);
        }
    }
}