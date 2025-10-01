using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace GGemCo2DCore
{
    public class DefaultObjectTrap : DefaultMapObject
    {
        [Header("피해/효과 설정")]
        [Tooltip("피해 타입")]
        [SerializeField] protected SkillConstants.DamageType damageType = SkillConstants.DamageType.Physic;
        [Tooltip("공격 시 가하는 총 피해량(1회성 공격 시 사용)")]
        [Min(0)] [SerializeField] protected long totalDamage = 1;
        [Tooltip("피격자에게 부여할 Affect Uid (0이면 부여 안 함)")]
        [Min(0)] [SerializeField] protected int targetAffectUid;
        [Tooltip("애니메이션 timescale")]
        [SerializeField] protected float animationTimeScale = 1;
        
        // ----------------------------
        // Internal State / Cache
        // ----------------------------
        protected enum TrapPhase { None, StartOneShot, Attack, EndOneShot }
        protected TrapPhase phase = TrapPhase.None;
        
        // 애니메이션 길이 캐시
        protected readonly Dictionary<string, float> clipLength = new();

        // 보유 여부 캐시
        protected bool hasStart, hasAttack, hasEnd;

        // 애니 이름 상수
        protected const string AnimStart = "start";
        protected const string AnimAttack = "attack";
        protected const string AnimEnd = "end";
        protected const string AnimWait = "wait";
        
        // 현재 트리거 내부의 대상(플레이어) 캐시
        protected CharacterBase playerInRange;

        [Header("레퍼런스")]
        [Tooltip("공격 판정용 트리거 Collider. 자동 탐색")]
        private Collider2D _attackRange;
        [Tooltip("공격 시작 트리거 Collider.")]
        [SerializeField] protected TrapTriggerDetector trapTriggerDetector;
        
        private IMapObjectAnimationController _animationController;
        private AnimationEventMediator _eventMediator;
        private const float DefaultOneShotTimeout = 0.2f;
        
        // 트랩 작동 여부
        private bool _isBusy;

        protected override void Awake()
        {
            base.Awake();
            
            // 1) 컨트롤러 결합 시도 (Spine → Sprite 순)
#if GGEMCO_USE_SPINE
            if (!TryBindSpine())
#endif
            {
                TryBindSpriteAnimator();
            }

            if (_animationController == null)
            {
                GcLogger.LogError("[ObjectTrapFixed] Animator 또는 Spine 컨트롤러를 찾지 못했습니다.");
                enabled = false;
                return;
            }

            // 공격 트리거 콜라이더 결합
            if (!_attackRange)
            {
                _attackRange = GetComponentInChildren<Collider2D>();
            }
            if (!_attackRange)
            {
                GcLogger.LogError("[ObjectTrapFixed] 공격용 Trigger Collider2D(attackRange)가 없습니다.");
                enabled = false;
                return;
            }

            if (trapTriggerDetector != null)
            {
                trapTriggerDetector.GetComponent<TrapTriggerDetector>()?.SetTargetTrap(this);
            }

            SetBusy(false);
            
            // 트리거 강제
            SetAttackRangeEnabled(false);
            SetTriggerRangeEnabled(false);

            // 애니 길이/보유 여부 캐시
            CacheAnimations();
        }
        // ----------------------------
        // Animator / Spine 바인딩
        // ----------------------------

#if GGEMCO_USE_SPINE
        private bool TryBindSpine()
        {
            var skeleton = GetComponent<SkeletonAnimation>();
            if (!skeleton) return false;

            var spineAnimController = GetComponent<CharacterAnimationControllerSpine>() ??
                                      gameObject.AddComponent<CharacterAnimationControllerSpine>();

            _animationController = spineAnimController.GetComponent<IMapObjectAnimationController>();

            // Spine 이벤트 → 미디에이터
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
                    // animationTimeScale 적용: 속도가 빠르면 재생 길이는 짧아짐
                    float scaledLength = kv.Value / Mathf.Max(animationTimeScale, 0.0001f);
                    clipLength[kv.Key] = scaledLength;
                }
            }

            hasStart = _animationController.HasAnimation(AnimStart);
            hasAttack = _animationController.HasAnimation(AnimAttack);
            hasEnd = _animationController.HasAnimation(AnimEnd);
        }

        /// <summary>피해 적용(공용 메서드)</summary>
        protected void ApplyDamage(CharacterBase player)
        {
            if (!player || totalDamage <= 0) return;

            var meta = new MetadataDamage
            {
                damage     = totalDamage,
                attacker   = gameObject,
                damageType = damageType,
                affectUid  = targetAffectUid
            };
            player.TakeDamage(meta);
        }
        // ----------------------------
        // Utilities
        // ----------------------------

        /// <summary>Player의 HitArea 콜라이더인지 검사하고 CharacterBase를 반환합니다.</summary>
        protected bool IsPlayerHitArea(Collider2D other, out CharacterBase player)
        {
            player = null;
            if (!other) return false;

            // 태그 기반 필터 (ConfigTags 사용)
            if (!other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return false;

            var hitArea = other.GetComponent<CharacterHitArea>();
            if (!hitArea) return false;

            player = hitArea.target;
            return player != null;
        }
        protected void SetAttackRangeEnabled(bool set)
        {
            if (!_attackRange) return;
            _attackRange.enabled = set;
            _attackRange.isTrigger = set;
        }

        protected void SetTriggerRangeEnabled(bool set)
        {
            if (!trapTriggerDetector) return;
            trapTriggerDetector.SetTriggerEnabled(set);
        }

        protected void PlayAnimSafe(string stateName, bool loop = false)
        {
            _animationController?.PlayMapObjectAnimation(stateName, loop, animationTimeScale);
        }
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // 에디터에서 음수 방지 클램프
            if (totalDamage <= 0) totalDamage = 0;
            if (targetAffectUid <= 0) targetAffectUid = 0;
        }
#endif
        protected float GetClipDuration(string clipName)
        {
            if (clipLength.TryGetValue(clipName, out var len) && len > 0f)
                return len + 0.02f; // 아주 작은 여유 버퍼
            return DefaultOneShotTimeout;
        }

        protected bool IsBusy()
        {
            return _isBusy;
        }

        protected void SetBusy(bool set)
        {
            _isBusy = set;
        }

        protected void SetPlayerInRange(CharacterBase player)
        {
            playerInRange = player;
        }

        public virtual void OnTrigger(Collider2D other)
        {
        }
    }
}