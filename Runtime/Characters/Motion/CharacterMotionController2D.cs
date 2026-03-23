using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 2D 캐릭터의 공용 모션 이동(전진/대시/러시/CC 이동 등)을 Distance 기반으로 처리하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// - 입력(플레이어 조작)과 무관하게 동작하도록 설계되어 플레이어/몬스터 공용으로 사용할 수 있습니다. <br/>
    /// - 모션은 채널(<see cref="MotionChannel"/>) 단위로 관리되며, 기본적으로 CrowdControl 채널이 Skill 채널보다 우선합니다. <br/>
    /// - 이동 계산 로직은 Solver(<see cref="IMotionSolver"/>)로 분리되어, 모션 종류가 늘어나도 컨트롤러 복잡도가 증가하지 않도록 설계합니다. <br/>
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
        public bool TryStartMotion(in MotionRequest request)
        {
            if (rb == null) return false;
            if (request.Distance <= 0f && request.HoldSecondsAfter <= 0f && request.ArcHeight <= 0f) return false;

            ref MotionState state = ref GetStateRef(request.Channel);

            if (state.IsPlaying && !request.AllowReplace)
                return false;

            state.Start(request);
            return true;
        }

        /// <summary>
        /// 지정한 채널의 모션을 중단합니다.
        /// </summary>
        public void CancelMotion(MotionChannel channel, int reason = 0)
        {
            ref MotionState state = ref GetStateRef(channel);
            if (!state.IsPlaying) return;

            bool stopAtEnd = state.StopAtEnd;
            state.Stop();

            // velocity 기반 구현을 사용하는 경우를 대비해 정지 정책 제공
            if (stopAtEnd && rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.SetLinearVelocity(new Vector2(0f, rb.GetLinearVelocity().y));
            }
        }

        /// <summary>
        /// 지정한 채널의 모션이 재생 중인지 확인합니다.
        /// </summary>
        public bool IsPlaying(MotionChannel channel)
        {
            ref MotionState state = ref GetStateRef(channel);
            return state.IsPlaying;
        }

        public bool TryGetMotionProgress(MotionChannel channel, out float progress01)
        {
            ref MotionState state = ref GetStateRef(channel);
            return state.TryGetProgress(out progress01);
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
        private void Tick(ref MotionState state, float dt)
        {
            if (!state.IsPlaying) return;

            state.Solver.Tick(ref state, dt, out Vector2 delta);
            ApplyDelta(delta, state.UseMovePosition);

            if (state.IsComplete)
            {
                bool stopAtEnd = state.StopAtEnd;
                state.Stop();
                StopVelocityIfNeeded(stopAtEnd);
            }
        }

        /// <summary>
        /// 계산된 증분 이동량을 <see cref="Rigidbody2D"/>에 적용합니다.
        /// </summary>
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
                float dt = Time.fixedDeltaTime;
                float vx = dt > 1e-6f ? (delta.x / dt) : 0f;
                float vy = dt > 1e-6f ? (delta.y / dt) : rb.GetLinearVelocity().y;
                rb.SetLinearVelocity(new Vector2(vx, vy));
            }
        }

        private void StopVelocityIfNeeded(bool stopAtEnd)
        {
            if (!stopAtEnd) return;
            if (rb == null) return;
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;

            rb.SetLinearVelocity(new Vector2(0f, rb.GetLinearVelocity().y));
        }

        private ref MotionState GetStateRef(MotionChannel channel)
        {
            return ref (channel == MotionChannel.CrowdControl ? ref _crowdControl : ref _skill);
        }

        /// <summary>
        /// 단일 모션 채널의 실행 상태(진행 시간, 누적 이동, 홀드 등)를 보관하는 내부 상태 구조체입니다.
        /// </summary>
        internal struct MotionState
        {
            public bool IsPlaying;
            public bool IsComplete;

            public MotionChannel Channel;
            public MotionKind Kind;
            public IMotionSolver Solver;

            public Vector2 Direction;
            public float Duration;
            public float Elapsed;
            public float Distance;
            public float MovedDistance;
            public Easing.EaseType EaseType;

            public float ArcHeight;

            /// <summary>Arc 누적 적용값(y)</summary>
            public float AppliedArcY;

            /// <summary>Arc 진행(상승/낙하) easing</summary>
            public Easing.EaseType ArcRiseEaseType;
            public Easing.EaseType ArcFallEaseType;

            /// <summary>Apex 유지 구간 폭(정규화 0..1). 0이면 ApexHold 없이 Rise->Fall로 전환됩니다.</summary>
            public float ArcApexHoldNormalized;

            /// <summary>Arc 상승 구간 비율(정규화 전 원본 값).</summary>
            public float ArcRiseRatioNormalized;

            /// <summary>Arc 하강 구간 비율(정규화 전 원본 값).</summary>
            public float ArcFallRatioNormalized;

            public float HoldSecondsAfter;
            public float HoldRemaining;

            public bool StopAtEnd;
            public bool UseMovePosition;

            public void Start(in MotionRequest req)
            {
                IsPlaying = true;
                IsComplete = false;

                Channel = req.Channel;
                Kind = req.Kind;
                Direction = req.Direction;
                Duration = req.DurationSeconds;
                Elapsed = 0f;
                Distance = req.Distance;
                MovedDistance = 0f;
                EaseType = req.EaseType;

                ArcHeight = req.ArcHeight;
                AppliedArcY = 0f;
                ArcRiseEaseType = req.ArcRiseEaseType;
                ArcFallEaseType = req.ArcFallEaseType;
                ArcApexHoldNormalized = req.ArcApexHoldNormalized;
                ArcRiseRatioNormalized = req.ArcRiseRatioNormalized;
                ArcFallRatioNormalized = req.ArcFallRatioNormalized;

                HoldSecondsAfter = req.HoldSecondsAfter;
                HoldRemaining = req.HoldSecondsAfter;

                StopAtEnd = req.StopAtEnd;
                UseMovePosition = req.UseMovePosition;

                // Solver 선택
                Solver = SelectSolver(req);
            }

            public void MarkComplete()
            {
                IsComplete = true;
            }

            public bool TryGetProgress(out float progress01)
            {
                progress01 = 0f;
                if (!IsPlaying)
                    return false;

                if (Duration <= 1e-6f)
                {
                    progress01 = 1f;
                    return true;
                }

                progress01 = Mathf.Clamp01(Elapsed / Duration);
                return true;
            }

            public void Stop()
            {
                IsPlaying = false;
                IsComplete = false;

                Elapsed = 0f;
                Duration = 0f;

                Distance = 0f;
                MovedDistance = 0f;

                AppliedArcY = 0f;

                HoldSecondsAfter = 0f;
                HoldRemaining = 0f;
            }

            private static IMotionSolver SelectSolver(in MotionRequest req)
            {
                // Linear
                if (req.Kind == MotionKind.Linear)
                {
                    if (req.HoldSecondsAfter > 0f)
                        return MotionSolverLinearMoveHold.Instance;

                    return MotionSolverLinearMove.Instance;
                }

                // Arc
                if (req.Kind == MotionKind.Arc)
                {
                    if (req.ArcMode == MotionArcMode.DistancePhased)
                        return MotionSolverArcPhased.Instance;

                    return MotionSolverArcLegacySine.Instance;
                }

                return MotionSolverLinearMove.Instance;
            }
        }
    }
}
