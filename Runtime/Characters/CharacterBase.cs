using System;
using System.Collections;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 공용 
    /// </summary>
    public class CharacterBase : CharacterStat
    {
        [Header("캐릭터 정보")]
        // 캐릭터 타입
        public CharacterConstants.Type type;
        // 캐릭터 테이블 Uid
        public int uid;
        // 스폰될때 부여되는 가상번호 vid
        public int vid;
        // 현재 이동 스텝
        public float currentMoveStep;
        // 어그로
        private CharacterConstants.AttackType attackType;
        private bool isAggro;
        public string characterName;
        
        [Header("캐릭터 방향 관련")]
        // 원본 방향
        public CharacterConstants.FacingDirection8 defaultFacingDirection8 = CharacterConstants.FacingDirection8.Left;
        // 현재 방향
        private CharacterConstants.FacingDirection8 currentFacing = CharacterConstants.FacingDirection8.Right;
        public CharacterConstants.FacingDirection8 CurrentFacing => currentFacing;
        // 좌우 flip 여부. 맵에 배치할 때, 연출 캐릭터 배치할 때 사용
        // 방향은 CurrentFacing 으로 판단한다. 
        public bool isFlip;
        // 방향
        public Vector3 directionNormalize;
        // 좌우 flip 가능 여부
        private bool isPossibleFlip = true;
        // 초기 scale x 값
        public float originalScaleX;
        


        [Header("애니메이션 및 렌더링 관련")]
        // 애니메이션 컨트롤러
        public ICharacterAnimationController CharacterAnimationController;
        private Renderer characterRenderer;
        private CharacterConstants.CharacterSortingOrder sortingOrder;
        
        [Header("상태 및 스탯")]
        protected readonly BehaviorSubject<long> CurrentHp = new(0);
        protected readonly BehaviorSubject<long> CurrentMp = new(0);

        [Header("스킬")] 
        protected bool IsUseSkill = false;
        private SkillController _skillController;
        
        [Header("스폰 데이터")] 
        public CharacterRegenData CharacterRegenData;
        
        // 현재 상태
        private CharacterConstants.CharacterStatus currentStatus;
        // 몬스터 죽은 후 맵에서 지우기까지에 시간
        private float delayDestroyMonster;
        // fade in, out 효과 시작 여부. 맵에서 컬링 될때 사용
        private bool isStartFade;
        private float characterHeight;
        private float characterWidth;
        // 공격한 GameObject 의 Transform
        public Transform attackerTransform;
        // 캐릭터 간의 충돌 체크용
        public CapsuleCollider2D colliderCheckCharacter;
        // 캐릭터 hit area 체크용
        public CapsuleCollider2D colliderCheckHitArea;
        // 맵 height 값, sorting order 계산에 사용
        private float mapSizeHeight;
        
        protected override void Awake()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            base.Awake();
            CharacterRegenData = null;
            AffectController = new AffectController(this);
            SetAttackType(CharacterConstants.AttackType.None);
            SetAggro(false);
            SetStatusIdle();
            // 태그 먼저 처리
            InitTagSortingLayer();
            InitComponents();
            delayDestroyMonster = AddressableLoaderSettings.Instance.settings.delayDestroyMonster;
            if (IsUseSkill)
            {
                _skillController = new SkillController();
                _skillController.Initialize(this);
            }
            defaultFacingDirection8 = AddressableLoaderSettings.Instance.playerSettings.facingDirection8;
        }
        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public virtual void InitTagSortingLayer()
        {
            if (characterRenderer == null)
            {
                characterRenderer = GetComponent<Renderer>();
            }
            characterRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.Character);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected virtual void InitComponents()
        {
            
        }
        protected override void Start()
        {
            base.Start();
            originalScaleX = transform.localScale.x;
            
            InitializeByTable();
            InitializeByAnimationTable();
            InitializeByRegenData();
            
            TotalMoveSpeed
                .Subscribe(UpdateAnimationMoveTimeScale)
                .AddTo(this);

            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            mapSizeHeight = size.y;
            Stop();
        }
        /// <summary>
        /// 테이블에서 가져온 몬스터 정보 셋팅
        /// </summary>
        protected virtual void InitializeByTable()
        {
        }
        /// <summary>
        /// regen_data 의 정보 셋팅
        /// </summary>
        protected virtual void InitializeByRegenData()
        {
            
        }
        /// <summary>
        /// animation 테이블 정보 셋팅
        /// </summary>
        private void InitializeByAnimationTable()
        {
            if (uid <= 0) return;
            int animationUid = 0;
            if (type == CharacterConstants.Type.Npc)
            {
                var info = TableLoaderManager.Instance.GetNpcData(uid);
                if (info == null) return;
                animationUid = info.AnimationUid;
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                var info = TableLoaderManager.Instance.GetMonsterData(uid);
                if (info == null) return;
                animationUid = info.AnimationUid;
            }
            if (animationUid <= 0) return;
            StruckTableAnimation struckTableAnimation = TableLoaderManager.Instance.GetAnimationData(animationUid);
            if (struckTableAnimation is not { Uid: > 0 }) return;
            currentMoveStep = struckTableAnimation.MoveStep;
            if (colliderCheckCharacter != null)
            {
                colliderCheckCharacter.size = new Vector2(struckTableAnimation.AttackRange, struckTableAnimation.AttackRange/2f);
            }
            if (colliderCheckHitArea != null)
            {
                colliderCheckHitArea.offset = new Vector2(0, struckTableAnimation.Height/2f);
                colliderCheckHitArea.size = struckTableAnimation.HitAreaSize;
            }

            SetHeight(struckTableAnimation.Height);
            defaultFacingDirection8 = struckTableAnimation.DefaultFacingDirection8;
        }
        /// <summary>
        /// 캐릭터가 flip 되었는지 체크
        /// </summary>
        /// <returns></returns>
        // public bool IsFlipped() {
        //     return Mathf.Approximately(transform.localScale.x, originalScaleX * -1f);
        // }
        public void SetIsPossibleFlip(bool set) => isPossibleFlip = set;

        private bool IsPossibleFlip() => isPossibleFlip;
        /// <summary>
        /// 캐릭터 방향 셋팅하기
        /// </summary>
        /// <param name="value"></param>
        public void SetFlip(bool value)
        {
            if (IsPossibleFlip() != true) return;
            switch (defaultFacingDirection8)
            {
                case CharacterConstants.FacingDirection8.Left:
                    SetFacing(value ? CharacterConstants.FacingDirection8.Right : CharacterConstants.FacingDirection8.Left);
                    break;
                case CharacterConstants.FacingDirection8.Right:
                    SetFacing(value ? CharacterConstants.FacingDirection8.Left : CharacterConstants.FacingDirection8.Right);
                    break;
                case CharacterConstants.FacingDirection8.None:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.Up:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
                case CharacterConstants.FacingDirection8.Down:
                case CharacterConstants.FacingDirection8.DownRight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public void SetFacing(CharacterConstants.FacingDirection8 dir)
        {
            if (IsPossibleFlip() != true || dir == CharacterConstants.FacingDirection8.None) return;
            currentFacing = dir;

            float sign = 1;
            if ((defaultFacingDirection8 == CharacterConstants.FacingDirection8.Right &&
                dir == CharacterConstants.FacingDirection8.Left) || 
                (defaultFacingDirection8 == CharacterConstants.FacingDirection8.Left && 
                dir == CharacterConstants.FacingDirection8.Right))
            {
                sign = -1;
            }

            transform.localScale = new Vector3(originalScaleX * sign, transform.localScale.y, transform.localScale.z);
        }
        /// <summary>
        /// 타겟 오브젝트가 있을경우 방향 셋팅하기
        /// </summary>
        /// <param name="targetTransform"></param>
        protected void SetFlipToTarget(Transform targetTransform)
        {
            SetFlip(transform.position.x <= targetTransform.position.x);
        }
        /// <summary>
        /// 실제 보는 방향: 디폴트 방향이 오른쪽이면 localScale.x가 양수면 오른쪽, 음수면 왼쪽
        /// </summary>
        /// <returns></returns>
        private float GetFacingDirection()
        {
            float sign = Mathf.Sign(transform.localScale.x);
            return defaultFacingDirection8 == CharacterConstants.FacingDirection8.Right ? sign : -sign;
        }
        /// <summary>
        /// 플레이어와 몬스터가 마주보고 있는지 체크 
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        protected bool AreFacingEachOther(Transform monster)
        {
            CharacterBase player = GetComponent<CharacterBase>();
            CharacterBase monsterChar = monster.GetComponent<CharacterBase>();

            float playerDir = player.GetFacingDirection();
            float monsterDir = monsterChar.GetFacingDirection();

            float directionToMonster = Mathf.Sign(monster.position.x - transform.position.x);

            return Mathf.Approximately(playerDir, directionToMonster) && Mathf.Approximately(monsterDir, -directionToMonster);
        }
        /// <summary>
        /// 캐릭터 순서. sorting order 처리 
        /// </summary>
        private void UpdatePosition()
        {
            if (sortingOrder == CharacterConstants.CharacterSortingOrder.Fixed) return;

            int baseSortingOrder = MathHelper.GetSortingOrder(mapSizeHeight, transform.position.y);
            
            baseSortingOrder = sortingOrder switch
            {
                CharacterConstants.CharacterSortingOrder.AlwaysOnTop => CharacterConstants.SortingOrderTop,
                CharacterConstants.CharacterSortingOrder.AlwaysOnBottom => CharacterConstants.SortingOrderBottom,
                _ => baseSortingOrder
            };

            characterRenderer.sortingOrder = baseSortingOrder;
        }
        protected virtual void Update()
        {
            if (IsStatusDead()) return;
            UpdatePosition();
        }
        /// <summary>
        /// 강제로 이동시키기
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void MoveTeleport(float x, float y)
        {
            transform.position = new Vector3(x, y, transform.position.z);
        }
        public bool IsStatusDead() => currentStatus == CharacterConstants.CharacterStatus.Dead;
        public bool IsStatusAttack() => currentStatus == CharacterConstants.CharacterStatus.Attack;
        public bool IsStatusDontMove() => currentStatus == CharacterConstants.CharacterStatus.DontMove;
        public bool IsStatusRun() => currentStatus == CharacterConstants.CharacterStatus.Run;
        public bool IsStatusIdle() => currentStatus == CharacterConstants.CharacterStatus.Idle;
        public bool IsStatusNone() => currentStatus == CharacterConstants.CharacterStatus.None;
        public bool IsStatusMoveForce() => currentStatus == CharacterConstants.CharacterStatus.MoveForce;
        public bool IsStatusDamage() => currentStatus == CharacterConstants.CharacterStatus.Damage;
        public CharacterConstants.CharacterStatus GetCurrentStatus() => currentStatus;
        
        private void SetStatus(CharacterConstants.CharacterStatus value) => currentStatus = value;
        public void SetStatusDead() => SetStatus(CharacterConstants.CharacterStatus.Dead);
        public void SetStatusIdle() => SetStatus(CharacterConstants.CharacterStatus.Idle);
        public void SetStatusRun() => SetStatus(CharacterConstants.CharacterStatus.Run);
        public void SetStatusAttack() => SetStatus(CharacterConstants.CharacterStatus.Attack);
        public void SetStatusDontMove() => SetStatus(CharacterConstants.CharacterStatus.DontMove);
        public void SetStatusMoveForce() => SetStatus(CharacterConstants.CharacterStatus.MoveForce);
        private void SetStatusDamage() => SetStatus(CharacterConstants.CharacterStatus.Damage);

        public void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 0);
            originalScaleX = scale;
        }
        /// <summary>
        /// fade in 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeIn()
        {
            if (isStartFade) return;
            isStartFade = true;
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
            OnStartFadeIn();
        }

        protected virtual void OnStartFadeIn()
        {
        }

        /// <summary>
        /// fade out 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeOut()
        {
            if (isStartFade) return;
            isStartFade = true;
            StartCoroutine(FadeOut(ConfigCommon.CharacterFadeSec));
            OnStartFadeOut();
        }

        protected virtual void OnStartFadeOut()
        {
        }

        private IEnumerator FadeIn(float duration)
        {
            yield return CharacterAnimationController.FadeEffect(duration, true);
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return CharacterAnimationController.FadeEffect(duration, false);
            gameObject.SetActive(false);
        }
        public void SetIsStartFade(bool value)
        {
            isStartFade = value;
        }

        public float GetHeight()
        {
            return characterHeight;
        }

        protected void SetHeight(float value)
        {
            characterHeight = value;
        }
        /// <summary>
        /// localScale 이 적용된 캐릭터 크기 가져오기
        /// </summary>
        /// <returns></returns>
        public virtual float GetHeightByScale()
        {
            return characterHeight * Math.Abs(transform.localScale.x);
        }
        public float GetWidth()
        {
            return characterWidth;
        }

        protected void SetWidth(float value)
        {
            characterWidth = value;
        }
        public virtual float GetCurrentMoveStep()
        {
            return currentMoveStep;
        }
        /// <summary>
        /// attack 이벤트 처리 
        /// </summary>
        public virtual void OnEventAttack()
        {
        }
        /// <summary>
        /// 캐릭터가 죽었을때 처리 
        /// </summary>
        protected virtual void OnDead()
        {
            CharacterAnimationController.PlayDeadAnimation();
            // 어펙트 모두 지우기
            if (AffectController != null)
            {
                AffectController.RemoveAllAffects();
            }
        }
        /// <summary>
        /// 내가 데미지 받았을때 처리 
        /// </summary>
        /// <param name="damage">받은 데미지</param>
        /// <param name="attacker">누가 때렸는지</param>
        /// <param name="damageType">속성 데미지 타입</param>
        public bool TakeDamage(long damage, GameObject attacker, SkillConstants.DamageType damageType = SkillConstants.DamageType.None)
        {
            if (SceneGame.Instance.CutsceneManager.IsPlaying()) return false;
            if (IsStatusDead())
            {
                // GcLogger.Log("monster dead");
                return false;
            }
            if (damage <= 0) return false;
            
            // 데미지 텍스트 색상 설정
            Color damageTextColor = Color.white;
            Vector3 damageTextPosition = transform.position + new Vector3(0, GetHeight() * Mathf.Abs(originalScaleX), 0);
            // 속성 데미지일때, 저항값 처리
            if (damageType != SkillConstants.DamageType.None)
            {
                if (damageType == SkillConstants.DamageType.Fire)
                {
                    damage = (long)(damage * ((100f - TotalRegistFire.Value) / 100f));
                    damageTextColor = Color.red;
                }
                else if (damageType == SkillConstants.DamageType.Cold)
                {
                    damage = (long)(damage * ((100f - TotalRegistCold.Value) / 100f));
                    damageTextColor = Color.blue;
                }
                else if (damageType == SkillConstants.DamageType.Lightning)
                {
                    damage = (long)(damage * ((100f - TotalRegistLightning.Value) / 100f));
                    damageTextColor = Color.yellow;
                }

                if (damage <= 0)
                {
                    MetadataDamageText metadataDamageText = new MetadataDamageText
                    {
                        Damage = damage,
                        Color = Color.yellow,
                        SpecialDamageText = "immune",
                        WorldPosition = damageTextPosition,
                        FontSize = 20
                    };
                    SceneGame.Instance.damageTextManager.ShowDamageText(metadataDamageText);
                }
            }
            if (damage <= 0) return false;

            long remainHp = CurrentHp.Value - damage;
            // -1 이면 죽지 않는다
            if (BaseHp < 0)
            {
                remainHp = 1;
            }

            if (CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                damageTextColor = Color.red;
            }
            MetadataDamageText metadataDamageText2 = new MetadataDamageText
            {
                Damage = damage,
                Color = damageTextColor,
                WorldPosition = damageTextPosition
            };
            SceneGame.Instance.damageTextManager.ShowDamageText(metadataDamageText2);
            
            if (remainHp <= 0)
            {
                currentStatus = CharacterConstants.CharacterStatus.Dead;
                Destroy(gameObject, delayDestroyMonster);

                OnDead();
            }
            else
            {
                OnDamage(attacker);
            }
            CurrentHp.OnNext(remainHp);

            return true;
        }

        protected virtual void OnDamage(GameObject attacker)
        {
            SetStatusDamage();
            CharacterAnimationController?.PlayDamageAnimation();
        }
        /// <summary>
        /// 공격한 오브젝트 설정하기 
        /// </summary>
        /// <param name="attacker"></param>
        public void SetAttackerTarget(Transform attacker)
        {
            attackerTransform = attacker;
        }

        public bool IsAttackerStatusDead()
        {
            if (attackerTransform == null || attackerTransform.GetComponent<CharacterBase>() == null) return false;
            return attackerTransform.GetComponent<CharacterBase>().IsStatusDead();
        }

        public void SetAggro(bool set)
        {
            isAggro = set;
        }
        public bool IsAggro()
        {
            return isAggro;
        }
        public CharacterConstants.AttackType GetAttackType()
        {
            return attackType;
        }

        private void SetAttackType(CharacterConstants.AttackType pattackType)
        {
            attackType = pattackType;
        }
        /// <summary>
        /// 어펙트 추가하기
        /// </summary>
        /// <param name="affectUid"></param>
        public void AddAffect(int affectUid)
        {
            var info = TableLoaderManager.Instance.GetAffectData(affectUid);
            if (info == null)
            {
                GcLogger.LogError("affect 테이블에 없는 어펙트 입니다. affect Uid: "+affectUid);
                return;
            }
            ApplyAffect(affectUid);

            OnAffect(affectUid);
        }

        protected virtual void OnAffect(int affectUid)
        {
            
        }
        /// <summary>
        /// total move speed 가 변경되었을때 wait 애니메이션의 time scale 도 변경해주기 위해서
        /// track index = 0 의 time scale 을 변경해준다.
        /// </summary>
        /// <param name="value"></param>
        private void UpdateAnimationMoveTimeScale(long value)
        {
            CharacterAnimationController.UpdateTimeScaleMove(value/100f);
        }
        /// <summary>
        /// 현재 마력 더하기
        /// </summary>
        /// <param name="value"></param>
        public void AddMp(int value)
        {
            long newVale = CurrentMp.Value + value;
            if (newVale > TotalMp.Value)
            {
                newVale = TotalMp.Value;
            }
            CurrentMp.OnNext(newVale);
        }
        /// <summary>
        /// 현재 마력 빼기
        /// </summary>
        /// <param name="value"></param>
        public void MinusMp(int value)
        {
            long newVale = CurrentMp.Value - value;
            if (newVale < 0)
            {
                newVale = 0;
            }
            CurrentMp.OnNext(newVale);
        }
        /// <summary>
        /// disable 되었을때 어펙트 효과 모두 지워주기
        /// </summary>
        private void OnDisable()
        {
            AffectController?.RemoveAllAffects();
        }

        public void Stop()
        {
            if (IsStatusDead()) return;
            
            SetStatusIdle();
            CharacterAnimationController?.PlayWaitAnimation();
        }
        public float GetRandomPositionYInHitArea()
        {
            if (!colliderCheckHitArea)
            {
                return transform.position.y;
            }
            // 캡슐의 로컬 공간 기준 Y 범위 계산
            float halfHeight = colliderCheckHitArea.size.y / 2f;
            float minLocalY = colliderCheckHitArea.offset.y - halfHeight;
            float maxLocalY = colliderCheckHitArea.offset.y + halfHeight;

            // 로컬 Y 기준 무작위 값
            float randomLocalY = Random.Range(minLocalY, maxLocalY);

            // 로컬 좌표 → 월드 좌표 변환
            Vector2 localPoint = new Vector2(0f, randomLocalY);
            Vector2 worldPoint = transform.TransformPoint(localPoint);
            return worldPoint.y;
        }
        public virtual void LaunchProjectile(int projectileUid)
        {
            var info = TableLoaderManager.Instance.GetProjectileData(projectileUid);
            if (info == null) return;
            StartCoroutine(CreateProjectile(info));
        }
        protected virtual IEnumerator CreateProjectile(StruckTableProjectile info)
        {
            for (int i = 0; i < info.Count; i++)
            {
                yield return new WaitForSeconds(info.SecDelayByOne);
            }
        }

        public virtual void UseSkill(int skillUid, int skillLevel)
        {
            _skillController?.MakeSkill(skillUid, skillLevel);
        }
    }
}