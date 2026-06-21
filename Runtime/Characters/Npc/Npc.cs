using System.Collections.Generic;
using UnityEngine;
using R3;

namespace GGemCo2DCore
{
    /// <summary>
    /// Npc 기본 클레스
    /// </summary>
    public class Npc : CharacterBase
    {
        private GameObject _containerNpcName;
        private TagNameNpc _tagNameNpc;
        private GameObject _sliderHpBar;
        private GameObject _prefabSliderHpBar;
        private Transform _containerNpcHpBar;
        private StruckTableNpc _struckTableNpc;

        protected override void Awake()
        {
            base.Awake();
        }
        protected override void Start()
        {
            base.Start();
            
            CreateTagName();
            CreateHpBar();
            
            CurrentHp
                .Subscribe(SetSliderHp)
                .AddTo(this);
        }
        /// <summary>
        /// 아이템 이름 tag 만들기
        /// </summary>
        private void CreateTagName()
        {
            if (!_struckTableNpc.ShowNameTag) return;
            GameObject prefabTagNameNpc = ConfigResources.TextNpcNameTag.Load();
            if (prefabTagNameNpc == null) return;
            if (_containerNpcName == null)
            {
                _containerNpcName = SceneGame.Instance.containerDropItemName;
            }
            GameObject objectTagNameItem = Instantiate(prefabTagNameNpc, _containerNpcName.transform);
            if (objectTagNameItem == null) return;
            _tagNameNpc = objectTagNameItem.GetComponent<TagNameNpc>();
            if (_tagNameNpc == null) return;
            _tagNameNpc.Initialize(gameObject);
        }

        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.Npc);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected override void InitComponents()
        {
            base.InitComponents();
            gameObject.AddComponent<ControllerNpc>();
        }
        /// <summary>
        /// 테이블에서 가져온 npc 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            base.InitializeByTable();
            if (TableLoaderManager.Instance == null) return;
            if (uid <= 0) return;
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            _struckTableNpc = tableLoaderManager.GetNpcData(uid);
            // GcLogger.Log("InitializationStat uid: "+uid+" / info.uid: "+info.uid+" / StatMoveSpeed: "+info.statMoveSpeed);
            if (_struckTableNpc.Uid > 0)
            {
                const int statAtk = 0;
                const int statDef = 0;
                const int statMp = 0;
                const int statStamina = 0;
                const int statAttackSpeed = 0;
                const int statRegistFire = 0;
                const int statRegistCold = 0;
                const int statRegistLightning = 0;
                const int statRegistPoison = 0;
                SetBaseInfos(statAtk, statDef, _struckTableNpc.StatHp, statMp, statStamina, 0,
                    _struckTableNpc.StatMoveSpeed, statAttackSpeed, statRegistFire,
                    statRegistCold, statRegistLightning, statRegistPoison);
                float scale = _struckTableNpc.Scale;
                SetScale(scale);
                CurrentHp.OnNext(_struckTableNpc.StatHp);
            }
        }

        /// <summary>
        /// 리젠 데이터에 저장된 맵 배치 정보를 NPC 런타임 상태에 반영합니다.
        /// </summary>
        protected override void InitializeByRegenData()
        {
            // 맵 배치툴로 저장한 정보가 있을 경우 
            if (CharacterRegenData == null) return;

            // NPC도 같은 리젠 데이터 구조를 사용하므로 명시 표시 정책을 동일하게 반영합니다.
            SetMapVisibilityPolicy(CharacterRegenData.MapVisibilityPolicy);
            // UpdateDirection() 에서 초기 방향 처리를 위해 추가
            directionNormalize = new Vector3(CharacterRegenData.IsFlip?1:-1, 0, 0);
            SetFlip(CharacterRegenData.IsFlip);
        }
        
        public override void OnTriggerEnterByAttackRange(Collider2D collision)
        {
            base.OnTriggerEnterByAttackRange(collision);
            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return;
            var hitArea = collision.gameObject.GetComponent<CharacterHitArea>();
            if (!hitArea) return;
            SceneGame.Instance.InteractionManager.SetInfo(this, BuildInteractionTextContext());
        }

        /// <summary>
        /// NPC 테이블에 저장된 인터랙션 파라미터 문자열을 대사 텍스트 컨텍스트로 변환합니다.
        /// </summary>
        /// <returns>현재 NPC 인터랙션에 사용할 텍스트 컨텍스트입니다.</returns>
        internal InteractionDialogueTextContext BuildInteractionTextContext()
        {
            if (_struckTableNpc == null)
            {
                return InteractionDialogueTextContext.Empty;
            }

            InteractionDialogueTextContext staticContext =
                InteractionDialogueTextContextParser.Parse(_struckTableNpc.InteractionParameters);
            InteractionDialogueTextContext dynamicContext = BuildDynamicInteractionTextContext();
            return InteractionDialogueTextContext.Merge(staticContext, dynamicContext);
        }

        /// <summary>
        /// NPC 테이블에 지정된 동적 파라미터 키를 현재 런타임 값으로 해석합니다.
        /// </summary>
        /// <returns>현재 NPC 인터랙션에 사용할 동적 텍스트 컨텍스트입니다.</returns>
        private InteractionDialogueTextContext BuildDynamicInteractionTextContext()
        {
            if (_struckTableNpc == null)
            {
                return InteractionDialogueTextContext.Empty;
            }

            return InteractionDynamicParameterResolver.Resolve(_struckTableNpc.InteractionDynamicParameterKey);
        }

        public override bool OnTriggerExitByAttackRange(Collider2D collision)
        {
            if (!base.OnTriggerExitByAttackRange(collision)) return false;

            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return false;
            var hitArea = collision.gameObject.GetComponent<CharacterHitArea>();
            if (!hitArea) return false;
            SceneGame.Instance.InteractionManager.RemoveCurrentNpc();
            SceneGame.Instance.InteractionManager.EndInteraction();
            return true;
        }
        /// <summary>
        /// Destroy 되었을때 태그 지워주기
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_tagNameNpc != null)
            {
                Destroy(_tagNameNpc.gameObject);    
            }
            if (_sliderHpBar != null)
            {
                Destroy(_sliderHpBar.gameObject);    
            }
        }

        /// <summary>
        /// 몬스터가 죽었을때 처리 
        /// </summary>
        protected override void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            base.OnDead(dieReasonType, attacker);
            
            var isPlayer = attacker && attacker.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            OnStartFadeOut();
            SceneGame.Instance.ItemManager.OnNpcDead(uid, gameObject);
        }
        protected override void OnStartFadeIn()
        {
            if (_tagNameNpc != null)
            {
                _tagNameNpc.GetComponent<TagNameNpc>().StartFadeIn();
            }
            if (_sliderHpBar != null)
            {
                _sliderHpBar.GetComponent<NpcHpBar>().StartFadeIn();
            }
        }
        protected override void OnStartFadeOut()
        {
            if (_tagNameNpc != null)
            {
                _tagNameNpc.GetComponent<TagNameNpc>().StartFadeOut();
            }
            if (_sliderHpBar != null)
            {
                _sliderHpBar.GetComponent<NpcHpBar>().StartFadeOut();
            }
        }

        private void CreateHpBar()
        {
            if (SceneGame.Instance.containerMonsterHpBar == null)
            {
                GcLogger.LogError("SceneGame 에 containerNpcHpBar 가 설정되지 않았습니다.");
                return;
            }

            if (!_struckTableNpc.ShowHpBar) return;
            _prefabSliderHpBar = ConfigResources.SliderNpcHp.Load();
            if (_prefabSliderHpBar == null) return;
            _containerNpcHpBar = SceneGame.Instance.containerMonsterHpBar.transform;
            _sliderHpBar = Instantiate(_prefabSliderHpBar, _containerNpcHpBar);
            NpcHpBar monsterHpBar = _sliderHpBar.GetComponent<NpcHpBar>();
            monsterHpBar.Initialize(this);
        }
        private void SetSliderHp(long value)
        {
            if (_sliderHpBar == null) return;
            _sliderHpBar.GetComponent<NpcHpBar>().SetValue(value);
        }
        public override void OnAnimationCompleteDead()
        {
            base.OnAnimationCompleteDead();
            Destroy(gameObject);
        }

        public bool TryCaptureNameTagVisible(out bool isVisible)
        {
            isVisible = false;
            if (_tagNameNpc == null)
            {
                return false;
            }

            isVisible = _tagNameNpc.IsVisible();
            return true;
        }

        public void SetNameTagVisibleImmediate(bool isVisible)
        {
            if (_tagNameNpc == null)
            {
                return;
            }

            _tagNameNpc.SetVisibleImmediate(isVisible);
        }

        public bool IsSubCategoryTree()
        {
            return _struckTableNpc.SubCategory == CharacterConstantsNpc.NpcSubCategory.Tree;
        }

        public bool IsSubCategoryOre()
        {
            return _struckTableNpc.SubCategory == CharacterConstantsNpc.NpcSubCategory.Ore;
        }
    }
}
