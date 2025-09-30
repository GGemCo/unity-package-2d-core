using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using Unity.Properties;
using UnityEngine;

namespace GGemCo2DCore
{
    public class ObjectTrapFixed : DefaultMapObject
    {
        [Header("설정")]
        [Tooltip("전조증상 표시 후 대기 시간(초)")]
        [SerializeField] private float timeEndStart;
        [Tooltip("공격 후 대기 시간(초)")]
        [SerializeField] private float timeEndAttack;
        [Tooltip("반복 공격 시간(초)")]
        [SerializeField] private float timeRepeat;
        [SerializeField] private long totalDamage;
        [SerializeField] private int targetAffectUid;
        [SerializeField] private SkillConstants.DamageType damageType;
        [Tooltip("무한 공격")]
        [SerializeField] private bool infinityAttack;
        [Tooltip("무한 공격일 때, 몇 초마다 공격 판정 할 것인지")]
        [SerializeField] private float timeInfinityAttack;
        private float _timeInfinityAttack;
        
        private IMapObjectAnimationController _mapObjectAnimationController;
        private AnimationEventMediator _animationEventMediator;
        private Collider2D _colliderAttackRange;

        // --- Phase ---
        private enum TrapPhase { None, StartOneShot, Attack, EndOneShot }
        private TrapPhase _phase = TrapPhase.None;
        // --- 워치독 ---
        private TrapPhase _awaitingEventFor = TrapPhase.None;
        private float _awaitingDeadline;
        private const float DefaultOneShotTimeout = 0.2f;
        // 애니메이션 길이 캐시
        private Dictionary<string, float> _clipLength = new();
        // --- 보유 여부 ---
        private bool _hasStart, _hasAttack, _hasEnd;
        // --- 애니메이션 이름 ---
        private const string AnimStart = "start";
        private const string AnimAttack  = "attack";
        private const string AnimEnd   = "end";
        
        protected override void Awake()
        {
            base.Awake();
            _mapObjectAnimationController = null;
#if GGEMCO_USE_SPINE
            var spine = GetComponent<SkeletonAnimation>();
            if (spine)
            {
                CharacterAnimationControllerSpine characterAnimationControllerSpine =
                    GetComponent<CharacterAnimationControllerSpine>();
                if (!characterAnimationControllerSpine) 
                    characterAnimationControllerSpine = gameObject.AddComponent<CharacterAnimationControllerSpine>();
                _mapObjectAnimationController = characterAnimationControllerSpine.GetComponent<IMapObjectAnimationController>();
                
                // Spine2dController 에 EventListener 설정
                var spineController = GetComponent<Spine2dController>();
                if (!spineController) 
                    spineController = gameObject.AddComponent<Spine2dController>();
                _animationEventMediator = new AnimationEventMediator();
                spineController.EventListener = _animationEventMediator;
            }
#endif
            if (_mapObjectAnimationController != null) return;
            var animator = GetComponent<Animator>();
            if (!animator)
            {
                GcLogger.LogError($"Animator 또는 Spine2d 컨트롤러가 없습니다.");
                return;
            }

            MapObjectAnimationControllerSprite characterAnimationControllerSprite =
                GetComponent<MapObjectAnimationControllerSprite>();
            if (!characterAnimationControllerSprite)
                characterAnimationControllerSprite = gameObject.AddComponent<MapObjectAnimationControllerSprite>();
            _mapObjectAnimationController = characterAnimationControllerSprite.GetComponent<IMapObjectAnimationController>();

            var animatorController = GetComponent<Animation2dController>();
            if (!animatorController)
                animatorController = gameObject.AddComponent<Animation2dController>();
            _animationEventMediator = new AnimationEventMediator();
            animatorController.EventListener = _animationEventMediator;
            
            
            
            _clipLength = _mapObjectAnimationController.GetAnimationAllLength();
            
            _hasStart = HasAnimation(AnimStart);
            _hasAttack  = HasAnimation(AnimAttack);
            _hasEnd   = HasAnimation(AnimEnd);

            _colliderAttackRange = GetComponentInChildren<Collider2D>();
            SetColliderEnable(false);
        }
        private bool HasAnimation(string stateName)
        {
            if (_mapObjectAnimationController is { } ctrl) return ctrl.HasAnimation(stateName);
            return false;
        }
        private void PlayAnimSafe(string stateName)
        {
            _mapObjectAnimationController?.PlayCharacterAnimation(stateName);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private void Start()
        {
            Invoke(nameof(StartTest), 2f);
        }

        private void StartTest()
        {
            SetColliderEnable(false);
            EnterPhase(TrapPhase.StartOneShot);
        }

        private float GetClipDurationWithFallback(string clipName)
        {
            if (_clipLength.TryGetValue(clipName, out var len) && len > 0f) return len + 0.02f;
            return DefaultOneShotTimeout;
        }
        private void StartAwaiting(TrapPhase phase, string clipName, float duration)
        {
            _awaitingEventFor = phase;
            _awaitingDeadline = Time.time + GetClipDurationWithFallback(clipName) + duration;
        }
        private void ClearAwaiting()
        {
            _awaitingEventFor = TrapPhase.None;
            _awaitingDeadline = 0f;
        }
        
        public void Update()
        {
            if (_phase == TrapPhase.None) return;

            // 워치독
            if (_awaitingEventFor != TrapPhase.None && Time.time >= _awaitingDeadline)
            {
                if (_awaitingEventFor == TrapPhase.StartOneShot) HandleStart();
                else if (_awaitingEventFor == TrapPhase.Attack) HandleAttack();
                else if (_awaitingEventFor == TrapPhase.EndOneShot) HandleEnd();
            }

            if (_phase == TrapPhase.Attack)
            {
            }
        }

        private void EnterPhase(TrapPhase next)
        {
            _phase = next;
            ClearAwaiting();

            switch (next)
            {
                case TrapPhase.StartOneShot:
                    // ApplyNoGravityDuringDash();                     // ← 대시 시작 시 중력 제거
                    if (_hasStart)
                    {
                        PlayAnimSafe(AnimStart);
                        StartAwaiting(next, AnimStart, timeEndStart);
                    }
                    else
                    {
                        HandleStart();
                    }
                    break;

                case TrapPhase.Attack:
                    // ApplyNoGravityDuringDash();                     // ← StartOneShot을 건너뛰는 폴백 대비
                    if (_hasAttack)
                    {
                        PlayAnimSafe(AnimAttack);
                        StartAwaiting(next, AnimAttack, timeEndAttack);
                    }
                    else
                    {
                        HandleAttack();
                    }
                    break;

                case TrapPhase.EndOneShot:
                    if (_hasEnd)
                    {
                        PlayAnimSafe(AnimEnd);
                        StartAwaiting(next, AnimEnd, 0);
                    }
                    else
                    {
                        HandleEnd();
                    }
                    break;
            }
        }

        private void SetColliderEnable(bool set)
        {
            if (_colliderAttackRange == null) return;
            _colliderAttackRange.enabled = set;
            _colliderAttackRange.isTrigger = set;
        }
        private void HandleStart()
        {
            if (_phase != TrapPhase.StartOneShot) return;
            ClearAwaiting();

            EnterPhase(TrapPhase.Attack);
            SetColliderEnable(true);
        }
        private void HandleAttack()
        {
            if (_phase != TrapPhase.Attack) return;
            ClearAwaiting();

            // 무한 공격이면, end 로 가지 않기
            if (infinityAttack) return;
            
            EnterPhase(TrapPhase.EndOneShot);
            SetColliderEnable(false);
        }
        private void HandleEnd()
        {
            if (_phase != TrapPhase.EndOneShot) return;
            ClearAwaiting();

            _phase  = TrapPhase.None;

            if (timeRepeat > 0)
            {
                StartCoroutine(StartAttack());
            }
        }

        private IEnumerator StartAttack()
        {
            yield return new WaitForSeconds(timeRepeat);
            StartTest();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other || !other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            CharacterHitArea characterHitArea = other.GetComponent<CharacterHitArea>();
            if (characterHitArea == null) return;
            
            CharacterBase player = characterHitArea.target;
            
            MetadataDamage metadataDamage = new MetadataDamage
            {
                damage = totalDamage,
                attacker = gameObject,
                damageType = damageType,
                affectUid = targetAffectUid
            };
            player.TakeDamage(metadataDamage);
            player.SetRigidBody2DSleepMode(RigidbodySleepMode2D.NeverSleep);
            _timeInfinityAttack = Time.time + timeInfinityAttack;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other || !other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            CharacterHitArea characterHitArea = other.GetComponent<CharacterHitArea>();
            if (characterHitArea == null) return;
            CharacterBase player = characterHitArea.target;
            player.SetRigidBody2DSleepMode(RigidbodySleepMode2D.StartAwake);
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            // 무한 공격이 아닌 경우, 지속 타격 로직 비활성
            if (!infinityAttack) return;
            
            if (!other || !other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            CharacterHitArea characterHitArea = other.GetComponent<CharacterHitArea>();
            if (characterHitArea == null) return;
            
            // 올바른 조건 (쿨다운 중이면 리턴)
            if (Time.time < _timeInfinityAttack) return;
            
            CharacterBase player = characterHitArea.target;
            
            MetadataDamage metadataDamage = new MetadataDamage
            {
                damage = 1,
                attacker = gameObject,
                damageType = damageType,
                affectUid = targetAffectUid
            };
            player.TakeDamage(metadataDamage);
            _timeInfinityAttack = Time.time + timeInfinityAttack;
        }
    }
}