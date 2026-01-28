using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 동일 GameObject에 여러 Brain이 있을 때, 가장 높은 우선순위의 활성 Brain 1개만 틱을 수행하도록 하는 중앙 틱커.
    /// </summary>
    /// <remarks>
    /// - Brain 자체 Update/FixedUpdate를 사용하지 않고, 본 틱커를 통해 틱을 표준화한다.
    /// - 확장 패키지(BT 등)는 Core를 참조하여 <see cref="IMonsterBrainTickable"/>을 구현하면 된다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MonsterBrainTicker : MonoBehaviour
    {
        private static readonly List<IMonsterBrainTickable> Tickables = new List<IMonsterBrainTickable>(8);

#if GGEMCO_2D_CONTROL
        private void FixedUpdate()
#else
        private void Update()
#endif
        {
            if (!isActiveAndEnabled) return;

            if (!MonsterBrainSelector.TryGetHighestActiveTickable(gameObject, Tickables, out var brain))
            {
                return;
            }

            brain.Tick();
        }
    }
}
