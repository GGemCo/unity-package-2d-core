using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 2D 캐릭터의 공용 모션 이동(전진/대시/러시/CC 이동 등)을 Distance 기반으로 처리하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// - 입력(플레이어 조작)과 무관하게 동작하도록 설계되어 플레이어/몬스터 공용으로 사용할 수 있습니다. <br/>
    /// - 모션은 채널(<see cref="MotionChannel"/>) 단위로 관리되며, 기본적으로 CrowdControl 채널이 Skill 채널보다 우선합니다. <br/>
    /// - 이동 곡선은 Easing으로 시간축(0~1)을 보정하고, 누적 거리에서 증분 이동량을 계산하여 프레임 오차를 줄입니다. <br/>
    /// - <see cref="Rigidbody2D"/>가 Kinematic이면 <see cref="Rigidbody2D.MovePosition"/> 기반 이동을 권장합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterMotionController2D : MonoBehaviour, ICharacterMotionController
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;

        private MotionState _skill;
        private MotionState _crowdControl;

        private void Reset()
        {
            rb = GetComponentInParent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (rb == null)
                rb = GetComponentInParent<Rigidbody2D>();
        }

        /// <summary>
        /// 요청된 모션을 시작합니다.
        /// </summary>
        /// <param name="request">채널/종류/방향/시간/거리/Easing/정지 정책 등을 포함한 모션 요청입니다.</param>
        /// <returns>모션 시작에 성공하면 <c>true</c>, 거부되면 <c>false</c>를 반환합니다.</returns>
        /// <remarks>
        /// 다음 조건에서는 시작이 거부됩니다.
        /// <list type="bullet">
        /// <item><description><see cref="Rigidbody2D"/> 참조가 없는 경우</description></item>
        /// <item><description>거리와 홀드 시간이 모두 0 이하인 경우(이동/대기 의미가 없음)</description></item>
        /// <item><description>동일 채널이 재생 중이고 <c>AllowReplace</c>가 false인 경우</description></item>
        /// </list>
        /// </remarks>
        public bool TryStartMotion(in MotionRequest request)
        {
            if (rb == null) return false;
            if (request.Distance <= 0f && request.HoldSecondsAfter <= 0f) return false;

            ref MotionState state = ref GetStateRef(request.Channel);

            if (state.IsPlaying && !request.AllowReplace)
                return false;

            state.Start(request);
            return true;
        }

        /// <summary>
        /// 지정한 채널의 모션을 중단합니다.
        /// </summary>
        /// <param name="channel">중단할 모션 채널입니다.</param>
        /// <param name="reason">중단 사유 코드(로깅/분석용)입니다.</param>
        /// <remarks>
        /// velocity 기반 이동을 사용 중인 경우를 대비해,
        /// 모션이 종료 시 정지 정책(<c>StopAtEnd</c>)을 갖고 있고 바디 타입이 Dynamic이면 x축 속도를 0으로 정리합니다.
        /// </remarks>
        public void CancelMotion(MotionChannel channel, int reason = 0)
        {
            ref MotionState state = ref GetStateRef(channel);
            if (!state.IsPlaying) return;

            state.Stop();

            // velocity 기반 구현을 사용하는 경우를 대비해 정지 정책 제공
            if (state.StopAtEnd && rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.SetLinearVelocity(new Vector2(0f, rb.GetLinearVelocity().y));
            }
        }

        /// <summary>
        /// 지정한 채널의 모션이 재생 중인지 확인합니다.
        /// </summary>
        /// <param name="channel">확인할 모션 채널입니다.</param>
        /// <returns>재생 중이면 <c>true</c>, 아니면 <c>false</c>입니다.</returns>
        public bool IsPlaying(MotionChannel channel)
        {
            ref MotionState state = ref GetStateRef(channel);
            return state.IsPlaying;
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                _skill.Stop();
                _crowdControl.Stop();
                return;
            }

            float dt = Time.fixedDeltaTime;

            // CrowdControl 모션이 Skill 모션보다 우선합니다.
            // (정책이 바뀌면 우선순위 규칙만 변경하면 됩니다.)
            if (_crowdControl.IsPlaying)
            {
                Tick(ref _crowdControl, dt);
                return;
            }

            if (_skill.IsPlaying)
            {
                Tick(ref _skill, dt);
            }
        }

        /// <summary>
        /// 모션 상태를 한 프레임 진행시키고, 증분 이동을 적용합니다.
        /// </summary>
        /// <param name="state">진행할 모션 상태입니다.</param>
        /// <param name="dt">고정 프레임 시간(초)입니다.</param>
        /// <remarks>
        /// 누적 목표 거리(거리 * eased)를 계산한 뒤, 이전 누적값과의 차(delta)를 구해 실제 이동에 사용합니다.
        /// 이 방식은 프레임별 오차 누적을 줄이고 최종 도달 거리를 안정적으로 맞추기 위함입니다.
        /// </remarks>
        private void Tick(ref MotionState state, float dt)
        {
            if (!state.IsPlaying) return;

            state.Elapsed += dt;

            // 정규화 시간(0~1)
            float t = state.Duration <= 1e-6f ? 1f : Mathf.Clamp01(state.Elapsed / state.Duration);

            // Easing 적용(0~1)
            float eased = Easing.Apply(t, state.EaseType);

            // 목표 누적 이동거리
            float targetDistance = state.Distance * eased;

            // 이번 프레임 이동해야 할 거리(증분)
            float deltaDistance = targetDistance - state.MovedDistance;
            state.MovedDistance = targetDistance;

            // 수평/기본 이동
            Vector2 delta = state.Direction * deltaDistance;

            // Arc(수직) 적용: sin(pi * t) * height
            if (state.Kind == MotionKind.Arc && state.ArcHeight > 0f)
            {
                float arc = Mathf.Sin(Mathf.PI * t) * state.ArcHeight;
                float deltaArc = arc - state.AppliedArc;
                state.AppliedArc = arc;
                delta += Vector2.up * deltaArc;
            }

            ApplyDelta(delta, state.UseMovePosition);

            // 이동 구간 종료
            if (t >= 1f)
            {
                // 이동 종료 후 대기(hold)
                if (state.HoldSecondsAfter > 0f)
                {
                    state.HoldRemaining -= dt;
                    if (state.HoldRemaining <= 0f)
                    {
                        state.Stop();
                        StopVelocityIfNeeded(state);
                    }
                }
                else
                {
                    state.Stop();
                    StopVelocityIfNeeded(state);
                }
            }
        }

        /// <summary>
        /// 계산된 증분 이동량을 <see cref="Rigidbody2D"/>에 적용합니다.
        /// </summary>
        /// <param name="delta">이번 프레임에 적용할 이동량(월드 좌표)입니다.</param>
        /// <param name="useMovePosition">
        /// Kinematic 바디에서 <see cref="Rigidbody2D.MovePosition"/> 기반 이동을 사용할지 여부입니다.
        /// </param>
        /// <remarks>
        /// - Kinematic + useMovePosition: 누적 위치 기반으로 MovePosition을 적용합니다. <br/>
        /// - 그 외: 증분 거리/시간으로 순간 속도를 산출해 velocity로 적용합니다.
        /// </remarks>
        private void ApplyDelta(Vector2 delta, bool useMovePosition)
        {
            if (delta.sqrMagnitude <= 1e-12f) return;

            // Kinematic: MovePosition 권장
            if (useMovePosition && rb.bodyType == RigidbodyType2D.Kinematic)
            {
                rb.MovePosition(rb.position + delta);
            }
            else
            {
                // Dynamic 또는 정책상 velocity 사용
                // - Distance 기반이므로, 증분거리/시간으로 순간 속도를 산출한다.
                float dt = Time.fixedDeltaTime;
                float vx = dt > 1e-6f ? (delta.x / dt) : 0f;
                float vy = dt > 1e-6f ? (delta.y / dt) : rb.GetLinearVelocity().y;
                rb.SetLinearVelocity(new Vector2(vx, vy));
            }
        }

        /// <summary>
        /// 모션 종료 시 정지 정책에 따라 속도를 정리합니다.
        /// </summary>
        /// <param name="state">종료된 모션 상태입니다.</param>
        /// <remarks>
        /// Dynamic 바디에서 velocity 기반 이동을 사용했을 때,
        /// StopAtEnd가 true면 x축 속도를 0으로 초기화하여 잔여 관성을 제거합니다.
        /// </remarks>
        private void StopVelocityIfNeeded(in MotionState state)
        {
            if (state.StopAtEnd && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.SetLinearVelocity(new Vector2(0f, rb.GetLinearVelocity().y));
            }
        }

        /// <summary>
        /// 채널에 대응하는 모션 상태 참조를 반환합니다.
        /// </summary>
        /// <param name="channel">가져올 채널입니다.</param>
        /// <returns>해당 채널의 <see cref="MotionState"/> 참조입니다.</returns>
        private ref MotionState GetStateRef(MotionChannel channel)
        {
            return ref (channel == MotionChannel.CrowdControl ? ref _crowdControl : ref _skill);
        }

        /// <summary>
        /// 단일 모션 채널의 실행 상태(진행 시간, 누적 이동, 홀드 등)를 보관하는 내부 상태 구조체입니다.
        /// </summary>
        private struct MotionState
        {
            public bool IsPlaying;
            public MotionChannel Channel;
            public MotionKind Kind;

            public Vector2 Direction;
            public float Duration;
            public float Elapsed;
            public float Distance;
            public float MovedDistance;
            public Easing.EaseType EaseType;

            public float ArcHeight;
            public float AppliedArc;

            public float HoldSecondsAfter;
            public float HoldRemaining;

            public bool StopAtEnd;
            public bool UseMovePosition;

            /// <summary>
            /// 요청을 기반으로 모션 상태를 초기화하고 재생을 시작합니다.
            /// </summary>
            /// <param name="req">초기화에 사용할 모션 요청입니다.</param>
            public void Start(in MotionRequest req)
            {
                IsPlaying = true;
                Channel = req.Channel;
                Kind = req.Kind;
                Direction = req.Direction;
                Duration = req.DurationSeconds;
                Elapsed = 0f;
                Distance = req.Distance;
                MovedDistance = 0f;
                EaseType = req.EaseType;
                ArcHeight = req.ArcHeight;
                AppliedArc = 0f;
                HoldSecondsAfter = req.HoldSecondsAfter;
                HoldRemaining = req.HoldSecondsAfter;
                StopAtEnd = req.StopAtEnd;
                UseMovePosition = req.UseMovePosition;
            }

            /// <summary>
            /// 모션 재생을 중단하고, 진행/누적 값을 초기화합니다.
            /// </summary>
            public void Stop()
            {
                IsPlaying = false;
                Elapsed = 0f;
                Duration = 0f;
                Distance = 0f;
                MovedDistance = 0f;
                AppliedArc = 0f;
                HoldSecondsAfter = 0f;
                HoldRemaining = 0f;
            }
        }
    }
}