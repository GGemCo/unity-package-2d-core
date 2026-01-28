using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 동일 GameObject에 여러 <see cref="IMonsterBrain"/>이 존재할 때, 실제로 동작할 Brain을 선택한다.
    /// </summary>
    public static class MonsterBrainSelector
    {
        // GetComponents(List) 오버로드를 사용하여 GC를 최소화한다.
        private static readonly List<IMonsterBrain> Brains = new List<IMonsterBrain>(8);
        private static readonly List<IMonsterBrainTickable> Tickables = new List<IMonsterBrainTickable>(8);

        /// <summary>
        /// 지정한 Brain이 현재 오브젝트에서 가장 높은 우선순위의 활성 Brain인지 판단한다.
        /// </summary>
        public static bool IsHighestPriority(IMonsterBrain brain, GameObject owner)
        {
            if (brain == null || owner == null) return false;
            if (!brain.IsActive) return false;

            Brains.Clear();
            owner.GetComponents(Brains);

            return TrySelectBest(Brains, out var best) && ReferenceEquals(best, brain);
        }

        /// <summary>
        /// 현재 오브젝트에서 가장 높은 우선순위의 활성 Brain을 반환한다.
        /// </summary>
        public static bool TryGetHighestActiveBrain(GameObject owner, out IMonsterBrain brain)
        {
            brain = null;
            if (owner == null) return false;

            Brains.Clear();
            owner.GetComponents(Brains);

            return TrySelectBest(Brains, out brain);
        }

        /// <summary>
        /// 현재 오브젝트에서 가장 높은 우선순위의 활성 Tickable Brain을 반환한다.
        /// </summary>
        /// <remarks>
        /// - 내부 리스트를 외부에서 재사용할 수 있도록, 임시 리스트를 인자로 받을 수 있다.
        /// </remarks>
        public static bool TryGetHighestActiveTickable(GameObject owner, List<IMonsterBrainTickable> temp, out IMonsterBrainTickable brain)
        {
            brain = null;
            if (owner == null) return false;

            var list = temp ?? Tickables;
            list.Clear();
            owner.GetComponents(list);

            return TrySelectBest(list, out brain);
        }

        /// <summary>
        /// (호환용) Unity의 GetComponent 결과를 그대로 반환한다. 우선순위 선택을 보장하지 않으므로 새 코드에서는 사용을 권장하지 않는다.
        /// </summary>
        public static IMonsterBrain GetBrain(GameObject gameObject)
        {
            return gameObject != null ? gameObject.GetComponent<IMonsterBrain>() : null;
        }

        private static bool TrySelectBest<T>(List<T> list, out T best) where T : class, IMonsterBrain
        {
            best = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null || !b.IsActive) continue;

                int p = b.Priority;
                if (p > bestPriority)
                {
                    bestPriority = p;
                    best = b;
                }
            }

            return best != null;
        }
    }
}
