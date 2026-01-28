using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 동일 GameObject에 여러 <see cref="IMonsterBrain"/>이 존재할 때, 실제로 틱을 수행할 Brain을 선택한다.
    /// </summary>
    public static class MonsterBrainSelector
    {
        // GetComponents(List) 오버로드를 사용하여 GC를 최소화한다.
        private static readonly List<IMonsterBrain> Brains = new List<IMonsterBrain>(8);

        /// <summary>
        /// 지정한 Brain이 현재 오브젝트에서 가장 높은 우선순위의 활성 Brain인지 판단한다.
        /// </summary>
        public static bool IsHighestPriority(IMonsterBrain brain, GameObject owner)
        {
            if (brain == null || owner == null) return false;
            if (!brain.IsActive) return false;

            Brains.Clear();
            owner.GetComponents(Brains);

            int bestPriority = int.MinValue;
            IMonsterBrain best = null;

            for (int i = 0; i < Brains.Count; i++)
            {
                var b = Brains[i];
                if (b == null || !b.IsActive) continue;

                int p = b.Priority;
                if (p > bestPriority)
                {
                    bestPriority = p;
                    best = b;
                }
            }

            return ReferenceEquals(best, brain);
        }

        public static IMonsterBrain GetBrain(GameObject gameObject)
        {
            return gameObject.GetComponent<IMonsterBrain>();
        }
    }
}
