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
            _currentLevel.DistinctUntilChanged()
                .CombineLatest(_currentMapUid, _currentExp, _currentGold, _currentSilver, (_, _, _, _, _) => Unit.Default)
                .Subscribe(_ => SavePlayerData())
                .AddTo(_disposables);
        }

        /// <summary>
        /// 데이터 저장
        /// </summary>
        private void SavePlayerData()
        {
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
            }

            // 필요 경험치 업데이트
            UpdateRequiredExp(_tableExp.GetNeedExp(CurrentLevel + 1));
        }
        /// <summary>
        /// 몬스터 처치 시 경험치 추가
        /// </summary>
        /// <param name="monsterVid"></param>
        /// <param name="monsterUid"></param>
        /// <param name="monsterObject"></param>
        public void AddExp(int monsterVid, int monsterUid, GameObject monsterObject)
        {
            var monsterData = _tableMonster.GetDataByUid(monsterUid);
            if (monsterData == null) return;

            AddExp(monsterData.RewardExp);
        }

        public void AddExp(long exp)
        {
            long newExp = CurrentExp + exp;
            int nextLevel = CurrentLevel;
            while (nextLevel < _maxPlayerLevel && newExp >= _tableExp.GetNeedExp(nextLevel + 1))
            {
                newExp -= _tableExp.GetNeedExp(nextLevel + 1);
                nextLevel++;
            }

            // 최종 값 업데이트
            CurrentLevel = Mathf.Min(nextLevel, _maxPlayerLevel);
            CurrentExp = nextLevel < _maxPlayerLevel ? newExp : 0;
            UpdateRequiredExp(nextLevel < _maxPlayerLevel ? _tableExp.GetNeedExp(nextLevel + 1) : 0);
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
                    return new ResultCommon(ResultCommon.Type.Success);
                case CurrencyConstants.Type.Silver:
                    CurrentSilver += value;
                    return new ResultCommon(ResultCommon.Type.Success);
                case CurrencyConstants.Type.None:
                default:
                    break;
            }
            return new ResultCommon(ResultCommon.Type.Fail, $"재화 타입 정보가 없습니다. currencyType: {currencyType}");
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
                return new ResultCommon(ResultCommon.Type.Fail, $"재화 정보가 없습니다. currencyType: {currencyType}");
            string currency = CurrencyConstants.GetNameByCurrencyType(currencyType);
            if (currencyType == CurrencyConstants.Type.Gold && CurrentGold >= currencyValue * count)
            {
                return new ResultCommon(ResultCommon.Type.Success);
            }
            if (currencyType == CurrencyConstants.Type.Silver && CurrentSilver >= currencyValue * count)
            {
                return new ResultCommon(ResultCommon.Type.Success);
            }

            return new ResultCommon(ResultCommon.Type.Fail, $"Not enough {currency}."); // $"{currency} 가 부족합니다.");
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
                if (resultCommon.Code == ResultCommon.Type.Fail)
                {
                    return new ResultCommon(ResultCommon.Type.Fail,
                        $"Not enough {CurrencyConstants.GetNameByCurrencyType(info.Key)}."); //$"{CurrencyConstants.GetNameByCurrencyType(info.Key)} 가 부족합니다.");
                }
            }

            return new ResultCommon(ResultCommon.Type.Success);
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
                        return new ResultCommon(ResultCommon.Type.Fail, "There is not enough Gold.");//"골드가 부족합니다."
                    }
                    CurrentGold -= value;
                    return new ResultCommon(ResultCommon.Type.Success);
                case CurrencyConstants.Type.Silver:
                    if (CurrentSilver < value)
                    {
                        return new ResultCommon(ResultCommon.Type.Fail, "There is not enough Silver.");//"실버가 부족합니다."
                    }
                    CurrentSilver -= value;
                    return new ResultCommon(ResultCommon.Type.Success);
                case CurrencyConstants.Type.None:
                default:
                    break;
            }
            return new ResultCommon(ResultCommon.Type.Fail, $"재화 타입 정보가 없습니다. currencyType: {currencyType}");
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
