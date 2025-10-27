using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 
    /// </summary>
    public class Player : CharacterBase
    {
        public UnityEvent onEventDeadByEndGround;
        
        // 공격할 몬스터 
        private GameObject _targetMonster;
        private EquipController _equipController;
        private ToolController _toolController;
        private ControllerPlayer _controllerPlayer;
        private PlayerData _playerData;
        private SceneGame _sceneGame;
        // 충돌 체크할 몬스터 수  
        private const int CountCollider = 10;
        private Collider2D[] _collider2Ds;
        private GGemCoPlayerSettings _playerSettings;

        private PlayerUIController _playerUIController;

        protected override void Awake()
        {
            onEventDeadByEndGround = new UnityEvent();
            // 먼저 선언한다.
            IsUseSkill = true;
            _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            _collider2Ds = new Collider2D[CountCollider];
            base.Awake();
            _playerUIController = new PlayerUIController();
            _playerUIController.Initialize(this);
        }
        protected override void Start()
        {
            base.Start();
            _sceneGame = SceneGame.Instance;
            _playerData = _sceneGame.saveDataManager.Player;
            // 연출중 체크를 위해 추가
            _controllerPlayer.Initialize(_sceneGame.CutsceneManager);
            _sceneGame.mapManager.onLoadStartMap.AddListener(OnLoadStartMap);

            _playerUIController.InitSubscribe();

            LoadEquipItems();
        }
        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.Player);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected override void InitComponents()
        {
            // AddComponent 순서 중요
            base.InitComponents();
            _controllerPlayer = gameObject.AddComponent<ControllerPlayer>();
            _equipController = gameObject.AddComponent<EquipController>();
            _toolController = gameObject.AddComponent<ToolController>();
        }

        /// <summary>
        /// GGemCoPlayerSettings 에서 가져온 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            SetBaseInfos(_playerSettings.statAtk, _playerSettings.statDef, _playerSettings.statHp, _playerSettings.statMp,
                _playerSettings.statMoveSpeed, _playerSettings.statAttackSpeed, _playerSettings.statRegistFire,
                _playerSettings.statRegistCold, _playerSettings.statRegistLightning);
            CurrentHp.OnNext(TotalHp.Value);
            CurrentMp.OnNext(TotalMp.Value);
            currentMoveStep = _playerSettings.statMoveStep;
            originalScaleX = transform.localScale.x;
            SetScale(_playerSettings.startScale);
            SetWidth(_playerSettings.size.x);
            SetHeight(_playerSettings.size.y);
        }

        /// <summary>
        /// 세이브 데이터에 있는 장착 아이템 정보 가져와서 장착 시키기
        /// </summary>
        private void LoadEquipItems()
        {
            Dictionary<int, SaveDataIcon> dictionary =
                _sceneGame.saveDataManager.Equip.GetAllItemCounts();
            foreach (var info in dictionary)
            {
                if (info.Value == null) continue;
                int itemUid = info.Value.Uid;
                int itemCount = info.Value.Count;
                if (itemUid <= 0) continue;
                EquipItem(info.Key, itemUid, itemCount);
            }
        }
        protected void OnTriggerEnter2D(Collider2D collision)
        {
            // 워프 일때
            if (collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapObjectWarp)))
            {
                ObjectWarp objectWarp = collision.gameObject.GetComponent<ObjectWarp>();
                _sceneGame.mapManager.LoadMapByWarp(objectWarp);
            }
            // 드랍 아이템 일때
            else if (collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.DropItem)))
            {
                _sceneGame.ItemManager.PlayerTaken(collision.gameObject);
            }
        }
        /// <summary>
        /// 장비 장착하기
        /// </summary>
        /// <param name="partIndex"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        public void EquipItem(int partIndex, int itemUid, int itemCount)
        {
            bool result = _equipController.EquipItem(partIndex, itemUid);
            if (!result) return;
            if (itemUid <= 0)
            {
                UnEquipItem(partIndex);
                return;
            }
            CharacterAnimationController.ChangeCharacterImageInSlot(partIndex, itemUid);
        }
        /// <summary>
        /// 장비 해제 하기
        /// </summary>
        /// <param name="partIndex"></param>
        public void UnEquipItem(int partIndex)
        {
            bool result = _equipController.UnEquipItem(partIndex);
            if (!result) return;
            
            CharacterAnimationController.ChangeCharacterImageInSlot(partIndex);
        }
        /// <summary>
        /// attack 이벤트 처리 
        /// </summary>
        public override void OnEventAttack(StruckAnimationEventAttack struckAnimationEventAttack)
        {
            if (IsStatusDead()) return;
            
            // GcLogger.Log(@event);
            long totalDamage = CalculateFinalAttack();

            if (!colliderAttackRange)
            {
                GcLogger.LogError($"공격 범위 Collider가 없습니다.");
                return;
            }
        
            // 캡슐 콜라이더 2D와 충돌 중인 모든 콜라이더를 검색
            Vector2 size = new Vector2(colliderAttackRange.size.x * Mathf.Abs(transform.localScale.x), colliderAttackRange.size.y * transform.localScale.y);
            Vector2 point = (Vector2)transform.position + colliderAttackRange.offset * transform.localScale;
            
            int countDamageMonster = 0;
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
                if (!hit || !hit.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) continue;
                CharacterHitArea characterHitArea = hit.GetComponent<CharacterHitArea>();
                if (characterHitArea == null) continue;
                
                // GcLogger.Log("Player attacked the monster after animation!");
                CharacterBase monster = characterHitArea.target;
                
                MetadataDamage metadataDamage = new MetadataDamage
                {
                    damage = totalDamage,
                    attacker = gameObject,
                    damageType = SkillConstants.DamageType.Physic,
                    affectUid = struckAnimationEventAttack.TargetAffectUid
                };

                // 몬스터와 마주보고 있으면 공격 
                if (AreFacingEachOther(monster.transform))
                {
                    monster.TakeDamage(metadataDamage);
                    ++countDamageMonster;
                }
                // 몬스터와 같은 곳을 바라보고 있으면,
                else if (CurrentFacing == monster.CurrentFacing)
                {
                    switch (CurrentFacing)
                    {
                        case CharacterConstants.FacingDirection8.Right:
                        {
                            if (monster.transform.position.x >= transform.position.x)
                            {
                                monster.TakeDamage(metadataDamage);
                                ++countDamageMonster;
                            }
                            break;
                        }
                        case CharacterConstants.FacingDirection8.Left:
                        {
                            if (monster.transform.position.x <= transform.position.x)
                            {
                                monster.TakeDamage(metadataDamage);
                                ++countDamageMonster;
                            }
                            break;
                        }
                    }
                }
                        
                // CountCollider 마리 한테만 데미지 준다 
                if (countDamageMonster > CountCollider)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// 현재 생명력이 최대치인지
        /// </summary>
        /// <returns></returns>
        public bool IsMaxHp()
        {
            return CurrentHp.Value >= TotalHp.Value;
        }
        /// <summary>
        /// 현재 생명력 더하기
        /// </summary>
        /// <param name="value"></param>
        public void AddHp(int value)
        {
            long newVale = CurrentHp.Value + value;
            if (newVale > TotalHp.Value)
            {
                newVale = TotalHp.Value;
            }
            CurrentHp.OnNext(newVale);
        }
        /// <summary>
        /// 현재 마력이 최대치 인지
        /// </summary>
        /// <returns></returns>
        public bool IsMaxMp()
        {
            return CurrentMp.Value >= TotalMp.Value;
        }
        /// <summary>
        /// 현재 마력이 최대치 인지
        /// </summary>
        /// <returns></returns>
        public bool CheckNeedMp(int needMp)
        {
            return CurrentMp.Value >= needMp;
        }
        
        public bool IsRequireLevel(int compareLevel)
        {
            bool result = _playerData?.CurrentLevel >= compareLevel;
            if (!result)
            {
                _sceneGame.systemMessageManager.ShowMessageWarning($"Player_LevelTooLow"); //$"플레이어 레벨이 부족합니다. 필요 레벨 : {compareLevel}");
            }
            return result;
        }
        /// <summary>
        /// 어펙트 발동시 UIWindowPlayerBuffInfo 에 추가하기
        /// </summary>
        /// <param name="affectUid"></param>
        /// <param name="duration"></param>
        protected override void OnAffect(int affectUid, float duration = 0)
        {
            _playerUIController?.AddAffectIcon(affectUid, duration);
        }

        public void SetMapSize(Vector2 mapSize)
        {
            _controllerPlayer?.ChangeMapSize(mapSize);
        }
        /// <summary>
        /// 맵 이동 시작시 stop 처리 
        /// </summary>
        private void OnLoadStartMap()
        {
            Stop();
        }
        public override void OnAnimationCompleteDead()
        {
            base.OnAnimationCompleteDead();
            _sceneGame.SetState(SceneGame.GameState.End);
            Destroy(gameObject, 0.5f);
        }
        /// <summary>
        /// 사망 했다가 부활할 때, stat 리셋 해주기
        /// </summary>
        public void ResetStatsByDead()
        {
            InitializeByTable();
        }
        
        public override bool IsEquipSimulationTool()
        {
            return _toolController && _toolController.IsEquipSimulationTool();
        }
        public override bool IsEquipAxe()
        {
            return _toolController && _toolController.IsEquipAxe();
        }
        public override bool IsEquipPickAxe()
        {
            return _toolController && _toolController.IsEquipPickAxe();
        }
        public override bool IsEquipSickle()
        {
            return _toolController && _toolController.IsEquipSickle();
        }
        public override bool IsEquipHoe()
        {
            return _toolController && _toolController.IsEquipHoe();
        }
        public override bool IsEquipWatering()
        {
            return _toolController && _toolController.IsEquipWatering();
        }
        public override bool IsEquipSeed()
        {
            return _toolController && _toolController.IsEquipSeed();
        }

        public void EquipTool(int itemUid)
        {
            if (_toolController == null)
            {
                GcLogger.LogError($"{nameof(ToolController)} 가 없습니다.");
                return;
            }

            _toolController.Equip(itemUid);
        }
        public void UnEquipTool()
        {
            if (_toolController == null)
            {
                GcLogger.LogError($"{nameof(ToolController)} 가 없습니다.");
                return;
            }

            _toolController.UnEquip();
        }

        public StruckTableItem GetCurrentEquipTool()
        {
            return _toolController.GetCurrentTool();
        }
        public override void ChangePickUpSprite()
        {
            if (!characterPickUpPosition) return;
            var item = _toolController.GetCurrentTool();
                
            var key = "blank";
            if (item != null)
            {
                key = item.FileName;
            }
            var sprite = AddressableLoaderItem.Instance.GetImageIconItemByName(key);
            characterPickUpPosition.ChangePickUpSprite(sprite);
        }
    }
}