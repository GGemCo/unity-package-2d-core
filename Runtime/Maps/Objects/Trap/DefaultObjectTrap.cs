using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 모든 트랩 오브젝트의 공통 베이스 클래스.
    /// - 피해/효과 처리(ApplyDamage)
    /// - 애니메이션 컨트롤(Spine or Animator)
    /// - 공격/트리거 콜라이더 관리(SetAttackRangeEnabled/SetTriggerRangeEnabled)
    /// - 상태 관리(TrapPhase) 및 애니 이벤트 누락 대비 워치독 공통화
    /// </summary>
    public class DefaultObjectTrap : DefaultMapObject
    {
        #region Serialized: Damage & Effect
        [Header("피해/효과 설정")]
        [Tooltip("피해 타입 (물리/마법 등)")]
        [SerializeField] protected SkillConstants.DamageType damageType = SkillConstants.DamageType.Physic;

        [Tooltip("공격 시 가하는 총 피해량(1회성 공격 시 사용)")]
        [Min(0)] [SerializeField] protected long totalDamage = 1;

        [Tooltip("피격자에게 부여할 Affect Uid (0이면 부여하지 않음)")]
        [Min(0)] [SerializeField] protected int targetAffectUid;

        [Tooltip("애니메이션 재생 속도 (1 = 기본)")]
        [SerializeField] protected float animationTimeScale = 1f;
        #endregion

        #region Serialized: References
        [Header("레퍼런스")]
        [Tooltip("공격 판정용 Trigger Collider (ObjectTrapAttackRange)")]
        [SerializeField] protected ObjectTrapAttackRange objectTrapAttackRange;

        [Tooltip("트랩 시작/종료를 위한 Trigger Detector (TrapTriggerDetector)")]
        [SerializeField] protected TrapTriggerDetector trapTriggerDetector;
        #endregion

        #region Internal State / Cache
        protected enum TrapPhase { None, StartOneShot, Attack, EndOneShot }
        protected TrapPhase phase = TrapPhase.None;

        // 애니메이션 길이 캐시 (클립명→길이)
        protected readonly Dictionary<string, float> clipLength = new();
        protected bool hasStart, hasAttack, hasEnd; // 보유 여부 캐시

        // 애니 이름 상수
        protected const string AnimStart = "start";
        protected const string AnimAttack = "attack";
        protected const string AnimEnd = "end";
        protected const string AnimWait = "wait";

        // 현재 트리거 내부의 대상(플레이어) 캐시
        protected CharacterBase playerInRange;

        // 애니/이벤트 브릿지
        private IMapObjectAnimationController _animationController;
        private AnimationEventMediator _eventMediator;

        // 워치독(애니 이벤트 누락 대비) 공통 필드
        protected TrapPhase awaitingPhase = TrapPhase.None;
        protected float awaitingDeadline;
        private const float DefaultOneShotTimeout = 0.2f;

        // 중복 트리거 가드
        private bool _isBusy;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

#if GGEMCO_USE_SPINE
            if (!TryBindSpine())
#endif
            {
                TryBindSpriteAnimator();
            }

            if (_animationController == null)
            {
                GcLogger.LogError("[DefaultObjectTrap] Animator/Spine 컨트롤러를 찾지 못했습니다.");
                enabled = false; return;
            }

            if (!objectTrapAttackRange)
            {
                GcLogger.LogError("[DefaultObjectTrap] 공격 범위 Trigger Collider가 없습니다.");
                enabled = false; return;
            }

            SetBusy(false);

            // 공격/트리거 연결 주입
            objectTrapAttackRange?.SetTargetTrap(this);
            trapTriggerDetector?.SetTargetTrap(this);

            CacheAnimations();
        }
        #endregion

        #region Animator / Spine Binding
#if GGEMCO_USE_SPINE
        private bool TryBindSpine()
        {
            var skeleton = GetComponent<SkeletonAnimation>();
            if (!skeleton) return false;

            var spineAnimController = GetComponent<CharacterAnimationControllerSpine>() ??
                                      gameObject.AddComponent<CharacterAnimationControllerSpine>();
            _animationController = spineAnimController.GetComponent<IMapObjectAnimationController>();

            var spineController = GetComponent<Spine2dController>() ?? gameObject.AddComponent<Spine2dController>();
            _eventMediator = new AnimationEventMediator();
            spineController.EventListener = _eventMediator;

            return _animationController != null;
        }
#endif
        private void TryBindSpriteAnimator()
        {
            var animator = GetComponent<Animator>();
            if (!animator) return;

            var spriteAnimController = GetComponent<MapObjectAnimationControllerSprite>() ??
                                       gameObject.AddComponent<MapObjectAnimationControllerSprite>();
            _animationController = spriteAnimController.GetComponent<IMapObjectAnimationController>();

            var ani2d = GetComponent<Animation2dController>() ?? gameObject.AddComponent<Animation2dController>();
            _eventMediator = new AnimationEventMediator();
            ani2d.EventListener = _eventMediator;
        }
        private void CacheAnimations()
        {
            clipLength.Clear();
            var all = _animationController.GetAnimationAllLength();
            if (all != null)
            {
                foreach (var kv in all)
                {
                    float scaled = kv.Value / Mathf.Max(animationTimeScale, 0.0001f);
                    clipLength[kv.Key] = scaled;
                }
            }
            hasStart = _animationController.HasAnimation(AnimStart);
            hasAttack = _animationController.HasAnimation(AnimAttack);
            hasEnd = _animationController.HasAnimation(AnimEnd);
        }
        #endregion

        #region Damage / Target
        /// <summary>플레이어(캐릭터)에게 피해를 적용합니다.</summary>
        public void ApplyDamage(CharacterBase player)
        {
            if (!player || totalDamage <= 0) return;
            var meta = new MetadataDamage
            {
                damage = totalDamage,
                attacker = gameObject,
                damageType = damageType,
                affectUid = targetAffectUid,
            };
            player.TakeDamage(meta);
        }

        /// <summary>
        /// Player HitArea 콜라이더인지 검사하고 CharacterBase를 반환합니다.
        /// </summary>
        protected bool IsPlayerHitArea(Collider2D other, out CharacterBase player)
        {
            player = null; if (!other) return false;
            if (!other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return false;
            var hitArea = other.GetComponent<CharacterHitArea>();
            if (!hitArea) return false;
            player = hitArea.target;
            return player != null;
        }
        #endregion

        #region Triggers / Utilities
        protected void SetAttackRangeEnabled(bool set)
        { objectTrapAttackRange?.SetTriggerEnabled(set); }
        protected void SetTriggerRangeEnabled(bool set)
        { trapTriggerDetector?.SetTriggerEnabled(set); }

        protected void PlayAnimSafe(string stateName, bool loop = false)
        { _animationController?.PlayMapObjectAnimation(stateName, loop, animationTimeScale); }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (totalDamage <= 0) totalDamage = 0;
            if (targetAffectUid <= 0) targetAffectUid = 0;
        }
#endif
        protected float GetClipDuration(string clipName)
        {
            if (clipLength.TryGetValue(clipName, out var len) && len > 0f)
                return len + 0.02f; // 미세 버퍼
            return DefaultOneShotTimeout;
        }
        protected bool IsBusy() => _isBusy;
        protected void SetBusy(bool set) => _isBusy = set;
        protected void SetPlayerInRange(CharacterBase player) => playerInRange = player;

        /// <summary>
        /// 외부 트리거(Detector)에서 진입 시 호출하는 엔트리 포인트(파생에서 override).
        /// </summary>
        public virtual void OnTrigger(Collider2D other) { }

        /// <summary>
        /// Stay 체크를 위해 Rigidbody2D의 슬립 모드를 변경하여 트리거 이벤트가 중단되지 않도록 합니다.
        /// </summary>
        public void OnStay(bool set, CharacterBase player)
        {
            if (player) SetPlayerInRange(player);
            playerInRange?.SetRigidBody2DSleepMode(set ? RigidbodySleepMode2D.NeverSleep : RigidbodySleepMode2D.StartAwake);
        }
        #endregion

        #region Watchdog (Animation Timeout) – Commonized
        /// <summary>워치독 시작: (클립 길이 + 추가 지연) 후 다음 페이즈로 넘어가도록 마감 시각 설정</summary>
        protected void StartAwaiting(TrapPhase phaseToWait, string clipName, float extraDelay)
        {
            awaitingPhase = phaseToWait;
            awaitingDeadline = Time.time + GetClipDuration(clipName) + Mathf.Max(0f, extraDelay);
        }
        /// <summary>워치독 해제</summary>
        protected void ClearAwaiting()
        { awaitingPhase = TrapPhase.None; awaitingDeadline = 0f; }

        /// <summary>
        /// 파생 클래스 Update에서 호출: 마감 시각이 경과한 경우 true를 반환하여 다음 단계 처리를 유도
        /// </summary>
        protected bool TryWatchdogExpired(out TrapPhase expiredPhase)
        {
            expiredPhase = TrapPhase.None;
            if (awaitingPhase == TrapPhase.None) return false;
            if (Time.time < awaitingDeadline) return false;
            expiredPhase = awaitingPhase;  // 현재 대기 중이던 페이즈를 넘겨줌
            ClearAwaiting();
            return true;
        }
        #endregion
    }
}