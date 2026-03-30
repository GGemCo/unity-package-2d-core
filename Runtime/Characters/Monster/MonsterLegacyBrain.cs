using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기존(레거시) 몬스터 AI 의사결정을 수행하는 Brain.
    /// </summary>
    /// <remarks>
    /// - 실행(이동/공격)은 <see cref="ControllerMonster"/>가 담당한다.
    /// - 본 클래스는 "무엇을 할지" 판단하고 <see cref="ControllerMonster.TickLegacy"/>를 호출한다.
    /// - BT 등 외부 Brain이 붙으면 <see cref="IMonsterBrain.Priority"/> 우선순위에 의해 자동으로 억제된다.
    /// - Brain 틱은 <see cref="MonsterBrainTicker"/>가 담당한다(본 클래스는 Update/FixedUpdate를 사용하지 않는다).
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ControllerMonster))]
    public sealed class MonsterLegacyBrain : MonoBehaviour, IMonsterBrainTickable
    {
        // BT보다 낮게 설정한다.
        public int Priority => 0;

        public bool IsActive => enabled && isActiveAndEnabled;

        private ControllerMonster _controller;

        private void Awake()
        {
            _controller = GetComponent<ControllerMonster>();
        }

        public void Tick()
        {
            if (_controller == null) return;
            if (!MonsterBrainSelector.IsHighestPriority(this, gameObject)) return;

            // Hit Stop 중에는 레거시 Brain 의사결정도 완전히 멈춘다.
            // 기존에도 TickLegacy() 내부 CheckPossibleControl()이 DontControl 상태에서 차단하지만,
            // 여기서 선차단하면 불필요한 Tick/공격 코루틴 정리 호출을 줄일 수 있다.
            if (_controller.targetCharacter != null)
            {
                if (_controller.targetCharacter.IsHitStopped) return;
                if (_controller.targetCharacter.IsStatusDead()) return;
            }

            if (_controller.ShouldSuspendBrain) return;

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
                    // Trigger 진입은 감지만 담당한다.
                    // 실제 공격 시작은 TickLegacy()에서 공통 제어 가능 여부를 확인한 뒤 결정한다.
                    return;
                }
                // 선공
                else if (_controller.targetCharacter.GetAttackType() == CharacterConstants.AttackType.AggroFirst &&
                         _controller.targetCharacter.IsAggro() == false)
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
