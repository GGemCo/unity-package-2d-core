using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 지정한 경로(웨이포인트)를 따라 이동하는 트랩.
    /// - Transform[] waypoints를 순서대로 이동합니다.
    /// - 이동 모드: Loop / PingPong / Once
    /// - 시작 모드: Auto(활성화 즉시 시작) / OnExternalTrigger(외부 감지기로부터 신호 시 시작)
    /// - 충돌 시 1회성 피해 적용(히트 쿨다운 제공). 지속 피해가 필요하면 ObjectTrapInfinity 사용 권장.
    /// - 옵션: 웨이포인트 도착 시 대기, 경로 방향으로 회전, 이동 애니메이션 루프 재생
    /// 퍼포먼스:
    /// - 이동은 단일 코루틴으로 처리하며, OnDisable 시 안전 정리합니다.
    /// - GetComponent 호출은 Awake에서 1회만 수행(상위 DefaultObjectTrap이 참조 캐시를 담당).
    /// 참고: Unity 공식 문서의 Transform, Time.deltaTime, Collider2D.isTrigger, OnTriggerEnter2D
    /// </summary>
    public sealed class ObjectTrapMoving : DefaultObjectTrap, ITrapTriggerController, ITrapAttackRangeHandlerEnter, ITrapAttackRangeHandlerStay
    {
        // ----- ITrapExternalControl 구현 -----
        public bool IsActive => _moveCo != null;
        
        // ===== Serialized Settings =====
        
        [Header("경로(웨이포인트)")]
        [Tooltip("이동 경로를 구성하는 웨이포인트들(월드 좌표 기준). 최소 2개 필요")]
        [SerializeField] private Transform[] waypoints;

        [Tooltip("웨이포인트 좌표계: WorldStatic → 부모 이동과 무관하게 월드 좌표 고정 / RelativeToSelf → 부모 기준 상대 좌표")]
        [SerializeField] private WaypointSpace waypointSpace = WaypointSpace.WorldStatic;
        
        [Tooltip("웨이포인트에 도달했다고 판단할 거리(유효 범위). 너무 작으면 떨림/정지 지연이 발생할 수 있음")]
        [Min(0.001f)]
        [SerializeField] private float arriveThreshold = 0.05f;

        [Header("이동 설정")]
        [Tooltip("초당 이동 속도(유닛/초)")]
        [Min(0.01f)]
        [SerializeField] private float moveSpeed = 2.0f;

        [Tooltip("웨이포인트 도착 시 대기 시간(초)")]
        [Min(0f)]
        [SerializeField] private float waitAtPoint;

        [Tooltip("이동 모드: Loop(끝→처음), PingPong(왕복), Once(마지막 도착 후 정지)")]
        [SerializeField] private MoveMode moveMode = MoveMode.Loop;

        [Tooltip("활성화 시 자동 시작(=Auto) 또는 외부 트리거 신호 후 시작(=OnExternalTrigger)")]
        [SerializeField] private StartMode startMode = StartMode.Auto;
        
        [Tooltip("End(중지) 요청 시 인덱스/방향을 초기화할지 여부. false면 Resume 시 이어서 이동")]
        // todo. true로 할 경우 어떻게 처리할가. 지금은 중지 요청이 왔을 때 위치에서 처음 위치로 이동한다.
        private readonly bool _resetOnEnd = false;

        [Space(6)]
        [Header("회전/연출")]
        [Tooltip("경로 진행 방향을 향하도록 Z 회전(2D)을 적용합니다.")]
        [SerializeField] private bool orientAlongPath = true;

        [Tooltip("orientAlongPath=true일 때, 추가 오프셋 각도(도 단위, 시계방향이 양수)")]
        [SerializeField] private float rotationOffsetDeg;

        [Tooltip("이동 중 애니메이션(AnimMove)")]
        [SerializeField] private string animMove = "attack";

        [Tooltip("이동 중 애니메이션(AnimMove)을 루프 재생합니다(해당 클립이 존재해야 함).")]
        [SerializeField] private bool playMoveAnimation = true;

        [Space(6)]
        [Header("충돌/피해")]
        [Tooltip("같은 대상에 연속 타격을 방지하기 위한 히트 쿨다운(초). 0이면 제한 없음")]
        [Min(0f)]
        [SerializeField] private float hitCooldown = 0.2f;

        // 월드 좌표 캐시 (WorldStatic 모드에서 사용)
        [SerializeField, HideInInspector] private Vector3[] cachedWorldPoints;
        
        // ===== Enums =====
        private enum MoveMode { Loop, PingPong, Once }
        private enum StartMode { Auto, OnExternalTrigger }
        // 웨이포인트 기준 좌표계
        private enum WaypointSpace { WorldStatic, RelativeToSelf }

        // ===== Internal State =====
        private Coroutine _moveCo;
        private int _currentIndex;
        private int _dir = 1; // +1: forward, -1: backward (PingPong용)
        private bool _everStarted;   // 최초 시작 여부 (처음 RequestStart 때만 StartFresh를 원할 때 사용)

        // 대상별 마지막 피격 시각 (InstanceID 기반)
        private readonly Dictionary<int, float> _lastHitTimeByTarget = new();

        // ===== Unity Lifecycle =====

        private void OnEnable()
        {
            // 공격 콜라이더는 이동 내내 활성화(이동형 함정 특성상 상시 위험 요소)
            SetAttackRangeEnabled(true);
            SetTriggerRangeEnabled(false); // 별도 외부 감지기가 있는 경우만 사용

            // 시작 조건 검증
            if (!ValidatePath())
            {
                enabled = false;
                return;
            }
            // 필요 시 월드 좌표 캐싱
            if (waypointSpace == WaypointSpace.WorldStatic)
                CacheWaypointWorldPositions();

            // 시작 모드에 따라 이동 시작
            if (startMode == StartMode.Auto)
                BeginMove();
            // OnExternalTrigger 모드는 외부에서 OnTrigger(...)가 들어올 때 시작
        }

        private void OnDisable()
        {
            StopMove();
            SetAttackRangeEnabled(false);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (moveSpeed < 0.01f) moveSpeed = 0.01f;
            if (arriveThreshold < 0.001f) arriveThreshold = 0.001f;
            if (hitCooldown < 0f) hitCooldown = 0f;
            
            // 플레이 중엔 캐시 갱신 금지 (경로 틀어짐 방지)
            if (UnityEditor.EditorApplication.isPlaying)
                return;
            // 에디터에서 웨이포인트 편집 즉시 캐시 갱신(씬 뷰 기즈모 안정성)
            if (waypointSpace == WaypointSpace.WorldStatic)
                CacheWaypointWorldPositions();
        }
#endif

        // ===== Public/External Entrypoints =====

        /// <summary>
        /// 외부 트리거 시스템(TrapTriggerDetector 등)에서 호출.
        /// - OnExternalTrigger 모드일 때 첫 신호에서 이동 시작.
        /// - Auto 모드에서는 무시(이미 이동 중일 가능성이 큼).
        /// </summary>
        public override void OnTrigger(Collider2D other)
        {
            if (startMode != StartMode.OnExternalTrigger) return;
            if (!IsPlayerHitArea(other, out var player)) return;
            if (_moveCo != null) return; // 이미 시작됨
            if (!ValidatePath()) return;

            BeginMove();
        }

        // ===== Movement =====

        /// <summary>경로 유효성 검사(웨이포인트 2개 이상)</summary>
        private bool ValidatePath()
        {
            if (waypoints == null || waypoints.Length < 2) { GcLogger.LogError("[ObjectTrapMoving] Waypoints 2+"); return false; }
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i]) continue;
                GcLogger.LogError($"[ObjectTrapMoving] Waypoint[{i}] is null"); return false;
            }
            return true;
        }

        /// <summary>이동 시작. 인덱스/방향 초기화 후 코루틴 가동</summary>
        private void BeginMove()
        {
            _currentIndex = 0;
            _dir = 1;

            // 이동 애니 루프 재생(선택)
            if (playMoveAnimation)
                PlayAnimSafe(animMove, true);

            StopMove();
            _moveCo = StartCoroutine(CoMovePath());
        }

        /// <summary>이동 정지</summary>
        private void StopMove()
        {
            if (_moveCo == null) return;
            StopCoroutine(_moveCo);
            _moveCo = null;
        }

        /// <summary>
        /// 웨이포인트 배열을 따라 이동하는 메인 루프.
        /// - MoveTowards 기반으로 프레임마다 위치 갱신
        /// - 도착 판단: arriveThreshold
        /// - 웨이포인트 도착 시 waitAtPoint 만큼 대기
        /// - moveMode에 따라 다음 인덱스 계산
        /// - Once 모드에서 마지막에 도달하면 코루틴 종료
        /// </summary>
        private IEnumerator CoMovePath()
        {
            Transform self = transform;

            while (true)
            {
                // 다음 목표 인덱스 계산
                int nextIndex = _currentIndex + _dir;

                // 경계 검사 및 모드별 보정
                if (nextIndex < 0 || nextIndex >= waypoints.Length)
                {
                    switch (moveMode)
                    {
                        case MoveMode.Loop:
                            nextIndex = (nextIndex < 0) ? waypoints.Length - 1 : 0;
                            break;

                        case MoveMode.PingPong:
                            _dir *= -1;
                            nextIndex = Mathf.Clamp(_currentIndex + _dir, 0, waypoints.Length - 1);
                            break;

                        case MoveMode.Once:
                            // 마지막 도착으로 간주하고 종료
                            yield break;
                    }
                }

                Vector3 to = GetWaypointPosition(nextIndex);

                // 도달할 때까지 이동
                while ((to - self.position).sqrMagnitude > arriveThreshold * arriveThreshold)
                {
                    // 진행 방향을 바라보도록 회전(옵션)
                    if (orientAlongPath)
                    {
                        Vector2 dir = (to - self.position);
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetDeg;
                        self.rotation = Quaternion.Euler(0f, 0f, angle);
                    }

                    // 이동
                    self.position = Vector3.MoveTowards(self.position, to, moveSpeed * Time.deltaTime);
                    yield return null;
                    
                    // (WorldStatic) 중간에 에디터에서 포인트를 옮긴 경우에도 추적하려면, 매 프레임 최신화(비권장) 대신
                    // 필요 시 OnValidate에서 캐시를 갱신하세요.
                }

                // 정확히 목표점 스냅(떨림 방지)
                self.position = to;
                _currentIndex = nextIndex;

                // 웨이포인트 대기
                if (waitAtPoint > 0f)
                    yield return new WaitForSeconds(waitAtPoint);
            }
        }
        // 좌표계에 따라 목표 좌표 제공
        private Vector3 GetWaypointPosition(int index)
        {
            if (waypointSpace == WaypointSpace.WorldStatic && cachedWorldPoints != null && index >= 0 && index < cachedWorldPoints.Length)
                return cachedWorldPoints[index];

            // RelativeToSelf: 현재 자식 Transform의 월드 좌표 사용(부모 따라 이동)
            return waypoints[index].position;
        }

        // ===== Hit / Damage =====

        /// <summary>
        /// 이동형 함정은 상시 공격 판정을 갖되, 같은 대상에 연속 타격이 발생하지 않도록
        /// OnTriggerEnter2D에서 1회성 피해 + 히트 쿨다운을 사용합니다.
        /// (지속 타격이 필요하면 ObjectTrapInfinity 사용)
        /// </summary>
        public void OnEnter(CharacterBase player)
        {
            if (!player) return;

            // 쿨다운 체크
            if (hitCooldown > 0f)
            {
                int id = player.GetInstanceID();
                if (_lastHitTimeByTarget.TryGetValue(id, out var lastTime))
                {
                    if (Time.time - lastTime < hitCooldown) return;
                }
                _lastHitTimeByTarget[id] = Time.time;
            }

            ApplyDamage(player);
        }

        public void OnStay(CharacterBase player)
        {
            if (!player) return;

            // 쿨다운 체크
            if (hitCooldown > 0f)
            {
                int id = player.GetInstanceID();
                if (_lastHitTimeByTarget.TryGetValue(id, out var lastTime))
                {
                    if (Time.time - lastTime < hitCooldown) return;
                }
                _lastHitTimeByTarget[id] = Time.time;
            }

            ApplyDamage(player);
        }
        
        /// <summary>
        /// 웨이포인트 월드 좌표 캐시
        /// </summary>
        private void CacheWaypointWorldPositions()
        {
            if (waypoints == null || waypoints.Length < 2) { cachedWorldPoints = null; return; }

            var list = new List<Vector3>(waypoints.Length);
            foreach (var waypoint in waypoints)
            {
                if (!waypoint) { cachedWorldPoints = null; return; }
                list.Add(waypoint.position); // 월드 좌표 저장
            }
            cachedWorldPoints = list.ToArray();
        }
        /// <summary>
        /// 외부에서 시작 요청: 이미 동작 중이면 무시, 아니면 경로 검증 후 이동 시작
        /// </summary>
        public void RequestStart(Collider2D triggerSource)
        {
            if (IsActive) return;
            if (!ValidatePath()) return;

            // 처음 시작
            if (!_everStarted)
            {
                StartFresh();
                _everStarted = true;
            }
            // 이어서 재생
            else
            {
                Resume();
            }
        }

        /// <summary>
        /// 외부에서 종료 요청: 이동 정지 및 공격 콜라이더/애니 초기화
        /// </summary>
        public void RequestEnd()
        {
            if (!IsActive) return;

            Pause(); // 코루틴만 중단, 위치/인덱스/방향은 유지
            SetAttackRangeEnabled(false);
            PlayAnimSafe(AnimWait, true);

            if (_resetOnEnd)
            {
                // 다음 Start 시 처음부터 가고 싶다면 상태 초기화
                _everStarted = false;
            }
        }
        private void StartFresh()
        {
            // "완전 초기화"로 시작
            _currentIndex = 0;
            _dir = 1;

            SetAttackRangeEnabled(true);
            if (playMoveAnimation) PlayAnimSafe(animMove, true);

            StopMove();
            _moveCo = StartCoroutine(CoMovePath());
        }

        private void Resume()
        {
            // "현재 상태 유지"로 재개
            SetAttackRangeEnabled(true);
            if (playMoveAnimation) PlayAnimSafe(animMove, true);

            StopMove();
            _moveCo = StartCoroutine(CoMovePath());
        }

        private void Pause()
        {
            // 상태는 유지하고 코루틴만 중단 → 다음 Resume 시 이어서 이동
            StopMove();
        }

        // ===== Gizmos (Editor) =====

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            // 편집 모드에서만 캐시 갱신 (플레이 중엔 고정된 cachedWorldPoints 사용)
            // WorldStatic 모드에서는 트랩 위치 변경에 따라 다시 캐싱
            if (!UnityEditor.EditorApplication.isPlaying && waypointSpace == WaypointSpace.WorldStatic)
            {
                CacheWaypointWorldPositions();
            }
            
            // 기즈모는 항상 월드 좌표 기준으로 그린다.
            var pts = (waypointSpace == WaypointSpace.WorldStatic && cachedWorldPoints != null && cachedWorldPoints.Length == waypoints.Length)
                ? cachedWorldPoints
                : GetCurrentWorldPointsFallback();

            if (pts == null || pts.Length < 2) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
            for (int i = 0; i < pts.Length; i++)
            {
                Gizmos.DrawWireSphere(pts[i], 0.1f);
                if (i + 1 < pts.Length)
                    Gizmos.DrawLine(pts[i], pts[i + 1]);
            }
            // 루프 표시
            if (moveMode == MoveMode.Loop)
                Gizmos.DrawLine(pts[^1], pts[0]);
        }
        
        private Vector3[] GetCurrentWorldPointsFallback()
        {
            var list = new List<Vector3>();
            foreach (var t in waypoints) { if (!t) return null; list.Add(t.position); }
            return list.ToArray();
        }
#endif
    }
}
