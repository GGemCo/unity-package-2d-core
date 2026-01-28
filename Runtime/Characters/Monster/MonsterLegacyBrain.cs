using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기존(레거시) 몬스터 AI 의사결정을 수행하는 Brain.
    /// </summary>
    /// <remarks>
    /// - 실제 이동/공격 실행은 <see cref="ControllerMonster"/>가 담당하며,
    ///   본 클래스는 "무엇을 할지" 판단하고 ControllerMonster의 레거시 틱을 호출한다.
    /// - BT 등 외부 Brain이 붙으면 <see cref="IMonsterBrain.Priority"/> 우선순위에 의해 자동으로 억제된다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ControllerMonster))]
    public sealed class MonsterLegacyBrain : MonoBehaviour, IMonsterBrain
    {
        // BT보다 낮게 설정한다.
        public int Priority => 0;

        public bool IsActive => enabled && isActiveAndEnabled;

        private ControllerMonster _controller;

        private void Awake()
        {
            _controller = GetComponent<ControllerMonster>();
        }

#if GGEMCO_2D_CONTROL
        private void FixedUpdate()
#else
        private void Update()
#endif
        {
            if (_controller == null) return;
            if (!MonsterBrainSelector.IsHighestPriority(this, gameObject)) return;

            _controller.TickLegacy();
        }
        public void OnCharacterTriggerEnter(Collider2D collision)
        {
            if (!IsActive) return;

            if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                if (_controller.targetCharacter.IsStatusDead()) return;
                
                if (_controller.targetCharacter.IsAggro() && _controller.targetCharacter.attackerTransform != null)
                {
                    _controller.Attack();
                }
                // 선공
                else if (_controller.targetCharacter.GetAttackType() == CharacterConstants.AttackType.AggroFirst && _controller.targetCharacter.IsAggro() == false)
                {
                    _controller.targetCharacter.SetAggro(true);
                    _controller.targetCharacter.SetAttackerTarget(collision.gameObject.transform);
                }
            }
        }

        public void OnCharacterTriggerExit(Collider2D collision)
        {
            if (!IsActive) return;

            if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                _controller.StopAttackCoroutine();
            }
        }

    }
}
