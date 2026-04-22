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

        private ContactFilter2D _attackHitFilter;
        private int _monsterHitAreaLayerMask;
        
        protected override void Awake()
        {
            onEventDeadByEndGround = new UnityEvent();
            // 먼저 선언한다.
            IsUseSkill = true;
            _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            _collider2Ds = new Collider2D[CountCollider];
        
            _monsterHitAreaLayerMask = LayerMask.GetMask(
                ConfigLayer.GetValue(ConfigLayer.Keys.HitAreaMonster));

            _attackHitFilter = CompatPhysics2D.CreateLayerFilter(
                _monsterHitAreaLayerMask,
                true);
            base.Awake();
            _playerUIController = new PlayerUIController();
            _playerUIController.Initialize(this);
        }

        /// <summary>
        /// 기본 방향 정책으로 스테미나 HUD 피격 피드백을 재생합니다.
        /// </summary>
        public void PlayDefaultStaminaDamageFeedback()
        {
            _playerUIController?.PlayStaminaDamageFeedback();
        }

        /// <summary>
        /// 공격자 Transform을 기준으로 스테미나 HUD 피격 피드백을 재생합니다.
        /// </summary>
        /// <param name="attackerTransform">피격을 발생시킨 공격자의 Transform입니다.</param>
        public void PlayStaminaDamageFeedbackFromAttacker(Transform attackerTransform)
        {
            if (_playerUIController == null)
            {
                return;
            }

            if (attackerTransform == null)
            {
                _playerUIController.PlayStaminaDamageFeedback();
                return;
            }

            _playerUIController.PlayStaminaDamageFeedbackFromAttacker(attackerTransform);
        }

        /// <summary>
        /// 플레이어는 자신의 Settings(주소어블 설정)를 우선 사용합니다.
        /// </summary>
        protected override GGemCoPlayerSettings GetPlayerSettingsForResourcePolicy()
        {
            return _playerSettings != null ? _playerSettings : base.GetPlayerSettingsForResourcePolicy();
        }

        protected override void Start()
        {
            // 순서 중요
            _sceneGame = SceneGame.Instance;
            _playerData = _sceneGame.saveDataManager.Player;
            base.Start();

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
            if (colliderHitArea)
                colliderHitArea.gameObject.layer = LayerMask.NameToLayer(ConfigLayer.GetValue(ConfigLayer.Keys.HitAreaPlayer));
        }

        /// <summary>
        /// GGemCoPlayerSettings 에서 가져온 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            
            // 저장된 Item Bonus Max HP(일반/임시) 복원
            // - PlayerData에 저장된 누적치를 Stat Provider(ItemBonusModifierProvider)에 다시 주입해야 TotalHp/TotalTempHp에 반영됩니다.
            // - 이 시점에 먼저 반영해두면, 아래의 startHp/startMp/startStamina 초기화가 "복원된 최대치" 기준으로 계산됩니다.
            if (_playerData != null)
            {
                // raiseEvent 를 true 로 해주어야 TotalHpTemp 가 업데이트 된다.
                SetItemBonusHpBonuses(_playerData.TotalItemBonusHpNormal, _playerData.TotalItemBonusHpTemp, raiseEvent: true);
            }
            
            SetBaseInfos(_playerSettings.statAtk, _playerSettings.statDef, _playerSettings.statHp,
                _playerSettings.statMp, _playerSettings.statStamina, 0,
                _playerSettings.statMoveSpeed, _playerSettings.statAttackSpeed, _playerSettings.statRegistFire,
                _playerSettings.statRegistCold, _playerSettings.statRegistLightning, _playerSettings.statRegistPoison);
            // 시작 자원 값은 '최대치'가 아니라, 설정에 따라 별도로 초기화할 수 있다.
            // (예: HP=최대치의 50%, MP=0, Stamina=최대치의 50% 등)
            CurrentHp.OnNext(_playerSettings.startHp.Evaluate(TotalHp.Value));
            CurrentMp.OnNext(_playerSettings.startMp.Evaluate(TotalMp.Value));
            CurrentStamina.OnNext(_playerSettings.startStamina.Evaluate(TotalStamina.Value));
            CurrentSuperArmor.OnNext(0);

            currentMoveStep = _playerSettings.statMoveStep;
            originalScaleX = transform.localScale.x;
            SetScale(_playerSettings.startScale);
            SetWidth(_playerSettings.size.x);
            SetHeight(_playerSettings.size.y);
            
            if (_playerData != null)
            {
                SetItemBonusHpCurrent(_playerData.CurrentItemBonusHpTemp);
                UpdateCurrentHpTemp();
            }
        }

        /// <summary>
        /// 아이템 사용으로 "일반 최대 HP" 누적치를 증가시킵니다(저장 + 스탯 반영).
        /// </summary>
        public override void AddItemBonusMaxHpNormal(long amount, bool raiseEvent = true)
        {
            if (amount <= 0) return;

            // ItemBonusModifierProvider를 통해서 TotalHpTemp 를 증가 시킨다.
            base.AddItemBonusMaxHpNormal(amount, raiseEvent);
            // 저장값 갱신
            _playerData?.AddTotalItemBonusHpNormal(amount);
        }

        public override void AddItemBonusMaxHpTemp(long amount, bool raiseEvent = true, bool fillCurrent = true)
        {
            base.AddItemBonusMaxHpTemp(amount, raiseEvent, fillCurrent);
            
            _playerData?.AddTotalItemBonusHpTemp(amount);
            
            if (fillCurrent)
                _playerData?.SetCurrentItemBonusHpTemp(TotalHpTempItem);
        }

        /// <summary>
        /// ItemBonusHpCurrent(임시/추가 HP 현재치)가 감소했을 때, “하트 1개 완전 소모” 여부를 판정하여
        /// ItemBonusHpTemp(임시/추가 HP 최대치)를 영구 감소(저장)합니다.
        /// </summary>
        /// <remarks>
        /// - ItemBonusHpTemp는 PlayerData에 저장되어, 게임 재시작 후에도 유지됩니다.
        /// - UI의 하트 삭제는 TotalHpTemp(=ItemBonusHpTemp 반영) 변화로 자연스럽게 발생합니다.
        /// </remarks>
        protected override void OnConsumedHpTempItem(long beforeCurrent, long afterCurrent, long consumedAmount)
        {
            base.OnConsumedHpTempItem(beforeCurrent, afterCurrent, consumedAmount);
            
            _playerData?.SetCurrentItemBonusHpTemp(afterCurrent);
            
            TryApplyPermanentItemBonusTempHeartDeletion(beforeCurrent, afterCurrent);
        }

        private void TryApplyPermanentItemBonusTempHeartDeletion(long beforeCurrent, long afterCurrent)
        {
            if (_playerData == null) return;
            if (_playerSettings == null) return;

            int perPiece = Mathf.Max(1, _playerSettings.itemBonusTempHpPerPiece);
            int piecesPerHeart = Mathf.Max(1, _playerSettings.itemBonusTempPiecesPerHeart);
            long heartHp = (long)perPiece * piecesPerHeart;
            if (heartHp <= 0) return;

            long maxBefore = _playerData.TotalItemBonusHpTemp;
            if (maxBefore <= 0) return;

            // 방어: 저장/런타임 불일치로 current가 max를 초과한 경우, 계산 안정화를 위해 클램프
            beforeCurrent = ClampLong(beforeCurrent, 0, maxBefore);
            afterCurrent = ClampLong(afterCurrent, 0, maxBefore);

            long consumedBefore = maxBefore - beforeCurrent;
            long consumedAfter = maxBefore - afterCurrent;
            if (consumedBefore < 0) consumedBefore = 0;
            if (consumedAfter < 0) consumedAfter = 0;

            long depletedHearts = (consumedAfter / heartHp) - (consumedBefore / heartHp);
            if (depletedHearts <= 0) return;

            long reduceHp = depletedHearts * heartHp;
            long newMax = maxBefore - reduceHp;
            if (newMax < 0) newMax = 0;

            // 1) 저장값(최대치) 영구 감소
            _playerData.SetTotalItemBonusHpTemp(newMax, save: false);

            // 2) 스탯 Provider 갱신 (TotalHpTemp에 반영)
            SetItemBonusHpBonuses(_playerData.TotalItemBonusHpNormal, _playerData.TotalItemBonusHpTemp, raiseEvent: true);

            // 3) 현재치 안전 보정 (일반적으로 afterCurrent가 newMax 이하이지만, 데이터 불일치 방어용)
            if (_playerData.CurrentItemBonusHpTemp > _playerData.TotalItemBonusHpTemp)
            {
                _playerData.SetCurrentItemBonusHpTemp(_playerData.TotalItemBonusHpTemp, save: false);
                SetItemBonusHpCurrent(_playerData.CurrentItemBonusHpTemp);
            }

            _playerData.SaveItemBonusHpState();
        }

        private static long ClampLong(long value, long min, long max)
        {
            if (value < min) return min;
            return value > max ? max : value;
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
            
            // 몬스터의 HitArea를 체크하기 위해 _monsterHitAreaLayerMask 적용 중
            int hitCount = CompatPhysics2D.OverlapCapsuleNonAlloc(point, size, colliderAttackRange.direction,
                0f, _attackHitFilter, _collider2Ds);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
                // if (!hit || !hit.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) continue;
                CharacterHitArea characterHitArea = hit.GetComponent<CharacterHitArea>();
                if (characterHitArea == null) continue;
                if (characterHitArea.target == null) continue;
                if (characterHitArea.target == this) continue;
                
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
        /// 플레이어 사망 처리
        /// - Item Bonus HP(추가 하트)는 즉시 소멸(0)되어야 하며, 저장에도 반영되어야 합니다.
        /// </summary>
        protected override void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None,
            GameObject attacker = null)
        {
            // 먼저 ItemBonus를 0으로 초기화(저장 구독이 연결되어 있다면 즉시 저장 반영)
            SetItemBonusHpCurrent(0);

            base.OnDead(dieReasonType, attacker);
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
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

        public int CurrentLevel => _playerData?.CurrentLevel ?? 1;
        public long CurrentGold => _playerData?.CurrentGold ?? 0;
        public long CurrentSilver => _playerData?.CurrentSilver ?? 0;

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

        public bool TryPurchaseStatPoints(int amount = 1)
        {
            if (_playerData == null) return false;
            return _playerData.TryPurchaseStatPoints(amount);
        }

        public bool CanPurchaseStatPoints()
        {
            return _playerData != null && _playerData.CanPurchaseStatPoints();
        }

        public bool CanAffordStatPointPurchase(int amount = 1)
        {
            return _playerData != null && _playerData.CanAffordStatPointPurchase(amount);
        }

        public CurrencyConstants.Type GetStatPointPurchaseCurrencyType()
        {
            return _playerData != null ? _playerData.GetStatPointPurchaseCurrencyType() : CurrencyConstants.Type.None;
        }

        public long GetStatPointPurchasePrice(int amount = 1)
        {
            return _playerData != null ? _playerData.GetStatPointPurchasePrice(amount) : 0;
        }

        public bool UsesReservedGoldBudgetForStatPointDraft()
        {
            return _playerData != null && _playerData.UsesReservedGoldBudgetForStatPointDraft();
        }

        public long GetReservedStatPointDraftPriceForAdditionalInvestCount(int additionalInvestCount)
        {
            return _playerData != null ? _playerData.GetReservedStatPointDraftPriceForAdditionalInvestCount(additionalInvestCount) : 0;
        }

        public long CalculateReservedStatPointDraftGoldCost(int originalUnspent, int originalInvestedTotal, int draftInvestedTotal)
        {
            return _playerData != null ? _playerData.CalculateReservedStatPointDraftGoldCost(originalUnspent, originalInvestedTotal, draftInvestedTotal) : 0;
        }

        public long GetPreviewGoldAfterReservedStatPointDraft(long reservedGold)
        {
            if (_playerData == null) return 0;
            return Math.Max(0L, _playerData.CurrentGold - reservedGold);
        }

        public bool CanAffordReservedStatPointDraftCost(long reservedCost)
        {
            return _playerData != null && _playerData.CanAffordReservedStatPointDraftCost(reservedCost);
        }

        public bool CanRefundCommittedStatPoints()
        {
            return _playerData == null || _playerData.CanRefundCommittedStatPoints();
        }

        public bool DoesStatPointInvestIncreaseLevel()
        {
            var settings = _playerSettings != null ? _playerSettings : AddressableLoaderSettings.Instance.playerSettings;
            if (settings == null) return false;
            return settings.statPointLevelUpOnInvestPolicy == GGemCoPlayerSettings.StatPointLevelUpOnInvestPolicy.IncreaseLevelByInvestedPoints;
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
            int investedStamina,
            long reservedDraftGoldCost = 0)
        {
            if (_playerData == null) return false;
            // Apply 버튼은 '일괄 커밋'이므로, 변경 직후 totals가 즉시 갱신되어야
            // UIWindowPlayerInfo가 같은 프레임에 최신 값을 표시할 수 있습니다.
            // (Reactive 구독 경로는 스케줄링 타이밍에 따라 한 프레임 뒤에 반영될 수 있음)
            bool ok = reservedDraftGoldCost > 0 || UsesReservedGoldBudgetForStatPointDraft()
                ? _playerData.TryApplyStatPointAllocationWithReservedDraftGold(unspent, investedAtk, investedDef, investedHp, investedMp, investedStamina, reservedDraftGoldCost)
                : _playerData.TryApplyStatPointAllocation(unspent, investedAtk, investedDef, investedHp, investedMp, investedStamina);
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

            using (SuppressAutoResourceSync())
            {
                SetStatPointModifiers(flat, percent);
                RecalculateStats();
            }

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
                case ConfigCommon.CalculateType.Flat:
                    flatOut[statKey] = flatOut.GetValueOrDefault(statKey, 0) + Mathf.RoundToInt(total);
                    break;
                case ConfigCommon.CalculateType.PercentOfMax:
                    percentOut[statKey] = percentOut.GetValueOrDefault(statKey, 0f) + total;
                    break;
            }
        }
        #endregion
    }
}