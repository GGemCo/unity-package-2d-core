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
        private IMonsterBrainSuspendProvider _suspendProvider;

        /// <summary>
        /// 동일 오브젝트에 있는 Brain 일시정지 제공자를 캐싱합니다.
        /// </summary>
        private void Awake()
        {
            _suspendProvider = GetComponent<IMonsterBrainSuspendProvider>();
        }

#if GGEMCO_2D_CONTROL
        private void FixedUpdate()
#else
        private void Update()
#endif
        {
            if (!isActiveAndEnabled) return;
            if (ShouldSuspendBrain()) return;

            if (!MonsterBrainSelector.TryGetHighestActiveTickable(gameObject, Tickables, out var brain))
            {
                return;
            }

            brain.Tick();
        }

        /// <summary>
        /// 현재 몬스터 Brain 틱을 건너뛰어야 하는지 확인합니다.
        /// </summary>
        /// <returns>외부 잠금 또는 캐릭터 상태로 인해 Brain을 멈춰야 하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldSuspendBrain()
        {
            if (_suspendProvider != null)
            {
                return _suspendProvider.ShouldSuspendBrain;
            }

            _suspendProvider = GetComponent<IMonsterBrainSuspendProvider>();
            return _suspendProvider != null && _suspendProvider.ShouldSuspendBrain;
        }
    }
}
