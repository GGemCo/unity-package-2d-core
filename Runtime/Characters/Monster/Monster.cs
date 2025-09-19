using UnityEngine;
using R3;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 기본 클레스
    /// </summary>
    public class Monster : CharacterBase
    {
        // 선공/후공
        private CharacterConstants.AttackType _attackType;
        
        [Tooltip("X좌표 움직임 여부")]
        public bool canMoveX = true;
        [Tooltip("Y좌표 움직임 여부")]
        public bool canMoveY = true;
        
        // 몬스터 행동 처리
        private ControllerMonster _controllerMonster;
        private float _delayDestroyMonster;
        // 생명력 slier
        [HideInInspector] public GameObject sliderHpBar;
        private GameObject _prefabSliderHpBar;
        private Transform _containerMonsterHpBar;

        // 충돌 체크할 플레이어 수  
        private const int CountCollider = 10;
        private Collider2D[] _collider2Ds;
        
        private ProjectileManager _projectileManager;
        
        protected override void Awake()
        {
            // 먼저 선언한다.
            IsUseSkill = true;
            _collider2Ds = new Collider2D[CountCollider];
            base.Awake();
            _attackType = CharacterConstants.AttackType.PassiveDefense;
            
            CurrentHp
                .Subscribe(SetSliderHp)
                .AddTo(this);
            _delayDestroyMonster = AddressableLoaderSettings.Instance.settings.delayDestroyMonster;
        }

        protected override void Start()
        {
            base.Start();
            _projectileManager = SceneGame.Instance.ProjectileManager;
        }
        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.Monster);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected override void InitComponents()
        {
            // AddComponent 순서 중요
            base.InitComponents();
            
            // 순서 중요. ControllerMonster 에서 콜라이더를 사용
            _controllerMonster = gameObject.AddComponent<ControllerMonster>();
            _controllerMonster.Initialize(_collider2Ds);
        }
        /// <summary>
        /// regen_data 의 정보 셋팅
        /// </summary>
        protected override void InitializeByRegenData()
        {
            // 맵 배치툴로 저장한 정보가 있을 경우 
            if (CharacterRegenData == null) return;
            // UpdateDirection() 에서 초기 방향 처리를 위해 추가
            directionNormalize = new Vector3(CharacterRegenData.IsFlip?1:-1, 0, 0);
            SetFlip(CharacterRegenData.IsFlip);
            canMoveX = CharacterRegenData.CanMoveX;
            canMoveY = CharacterRegenData.CanMoveY;
        }
        /// <summary>
        /// 테이블에서 가져온 몬스터 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            base.InitializeByTable();
            if (TableLoaderManager.Instance == null) return;
            if (uid <= 0) return;
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            // monster 테이블 정보 셋팅
            var info = tableLoaderManager.GetMonsterData(uid);
            // GcLogger.Log("InitializationStat uid: "+uid+" / info.uid: "+info.uid+" / StatMoveSpeed: "+info.statMoveSpeed);
            if (info.Uid <= 0) return;
            characterName = info.Name;
            SetBaseInfos(info.StatAtk, info.StatDef, info.StatHp, 0, info.StatMoveSpeed, info.StatAttackSpeed,
                info.RegistFire, info.RegistCold, info.RegistLightning);
            CurrentHp.OnNext(info.StatHp);
            SetScale(info.Scale);
            _attackType = info.AttackType;
        }

        protected override bool InitializeByAnimationTable()
        {
            if (!base.InitializeByAnimationTable()) return false;
            
            int animationUid = 0;
            if (type == CharacterConstants.Type.Npc)
            {
                var info = TableLoaderManager.Instance.GetNpcData(uid);
                if (info == null) return false;
                animationUid = info.AnimationUid;
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                var info = TableLoaderManager.Instance.GetMonsterData(uid);
                if (info == null) return false;
                animationUid = info.AnimationUid;
            }
            if (animationUid <= 0) return false;
            StruckTableAnimation struckTableAnimation = TableLoaderManager.Instance.GetAnimationData(animationUid);
            if (struckTableAnimation is not { Uid: > 0 }) return false;
            
            return true;
        }
        public void CreateHpBar()
        {
            if (SceneGame.Instance.containerMonsterHpBar == null)
            {
                GcLogger.LogError("SceneGame 에 containerMonsterHpBar 가 설정되지 않았습니다.");
                return;
            }
            _prefabSliderHpBar = ConfigResources.SliderMonsterHp.Load();
            if (_prefabSliderHpBar == null) return;
            _containerMonsterHpBar = SceneGame.Instance.containerMonsterHpBar.transform;
            sliderHpBar = Instantiate(_prefabSliderHpBar, _containerMonsterHpBar);
            MonsterHpBar monsterHpBar = sliderHpBar.GetComponent<MonsterHpBar>();
            monsterHpBar.Initialize(this);
        }

        /// <summary>
        /// 데미지 받으면 어그로 on. 공격자 등록하기
        /// </summary>
        /// <param name="attacker"></param>
        public override void OnDamage(GameObject attacker)
        {
            base.OnDamage(attacker);
            if (IsAggro() == false)
            {
                SetAggro(true);
            }
            SetAttackerTarget(attacker.transform);
            _controllerMonster?.StopAttackCoroutine();
        }
        /// <summary>
        /// 몬스터가 죽었을때 처리 
        /// </summary>
        protected override void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            base.OnDead(dieReasonType, attacker);
            if (sliderHpBar != null)
            {
                Destroy(sliderHpBar);
            }

            _controllerMonster?.StopAllCoroutines();
            
            var isPlayer = attacker && attacker.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            var data = new MonsterKilledEventData(
                dieReasonType,
                mapUid: CharacterRegenData.MapUid,
                monsterUid: uid,
                monsterVid: vid,
                monster: gameObject,
                attacker: attacker,
                isPlayerKiller: isPlayer,
                killerUid: null
            );
            GameEventManager.MonsterKilled(data);
        }
        protected void OnDestroy()
        {
            if (sliderHpBar != null)
            {
                Destroy(sliderHpBar);
            }
        }
        /// <summary>
        /// attack 이벤트 처리 
        /// </summary>
        public override void OnEventAttack(StruckAnimationEventAttack struckAnimationEventAttack)
        {
            if (IsStatusDead()) return;
            
            // GcLogger.Log(@event);
            long totalDamage = TotalAtk.Value;
        
            // 캡슐 콜라이더 2D와 충돌 중인 모든 콜라이더를 검색
            Vector2 size = new Vector2(colliderAttackRange.size.x * Mathf.Abs(transform.localScale.x), colliderAttackRange.size.y * transform.localScale.y);
            Vector2 point = (Vector2)transform.position + colliderAttackRange.offset * transform.localScale;
#if UNITY_6000_0_OR_NEWER
            int hitCount = Physics2D.OverlapCapsule(point, size, colliderAttackRange.direction, 0f,
                new ContactFilter2D().NoFilter(), _collider2Ds);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
#else
            Physics2D.OverlapCapsuleNonAlloc(point, size, colliderCheckCharacter.direction, 0f, _collider2Ds);
            foreach (var hit in _collider2Ds)
            {
#endif
                if (!hit || !hit.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) continue;
                CharacterHitArea characterHitArea = hit.GetComponent<CharacterHitArea>();
                if (characterHitArea == null) continue;
                
                CharacterBase player = characterHitArea.target;
                
                MetadataDamage metadataDamage = new MetadataDamage
                {
                    damage = totalDamage,
                    attacker = gameObject,
                    damageType = SkillConstants.DamageType.Physic,
                    affectUid = struckAnimationEventAttack.TargetAffectUid
                };
                player.TakeDamage(metadataDamage);
                break;
            }

        }

        private void SetSliderHp(long value)
        {
            if (sliderHpBar == null) return;
            sliderHpBar.GetComponent<MonsterHpBar>().SetValue(value);
        }
        protected override void OnStartFadeIn()
        {
            if (sliderHpBar == null) return;
            sliderHpBar.GetComponent<MonsterHpBar>().StartFadeIn();
        }
        protected override void OnStartFadeOut()
        {
            if (sliderHpBar == null) return;
            sliderHpBar.GetComponent<MonsterHpBar>().StartFadeOut();
        }
        public override void OnAnimationCompleteDead()
        {
            base.OnAnimationCompleteDead();
            Destroy(gameObject, _delayDestroyMonster);
        }
    }
}
