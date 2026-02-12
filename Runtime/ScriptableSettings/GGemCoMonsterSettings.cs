using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Monster.FileName, menuName = ConfigScriptableObject.Monster.MenuName, order = ConfigScriptableObject.Monster.Ordering)]
    public class GGemCoMonsterSettings : ScriptableObject
    {
        [Tooltip("슈퍼 아머가 0이 되었을 때, 그로기 상태가 됨. 그로기 상태를 유지하는 시간")]
        public float timeGroggy;
        
        [Header("Stacks")]
        [HideInInspector]
        [Min(0)] public int maxIgnoreStacks = 0;
        
        [Header("Regen")]
        [Tooltip("스택이 깎인 이후, 회복을 시작하기까지 대기 시간(초)")]
        [HideInInspector]
        [Min(0f)] public float regenDelay = 0f;

        [Tooltip("회복 틱 간격(초)")]
        [HideInInspector]
        [Min(0.01f)] public float regenInterval = 0f;

        [Tooltip("틱당 회복량(스택)")]
        [HideInInspector]
        [Min(1)] public int regenPerTick = 0;

        [Header("Break")]
        [HideInInspector]
        public CharacterConstants.StaggerBreakResetMode breakResetMode = CharacterConstants.StaggerBreakResetMode.KeepZero;

        [Header("Optional - Anti multi-hit spam")]
        [Tooltip("같은 AttackId가 매우 짧은 시간에 여러 번 들어올 때 스택이 과도하게 깎이는 것을 방지(초). 0이면 비활성.")]
        [HideInInspector]
        [Min(0f)] public float perAttackConsumeCooldown = 0f;
        
        
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
        }
    }
}
