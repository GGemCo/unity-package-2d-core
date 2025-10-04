using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타이머 기반 함정 (접촉 시 시동 → start → attack → end)
    /// - reuse=true: end 후 일정 시간 뒤 재활성화
    /// - reuse=false: end 후 Destroy
    /// </summary>
    public sealed class ObjectTrapTimer : DefaultObjectTrap, ITrapTriggerController, ITrapAttackRangeHandlerEnter
    {
        public bool IsActive => IsBusy();
        
        [Header("타이밍 설정")]
        [Tooltip("start 애니메이션 종료 후 attack 단계로 진입하기 전까지 추가 대기 시간 (초)")]
        [Min(0f)] [SerializeField] private float timeEndStart;

        [Header("재사용(Reuse)")]
        [Tooltip("End 단계 이후 트랩을 재사용할지 여부.\n- true: 일정 시간 뒤 재활성화\n- false: 오브젝트 파괴")]
        [SerializeField] private bool reuse;

        [Tooltip("reuse가 true일 때, End 단계 종료 후 재시작하기까지의 대기 시간 (초)")]
        [Min(0f)] [SerializeField] private float timeReuse = 1f;

        private void OnEnable()
        {
            phase = TrapPhase.None; ClearAwaiting();
            SetAttackRangeEnabled(false); SetTriggerRangeEnabled(true);
            SetBusy(false); PlayAnimSafe(AnimWait, true);
        }
        private void OnDisable() => SetAttackRangeEnabled(false);
        private void OnDestroy() => CancelInvoke();
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); if (timeEndStart < 0f) timeEndStart = 0f; if (timeReuse < 0f) timeReuse = 0f; }
#endif
        private void Update()
        {
            if (!TryWatchdogExpired(out var expired)) return;
            switch (expired)
            {
                case TrapPhase.StartOneShot: HandleStartFinished(); break;
                case TrapPhase.Attack:       HandleAttackFinished(); break;
                case TrapPhase.EndOneShot:   HandleEndFinished(); break;
            }
        }
        private void EnterPhase(TrapPhase next)
        {
            phase = next; ClearAwaiting();
            switch (next)
            {
                case TrapPhase.StartOneShot:
                    if (hasStart) { PlayAnimSafe(AnimStart); StartAwaiting(next, AnimStart, timeEndStart); }
                    else HandleStartFinished();
                    break;
                case TrapPhase.Attack:
                    if (hasAttack) { PlayAnimSafe(AnimAttack); StartAwaiting(next, AnimAttack, 0f); }
                    else HandleAttackFinished();
                    SetAttackRangeEnabled(true);
                    break;
                case TrapPhase.EndOneShot:
                    if (hasEnd) { PlayAnimSafe(AnimEnd); StartAwaiting(next, AnimEnd, 0f); }
                    else HandleEndFinished();
                    SetAttackRangeEnabled(false);
                    break;
            }
        }
        private void HandleStartFinished()
        { if (phase != TrapPhase.StartOneShot) return; EnterPhase(TrapPhase.Attack); }
        private void HandleAttackFinished()
        { if (phase != TrapPhase.Attack) return; EnterPhase(TrapPhase.EndOneShot); }
        private void HandleEndFinished()
        {
            if (phase != TrapPhase.EndOneShot) return; phase = TrapPhase.None;
            if (reuse) Invoke(nameof(Restart), timeReuse); else Destroy(gameObject);
        }
        private void Restart() { OnEnable(); }

        public void RequestStart(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;
            if (IsBusy()) return; SetBusy(true); SetPlayerInRange(player);
            EnterPhase(TrapPhase.StartOneShot);
        }

        public void RequestEnd()
        {
        }

        public void OnEnter(CharacterBase player)
        {
            if (!player) return;
            if (phase == TrapPhase.Attack) ApplyDamage(player);
        }
    }
}