using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 사냥 후 드랍되는 아이템 관리 매니저
    /// </summary>
    public class ItemManager
    {
        // 드랍되는 아이템 pool size
        private int _poolSize;
        // 드랍되는 아이템 pool queue
        private readonly Queue<Item> _poolDropItem = new Queue<Item>();
        // 드랍되는 아이템을 보기 쉽게 하기위한 container 
        private GameObject _containerPoolDropItem;
        // 드랍되는 아이템 prefab
        private GameObject _prefabDropItem;
        // 드랍되는 아이템 pool size 값을 원래 값으로 초기화 하는 coroutine
        private Coroutine _reducePoolCoroutine;
        // 드랍되는 아이템 pool size 값을 원래 값으로 초기화 시간
        private readonly float _poolReduceTime = 10f;
        private SceneGame _sceneGame;
        private UIWindowInventory _uiWindowInventory;

        private ItemOptionRoller _itemOptionRoller;
        private PlayerData _playerData;
        
        public enum DropRateType
        {
            None,
            ItemDropGroupUid,
            // 아무것도 드랍하지 않는다
            Nothing
        }
        public enum ItemDropGroup
        {
            None,
            ItemCategory,
            ItemSubCategory,
            ItemUid,
            ExcludeItemUid,
            // 아무것도 드랍하지 않는다
            Nothing,
        }

        private TableItem _tableItem;
        private Dictionary<ItemConstants.Category, List<StruckTableItem>> _dictionaryByCategory;
        private Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> _dictionaryBySubCategory;
        private Dictionary<int, List<StruckTableItemDropGroup>> _dropGroupDictionary = new Dictionary<int, List<StruckTableItemDropGroup>>();
        private Dictionary<int, List<StruckTableMonsterDropRate>> _monsterDropDictionary = new Dictionary<int, List<StruckTableMonsterDropRate>>();
        private Dictionary<int, List<StruckTableNpcDropRate>> _npcDropDictionary = new Dictionary<int, List<StruckTableNpcDropRate>>();

        /// <summary>
        /// 초기화. Awake 단계에서 호출됩니다.
        /// </summary>
        public void Initialize(SceneGame sceneGame)
        {
            _poolSize = 1;
            _poolDropItem.Clear();
            InitializePool();
            _tableItem = TableLoaderManager.Instance.TableItem;
            _dictionaryByCategory = _tableItem.GetDictionaryByCategory();
            _dictionaryBySubCategory = _tableItem.GetDictionaryBySubCategory();
            _dropGroupDictionary = TableLoaderManager.Instance.TableItemDropGroup.GetDropGroups();
            _monsterDropDictionary = TableLoaderManager.Instance.TableMonsterDropRate.GetMonsterDropDictionary();
            _npcDropDictionary = TableLoaderManager.Instance.TableNpcDropRate.GetNpcDropDictionary();
            _sceneGame = sceneGame;

            // 인스턴스 아이템 롤러 캐시
            _itemOptionRoller = new ItemOptionRoller(TableLoaderManager.Instance);
        }
        /// <summary>
        /// Addressable 에 등록된 damageText 를 불러와서 pool 을 만든다 
        /// </summary>
        private void InitializePool()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            _prefabDropItem = ConfigResources.DropItem.Load();
            if (_prefabDropItem == null) return;
            _containerPoolDropItem = new GameObject("ContainerPoolDropItem");
            ExpandPool(_poolSize); // 초기 풀 생성
        }
        /// <summary>
        /// 풀에서 아이템을 가져오고, 부족하면 새로운 아이템을 생성한다.
        /// </summary>
        private Item GetOrCreateItem()
        {
            if (_poolDropItem.Count == 0)
            {
                ExpandPool(_poolSize); // 풀 사이즈만큼 추가 생성
            }

            return _poolDropItem.Count > 0 ? _poolDropItem.Dequeue() : null;
        }

        /// <summary>
        /// 특정 개수만큼 풀을 확장하여 아이템을 추가 생성.
        /// </summary>
        private void ExpandPool(int amount)
        {
            if (_prefabDropItem == null || _containerPoolDropItem == null) return;
            for (int i = 0; i < amount; i++)
            {
                GameObject gameObjectText = Object.Instantiate(_prefabDropItem, _containerPoolDropItem.transform);
                Item item = gameObjectText.GetComponent<Item>();
                item.gameObject.SetActive(false);
                _poolDropItem.Enqueue(item);
            }
            // GcLogger.Log($"풀 확장: {amount}개 아이템 추가 (총 {poolDropItem.Count}개)");
        }

        public void OnStartBySceneGame()
        {
            _uiWindowInventory =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            _playerData = _sceneGame.saveDataManager.Player;
        }
        
        /// <summary>
        /// 월드에 드랍 아이템을 생성합니다.
        /// </summary>
        /// <param name="worldPosition">드랍될 월드 좌표입니다.</param>
        /// <param name="itemUid">아이템 UID입니다.</param>
        /// <param name="itemCount">드랍 수량입니다.</param>
        /// <param name="rarity">신규 인스턴스 생성 시 사용할 희귀도입니다.</param>
        /// <param name="dropLevel">신규 인스턴스 생성 시 사용할 드랍 레벨입니다.</param>
        /// <param name="existingInstanceId">
        /// 인벤토리/장비 등에서 이미 존재하던 인스턴스를 월드로 되돌릴 때 사용하는 인스턴스 ID입니다.
        /// 0 이하이면 신규 드랍 규칙으로 인스턴스를 생성합니다.
        /// </param>
        public void MakeDropItem(Vector3 worldPosition, int itemUid, int itemCount,
            ItemConstants.Class rarity = ItemConstants.Class.Normal, int dropLevel = 0, long existingInstanceId = 0)
        {
            TryMakeDropItem(
                new WorldItemDropRequest(
                    worldPosition,
                    itemUid,
                    itemCount,
                    rarity,
                    dropLevel,
                    existingInstanceId),
                out _);
        }

        /// <summary>
        /// 요청 정책에 따라 아이템을 직접 획득하거나 월드 드랍 오브젝트로 생성합니다.
        /// </summary>
        /// <param name="request">아이템 수량, 위치, 수명주기와 런타임 식별 정보입니다.</param>
        /// <param name="spawnedItem">월드에 생성된 아이템입니다. 직접 획득 처리된 경우 null입니다.</param>
        /// <returns>직접 획득 또는 월드 아이템 생성에 성공하면 <see langword="true"/>입니다.</returns>
        public bool TryMakeDropItem(in WorldItemDropRequest request, out Item spawnedItem)
        {
            spawnedItem = null;
            if (_tableItem == null || request.ItemUid <= 0 || request.ItemCount <= 0)
            {
                return false;
            }

            var info = _tableItem.GetDataByUid(request.ItemUid);
            if (info == null)
            {
                return false;
            }

            // 인벤토리 아이템 저장 구조는 int 수량을 사용하므로 재화 외 아이템은 범위를 제한합니다.
            if (info.Type != ItemConstants.Type.Currency && request.ItemCount > int.MaxValue)
            {
                return false;
            }

            long itemCount = request.ItemCount;
            long instanceId = request.ExistingInstanceId;

            // 인스턴스 아이템은 반드시 단일 개수로만 취급한다.
            if (instanceId > 0 && itemCount != 1)
            {
                itemCount = 1;
            }

            // 랜덤 옵션 인스턴스 생성(권장: Count=1일 때만 인스턴스로 취급)
            var store = _sceneGame?.saveDataManager?.ItemInstances;
            if (instanceId <= 0 && store != null && _itemOptionRoller != null && itemCount == 1)
            {
                int seed = Random.Range(int.MinValue, int.MaxValue);
                var instance = _itemOptionRoller.CreateInstance(
                    request.ItemUid,
                    request.Rarity,
                    request.DropLevel,
                    seed);

                // Normal 등급이라도 룰에 의해 롤이 발생할 수 있으므로, 롤 결과가 있으면 인스턴스로 등록한다.
                if (instance != null &&
                    (request.Rarity != ItemConstants.Class.Normal ||
                     (instance.RolledAffixes != null && instance.RolledAffixes.Count > 0)))
                {
                    instanceId = store.RegisterNew(instance);
                }
            }

            if (!request.ForceWorldDrop && ShouldAcquireDirectly(info))
            {
                return AcquireDirectly(request.ItemUid, itemCount, instanceId);
            }

            Item item = GetOrCreateItem();
            if (item == null)
            {
                return false;
            }

            item.Initialize(
                request.ItemUid,
                itemCount,
                request.WorldPosition,
                instanceId,
                request.SourceKey,
                request.RuntimeToken,
                request.DisableAutoDespawn,
                request.PickupPolicy);
            item.StartDrop();
            spawnedItem = item;
            
            // 풀을 일정 시간이 지나면 정리하도록 코루틴 시작
            if (_sceneGame != null)
            {
                _reducePoolCoroutine ??= _sceneGame.StartCoroutine(ReducePoolSize());
            }
            return true;
        }
        /// <summary>
        /// 일정 시간이 지나면 풀 크기를 다시 poolSize 값으로 줄인다.
        /// </summary>
        private IEnumerator ReducePoolSize()
        {
            yield return new WaitForSeconds(_poolReduceTime);

            while (_poolDropItem.Count > _poolSize)
            {
                Item itemToDestroy = _poolDropItem.Dequeue();
                if (itemToDestroy.GetItemUid() > 0) continue;
                Object.Destroy(itemToDestroy.gameObject);
            }

            // GcLogger.Log($"풀 크기 정리 완료: {poolSize}개 유지");
            _reducePoolCoroutine = null;
        }

        private bool ShouldAcquireDirectly(StruckTableItem info)
        {
            var settings = AddressableLoaderSettings.Instance?.itemSettings;
            if (settings == null || info == null)
                return false;

            return info.Type == ItemConstants.Type.Currency
                ? settings.acquireCurrenciesDirectly
                : settings.acquireItemsDirectly;
        }

        private ResultCommon CollectItemCore(int itemUid, long itemCount, long instanceId)
        {
            StruckTableItem info = _tableItem?.GetDataByUid(itemUid);
            if (info == null || itemCount <= 0)
            {
                return ResultCommon.Fail("Slot_InvalidItemCount", $"itemUid: {itemUid}, itemCount: {itemCount}");
            }

            if (info.Type == ItemConstants.Type.Currency)
            {
                PlayerData playerData = _sceneGame?.saveDataManager?.Player;
                if (playerData == null)
                {
                    return ResultCommon.Fail("Currency_PlayerDataMissing");
                }

                CurrencyConstants.Type currencyType = info.Category switch
                {
                    ItemConstants.Category.Gold => CurrencyConstants.Type.Gold,
                    ItemConstants.Category.Silver => CurrencyConstants.Type.Silver,
                    _ => CurrencyConstants.Type.None,
                };

                return playerData.AddCurrency(currencyType, itemCount);
            }

            InventoryData inventoryData = _sceneGame?.saveDataManager?.Inventory;
            if (inventoryData == null || itemCount > int.MaxValue)
            {
                return ResultCommon.Fail("Slot_InvalidItemCount", $"itemUid: {itemUid}, itemCount: {itemCount}");
            }

            return inventoryData.AddItem(
                new IconPayload(itemUid, (int)itemCount, instanceId));
        }

        private void NotifyItemCollected(
            int itemUid,
            long itemCount,
            ResultCommon result,
            string sourceKey = null,
            long runtimeToken = 0)
        {
            if (result == null || result.Result == ResultCommon.ResultType.Fail)
            {
                return;
            }

            if (_uiWindowInventory != null)
            {
                _uiWindowInventory.SetIcons(result);
            }

            var data = new ItemCollectedEventData(
                itemUid,
                itemCount,
                sourceKey: sourceKey,
                runtimeToken: runtimeToken);
            GameEventManager.ItemCollected(data);
        }

        private bool AcquireDirectly(int itemUid, long itemCount, long instanceId)
        {
            var result = CollectItemCore(itemUid, itemCount, instanceId);
            NotifyItemCollected(itemUid, itemCount, result);
            return result != null && result.Result != ResultCommon.ResultType.Fail;
        }
        /// <summary>
        /// 드랍되는 아이템 확률 계산하기 
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <returns></returns>
        private int GetDropItem(int monsterUid)
        {
            if (!_monsterDropDictionary.ContainsKey(monsterUid)) return 0;

            Dictionary<DropRateType, int> dropRates = new Dictionary<DropRateType, int>();

            // 드롭 확률을 미리 정리
            foreach (StruckTableMonsterDropRate dropEntry in _monsterDropDictionary[monsterUid])
            {
                dropRates[dropEntry.Type] = dropEntry.Rate;
            }

            int roll = Random.Range(0, 100);

            // ItemDropGroupUid 체크
            float cumulativePercent = 0f;
            int groupUid = 0;
            if (dropRates.ContainsKey(DropRateType.ItemDropGroupUid))
            {
                foreach (StruckTableMonsterDropRate dropEntry in _monsterDropDictionary[monsterUid])
                {
                    cumulativePercent += dropEntry.Rate;
                    if (roll < cumulativePercent)
                    {
                        groupUid = dropEntry.Value;
                        break;
                    }
                }
            }

            if (groupUid <= 0)
            {
                return 0;
            }
            if (!_dropGroupDictionary.ContainsKey(groupUid))
                return 0;
            
            roll = Random.Range(0, 100);
            cumulativePercent = 0f;
            foreach (StruckTableItemDropGroup group in _dropGroupDictionary[groupUid])
            {
                cumulativePercent += group.Rate;
                if (roll < cumulativePercent) 
                {
                    StruckTableItem item = FindItemByGroup(group);
                    if (item is { Uid: > 0 })
                    {
                        // GcLogger.Log("item drop. uid: "+ item.Uid + " / Name: "+item.Name);
                        return item.Uid;
                    }
                }
            }
            return 0;
        }
        /// <summary>
        /// 몬스터 죽은 후 드랍 아이템 가져오기
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <param name="monsterObject"></param>
        public void OnMonsterDead(int monsterUid, GameObject monsterObject)
        {
            MakeDropGold(monsterUid, monsterObject);
            int itemUid = GetDropItem(monsterUid);
            if (itemUid <= 0) return;
            // todo. Class 확률 적용해야 함.
            MakeDropItem(monsterObject.transform.position, itemUid, 1, ItemConstants.Class.Normal, _playerData.CurrentLevel);
        }
        /// <summary>
        /// 골드 드랍 처리
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <param name="monsterObject"></param>
        private void MakeDropGold(int monsterUid, GameObject monsterObject)
        {
            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info is { RewardGold: > 0 })
            {
                MakeDropItem(monsterObject.transform.position, CurrencyConstants.ItemUidGold, info.RewardGold);
            }
        }
        /// <summary>
        /// Item Drop Group 테이블에서 Type 별로 찾아보기 
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private StruckTableItem FindItemByGroup(StruckTableItemDropGroup group)
        {
            switch (group.Type)
            {
                case ItemDropGroup.ItemUid when int.TryParse(group.Value, out var itemUid):
                    return _tableItem.GetDataByUid(itemUid);
                case ItemDropGroup.ItemCategory:
                    ItemConstants.Category category = (ItemConstants.Category)Enum.Parse(typeof(ItemConstants.Category), group.Value);
                    return _dictionaryByCategory[category][Random.Range(0, _dictionaryByCategory[category].Count)];
                case ItemDropGroup.ItemSubCategory:
                {
                    ItemConstants.SubCategory subCategory = (ItemConstants.SubCategory)Enum.Parse(typeof(ItemConstants.SubCategory), group.Value);
                    return _dictionaryBySubCategory[subCategory][Random.Range(0, _dictionaryBySubCategory[subCategory].Count)];
                }
                case ItemDropGroup.Nothing:
                case ItemDropGroup.ExcludeItemUid:
                case ItemDropGroup.None:
                default:
                    return null;
            }
        }
        /// <summary>
        /// 현재 씬의 플레이어를 기준으로 드랍 아이템 획득을 처리합니다.
        /// </summary>
        /// <param name="item">획득을 시도한 월드 아이템입니다.</param>
        public void PlayerTaken(Item item)
        {
            Player player = _sceneGame?.player != null
                ? _sceneGame.player.GetComponent<Player>()
                : null;
            PlayerTaken(player, item);
        }

        /// <summary>
        /// 플레이어 상태와 월드 아이템의 획득 정책을 확인한 뒤 아이템을 지급합니다.
        /// </summary>
        /// <param name="player">아이템 획득을 시도한 플레이어입니다.</param>
        /// <param name="item">획득을 시도한 월드 아이템입니다.</param>
        public void PlayerTaken(Player player, Item item)
        {
            if (item == null ||
                item.GetItemUid() <= 0 ||
                !item.CanBeCollectedBy(player))
            {
                return;
            }

            long instanceId = item.GetInstanceId();
            var result = CollectItemCore(item.GetItemUid(), item.GetItemCountLong(), instanceId);
            if (result == null || result.Result == ResultCommon.ResultType.Fail)
            {
                return;
            }

            NotifyItemCollected(
                item.GetItemUid(),
                item.GetItemCountLong(),
                result,
                item.GetSourceKey(),
                item.GetRuntimeToken());

            // 획득 시에는 인스턴스를 유지해야 한다.
            item.ResetInfos(removeInstanceFromDb: false);
        }

        /// <summary>
        /// 아직 획득되지 않은 월드 드랍 아이템을 풀로 반환합니다.
        /// </summary>
        /// <param name="item">반환할 월드 드랍 아이템입니다.</param>
        public void ReleaseWorldDrop(Item item)
        {
            if (item == null || item.GetItemUid() <= 0)
            {
                return;
            }

            item.ResetInfos(removeInstanceFromDb: false);
        }

        /// <summary>
        /// 비활성화된 드랍 아이템을 풀에 다시 등록합니다.
        /// </summary>
        public void AddPoolDropItem(Item item)
        {
            if (item != null)
            {
                _poolDropItem.Enqueue(item);
            }
        }

        /// <summary>
        /// 드랍되는 아이템 확률 계산하기 
        /// </summary>
        /// <param name="npcUid"></param>
        /// <returns></returns>
        private (int, int) GetDropItemByNpc(int npcUid)
        {
            if (!_npcDropDictionary.ContainsKey(npcUid)) return (0, 0);

            Dictionary<DropRateType, int> dropRates = new Dictionary<DropRateType, int>();

            // 드롭 확률을 미리 정리
            foreach (StruckTableNpcDropRate dropEntry in _npcDropDictionary[npcUid])
            {
                dropRates[dropEntry.Type] = dropEntry.Rate;
            }

            int roll = Random.Range(0, 100);

            // ItemDropGroupUid 체크
            float cumulativePercent = 0f;
            int groupUid = 0;
            if (dropRates.ContainsKey(DropRateType.ItemDropGroupUid))
            {
                foreach (StruckTableNpcDropRate dropEntry in _npcDropDictionary[npcUid])
                {
                    cumulativePercent += dropEntry.Rate;
                    if (roll < cumulativePercent)
                    {
                        groupUid = dropEntry.Value;
                        break;
                    }
                }
            }

            if (groupUid <= 0 || !_dropGroupDictionary.ContainsKey(groupUid))
            {
                return (0, 0);
            }

            roll = Random.Range(0, 100);
            cumulativePercent = 0f;
            foreach (StruckTableItemDropGroup group in _dropGroupDictionary[groupUid])
            {
                cumulativePercent += group.Rate;
                if (roll < cumulativePercent) 
                {
                    StruckTableItem item = FindItemByGroup(group);
                    if (item is { Uid: > 0 })
                    {
                        // GcLogger.Log($"item drop. uid: {item.Uid} / Name: {item.Name}");
                        return (item.Uid, 1);
                    }
                }
            }
            return (0, 0);
        }
        public void OnNpcDead(int uid, GameObject npc)
        {
            var data = GetDropItemByNpc(uid);
            int itemUid = data.Item1;
            int count = data.Item2;
            if (itemUid <= 0) return;
            MakeDropItem(npc.transform.position, itemUid, count);
        }
#if UNITY_EDITOR
        /// <summary>
        /// 아이템 드랍 확률 테스트
        /// GGemCoTool 의 TestDropItemRate 에서 사용
        /// </summary>
        public class DropTestResult
        {
            public int monsterUid;
            public int iterations;
            public Dictionary<DropRateType, int> dropRateCounts;
            public Dictionary<ItemConstants.Category, int> categoryCounts;
            public Dictionary<ItemConstants.SubCategory, int> subCategoryCounts;
            public int totalDrops;
        }
        private DropTestResult _lastTestResult;
        public DropTestResult TestDropRates(int monsterUid, int iterations, Dictionary<ItemConstants.Category,List<StruckTableItem>> dictionaryByCategory, Dictionary<ItemConstants.SubCategory,List<StruckTableItem>> dictionaryBySubCategory, Dictionary<int,List<StruckTableItemDropGroup>> dropGroupDictionary, Dictionary<int,List<StruckTableMonsterDropRate>> monsterDropDictionary, TableItem ptableItem)
        {
            _lastTestResult = new DropTestResult
            {
                monsterUid = monsterUid,
                iterations = iterations,
                dropRateCounts = new Dictionary<DropRateType, int>(),
                categoryCounts = new Dictionary<ItemConstants.Category, int>(),
                subCategoryCounts = new Dictionary<ItemConstants.SubCategory, int>(),
                totalDrops = 0
            };
            
            _dictionaryByCategory = dictionaryByCategory;
            _dictionaryBySubCategory = dictionaryBySubCategory;
            _dropGroupDictionary = dropGroupDictionary;
            _monsterDropDictionary = monsterDropDictionary;
            _tableItem = ptableItem;
            
            foreach (DropRateType type in Enum.GetValues(typeof(DropRateType)))
            {
                _lastTestResult.dropRateCounts[type] = 0;
            }
            foreach (ItemConstants.Category category in Enum.GetValues(typeof(ItemConstants.Category)))
            {
                _lastTestResult.categoryCounts[category] = 0;
            }
            foreach (ItemConstants.SubCategory subCategory in Enum.GetValues(typeof(ItemConstants.SubCategory)))
            {
                _lastTestResult.subCategoryCounts[subCategory] = 0;
            }

            for (int i = 0; i < iterations; i++)
            {
                int itemUid = GetDropItem(monsterUid);

                if (itemUid <= 0)
                {
                    _lastTestResult.dropRateCounts[DropRateType.Nothing]++;
                }
                else
                {
                    _lastTestResult.dropRateCounts[DropRateType.ItemDropGroupUid]++;
                    var info = _tableItem.GetDataByUid(itemUid);
                    if (info == null) continue;

                    if (Enum.IsDefined(typeof(ItemConstants.Category), info.Category))
                    {
                        _lastTestResult.categoryCounts[info.Category]++;
                    }

                    if (Enum.IsDefined(typeof(ItemConstants.SubCategory), info.SubCategory))
                    {
                        _lastTestResult.subCategoryCounts[info.SubCategory]++;
                    }

                    _lastTestResult.totalDrops++;
                }
            }

            // GcLogger.Log($"테스트 완료: 몬스터 UID {monsterUid}, {iterations}회 실행됨.");
            return _lastTestResult;
        }
#endif
        public void OnDestroy()
        {
        }
    }
}
