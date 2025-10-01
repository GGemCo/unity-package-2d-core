using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타이머 기반 함정 (접촉 시 시동 → start → attack → end)
    /// - 플레이어의 HitArea가 트리거에 진입하면 StartOneShot 단계로 진입합니다.
    /// - start 대기 후 attack(공격), end(정리)를 거쳐 재사용(reuse)/파괴 흐름을 수행합니다.
    /// - 애니 이벤트 누락을 대비해 워치독(타임아웃)으로 안전하게 다음 단계로 넘어갑니다.
    /// </summary>
    public sealed class ObjectTrapTimer : DefaultObjectTrap
    {
        // ----------------------------
        // Serialized Settings (Designer)
        // ----------------------------

        [Header("타이밍 설정")]
        [Tooltip("start 애니메이션 종료 후 다음 단계(attack)로 넘어가기 전 추가 대기(초)")]
        [Min(0f)]
        [SerializeField] private float timeEndStart;

        [Space(6)]
        [Header("재사용(Reuse) 설정")]
        [Tooltip("true면 end 이후 오브젝트를 비활성화했다가 일정 시간 뒤 재활성화합니다.\nfalse면 end 이후 파괴(Destroy)합니다.")]
        [SerializeField] private bool reuse;

        [Tooltip("reuse가 true일 때, 다시 활성화(재시작)되기까지의 대기 시간(초)")]
        [Min(0f)]
        [SerializeField] private float timeReuse = 1f;

        // ----------------------------
        // Internal: Watchdog for animation events
        // ----------------------------
        private TrapPhase _awaitingPhase = TrapPhase.None;
        private float _awaitingDeadline;

        // ----------------------------
        // Unity Lifecycle
        // ----------------------------

        private void OnEnable()
        {
            // 상태 초기화
            phase = TrapPhase.None;
            ClearAwaiting();

            // 공격 콜라이더는 기본 비활성, 진입 판정용 트리거는 활성
            SetAttackRangeEnabled(false);
            SetTriggerRangeEnabled(true);

            // 외부 재진입(중복 트리거 방지) 플래그 리셋
            SetBusy(false);

            // 대기 애니(연속 루프)
            PlayAnimSafe(AnimWait, true);
        }

        private void OnDisable()
        {
            // 안전상 공격 콜라이더는 비활성화
            SetAttackRangeEnabled(false);
        }

        private void OnDestroy()
        {
            // 예약된 Invoke 등 정리
            CancelInvoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 슬라이더/필드 변경 시 유효성 정리(음수 방지 등)
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            if (timeEndStart < 0f) timeEndStart = 0f;
            if (timeReuse < 0f) timeReuse = 0f;
        }
#endif

        // ----------------------------
        // Phase State Machine
        // ----------------------------

        /// <summary>
        /// 단계 진입: start → attack → end
        /// - 각 단계에서 애니가 존재하면 재생 + 워치독 타이머 설정
        /// - 없으면 즉시 다음 처리(폴백)
        /// </summary>
        private void EnterPhase(TrapPhase next)
        {
            phase = next;
            ClearAwaiting();

            switch (next)
            {
                case TrapPhase.StartOneShot:
                    // start 애니: 보통 비루프(원샷) → 프로젝트 정책에 맞춰 loop=false 권장
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
                    // 공격 애니 재생 + 워치독(일반적으로 원샷)
                    if (hasAttack)
                    {
                        PlayAnimSafe(AnimAttack);
                        StartAwaiting(next, AnimAttack, 0f);
                    }
                    else
                    {
                        HandleAttackFinished();
                    }

                    // 공격 판정 활성 (트리거 내부의 플레이어에게 피해 적용은 별도 이벤트/콜백에서)
                    SetAttackRangeEnabled(true);
                    break;

                case TrapPhase.EndOneShot:
                    // 종료 연출(원샷) 후 정리
                    if (hasEnd)
                    {
                        PlayAnimSafe(AnimEnd);
                        StartAwaiting(next, AnimEnd, 0f);
                    }
                    else
                    {
                        HandleEndFinished();
                    }

                    // 종료 단계에서는 공격 비활성
                    SetAttackRangeEnabled(false);
                    break;
            }
        }

        private void Update()
        {
            // 워치독: 애니 이벤트 누락/길이 정보 미보고 시 타임아웃으로 다음 처리
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

        /// <summary>
        /// start 단계 종료 처리 → attack 진입
        /// </summary>
        private void HandleStartFinished()
        {
            if (phase != TrapPhase.StartOneShot) return;
            ClearAwaiting();
            EnterPhase(TrapPhase.Attack);
        }

        /// <summary>
        /// attack 단계 종료 처리 → end 진입
        /// </summary>
        private void HandleAttackFinished()
        {
            if (phase != TrapPhase.Attack) return;
            ClearAwaiting();
            EnterPhase(TrapPhase.EndOneShot);
        }

        /// <summary>
        /// end 단계 종료 처리 → 재사용 혹은 파괴
        /// </summary>
        private void HandleEndFinished()
        {
            if (phase != TrapPhase.EndOneShot) return;
            ClearAwaiting();
            phase = TrapPhase.None;

            if (reuse)
            {
                Invoke(nameof(Restart), timeReuse);
            }
            else
            {
                // 1회성: 객체 파괴
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// reuse=true일 때 재가동 준비: 상태 리셋 + 대기 애니
        /// </summary>
        private void Restart()
        {
            // OnEnable에서 wait 루프/초기화 재수행
            OnEnable();
        }

        // ----------------------------
        // Watchdog Helpers
        // ----------------------------

        /// <summary>
        /// 애니 이벤트 누락 대비: (클립 길이 + 추가 지연) 후에 다음 단계로 강제 진입할 시각을 설정
        /// </summary>
        private void StartAwaiting(TrapPhase phaseToWait, string clipName, float extraDelay)
        {
            _awaitingPhase   = phaseToWait;
            _awaitingDeadline = Time.time + GetClipDuration(clipName) + Mathf.Max(0f, extraDelay);
        }

        /// <summary>
        /// 워치독 해제
        /// </summary>
        private void ClearAwaiting()
        {
            _awaitingPhase = TrapPhase.None;
            _awaitingDeadline = 0f;
        }

        // ----------------------------
        // Trigger Entrypoints
        // ----------------------------

        /// <summary>
        /// 외부 트리거 시스템에서 호출되는 진입 지점(프로젝트 공용 인터페이스 가정)
        /// - 동일 대상이 빠르게 재진입하는 것을 방지하기 위해 Busy 가드
        /// - 플레이어 캐시 후 StartOneShot 진입
        /// </summary>
        public override void OnTrigger(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;

            // 이미 동작 중이면 무시(중복 시작 방지)
            if (IsBusy()) return;

            SetBusy(true);
            SetPlayerInRange(player);

            // 대기 애니에서 → start 단계 시작
            EnterPhase(TrapPhase.StartOneShot);
        }

        /// <summary>
        /// Unity 물리 트리거: 공격 단계에서 HitArea 진입 시 즉시 피해 1회 적용
        /// - 지속/틱 대미지가 아니라, “접촉 시 즉시 일격” 모델일 때 사용
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;

            // 공격 단계일 때만 즉시 일격 허용
            if (phase == TrapPhase.Attack)
            {
                ApplyDamage(player);
            }
        }
    }
}
