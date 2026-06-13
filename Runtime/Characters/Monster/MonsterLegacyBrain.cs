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

        /// <summary>
        /// 몬스터 공격 범위 Trigger에 플레이어가 진입했을 때 선공 전투 시작 정책을 몬스터에게 위임합니다.
        /// </summary>
        /// <param name="collision">공격 범위에 진입한 Collider입니다.</param>
        public void OnCharacterTriggerEnter(Collider2D collision)
        {
            if (!IsActive) return;
            if (collision == null) return;

            if (!collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            if (_controller.targetCharacter == null || _controller.targetCharacter.IsStatusDead()) return;

            if (_controller.IsAggro &&
                _controller.TryGetTarget(out Transform currentTarget) &&
                currentTarget != null)
            {
                // Trigger 진입은 감지만 담당한다.
                // 실제 공격 시작은 TickLegacy()에서 공통 제어 가능 여부를 확인한 뒤 결정한다.
                return;
            }

            if (_controller.targetCharacter is not Monster monster) return;

            // 신규 전투 범위 프로필이 활성화된 몬스터는 MonsterDetectionSensor2D가 선공 감지를 전담합니다.
            // 이 Trigger는 구형 프리팹에서 논리 감지 범위를 만들 수 없는 경우에만 호환 경로로 사용합니다.
            if (monster.CombatRangeProfile.IsDetectionEnabled) return;

            Player player = collision.GetComponentInParent<Player>();
            if (player == null) return;

            monster.OnDetectedPlayerByAttackRange(player);
        }

        /// <summary>
        /// 구형 공격 범위 Trigger에서 플레이어가 이탈했을 때 레거시 공격 코루틴을 정리합니다.
        /// </summary>
        /// <param name="collision">구형 공격 범위에서 이탈한 Collider입니다.</param>
        /// <remarks>
        /// 신규 범위 프로필에서는 실제 피해 판정 Collider 이탈이 AI 공격 상태를 변경하지 않습니다.
        /// 논리 기본 공격 범위 이탈은 <see cref="ControllerMonster.TickLegacy"/>에서 처리합니다.
        /// </remarks>
        public void OnCharacterTriggerExit(Collider2D collision)
        {
            if (!IsActive || collision == null) return;
            if (_controller.targetCharacter is Monster monster && monster.CombatRangeProfile.IsDetectionEnabled) return;

            if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                _controller.StopAttackCoroutine();
            }
        }
    }
}
