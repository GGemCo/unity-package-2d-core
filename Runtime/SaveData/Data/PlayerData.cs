using System.Collections.Generic;
using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 - 플레이어 정보
    /// </summary>
    public class PlayerData : DefaultData, ISaveData
    {
        private enum PlayerLevelChangeReason
        {
            Exp = 0,
            StatPointInvestment = 1,
        }

        private int _maxPlayerLevel;
        private TableLoaderManager _tableLoaderManager;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private SaveDataManager _saveDataManager;
        
        private readonly BehaviorSubject<int> _currentMapUid = new(0);
        private readonly BehaviorSubject<int> _currentLevel = new(1);
        private readonly BehaviorSubject<long> _currentExp = new(0);
        private readonly BehaviorSubject<long> _currentNeedExp = new(0);
        private readonly BehaviorSubject<long> _currentGold = new(0);
        private readonly BehaviorSubject<long> _currentSilver = new(0);
        // Stat Point (스탯 포인트)
        private readonly BehaviorSubject<int> _unspentStatPoints = new(0);
        private readonly BehaviorSubject<int> _investedStatPointAtk = new(0);
        private readonly BehaviorSubject<int> _investedStatPointDef = new(0);
        private readonly BehaviorSubject<int> _investedStatPointHp = new(0);
        private readonly BehaviorSubject<int> _investedStatPointMp = new(0);
        private readonly BehaviorSubject<int> _investedStatPointStamina = new(0);

        // 아이템 보너스 최대 HP(일반/임시) - 저장 대상
        public long TotalItemBonusHpNormal;
        public long TotalItemBonusHpTemp;

        // 임시 HP(Current) - 저장 대상(추가 하트/보호막의 현재치)
        public long CurrentItemBonusHpTemp;

        // 일괄 업데이트 중(Apply 버튼 등) 자동 저장/이벤트 폭주를 줄이기 위한 플래그
        private bool _isBatchUpdating;
        // 구독 초기화 직후 BehaviorSubject의 초기 1회 발행으로 저장이 호출되는 것을 막기 위한 플래그
        private bool _isInitializingAutoSaveSubscriptions;
        private static long ClampLong(long value, long min, long max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public int CurrentMapUid
        {
            get => _currentMapUid.Value;
            set => _currentMapUid.OnNext(value);
        }

        public int CurrentLevel
        {
            get => _currentLevel.Value;
            set => _currentLevel.OnNext(value);
        }

        public long CurrentExp
        {
            get => _currentExp.Value;
            set => _currentExp.OnNext(value);
        }
        public long CurrentGold
        {
            get => _currentGold.Value;
            set => _currentGold.OnNext(value);
        }
        public long CurrentSilver
        {
            get => _currentSilver.Value;
            set => _currentSilver.OnNext(value);
        }

        // =========================
        // Stat Point (스탯 포인트) - JSON 직렬화 대상(프로퍼티로 노출)
        // =========================
        public int UnspentStatPoints
        {
            get => _unspentStatPoints.Value;
            set => _unspentStatPoints.OnNext(Mathf.Max(0, value));
        }

        public int InvestedStatPointAtk
        {
            get => _investedStatPointAtk.Value;
            set => _investedStatPointAtk.OnNext(Mathf.Max(0, value));
        }

        public int InvestedStatPointDef
        {
            get => _investedStatPointDef.Value;
            set => _investedStatPointDef.OnNext(Mathf.Max(0, value));
        }

        public int InvestedStatPointHp
        {
            get => _investedStatPointHp.Value;
            set => _investedStatPointHp.OnNext(Mathf.Max(0, value));
        }

        public int InvestedStatPointMp
        {
            get => _investedStatPointMp.Value;
            set => _investedStatPointMp.OnNext(Mathf.Max(0, value));
        }

        public int InvestedStatPointStamina
        {
            get => _investedStatPointStamina.Value;
            set => _investedStatPointStamina.OnNext(Mathf.Max(0, value));
        }

        /// <summary>
        /// 스탯 포인트(미사용/투자) 변경 이벤트
        /// </summary>
        public Observable<Unit> OnStatPointsChanged()
        {
            // NOTE:
            // CombineLatest 결과를 Unit.Default로 매핑한 뒤 DistinctUntilChanged()를 걸면,
            // 이후 모든 이벤트가 동일(Unit.Default)로 간주되어 첫 1회만 통과하는 문제가 발생합니다.
            // 각 값 스트림에서 변화를 필터링하고, 최종 Unit에는 Distinct를 적용하지 않습니다.
            return _unspentStatPoints.DistinctUntilChanged()
                .CombineLatest(
                    _investedStatPointAtk.DistinctUntilChanged(),
                    _investedStatPointDef.DistinctUntilChanged(),
                    _investedStatPointHp.DistinctUntilChanged(),
                    _investedStatPointMp.DistinctUntilChanged(),
                    _investedStatPointStamina.DistinctUntilChanged(),
                    (_, _, _, _, _, _) => Unit.Default);
        }
        // json 에 포함되지 않도록 함수화 
        public Observable<int> OnCurrentLevelChanged()
        {
            return _currentLevel.DistinctUntilChanged();
        }

        public Observable<int> OnCurrentChapterChanged()
        {
            return _currentMapUid.DistinctUntilChanged();
        }

        public Observable<long> OnCurrentExpChanged()
        {
            return _currentExp.DistinctUntilChanged();
        }

        public Observable<long> OnCurrentNeedExpChanged()
        {
            return _currentNeedExp.DistinctUntilChanged();
        }
        public Observable<long> OnCurrentGoldChanged()
        {
            return _currentGold.DistinctUntilChanged();
        }
        public Observable<long> OnCurrentSilverChanged()
        {
            return _currentSilver.DistinctUntilChanged();
        }

        private TableMonster _tableMonster;
        private TableExp _tableExp;

        /// <summary>
        /// 초기화 (저장된 데이터를 불러오거나 새로운 데이터 생성)
        /// </summary>
        public void Initialize(SaveDataManager saveDataManager, TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            _saveDataManager = saveDataManager;
            _tableLoaderManager = loader;
            _maxPlayerLevel = AddressableLoaderSettings.Instance != null && AddressableLoaderSettings.Instance.playerSettings != null
                ? AddressableLoaderSettings.Instance.playerSettings.maxLevel
                : 0;
            // 최대 레벨이 없을때는 경험치 테이블에서 가져온다
            if (_maxPlayerLevel <= 0)
            {
                _maxPlayerLevel = loader.TableExp.GetLastLevel();
            }

            _tableMonster = _tableLoaderManager.TableMonster;
            _tableExp = _tableLoaderManager.TableExp;

            // 저장된 데이터가 있을 경우 불러오기
            LoadPlayerData(saveDataContainer);

            // 저장 이벤트 구독
            InitializeSubscriptions();
        }

        /// <summary>
        /// 변경 감지를 통해 자동으로 저장
        /// </summary>
        private void InitializeSubscriptions()
        {
            _isInitializingAutoSaveSubscriptions = true;
            try
            {
                Observable.Merge(
                        // BehaviorSubject는 구독 직후 현재값 1회를 즉시 발행하므로 Skip(1)으로 초기 발행을 무시합니다.
                        _currentLevel.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _currentMapUid.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _currentExp.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _currentGold.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _currentSilver.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _unspentStatPoints.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _investedStatPointAtk.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _investedStatPointDef.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _investedStatPointHp.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _investedStatPointMp.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default),
                        _investedStatPointStamina.DistinctUntilChanged().Skip(1).Select(_ => Unit.Default))
                    .Subscribe(_ =>
                    {
                        if (_isBatchUpdating || _isInitializingAutoSaveSubscriptions) return;
                        SavePlayerData();
                    })
                    .AddTo(_disposables);
            }
            finally
            {
                _isInitializingAutoSaveSubscriptions = false;
            }
        }

        /// <summary>
        /// 데이터 저장
        /// </summary>
        private void SavePlayerData()
        {
            if (_isBatchUpdating || _isInitializingAutoSaveSubscriptions) return;
            _saveDataManager.StartSaveData();
        }

        /// <summary>
        /// 저장된 데이터를 불러와서 적용
        /// </summary>
        private void LoadPlayerData(SaveDataContainer saveDataContainer)
        {
            if (saveDataContainer?.PlayerData != null)
            {
                CurrentMapUid = saveDataContainer.PlayerData.CurrentMapUid;
                CurrentLevel = saveDataContainer.PlayerData.CurrentLevel;
                CurrentExp = saveDataContainer.PlayerData.CurrentExp;
                CurrentGold = saveDataContainer.PlayerData.CurrentGold;
                CurrentSilver = saveDataContainer.PlayerData.CurrentSilver;
                UnspentStatPoints = saveDataContainer.PlayerData.UnspentStatPoints;
                InvestedStatPointAtk = saveDataContainer.PlayerData.InvestedStatPointAtk;
                InvestedStatPointDef = saveDataContainer.PlayerData.InvestedStatPointDef;
                InvestedStatPointHp = saveDataContainer.PlayerData.InvestedStatPointHp;
                InvestedStatPointMp = saveDataContainer.PlayerData.InvestedStatPointMp;
                InvestedStatPointStamina = saveDataContainer.PlayerData.InvestedStatPointStamina;
                TotalItemBonusHpNormal = saveDataContainer.PlayerData.TotalItemBonusHpNormal;
                TotalItemBonusHpTemp = saveDataContainer.PlayerData.TotalItemBonusHpTemp;
                CurrentItemBonusHpTemp = saveDataContainer.PlayerData.CurrentItemBonusHpTemp;
            }
            else
            {
                // 새 게임(세이브 없음) 초기 포인트 지급
                var settings = AddressableLoaderSettings.Instance.playerStatSettings;
                if (settings != null && settings.statPointInitial > 0)
                {
                    UnspentStatPoints = settings.statPointInitial;
                }
            }

            // 필요 경험치 업데이트
            UpdateRequiredExp(_tableExp.GetNeedExp(CurrentLevel + 1));
        }
        /// <summary>
        /// 몬스터 처치 시 경험치 추가
        /// </summary>
        /// <param name="monsterUid"></param>
        public void AddExpByMonster(int monsterUid)
        {
            var monsterData = _tableMonster.GetDataByUid(monsterUid);
            if (monsterData == null) return;

            AddExp(monsterData.RewardExp);
        }

        public void AddExp(long exp)
        {
            int prevLevel = CurrentLevel;
            long newExp = CurrentExp + exp;
            int nextLevel = prevLevel;
            while (nextLevel < _maxPlayerLevel && newExp >= _tableExp.GetNeedExp(nextLevel + 1))
            {
                newExp -= _tableExp.GetNeedExp(nextLevel + 1);
                nextLevel++;
            }

            int deltaLevel = nextLevel - prevLevel;
            ApplyLevelDelta(deltaLevel, PlayerLevelChangeReason.Exp);
            CurrentExp = CurrentLevel < _maxPlayerLevel ? newExp : 0;
        }
        
#if GGEMCO_ENABLE_CHEAT_TOOLS
        /// <summary>
        /// 치트 도구에서 플레이어 레벨을 1단계 상승시킵니다.
        /// <see cref="GGemCoScriptingDefineSymbols.EnableCheatTools"/> 심볼이 활성화되어 있을 때만 컴파일되며,
        /// 실행 시에는 <see cref="GGemCoCheatToolGate"/>를 통해 Release Simulation/Release 모드에서 다시 차단합니다.
        /// </summary>
        public void AddLevelUp()
        {
            if (!GGemCoCheatToolGate.CanUseCheatTools)
            {
                return;
            }

            if (_maxPlayerLevel <= 0)
            {
                GcLogger.LogError($"{nameof(GGemCoPlayerSettings)}에 max level 값을 설정해주세요.");
                return;
            }

            var nextLevel = CurrentLevel + 1;
            if (nextLevel > _maxPlayerLevel)
            {
                nextLevel = _maxPlayerLevel;
            }

            long exp = _tableLoaderManager.TableExp.GetNeedExp(nextLevel);
            if (exp <= 0)
            {
                GcLogger.LogError($"경험치 테이블(exp)에 정보가 없습니다. level: {nextLevel}");
                return;
            }

            AddExp(exp);
        }
#endif

        private GGemCoPlayerStatSettings GetPlayerStatSettings()
        {
            return AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.playerStatSettings : null;
        }

        private static bool CanAcquireStatPointsFromLevelUp(GGemCoPlayerStatSettings settings)
        {
            if (settings == null) return true;
            return settings.statPointAcquirePolicy == GGemCoPlayerStatSettings.StatPointAcquirePolicy.LevelUpOnly
                   || settings.statPointAcquirePolicy == GGemCoPlayerStatSettings.StatPointAcquirePolicy.LevelUpAndGoldPurchase;
        }

        private static bool CanAcquireStatPointsFromGoldPurchase(GGemCoPlayerStatSettings settings)
        {
            if (settings == null) return false;
            return settings.statPointAcquirePolicy == GGemCoPlayerStatSettings.StatPointAcquirePolicy.GoldPurchaseOnly
                   || settings.statPointAcquirePolicy == GGemCoPlayerStatSettings.StatPointAcquirePolicy.LevelUpAndGoldPurchase;
        }

        private static bool ShouldIncreaseLevelOnStatPointInvest(GGemCoPlayerStatSettings settings)
        {
            if (settings == null) return false;
            return settings.statPointLevelUpOnInvestPolicy == GGemCoPlayerStatSettings.StatPointLevelUpOnInvestPolicy.IncreaseLevelByInvestedPoints;
        }

        private static bool AllowCommittedStatPointRefund(GGemCoPlayerStatSettings settings)
        {
            if (settings == null) return true;
            return settings.statPointRefundPolicy == GGemCoPlayerStatSettings.StatPointRefundPolicy.AllowCommittedRefund;
        }

        private static bool UseReservedGoldDraftBudget(GGemCoPlayerStatSettings settings)
        {
            if (settings == null) return false;
            return settings.statPointAcquirePolicy == GGemCoPlayerStatSettings.StatPointAcquirePolicy.GoldPurchaseOnly;
        }

        private CurrencyConstants.Type GetReservedDraftCurrencyType(GGemCoPlayerStatSettings settings)
        {
            if (!UseReservedGoldDraftBudget(settings))
            {
                return CurrencyConstants.Type.None;
            }

            return CurrencyConstants.Type.Gold;
        }

        private long GetStatPointPurchaseFallbackPrice(GGemCoPlayerStatSettings settings)
        {
            if (settings == null) return 0;
            return settings.statPointPurchaseCurrencyValue > 0 ? settings.statPointPurchaseCurrencyValue : 0;
        }

        private long GetStatPointInvestPriceForAdditionalInvestCount(int additionalInvestCount, GGemCoPlayerStatSettings settings = null)
        {
            if (additionalInvestCount <= 0) return 0;
            settings ??= GetPlayerStatSettings();

            int targetLevel = CurrentLevel + additionalInvestCount;
            long tablePrice = _tableExp != null ? _tableExp.GetNeedStatPointGold(targetLevel) : 0;
            if (tablePrice > 0)
            {
                return tablePrice;
            }

            return GetStatPointPurchaseFallbackPrice(settings);
        }

        private long CalculateReservedDraftGoldCost(int originalUnspent, int originalInvestedTotal, int draftInvestedTotal)
        {
            var settings = GetPlayerStatSettings();
            if (!UseReservedGoldDraftBudget(settings))
            {
                return 0;
            }

            int additionalInvested = Mathf.Max(0, draftInvestedTotal - originalInvestedTotal);
            if (additionalInvested <= originalUnspent)
            {
                return 0;
            }

            long totalCost = 0;
            int firstPurchaseSequence = originalUnspent + 1;
            for (int sequence = firstPurchaseSequence; sequence <= additionalInvested; sequence++)
            {
                long price = GetStatPointInvestPriceForAdditionalInvestCount(sequence, settings);
                if (price <= 0)
                {
                    return -1;
                }

                totalCost += price;
            }

            return totalCost;
        }

        private int GetInvestedStatPointTotal()
        {
            return InvestedStatPointAtk + InvestedStatPointDef + InvestedStatPointHp + InvestedStatPointMp + InvestedStatPointStamina;
        }

        private void ApplyLevelDelta(int deltaLevel, PlayerLevelChangeReason reason)
        {
            if (deltaLevel <= 0) return;

            int nextLevel = Mathf.Min(CurrentLevel + deltaLevel, _maxPlayerLevel);
            int appliedDelta = nextLevel - CurrentLevel;
            if (appliedDelta <= 0) return;

            CurrentLevel = nextLevel;
            UpdateRequiredExp(CurrentLevel < _maxPlayerLevel ? _tableExp.GetNeedExp(CurrentLevel + 1) : 0);

            if (reason != PlayerLevelChangeReason.Exp)
            {
                return;
            }

            var settings = GetPlayerStatSettings();
            if (!CanAcquireStatPointsFromLevelUp(settings))
            {
                return;
            }

            int perLevel = settings != null ? settings.statPointPerLevel : 0;
            if (perLevel > 0)
            {
                UnspentStatPoints += appliedDelta * perLevel;
            }
        }

        public bool CanPurchaseStatPoints()
        {
            var settings = GetPlayerStatSettings();
            if (!CanAcquireStatPointsFromGoldPurchase(settings)) return false;
            if (settings == null) return false;

            // GoldPurchaseOnly 정책은 구매 버튼 대신 스탯 라인의 +/- 드래프트 예약 방식으로 동작합니다.
            if (UseReservedGoldDraftBudget(settings))
            {
                return false;
            }

            if (settings.statPointPurchaseCurrencyType == CurrencyConstants.Type.None) return false;
            return settings.statPointPurchaseCurrencyValue > 0;
        }

        public bool UsesReservedGoldBudgetForStatPointDraft()
        {
            return UseReservedGoldDraftBudget(GetPlayerStatSettings());
        }

        public CurrencyConstants.Type GetStatPointPurchaseCurrencyType()
        {
            var settings = GetPlayerStatSettings();
            if (UseReservedGoldDraftBudget(settings))
            {
                return CurrencyConstants.Type.Gold;
            }

            return settings != null ? settings.statPointPurchaseCurrencyType : CurrencyConstants.Type.None;
        }

        public long GetStatPointPurchasePrice(int amount = 1)
        {
            if (amount <= 0) return 0;
            var settings = GetPlayerStatSettings();
            if (settings == null) return 0;
            if (!CanAcquireStatPointsFromGoldPurchase(settings)) return 0;
            if (settings.statPointPurchaseCurrencyValue <= 0) return 0;
            return (long)settings.statPointPurchaseCurrencyValue * amount;
        }

        /// <summary>
        /// 현재 드래프트에서 추가로 투자된 포인트 수(additionalInvestCount)에 대응하는 1회 투자 골드 비용을 반환합니다.
        /// - additionalInvestCount=1 이면 "이번 드래프트에서 첫 추가 투자" 비용입니다.
        /// - GoldPurchaseOnly에서는 exp 테이블 NeedStatPointGold(level) 값을 우선 사용합니다.
        /// </summary>
        public long GetReservedStatPointDraftPriceForAdditionalInvestCount(int additionalInvestCount)
        {
            return GetStatPointInvestPriceForAdditionalInvestCount(additionalInvestCount, GetPlayerStatSettings());
        }

        public long CalculateReservedStatPointDraftGoldCost(int originalUnspent, int originalInvestedTotal, int draftInvestedTotal)
        {
            return CalculateReservedDraftGoldCost(originalUnspent, originalInvestedTotal, draftInvestedTotal);
        }

        public bool CanAffordReservedStatPointDraftCost(long reservedCost)
        {
            if (reservedCost < 0) return false;
            if (reservedCost == 0) return true;
            return CurrentGold >= reservedCost;
        }

        /// <summary>
        /// 플레이어 설정에 정의된 스탯 초기화 골드 비용을 반환합니다.
        /// </summary>
        /// <returns>스탯 초기화 비용입니다. 설정이 없거나 음수이면 0을 반환합니다.</returns>
        public long GetStatPointResetGoldCost()
        {
            var settings = GetPlayerStatSettings();
            if (settings == null) return 0;
            return settings.statPointResetCost > 0 ? settings.statPointResetCost : 0;
        }

        /// <summary>
        /// 현재 플레이어가 스탯 초기화 비용을 지불할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>비용이 없거나 현재 골드가 충분하면 true를 반환합니다.</returns>
        public bool CanAffordStatPointResetCost()
        {
            long resetGoldCost = GetStatPointResetGoldCost();
            return resetGoldCost <= 0 || CurrentGold >= resetGoldCost;
        }

        public bool CanAffordStatPointPurchase(int amount = 1)
        {
            if (!CanPurchaseStatPoints()) return false;
            if (amount <= 0) return false;

            var settings = GetPlayerStatSettings();
            if (settings == null) return false;

            ResultCommon result = CheckNeedCurrency(settings.statPointPurchaseCurrencyType, settings.statPointPurchaseCurrencyValue, amount);
            return result.Result == ResultCommon.ResultType.Success;
        }

        public bool TryPurchaseStatPoints(int amount = 1)
        {
            if (!CanPurchaseStatPoints()) return false;
            if (amount <= 0) return false;

            var settings = GetPlayerStatSettings();
            if (settings == null) return false;
            if (!CanAffordStatPointPurchase(amount)) return false;

            long totalPrice = (long)settings.statPointPurchaseCurrencyValue * amount;

            _isBatchUpdating = true;
            try
            {
                ResultCommon minusCurrency = MinusCurrency(settings.statPointPurchaseCurrencyType, totalPrice);
                if (minusCurrency.Result == ResultCommon.ResultType.Fail)
                {
                    return false;
                }

                UnspentStatPoints += amount;
            }
            finally
            {
                _isBatchUpdating = false;
            }

            SavePlayerData();
            return true;
        }

        public bool CanRefundCommittedStatPoints()
        {
            return AllowCommittedStatPointRefund(GetPlayerStatSettings());
        }

        /// <summary>
        /// 스탯 포인트 투자
        /// </summary>
        public bool TryInvestStatPoint(CharacterConstants.IndexPlayerInfo type, int amount = 1)
        {
            if (type == CharacterConstants.IndexPlayerInfo.None) return false;
            if (amount <= 0) return false;
            if (UnspentStatPoints < amount) return false;

            var settings = GetPlayerStatSettings();

            switch (type)
            {
                case CharacterConstants.IndexPlayerInfo.Atk: InvestedStatPointAtk += amount; break;
                case CharacterConstants.IndexPlayerInfo.Def: InvestedStatPointDef += amount; break;
                case CharacterConstants.IndexPlayerInfo.Hp: InvestedStatPointHp += amount; break;
                case CharacterConstants.IndexPlayerInfo.Mp: InvestedStatPointMp += amount; break;
                case CharacterConstants.IndexPlayerInfo.Stamina: InvestedStatPointStamina += amount; break;
                default: return false;
            }

            UnspentStatPoints -= amount;

            if (ShouldIncreaseLevelOnStatPointInvest(settings))
            {
                ApplyLevelDelta(amount, PlayerLevelChangeReason.StatPointInvestment);
            }

            return true;
        }

        /// <summary>
        /// 스탯 포인트 회수(되돌리기). 1차에서는 비용 없이 지원(정책은 추후 확장).
        /// </summary>
        public bool TryRefundStatPoint(CharacterConstants.IndexPlayerInfo type, int amount = 1)
        {
            if (type == CharacterConstants.IndexPlayerInfo.None) return false;
            if (amount <= 0) return false;
            if (!CanRefundCommittedStatPoints()) return false;

            switch (type)
            {
                case CharacterConstants.IndexPlayerInfo.Atk:
                    if (InvestedStatPointAtk < amount) return false;
                    InvestedStatPointAtk -= amount;
                    break;
                case CharacterConstants.IndexPlayerInfo.Def:
                    if (InvestedStatPointDef < amount) return false;
                    InvestedStatPointDef -= amount;
                    break;
                case CharacterConstants.IndexPlayerInfo.Hp:
                    if (InvestedStatPointHp < amount) return false;
                    InvestedStatPointHp -= amount;
                    break;
                case CharacterConstants.IndexPlayerInfo.Mp:
                    if (InvestedStatPointMp < amount) return false;
                    InvestedStatPointMp -= amount;
                    break;
                case CharacterConstants.IndexPlayerInfo.Stamina:
                    if (InvestedStatPointStamina < amount) return false;
                    InvestedStatPointStamina -= amount;
                    break;
                default:
                    return false;
            }

            UnspentStatPoints += amount;
            return true;
        }

        /// <summary>
        /// 스탯 포인트(미사용/투자) 상태를 일괄 적용합니다.
        /// - 총 포인트 보존(= 치트 방지)
        /// - Apply 버튼 1회 클릭으로 저장도 1회로 합칩니다.
        /// </summary>
        public bool TryApplyStatPointAllocation(
            int unspent,
            int investedAtk,
            int investedDef,
            int investedHp,
            int investedMp,
            int investedStamina)
        {
            return TryApplyStatPointAllocationInternal(
                unspent,
                investedAtk,
                investedDef,
                investedHp,
                investedMp,
                investedStamina,
                useReservedDraftGold: false,
                reservedDraftGoldCost: 0);
        }

        /// <summary>
        /// GoldPurchaseOnly 드래프트 예약 골드를 검증한 뒤, 스탯 포인트 투자 상태를 원자적으로 커밋합니다.
        /// - Plus/Minus 중에는 실제 골드를 차감하지 않고, Apply 시점에만 최종 차감합니다.
        /// - reservedDraftGoldCost는 UIWindow/Session에서 계산한 예약 골드 총합이며, 서버/치트 방지를 위해 여기서 다시 검증합니다.
        /// </summary>
        public bool TryApplyStatPointAllocationWithReservedDraftGold(
            int unspent,
            int investedAtk,
            int investedDef,
            int investedHp,
            int investedMp,
            int investedStamina,
            long reservedDraftGoldCost)
        {
            return TryApplyStatPointAllocationInternal(
                unspent,
                investedAtk,
                investedDef,
                investedHp,
                investedMp,
                investedStamina,
                useReservedDraftGold: true,
                reservedDraftGoldCost: reservedDraftGoldCost);
        }

        private bool TryApplyStatPointAllocationInternal(
            int unspent,
            int investedAtk,
            int investedDef,
            int investedHp,
            int investedMp,
            int investedStamina,
            bool useReservedDraftGold,
            long reservedDraftGoldCost)
        {
            if (unspent < 0) return false;
            if (investedAtk < 0 || investedDef < 0 || investedHp < 0 || investedMp < 0 || investedStamina < 0)
                return false;

            var settings = GetPlayerStatSettings();
            bool useReservedBudget = useReservedDraftGold && UseReservedGoldDraftBudget(settings);
            if (useReservedDraftGold && !useReservedBudget)
            {
                return false;
            }

            if (useReservedBudget)
            {
                if (investedAtk < InvestedStatPointAtk || investedDef < InvestedStatPointDef || investedHp < InvestedStatPointHp ||
                    investedMp < InvestedStatPointMp || investedStamina < InvestedStatPointStamina)
                {
                    return false;
                }
            }
            else if (!AllowCommittedStatPointRefund(settings))
            {
                if (investedAtk < InvestedStatPointAtk || investedDef < InvestedStatPointDef || investedHp < InvestedStatPointHp ||
                    investedMp < InvestedStatPointMp || investedStamina < InvestedStatPointStamina)
                {
                    return false;
                }
            }

            int currentInvestedTotal = GetInvestedStatPointTotal();
            int newInvestedTotal = investedAtk + investedDef + investedHp + investedMp + investedStamina;
            int investedDelta = Mathf.Max(0, newInvestedTotal - currentInvestedTotal);

            if (!useReservedBudget)
            {
                int currentTotal = UnspentStatPoints + currentInvestedTotal;
                int newTotal = unspent + newInvestedTotal;
                if (newTotal != currentTotal) return false;
            }
            else
            {
                int additionalInvested = Mathf.Max(0, newInvestedTotal - currentInvestedTotal);
                int expectedUnspent = Mathf.Max(0, UnspentStatPoints - additionalInvested);
                if (unspent != expectedUnspent)
                {
                    return false;
                }

                long expectedReservedDraftGoldCost = CalculateReservedDraftGoldCost(UnspentStatPoints, currentInvestedTotal, newInvestedTotal);
                if (expectedReservedDraftGoldCost < 0 || expectedReservedDraftGoldCost != reservedDraftGoldCost)
                {
                    return false;
                }

                if (!CanAffordReservedStatPointDraftCost(expectedReservedDraftGoldCost))
                {
                    return false;
                }
            }

            _isBatchUpdating = true;
            try
            {
                if (useReservedBudget && reservedDraftGoldCost > 0)
                {
                    ResultCommon minusCurrency = MinusCurrency(GetReservedDraftCurrencyType(settings), reservedDraftGoldCost);
                    if (minusCurrency.Result == ResultCommon.ResultType.Fail)
                    {
                        return false;
                    }
                }

                // 투자/미사용 값은 프로퍼티로 세팅(= JSON 직렬화 대상)
                UnspentStatPoints = unspent;
                InvestedStatPointAtk = investedAtk;
                InvestedStatPointDef = investedDef;
                InvestedStatPointHp = investedHp;
                InvestedStatPointMp = investedMp;
                InvestedStatPointStamina = investedStamina;
            }
            finally
            {
                _isBatchUpdating = false;
            }

            if (investedDelta > 0 && ShouldIncreaseLevelOnStatPointInvest(settings))
            {
                ApplyLevelDelta(investedDelta, PlayerLevelChangeReason.StatPointInvestment);
            }

            // 배치 종료 후 저장 1회
            SavePlayerData();
            return true;
        }

        /// <summary>
        /// 스탯 초기화 창에서 확정한 재분배 결과를 골드 비용과 함께 커밋합니다.
        /// 일반 스탯 분배 정책과 별도로, 유료 초기화 플로우에서는 기존에 적용된 투자 포인트 감소를 허용합니다.
        /// </summary>
        /// <param name="unspent">적용할 미사용 스탯 포인트입니다. 초기화 적용 시에는 0이어야 합니다.</param>
        /// <param name="investedAtk">적용할 공격력 투자 포인트입니다.</param>
        /// <param name="investedDef">적용할 방어력 투자 포인트입니다.</param>
        /// <param name="investedHp">적용할 체력 투자 포인트입니다.</param>
        /// <param name="investedMp">적용할 마력 투자 포인트입니다.</param>
        /// <param name="investedStamina">적용할 스테미나 투자 포인트입니다.</param>
        /// <returns>포인트 검증, 골드 차감, 저장 대상 값 반영이 모두 성공하면 true를 반환합니다.</returns>
        public bool TryApplyStatPointResetAllocation(
            int unspent,
            int investedAtk,
            int investedDef,
            int investedHp,
            int investedMp,
            int investedStamina)
        {
            if (unspent != 0) return false;
            if (investedAtk < 0 || investedDef < 0 || investedHp < 0 || investedMp < 0 || investedStamina < 0)
                return false;

            var settings = GetPlayerStatSettings();
            int currentInvestedTotal = GetInvestedStatPointTotal();
            int currentTotal = UnspentStatPoints + currentInvestedTotal;
            int newInvestedTotal = investedAtk + investedDef + investedHp + investedMp + investedStamina;
            int newTotal = unspent + newInvestedTotal;
            if (newTotal != currentTotal) return false;

            long resetGoldCost = GetStatPointResetGoldCost();
            if (resetGoldCost > 0 && CurrentGold < resetGoldCost)
            {
                return false;
            }

            int investedDelta = Mathf.Max(0, newInvestedTotal - currentInvestedTotal);

            _isBatchUpdating = true;
            try
            {
                if (resetGoldCost > 0)
                {
                    ResultCommon minusCurrency = MinusCurrency(CurrencyConstants.Type.Gold, resetGoldCost);
                    if (minusCurrency.Result == ResultCommon.ResultType.Fail)
                    {
                        return false;
                    }
                }

                // 스탯 초기화 커밋은 저장 대상 프로퍼티만 갱신하고, 자동 저장은 배치 종료 후 1회만 수행합니다.
                UnspentStatPoints = unspent;
                InvestedStatPointAtk = investedAtk;
                InvestedStatPointDef = investedDef;
                InvestedStatPointHp = investedHp;
                InvestedStatPointMp = investedMp;
                InvestedStatPointStamina = investedStamina;
            }
            finally
            {
                _isBatchUpdating = false;
            }

            if (investedDelta > 0 && ShouldIncreaseLevelOnStatPointInvest(settings))
            {
                ApplyLevelDelta(investedDelta, PlayerLevelChangeReason.StatPointInvestment);
            }

            SavePlayerData();
            return true;
        }


        /// <summary>
        /// 필요 경험치 업데이트
        /// </summary>
        private void UpdateRequiredExp(long value)
        {
            _currentNeedExp.OnNext(value);
        }
        public long CurrentNeedExp()
        {
            return _currentNeedExp.Value;
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
        /// <summary>
        /// 재화 추가하기
        /// </summary>
        /// <param name="currencyType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ResultCommon AddCurrency(CurrencyConstants.Type currencyType, int value)
        {
            switch (currencyType)
            {
                case CurrencyConstants.Type.Gold:
                    CurrentGold += value;
                    return ResultCommon.Success();
                case CurrencyConstants.Type.Silver:
                    CurrentSilver += value;
                    return ResultCommon.Success();
                case CurrencyConstants.Type.None:
                default:
                    break;
            }
            return ResultCommon.Fail("Currency_NoTypeInfo", $"currencyType: {currencyType}");//재화 타입 정보가 없습니다.
        }
        
        /// <summary>
        /// 가지고 있는 재화가 충분하지 체크하기
        /// </summary>
        /// <param name="currencyType"></param>
        /// <param name="currencyValue"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public ResultCommon CheckNeedCurrency(CurrencyConstants.Type currencyType, int currencyValue, int count = 1)
        {
            if (currencyType == CurrencyConstants.Type.None)
            {
                return ResultCommon.Fail("Currency_NoTypeInfo", $"currencyType: {currencyType}");
            }

            int requiredValue = currencyValue * count;
            long currentValue = currencyType switch
            {
                CurrencyConstants.Type.Gold => CurrentGold,
                CurrencyConstants.Type.Silver => CurrentSilver,
                _ => 0
            };

            if (currentValue >= requiredValue)
            {
                return ResultCommon.Success();
            }

            string currencyName = CurrencyConstants.GetNameByCurrencyType(currencyType);
            return ResultCommon.Fail("Currency_NotEnough", args: new object[] { currencyName });
        }
        
        /// <summary>
        /// 모든 재화를 채크해야하는 경우
        /// </summary>
        /// <param name="needCurrency"></param>
        /// <returns></returns>
        public ResultCommon CheckNeedCurrency(Dictionary<CurrencyConstants.Type, int> needCurrency)
        {
            foreach (var info in needCurrency)
            {
                ResultCommon resultCommon = CheckNeedCurrency(info.Key, info.Value);
                if (resultCommon.Result == ResultCommon.ResultType.Fail)
                {
                    return ResultCommon.Fail();
                }
            }

            return ResultCommon.Success();
        }
        /// <summary>
        /// 재화 빼기
        /// </summary>
        /// <param name="currencyType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ResultCommon MinusCurrency(CurrencyConstants.Type currencyType, long value)
        {
            switch (currencyType)
            {
                case CurrencyConstants.Type.Gold:
                    if (CurrentGold < value)
                    {
                        return ResultCommon.Fail("Currency_NotEnoughGold");//"골드가 부족합니다."
                    }
                    CurrentGold -= value;
                    return ResultCommon.Success();
                case CurrencyConstants.Type.Silver:
                    if (CurrentSilver < value)
                    {
                        return ResultCommon.Fail("Currency_NotEnoughSilver");//"실버가 부족합니다."
                    }
                    CurrentSilver -= value;
                    return ResultCommon.Success();
                case CurrencyConstants.Type.None:
                default:
                    break;
            }
            return ResultCommon.Fail($"Currency_NoTypeInfo", $"currencyType: {currencyType}");//재화 타입 정보가 없습니다.
        }
        /// <summary>
        /// 가지고 있는 재화로 몇개 까지 구매할 수 있는지 
        /// </summary>
        /// <param name="currencyType"></param>
        /// <param name="currencyValue"></param>
        public long GetPossibleBuyCount(CurrencyConstants.Type currencyType, int currencyValue)
        {
            if (currencyType == CurrencyConstants.Type.None)
            {
                GcLogger.LogError($"재화 정보가 없습니다. currencyType: {currencyType}");
                return 0;
            }

            long buyCount = 0;
            if (currencyType == CurrencyConstants.Type.Gold)
            {
                buyCount = CurrentGold / currencyValue;
            }
            else if (currencyType == CurrencyConstants.Type.Silver)
            {
                buyCount = CurrentSilver / currencyValue;
            }
            return buyCount;
        }

        public void AddTotalItemBonusHpNormal(long amount)
        {
            if (amount == 0) return;
            SetTotalItemBonusHpNormal(TotalItemBonusHpNormal + amount);
        }

        public void SetTotalItemBonusHpNormal(long value, bool save = true)
        {
            value = System.Math.Max(0, value);
            if (TotalItemBonusHpNormal == value) return;
            TotalItemBonusHpNormal = value;
            if (save)
            {
                SaveDatas();
            }
        }

        public void AddTotalItemBonusHpTemp(long amount)
        {
            if (amount == 0) return;
            SetTotalItemBonusHpTemp(TotalItemBonusHpTemp + amount);
        }

        public void SetTotalItemBonusHpTemp(long value, bool save = true)
        {
            value = System.Math.Max(0, value);
            if (TotalItemBonusHpTemp == value) return;
            TotalItemBonusHpTemp = value;

            if (CurrentItemBonusHpTemp > TotalItemBonusHpTemp)
            {
                CurrentItemBonusHpTemp = TotalItemBonusHpTemp;
            }

            if (save)
            {
                SaveDatas();
            }
        }

        public void SetCurrentItemBonusHpTemp(long value, bool save = true)
        {
            value = ClampLong(value, 0, TotalItemBonusHpTemp);
            if (CurrentItemBonusHpTemp == value) return;
            CurrentItemBonusHpTemp = value;
            if (save)
            {
                SaveDatas();
            }
        }

        public void SaveItemBonusHpState()
        {
            SaveDatas();
        }
    }
}
