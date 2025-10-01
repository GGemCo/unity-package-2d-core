using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 고정형 함정 오브젝트 (시작→공격→종료 1회성)
    /// - Animator 또는 Spine 중 하나로 "start/attack/end" 클립(또는 트랙)을 사용합니다.
    /// - 공격 판정은 Trigger Collider2D(attackRange)로 수행합니다.
    /// </summary>
    public sealed class ObjectTrapFixed : DefaultObjectTrap, ITrapAttackRangeHandlerEnter
    {
        // ----------------------------
        // Serialized Settings (Designer)
        // ----------------------------
        [Header("타이밍 설정")] 
        [Tooltip("start 애니메이션 이후 다음 단계로 넘어가기 전 대기(초)")] 
        [Min(0f)] [SerializeField] private float timeEndStart;
        [Tooltip("attack 애니메이션 이후 다음 단계로 넘어가기 전 대기(초)")]
        [Min(0f)] [SerializeField] private float timeEndAttack;
        [Tooltip("전체 사이클(시작→공격→종료) 완료 후 재시작까지 대기(초). 0이면 반복 안 함")]
        [Min(0f)] [SerializeField] private float timeRepeat;

        // 애니 이벤트 누락 대비 워치독
        private TrapPhase _awaitingPhase = TrapPhase.None;
        private float _awaitingDeadline;

        // 반복 코루틴 핸들
        private Coroutine _repeatCo;

        // ----------------------------
        // Lifecycle
        // ----------------------------

        private void OnEnable()
        {
            // 상태 초기화
            phase = TrapPhase.None;
            _awaitingPhase = TrapPhase.None;
            _awaitingDeadline = 0f;
        }

        private void Start()
        {
            BeginCycleOnce();
        }

        private void OnDisable()
        {
            if (_repeatCo != null)
            {
                StopCoroutine(_repeatCo);
                _repeatCo = null;
            }
            CancelInvoke(nameof(BeginCycleOnce));
            SetAttackRangeEnabled(false);
        }

        private void OnDestroy()
        {
            // 코루틴 안전 종료
            if (_repeatCo != null)
            {
                StopCoroutine(_repeatCo);
                _repeatCo = null;
            }
            CancelInvoke();
        }

        // ----------------------------
        // Public/Editor Helpers
        // ----------------------------

        /// <summary>에디터/런타임에서 1회 사이클을 시작합니다.</summary>
        [ContextMenu("Begin Cycle Once")]
        public void BeginCycleOnce()
        {
            SetAttackRangeEnabled(false);
            EnterPhase(TrapPhase.StartOneShot);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            // 에디터에서 음수 방지 클램프
            if (timeEndStart < 0f) timeEndStart = 0f;
            if (timeEndAttack < 0f) timeEndAttack = 0f;
            if (timeRepeat < 0f) timeRepeat = 0f;
        }
#endif

        // ----------------------------
        // Phase State Machine
        // ----------------------------
        private void EnterPhase(TrapPhase next)
        {
            phase = next;
            ClearAwaiting();

            switch (next)
            {
                case TrapPhase.StartOneShot:
                    if (hasStart)
                    {
                        PlayAnimSafe(AnimStart);
                        StartAwaiting(next, AnimStart, timeEndStart);
                    }
                    else
                    {
                        // 폴백: 애니 없음 → 즉시 다음 단계
                        HandleStartFinished();
                    }
                    break;

                case TrapPhase.Attack:
                    if (hasAttack)
                    {
                        PlayAnimSafe(AnimAttack);
                        StartAwaiting(next, AnimAttack, timeEndAttack);
                    }
                    else
                    {
                        HandleAttackFinished();
                    }
                    // 공격 판정 활성
                    SetAttackRangeEnabled(true);
                    break;

                case TrapPhase.EndOneShot:
                    if (hasEnd)
                    {
                        PlayAnimSafe(AnimEnd);
                        StartAwaiting(next, AnimEnd, 0f);
                    }
                    else
                    {
                        HandleEndFinished();
                    }
                    // 공격 판정 비활성
                    SetAttackRangeEnabled(false);
                    break;
            }
        }

        private void Update()
        {
            // 워치독: 애니 이벤트 누락/길이 미보고 시 타임아웃으로 다음 처리
            if (_awaitingPhase != TrapPhase.None && Time.time >= _awaitingDeadline)
            {
                switch (_awaitingPhase)
                {
                    case TrapPhase.StartOneShot: HandleStartFinished(); break;
                    case TrapPhase.Attack:       HandleAttackFinished(); break;
                    case TrapPhase.EndOneShot:   HandleEndFinished(); break;
                }
            }
        }

        private void HandleStartFinished()
        {
            if (phase != TrapPhase.StartOneShot) return;
            ClearAwaiting();
            EnterPhase(TrapPhase.Attack);
        }

        private void HandleAttackFinished()
        {
            if (phase != TrapPhase.Attack) return;
            ClearAwaiting();

            EnterPhase(TrapPhase.EndOneShot);
        }

        private void HandleEndFinished()
        {
            if (phase != TrapPhase.EndOneShot) return;
            ClearAwaiting();
            phase = TrapPhase.None;

            // 반복 스케줄
            if (timeRepeat > 0f && gameObject.activeInHierarchy)
            {
                if (_repeatCo != null) StopCoroutine(_repeatCo);
                _repeatCo = StartCoroutine(CoRepeat());
            }
        }

        private IEnumerator CoRepeat()
        {
            yield return new WaitForSeconds(timeRepeat);
            BeginCycleOnce();
            _repeatCo = null;
        }

        private void StartAwaiting(TrapPhase phase, string clipName, float extraDelay)
        {
            _awaitingPhase = phase;
            _awaitingDeadline = Time.time + GetClipDuration(clipName) + extraDelay;
        }

        private void ClearAwaiting()
        {
            _awaitingPhase = TrapPhase.None;
            _awaitingDeadline = 0f;
        }
        public void OnEnter(CharacterBase player)
        {
            if (!player) return;
            
            // 1회성 공격(즉시 일격) 모델이 필요하면 여기서 적용
            if (phase == TrapPhase.Attack)
            {
                ApplyDamage(player);
            }
        }
        public void OnStay(CharacterBase player)
        {
        }
    }
}
