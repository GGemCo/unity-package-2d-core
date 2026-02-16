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
        private int _maxPlayerLevel;
        private TableLoaderManager _tableLoaderManager;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
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

        // 일괄 업데이트 중(Apply 버튼 등) 자동 저장/이벤트 폭주를 줄이기 위한 플래그
        private bool _isBatchUpdating;

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
            return _unspentStatPoints.DistinctUntilChanged()
                .CombineLatest(_investedStatPointAtk, _investedStatPointDef, _investedStatPointHp, _investedStatPointMp,
                    _investedStatPointStamina,
                    (_, _, _, _, _, _) => Unit.Default)
                .DistinctUntilChanged();
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
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            _tableLoaderManager = loader;
            _maxPlayerLevel = AddressableLoaderSettings.Instance.playerSettings.maxLevel;
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
            Observable.Merge(
                    _currentLevel.DistinctUntilChanged().Select(_ => Unit.Default),
                    _currentMapUid.DistinctUntilChanged().Select(_ => Unit.Default),
                    _currentExp.DistinctUntilChanged().Select(_ => Unit.Default),
                    _currentGold.DistinctUntilChanged().Select(_ => Unit.Default),
                    _currentSilver.DistinctUntilChanged().Select(_ => Unit.Default),
                    _unspentStatPoints.DistinctUntilChanged().Select(_ => Unit.Default),
                    _investedStatPointAtk.DistinctUntilChanged().Select(_ => Unit.Default),
                    _investedStatPointDef.DistinctUntilChanged().Select(_ => Unit.Default),
                    _investedStatPointHp.DistinctUntilChanged().Select(_ => Unit.Default),
                    _investedStatPointMp.DistinctUntilChanged().Select(_ => Unit.Default),
                    _investedStatPointStamina.DistinctUntilChanged().Select(_ => Unit.Default))
                .Subscribe(_ =>
                {
                    if (_isBatchUpdating) return;
                    SavePlayerData();
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 데이터 저장
        /// </summary>
        private void SavePlayerData()
        {
            if (_isBatchUpdating) return;
            SceneGame.Instance.saveDataManager.StartSaveData();
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
            }
            else
            {
                // 새 게임(세이브 없음) 초기 포인트 지급
                var settings = AddressableLoaderSettings.Instance.playerSettings;
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

            // 최종 값 업데이트
            CurrentLevel = Mathf.Min(nextLevel, _maxPlayerLevel);
            CurrentExp = nextLevel < _maxPlayerLevel ? newExp : 0;
            UpdateRequiredExp(nextLevel < _maxPlayerLevel ? _tableExp.GetNeedExp(nextLevel + 1) : 0);

            // 레벨업 시 스탯 포인트 지급
            int deltaLevel = CurrentLevel - prevLevel;
            if (deltaLevel > 0)
            {
                var settings = AddressableLoaderSettings.Instance.playerSettings;
                int perLevel = settings != null ? settings.statPointPerLevel : 0;
                if (perLevel > 0)
                {
                    UnspentStatPoints += deltaLevel * perLevel;
                }
            }
        }
#if UNITY_EDITOR
        public void AddLevelUp()
        {
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

        /// <summary>
        /// 스탯 포인트 투자
        /// </summary>
        public bool TryInvestStatPoint(CharacterConstants.IndexPlayerInfo type, int amount = 1)
        {
            if (type == CharacterConstants.IndexPlayerInfo.None) return false;
            if (amount <= 0) return false;
            if (UnspentStatPoints < amount) return false;

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
            return true;
        }

        /// <summary>
        /// 스탯 포인트 회수(되돌리기). 1차에서는 비용 없이 지원(정책은 추후 확장).
        /// </summary>
        public bool TryRefundStatPoint(CharacterConstants.IndexPlayerInfo type, int amount = 1)
        {
            if (type == CharacterConstants.IndexPlayerInfo.None) return false;
            if (amount <= 0) return false;

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
            if (unspent < 0) return false;
            if (investedAtk < 0 || investedDef < 0 || investedHp < 0 || investedMp < 0 || investedStamina < 0)
                return false;

            int currentTotal = UnspentStatPoints + InvestedStatPointAtk + InvestedStatPointDef + InvestedStatPointHp +
                               InvestedStatPointMp + InvestedStatPointStamina;

            int newTotal = unspent + investedAtk + investedDef + investedHp + investedMp + investedStamina;
            if (newTotal != currentTotal) return false;

            _isBatchUpdating = true;
            try
            {
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

            // 배치 종료 후 저장 1회
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
                return ResultCommon.Fail($"Currency_NoTypeInfo", $"currencyType: {currencyType}"); //재화 타입 정보가 없습니다.
            string currency = CurrencyConstants.GetNameByCurrencyType(currencyType);
            if (currencyType == CurrencyConstants.Type.Gold && CurrentGold >= currencyValue * count)
            {
                return ResultCommon.Success();
            }
            if (currencyType == CurrencyConstants.Type.Silver && CurrentSilver >= currencyValue * count)
            {
                return ResultCommon.Success();
            }

            string message = string.Format(LocalizationManager.Instance.GetSystemByKey("Currency_NotEnough"), currency);
            return ResultCommon.Fail(message); // $"{currency} 가 부족합니다."
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
    }
}
