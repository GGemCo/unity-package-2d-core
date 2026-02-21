using System;
using System.Collections.Generic;
using R3;
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
            InitializeStatPointSystem();
            // 연출중 체크를 위해 추가
            _controllerPlayer.Initialize(_sceneGame.CutsceneManager);
            _sceneGame.mapManager.OnLoadStartMap += OnLoadStartMap;

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

            // 플레이어 공격 영역에 몬스터 진입 상태
            // - Control 패키지에서 AutoMove Suspend 정책을 적용할 때 사용
            if (gameObject.GetComponent<PlayerAttackAreaState>() == null)
            {
                gameObject.AddComponent<PlayerAttackAreaState>();
            }

            if (AddressableLoaderSettings.Instance && AddressableLoaderSettings.Instance.settings &&
                AddressableLoaderSettings.Instance.settings.enableAutoMove)
            {
                // 자동 이동(오토 워크)
                // - Control 패키지 사용 시: InputManager가 IAutoMoveVectorProvider를 통해 이동 벡터를 오버라이드
                // - Core 단독 사용 시: PlayerAutoMoveController가 직접 Run() 호출
                // 순서 중요.
                gameObject.AddComponent<PlayerAutoMoveController>();
            }
        }

        /// <summary>
        /// GGemCoPlayerSettings 에서 가져온 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            SetBaseInfos(_playerSettings.statAtk, _playerSettings.statDef, _playerSettings.statHp,
                _playerSettings.statMp, _playerSettings.statStamina, 0,
                _playerSettings.statMoveSpeed, _playerSettings.statAttackSpeed, _playerSettings.statRegistFire,
                _playerSettings.statRegistCold, _playerSettings.statRegistLightning, _playerSettings.statRegistPoison);
            CurrentHp.OnNext(TotalHp.Value);
            CurrentMp.OnNext(TotalMp.Value);
            CurrentStamina.OnNext(TotalStamina.Value);
            CurrentSuperArmor.OnNext(0);
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
                long instanceId = info.Value.InstanceId;
                if (itemUid <= 0) continue;
                EquipItem(info.Key, itemUid, itemCount, instanceId);
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
                _sceneGame.ItemManager.PlayerTaken(collision.gameObject.GetComponent<Item>());
            }
        }
        /// <summary>
        /// 장비 장착하기
        /// </summary>
        /// <param name="partIndex"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        /// <param name="instanceId"></param>
        public void EquipItem(int partIndex, int itemUid, int itemCount, long instanceId = 0)
        {
            bool result = _equipController.EquipItem(partIndex, itemUid, instanceId);
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
            
            // ContactFilter2D.noFilter 사용 (필요하면 레이어/트리거 정책을 별도 생성해서 전달)
            int hitCount = CompatPhysics2D.OverlapCapsuleNonAlloc(
                point, size, colliderAttackRange.direction, 0f,
                _collider2Ds);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
                if (!hit || !hit.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) continue;
                CharacterHitArea characterHitArea = hit.GetComponent<CharacterHitArea>();
                if (characterHitArea == null) continue;
                
                // GcLogger.Log("Player attacked the monster after animation!");
                CharacterBase monster = characterHitArea.target;
                
                MetadataDamage metadataDamage = new MetadataDamage
                {
                    damage = totalDamage,
                    attacker = gameObject,
                    damageType = ConfigCommon.DamageType.Physic,
                    affectUid = struckAnimationEventAttack.TargetAffectUid,
                    crowdControlUid = struckAnimationEventAttack.TargetCrowdControlUid,
                    StaggerStackDamage = 1,
                    HitReactionType = CharacterConstants.HitReactionType.Flinch
                };

                // 몬스터와 마주보고 있으면 공격 
                if (AreFacingEachOther(monster))
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

        private void OnDestroy()
        {
            _sceneGame.mapManager.OnLoadStartMap -= OnLoadStartMap;
        }
        
        /// <summary>
        /// 플레이어 공격 영역에 몬스터가 있을 때, 플레이어의 자동 이동을 멈추도록 InputManager에 요청한다
        /// </summary>
        /// <param name="collision"></param>
        public override void OnTriggerEnterByAttackRange(Collider2D collision)
        {
            base.OnTriggerEnterByAttackRange(collision);
            
            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) return;
            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;

            PlayerAttackAreaState state = GetComponent<PlayerAttackAreaState>();
            if (state == null) return;
            // 플레이어 자동 이동 정지 하기
            state.Enter(hitArea.gameObject);
        }
        public override bool OnTriggerExitByAttackRange(Collider2D collision)
        {
            base.OnTriggerEnterByAttackRange(collision);
            
            if (!collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) return false;
            var hitArea = collision.gameObject.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return false;

            PlayerAttackAreaState state = gameObject.GetComponent<PlayerAttackAreaState>();
            if (state == null) return false;

            state.Exit(hitArea.gameObject);
            return true;
        }


        #region (스탯 포인트)

        public int UnspentStatPoints => _playerData?.UnspentStatPoints ?? 0;
        public int InvestedStatPointAtk => _playerData?.InvestedStatPointAtk ?? 0;
        public int InvestedStatPointDef => _playerData?.InvestedStatPointDef ?? 0;
        public int InvestedStatPointHp => _playerData?.InvestedStatPointHp ?? 0;
        public int InvestedStatPointMp => _playerData?.InvestedStatPointMp ?? 0;
        public int InvestedStatPointStamina => _playerData?.InvestedStatPointStamina ?? 0;

        public bool TryInvestStatPoint(CharacterConstants.IndexPlayerInfo statPointType, int amount = 1)
        {
            if (_playerData == null) return false;
            return _playerData.TryInvestStatPoint(statPointType, amount);
        }

        public bool TryRefundStatPoint(CharacterConstants.IndexPlayerInfo statPointType, int amount = 1)
        {
            if (_playerData == null) return false;
            return _playerData.TryRefundStatPoint(statPointType, amount);
        }

        /// <summary>
        /// 스탯 포인트 투자 상태를 일괄 적용합니다.
        /// - Apply 버튼 등 '원자적 커밋' 용도
        /// </summary>
        public bool TryApplyStatPointAllocation(
            int unspent,
            int investedAtk,
            int investedDef,
            int investedHp,
            int investedMp,
            int investedStamina)
        {
            if (_playerData == null) return false;
            // Apply 버튼은 '일괄 커밋'이므로, 변경 직후 totals가 즉시 갱신되어야
            // UIWindowPlayerInfo가 같은 프레임에 최신 값을 표시할 수 있습니다.
            // (Reactive 구독 경로는 스케줄링 타이밍에 따라 한 프레임 뒤에 반영될 수 있음)
            bool ok = _playerData.TryApplyStatPointAllocation(unspent, investedAtk, investedDef, investedHp, investedMp, investedStamina);
            if (!ok) return false;

            // 즉시 반영(HP/MP/Stamina 현재값은 보존)
            ApplyStatPointModifiersPreserveResources();
            return true;
        }

        /// <summary>
        /// (부작용 없음) 특정 스탯 포인트 투자 상태를 가정했을 때의 총합 스탯을 계산합니다.
        /// - UIWindowPlayerInfo 미리보기 용도
        /// </summary>
        public CharacterTotals CalculateProjectedTotalsForStatPoints(
            int investedAtk,
            int investedDef,
            int investedHp,
            int investedMp,
            int investedStamina)
        {
            var settings = _playerSettings != null ? _playerSettings : AddressableLoaderSettings.Instance.playerSettings;
            if (settings == null)
            {
                // settings가 없으면 현재 totals로 fallback
                return new CharacterTotals(
                    TotalAtk.Value, TotalDef.Value, TotalHp.Value, TotalMp.Value, TotalStamina.Value,
                    TotalSuperArmor.Value,
                    TotalMoveSpeed.Value, TotalAttackSpeed.Value,
                    TotalCriticalDamage.Value, TotalCriticalProbability.Value,
                    TotalRegistFire.Value, TotalRegistCold.Value, TotalRegistLightning.Value, TotalRegistPoison.Value);
            }

            var flat = new Dictionary<string, int>(8);
            var percent = new Dictionary<string, float>(8);

            AddStatPointBonus(settings.statPointAtk, investedAtk, ConfigCommon.StatusStatAtk, flat, percent);
            AddStatPointBonus(settings.statPointDef, investedDef, ConfigCommon.StatusStatDef, flat, percent);
            AddStatPointBonus(settings.statPointHp, investedHp, ConfigCommon.StatusStatHp, flat, percent);
            AddStatPointBonus(settings.statPointMp, investedMp, ConfigCommon.StatusStatMp, flat, percent);
            AddStatPointBonus(settings.statPointStamina, investedStamina, ConfigCommon.StatusStatStamina, flat, percent);

            return CalculateTotalsWithPersistentModifiers(flat, percent);
        }

        private void InitializeStatPointSystem()
        {
            if (_playerData == null) return;

            // 최초 1회 반영(세이브 로드 이후)
            ApplyStatPointModifiersPreserveResources();

            // 이후 변경 이벤트 구독(투자/회수/레벨업 지급 등)
            _playerData.OnStatPointsChanged()
                .Subscribe(_ => ApplyStatPointModifiersPreserveResources())
                .AddTo(this);
        }

        private void ApplyStatPointModifiersPreserveResources()
        {
            // 최대치 변경 시 현재값(HP/MP/Stamina)은 비율 유지
            long oldHpMax = TotalHp.Value;
            long oldMpMax = TotalMp.Value;
            long oldStaminaMax = TotalStamina.Value;

            long oldHpCur = CurrentHp.Value;
            long oldMpCur = CurrentMp.Value;
            long oldStaminaCur = CurrentStamina.Value;

            var settings = _playerSettings != null ? _playerSettings : AddressableLoaderSettings.Instance.playerSettings;
            if (settings == null) return;

            var flat = new Dictionary<string, int>(8);
            var percent = new Dictionary<string, float>(8);

            AddStatPointBonus(settings.statPointAtk, _playerData.InvestedStatPointAtk, ConfigCommon.StatusStatAtk, flat, percent);
            AddStatPointBonus(settings.statPointDef, _playerData.InvestedStatPointDef, ConfigCommon.StatusStatDef, flat, percent);
            AddStatPointBonus(settings.statPointHp, _playerData.InvestedStatPointHp, ConfigCommon.StatusStatHp, flat, percent);
            AddStatPointBonus(settings.statPointMp, _playerData.InvestedStatPointMp, ConfigCommon.StatusStatMp, flat, percent);
            AddStatPointBonus(settings.statPointStamina, _playerData.InvestedStatPointStamina, ConfigCommon.StatusStatStamina, flat, percent);

            SetStatPointModifiers(flat, percent);
            RecalculateStats();

            // 비율 유지(0/0 케이스는 무시)
            if (oldHpMax > 0 && TotalHp.Value > 0)
            {
                CurrentHp.OnNext(PreserveRatio(oldHpCur, oldHpMax, TotalHp.Value));
            }
            if (oldMpMax > 0 && TotalMp.Value > 0)
            {
                CurrentMp.OnNext(PreserveRatio(oldMpCur, oldMpMax, TotalMp.Value));
            }
            if (oldStaminaMax > 0 && TotalStamina.Value > 0)
            {
                CurrentStamina.OnNext(PreserveRatio(oldStaminaCur, oldStaminaMax, TotalStamina.Value));
            }
        }

        private static long PreserveRatio(long current, long oldMax, long newMax)
        {
            if (oldMax <= 0) return Math.Clamp(current, 0, newMax);
            float ratio = Mathf.Clamp01((float)current / oldMax);
            long v = Mathf.RoundToInt(ratio * newMax);
            return Math.Clamp(v, 0, newMax);
        }

        private static void AddStatPointBonus(GGemCoPlayerSettings.StatPointBonus bonus, int investedPoints, string statKey,
            Dictionary<string, int> flatOut, Dictionary<string, float> percentOut)
        {
            if (investedPoints <= 0) return;
            if (string.IsNullOrEmpty(statKey)) return;

            float total = investedPoints * bonus.valuePerPoint;
            if (Mathf.Approximately(total, 0f)) return;

            switch (bonus.mode)
            {
                case GGemCoPlayerSettings.StatPointBonusMode.Flat:
                    flatOut[statKey] = flatOut.GetValueOrDefault(statKey, 0) + Mathf.RoundToInt(total);
                    break;
                case GGemCoPlayerSettings.StatPointBonusMode.Percent:
                    percentOut[statKey] = percentOut.GetValueOrDefault(statKey, 0f) + total;
                    break;
            }
        }
        #endregion
    }
}