using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이포인트 이동형 트랩.
    /// - 모드: Loop / PingPong / Once
    /// - 시작: Auto / OnExternalTrigger
    /// - 충돌 시 1회성 또는 주기적 피해(hitCooldown)
    /// </summary>
    public sealed class ObjectTrapMoving : DefaultObjectTrap, ITrapTriggerController, ITrapAttackRangeHandlerEnter, ITrapAttackRangeHandlerStay
    {
        public bool IsActive => _moveCo != null;

        [Header("경로(웨이포인트)")]
        [Tooltip("트랩이 순회할 웨이포인트 배열 (2개 이상 필요). WorldStatic이면 월드 좌표를 캐시해 따라갑니다.")]
        [SerializeField] private Transform[] waypoints;

        [Tooltip("웨이포인트 좌표계.\n- WorldStatic: 에디터/초기값 기준의 월드 좌표를 캐시하여 부모 이동과 무관하게 이동\n- RelativeToSelf: 현재 Transform 기준 상대 좌표(자식의 현재 위치 사용)")]
        [SerializeField] private WaypointSpace waypointSpace = WaypointSpace.WorldStatic;

        [Tooltip("웨이포인트에 도달했다고 판단할 거리(유닛). 너무 작으면 떨림/정지 지연 발생 가능")]
        [Min(0.001f)] [SerializeField] private float arriveThreshold = 0.05f;

        [Header("이동 설정")]
        [Tooltip("이동 속도(유닛/초)")]
        [Min(0.01f)] [SerializeField] private float moveSpeed = 2.0f;

        [Tooltip("웨이포인트 도착 후 머무는 시간(초). 0이면 즉시 다음 포인트로 진행")]
        [Min(0f)] [SerializeField] private float waitAtPoint;

        [Tooltip("이동 모드.\n- Loop: 마지막 → 처음으로 순환\n- PingPong: 왕복\n- Once: 마지막 도착 시 정지")]
        [SerializeField] private MoveMode moveMode = MoveMode.Loop;

        [Tooltip("시작 모드.\n- Auto: 활성화 시 자동 시작\n- OnExternalTrigger: 외부 트리거(TrapTriggerDetector) 신호 시 시작")]
        [SerializeField] private StartMode startMode = StartMode.Auto;

        [Tooltip("이동 방향을 바라보도록 Z 회전을 적용할지 여부")]
        [SerializeField] private bool orientAlongPath = true;

        [Tooltip("orientAlongPath가 true일 때 회전에 더해질 추가 Z 각도(도 단위, 시계 방향이 양수)")]
        [SerializeField] private float rotationOffsetDeg;

        [Tooltip("이동 중 재생할 애니메이션 클립 이름(해당 클립이 존재해야 함)")]
        [SerializeField] private string animMove = "attack";

        [Tooltip("이동 중 애니메이션을 루프 재생할지 여부")]
        [SerializeField] private bool playMoveAnimation = true;

        [Header("충돌/피해")]
        [Tooltip("같은 대상에 연속 타격을 방지하기 위한 쿨다운(초). 0이면 제한 없음")]
        [Min(0f)] [SerializeField] private float hitCooldown = 0.2f;

        [Tooltip("WorldStatic 모드에서 사용하는 웨이포인트 월드 좌표 캐시(런타임 자동 관리)")]
        [SerializeField, HideInInspector] private Vector3[] cachedWorldPoints;

        private enum MoveMode { Loop, PingPong, Once }
        private enum StartMode { Auto, OnExternalTrigger }
        private enum WaypointSpace { WorldStatic, RelativeToSelf }

        private Coroutine _moveCo; private int _currentIndex; private int _dir = 1;
        private bool _everStarted; // 최초 시작 여부
        private readonly Dictionary<int, float> _lastHitTimeByTarget = new();

        private void OnEnable()
        {
            SetAttackRangeEnabled(true); SetTriggerRangeEnabled(false);
            if (!ValidatePath()) { enabled = false; return; }
            if (waypointSpace == WaypointSpace.WorldStatic) CacheWaypointWorldPositions();
            if (startMode == StartMode.Auto) BeginMove();
        }
        private void OnDisable() { StopMove(); SetAttackRangeEnabled(false); }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (moveSpeed < 0.01f) moveSpeed = 0.01f;
            if (arriveThreshold < 0.001f) arriveThreshold = 0.001f;
            if (hitCooldown < 0f) hitCooldown = 0f;
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (waypointSpace == WaypointSpace.WorldStatic) CacheWaypointWorldPositions();
        }
#endif
        public override void OnTrigger(Collider2D other)
        {
            if (startMode != StartMode.OnExternalTrigger) return;
            if (!IsPlayerHitArea(other, out _)) return;
            if (_moveCo != null) return;
            if (!ValidatePath()) return;
            BeginMove();
        }

        private bool ValidatePath()
        {
            if (waypoints == null || waypoints.Length < 2)
            { GcLogger.LogError("[ObjectTrapMoving] Waypoints는 2개 이상이어야 합니다."); return false; }
            for (int i = 0; i < waypoints.Length; i++) if (!waypoints[i]) { GcLogger.LogError($"[ObjectTrapMoving] Waypoint[{i}] is null"); return false; }
            return true;
        }
        private void BeginMove()
        {
            _currentIndex = 0; _dir = 1;
            if (playMoveAnimation) PlayAnimSafe(animMove, true);
            StopMove(); _moveCo = StartCoroutine(CoMovePath());
        }
        private void StopMove() { if (_moveCo == null) return; StopCoroutine(_moveCo); _moveCo = null; }

        private IEnumerator CoMovePath()
        {
            Transform self = transform;
            while (true)
            {
                int nextIndex = _currentIndex + _dir;
                if (nextIndex < 0 || nextIndex >= waypoints.Length)
                {
                    switch (moveMode)
                    {
                        case MoveMode.Loop: nextIndex = (nextIndex < 0) ? waypoints.Length - 1 : 0; break;
                        case MoveMode.PingPong: _dir *= -1; nextIndex = Mathf.Clamp(_currentIndex + _dir, 0, waypoints.Length - 1); break;
                        case MoveMode.Once: yield break;
                    }
                }
                Vector3 to = GetWaypointPosition(nextIndex);
                while ((to - self.position).sqrMagnitude > arriveThreshold * arriveThreshold)
                {
                    if (orientAlongPath)
                    { var dir = (to - self.position); float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffsetDeg; self.rotation = Quaternion.Euler(0f, 0f, angle); }
                    self.position = Vector3.MoveTowards(self.position, to, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                self.position = to; _currentIndex = nextIndex;
                if (waitAtPoint > 0f) yield return new WaitForSeconds(waitAtPoint);
            }
        }
        private Vector3 GetWaypointPosition(int index)
        {
            if (waypointSpace == WaypointSpace.WorldStatic && cachedWorldPoints != null && index >= 0 && index < cachedWorldPoints.Length) return cachedWorldPoints[index];
            return waypoints[index].position;
        }
        private void CacheWaypointWorldPositions()
        {
            if (waypoints == null || waypoints.Length < 2) { cachedWorldPoints = null; return; }
            var list = new List<Vector3>(waypoints.Length);
            foreach (var t in waypoints) { if (!t) { cachedWorldPoints = null; return; } list.Add(t.position); }
            cachedWorldPoints = list.ToArray();
        }

        // --- Hit/Damage 쿨다운 공통 처리 ---
        private bool CanHit(CharacterBase player)
        {
            if (!player) return false; if (hitCooldown <= 0f) return true;
            int id = player.GetInstanceID();
            if (_lastHitTimeByTarget.TryGetValue(id, out var last) && Time.time - last < hitCooldown) return false;
            _lastHitTimeByTarget[id] = Time.time; return true;
        }
        public void OnEnter(CharacterBase player) { if (CanHit(player)) ApplyDamage(player); }
        public void OnStay(CharacterBase player)  { if (CanHit(player)) ApplyDamage(player); }

        // --- 외부 제어 ---
        public void RequestStart(Collider2D _) { if (IsActive) return; if (!ValidatePath()) return; if (!_everStarted) { StartFresh(); _everStarted = true; } else { Resume(); } }
        public void RequestEnd()
        {
            if (!IsActive) return; Pause(); SetAttackRangeEnabled(false); PlayAnimSafe(AnimWait, true);
            // 필요 시 상태 초기화 옵션 추가 가능
        }
        private void StartFresh() { _currentIndex = 0; _dir = 1; SetAttackRangeEnabled(true); if (playMoveAnimation) PlayAnimSafe(animMove, true); StopMove(); _moveCo = StartCoroutine(CoMovePath()); }
        private void Resume()     { SetAttackRangeEnabled(true); if (playMoveAnimation) PlayAnimSafe(animMove, true); StopMove(); _moveCo = StartCoroutine(CoMovePath()); }
        private void Pause()      { StopMove(); }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            if (!UnityEditor.EditorApplication.isPlaying && waypointSpace == WaypointSpace.WorldStatic) CacheWaypointWorldPositions();
            var pts = (waypointSpace == WaypointSpace.WorldStatic && cachedWorldPoints != null && cachedWorldPoints.Length == waypoints.Length) ? cachedWorldPoints : GetCurrentWorldPointsFallback();
            if (pts == null || pts.Length < 2) return;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
            for (int i = 0; i < pts.Length; i++) { Gizmos.DrawWireSphere(pts[i], 0.1f); if (i + 1 < pts.Length) Gizmos.DrawLine(pts[i], pts[i + 1]); }
            if (moveMode == MoveMode.Loop) Gizmos.DrawLine(pts[^1], pts[0]);
        }
        private Vector3[] GetCurrentWorldPointsFallback()
        {
            var list = new List<Vector3>(); foreach (var t in waypoints) { if (!t) return null; list.Add(t.position); } return list.ToArray();
        }
#endif
    }
}