using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 고정형 함정 오브젝트 (시작→공격→종료 1회성, timeRepeat로 반복 가능)
    /// - Animator/Spine 클립: start/attack/end
    /// - 워치독(애니 이벤트 누락 대비) 공통 유틸 사용
    /// </summary>
    public sealed class ObjectTrapFixed : DefaultObjectTrap, ITrapAttackRangeHandlerEnter
    {
        [Header("타이밍 설정")]
        [Tooltip("start 애니메이션 종료 후 Attack 단계로 넘어가기 전까지의 추가 대기 시간 (초)")]
        [Min(0f)] [SerializeField] private float timeEndStart;

        [Tooltip("attack 애니메이션 종료 후 End 단계로 넘어가기 전까지의 추가 대기 시간 (초)")]
        [Min(0f)] [SerializeField] private float timeEndAttack;

        [Tooltip("전체 사이클(start→attack→end) 완료 후 다시 시작하기 전까지의 대기 시간 (초). 0이면 반복하지 않음")]
        [Min(0f)] [SerializeField] private float timeRepeat;

        private Coroutine _repeatCo;

        private void OnEnable()
        { phase = TrapPhase.None; ClearAwaiting(); }
        private void Start() { BeginCycleOnce(); }
        private void OnDisable()
        {
            if (_repeatCo != null) { StopCoroutine(_repeatCo); _repeatCo = null; }
            CancelInvoke(nameof(BeginCycleOnce));
            SetAttackRangeEnabled(false);
        }
        private void OnDestroy()
        {
            if (_repeatCo != null) { StopCoroutine(_repeatCo); _repeatCo = null; }
            CancelInvoke();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (timeEndStart < 0f) timeEndStart = 0f;
            if (timeEndAttack < 0f) timeEndAttack = 0f;
            if (timeRepeat < 0f) timeRepeat = 0f;
        }
#endif
        public void BeginCycleOnce()
        {
            SetAttackRangeEnabled(false);
            EnterPhase(TrapPhase.StartOneShot);
        }

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
                    if (hasAttack) { PlayAnimSafe(AnimAttack); StartAwaiting(next, AnimAttack, timeEndAttack); }
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
            if (timeRepeat > 0f && gameObject.activeInHierarchy)
            { if (_repeatCo != null) StopCoroutine(_repeatCo); _repeatCo = StartCoroutine(CoRepeat()); }
        }
        private IEnumerator CoRepeat() { yield return new WaitForSeconds(timeRepeat); BeginCycleOnce(); _repeatCo = null; }

        public void OnEnter(CharacterBase player)
        { if (player && phase == TrapPhase.Attack) ApplyDamage(player); }
    }
}