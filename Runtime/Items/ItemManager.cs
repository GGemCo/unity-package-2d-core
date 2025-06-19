using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 몬스터 사냥 후 드랍되는 아이템 관리 매니저
    /// </summary>
    public class ItemManager
    {
        // 드랍되는 아이템 pool size
        private int poolSize;
        // 드랍되는 아이템 pool queue
        private readonly Queue<Item> poolDropItem = new Queue<Item>();
        // 드랍되는 아이템을 보기 쉽게 하기위한 container 
        private GameObject containerPoolDropItem;
        // 드랍되는 아이템 prefab
        private GameObject prefabDropItem;
        // 드랍되는 아이템 pool size 값을 원래 값으로 초기화 하는 coroutine
        private Coroutine reducePoolCoroutine;
        // 드랍되는 아이템 pool size 값을 원래 값으로 초기화 시간
        private readonly float poolReduceTime = 10f;
        private SceneGame sceneGame;
        private UIWindowInventory uiWindowInventory;
        
        public enum MonsterDropRateType
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

        private TableItem tableItem;
        private Dictionary<ItemConstants.Category, List<StruckTableItem>> dictionaryByCategory;
        private Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> dictionaryBySubCategory;
        private Dictionary<int, List<StruckTableItemDropGroup>> dropGroupDictionary = new Dictionary<int, List<StruckTableItemDropGroup>>();
        private Dictionary<int, List<StruckTableMonsterDropRate>> monsterDropDictionary = new Dictionary<int, List<StruckTableMonsterDropRate>>();

        /// <summary>
        /// 초기화. Awake 단계에서 호출됩니다.
        /// </summary>
        public void Initialize(SceneGame psceneGame)
        {
            poolSize = 1;
            poolDropItem.Clear();
            InitializePool();
            tableItem = TableLoaderManager.Instance.TableItem;
            dictionaryByCategory = tableItem.GetDictionaryByCategory();
            dictionaryBySubCategory = tableItem.GetDictionaryBySubCategory();
            dropGroupDictionary = TableLoaderManager.Instance.TableItemDropGroup.GetDropGroups();
            monsterDropDictionary = TableLoaderManager.Instance.TableMonsterDropRate.GetMonsterDropDictionary();
            sceneGame = psceneGame;
            uiWindowInventory =
                sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid.Inventory);
        }
        /// <summary>
        /// Addressable 에 등록된 damageText 를 불러와서 pool 을 만든다 
        /// </summary>
        private void InitializePool()
        {
            if (AddressableSettingsLoader.Instance == null) return;
            prefabDropItem = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabDropItem);
            if (prefabDropItem == null) return;
            containerPoolDropItem = new GameObject("ContainerPoolDropItem");
            ExpandPool(poolSize); // 초기 풀 생성
        }
        /// <summary>
        /// 풀에서 아이템을 가져오고, 부족하면 새로운 아이템을 생성한다.
        /// </summary>
        private Item GetOrCreateItem()
        {
            if (poolDropItem.Count == 0)
            {
                ExpandPool(poolSize); // 풀 사이즈만큼 추가 생성
            }
            return poolDropItem.Dequeue();
        }

        /// <summary>
        /// 특정 개수만큼 풀을 확장하여 아이템을 추가 생성.
        /// </summary>
        private void ExpandPool(int amount)
        {
            if (prefabDropItem == null || containerPoolDropItem == null) return;
            for (int i = 0; i < amount; i++)
            {
                GameObject gameObjectText = Object.Instantiate(prefabDropItem, containerPoolDropItem.transform);
                Item item = gameObjectText.GetComponent<Item>();
                item.gameObject.SetActive(false);
                poolDropItem.Enqueue(item);
            }
            // GcLogger.Log($"풀 확장: {amount}개 아이템 추가 (총 {poolDropItem.Count}개)");
        }
        /// <summary>
        /// 아이템 맵에 드랍하기 
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        public void MakeDropItem(Vector3 worldPosition, int itemUid, int itemCount)
        {
            var info = tableItem.GetDataByUid(itemUid);
            if (info == null) return;
            Item item = GetOrCreateItem();
            item.itemUid = itemUid;
            item.itemCount = itemCount;
            item.startPos= worldPosition;
            item.StartDrop();
            
            // 풀을 일정 시간이 지나면 정리하도록 코루틴 시작
            reducePoolCoroutine ??= sceneGame.StartCoroutine(ReducePoolSize());
        }
        /// <summary>
        /// 일정 시간이 지나면 풀 크기를 다시 poolSize 값으로 줄인다.
        /// </summary>
        private IEnumerator ReducePoolSize()
        {
            yield return new WaitForSeconds(poolReduceTime);

            while (poolDropItem.Count > poolSize)
            {
                Item itemToDestroy = poolDropItem.Dequeue();
                if (itemToDestroy.itemUid > 0) continue;
                Object.Destroy(itemToDestroy.gameObject);
            }

            // GcLogger.Log($"풀 크기 정리 완료: {poolSize}개 유지");
            reducePoolCoroutine = null;
        }
        /// <summary>
        /// 드랍되는 아이템 확률 계산하기 
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <returns></returns>
        private int GetDropItem(int monsterUid)
        {
            if (!monsterDropDictionary.ContainsKey(monsterUid)) return 0;

            Dictionary<MonsterDropRateType, int> dropRates = new Dictionary<MonsterDropRateType, int>();

            // 드롭 확률을 미리 정리
            foreach (StruckTableMonsterDropRate dropEntry in monsterDropDictionary[monsterUid])
            {
                dropRates[dropEntry.Type] = dropEntry.Rate;
            }

            int roll = Random.Range(0, 100);

            // ItemDropGroupUid 체크
            float cumulativePercent = 0f;
            int groupUid = 0;
            if (dropRates.ContainsKey(MonsterDropRateType.ItemDropGroupUid))
            {
                foreach (StruckTableMonsterDropRate dropEntry in monsterDropDictionary[monsterUid])
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
            if (!dropGroupDictionary.ContainsKey(groupUid))
                return 0;
            
            roll = Random.Range(0, 100);
            cumulativePercent = 0f;
            foreach (StruckTableItemDropGroup group in dropGroupDictionary[groupUid])
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
        /// <param name="monsterVid"></param>
        /// <param name="monsterUid"></param>
        /// <param name="monsterObject"></param>
        public void OnMonsterDead(int monsterVid, int monsterUid, GameObject monsterObject)
        {
            int itemUid = GetDropItem(monsterUid);
            if (itemUid <= 0) return;
            MakeDropItem(monsterObject.transform.position, itemUid, 1);
            MakeDropGold(monsterUid, monsterObject);
        }
        /// <summary>
        /// 골드 드랍 처리
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <param name="monsterObject"></param>
        private void MakeDropGold(int monsterUid, GameObject monsterObject)
        {
            var info = TableLoaderManager.Instance.TableMonster.GetDataByUid(monsterUid);
            if (info != null && info.RewardGold > 0)
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
                    return tableItem.GetDataByUid(itemUid);
                case ItemDropGroup.ItemCategory:
                    ItemConstants.Category category = (ItemConstants.Category)Enum.Parse(typeof(ItemConstants.Category), group.Value);
                    return dictionaryByCategory[category][Random.Range(0, dictionaryByCategory[category].Count)];
                case ItemDropGroup.ItemSubCategory:
                {
                    ItemConstants.SubCategory subCategory = (ItemConstants.SubCategory)Enum.Parse(typeof(ItemConstants.SubCategory), group.Value);
                    return dictionaryBySubCategory[subCategory][Random.Range(0, dictionaryBySubCategory[subCategory].Count)];
                }
                case ItemDropGroup.Nothing:
                case ItemDropGroup.ExcludeItemUid:
                case ItemDropGroup.None:
                default:
                    return null;
            }
        }
        /// <summary>
        /// 플레이어가 드랍 아이템을 먹었을때 처리 
        /// </summary>
        public void PlayerTaken(GameObject dropItem)
        {
            Item item = dropItem.GetComponent<Item>();
            if (item ==null || item.itemUid <= 0) return;
            var result = sceneGame.saveDataManager.Inventory.AddItem(item.itemUid, item.itemCount);
            if (uiWindowInventory != null)
            {
                uiWindowInventory.SetIcons(result);
                GameEventManager.ItemCollected(item.itemUid, item.itemCount);
            }
            
            item.Reset();
        }

        public void AddPoolDropItem(Item item)
        {
            poolDropItem.Enqueue(item);
        }
#if UNITY_EDITOR
        /// <summary>
        /// 아이템 드랍 확률 테스트
        /// GGemCoTool 의 TestDropItemRate 에서 사용
        /// </summary>
        public class DropTestResult
        {
            public int MonsterUid;
            public int Iterations;
            public Dictionary<MonsterDropRateType, int> DropRateCounts;
            public Dictionary<ItemConstants.Category, int> CategoryCounts;
            public Dictionary<ItemConstants.SubCategory, int> SubCategoryCounts;
            public int TotalDrops;
        }
        private DropTestResult lastTestResult;
        public DropTestResult TestDropRates(int monsterUid, int iterations, Dictionary<ItemConstants.Category,List<StruckTableItem>> pdictionaryByCategory, Dictionary<ItemConstants.SubCategory,List<StruckTableItem>> pdictionaryBySubCategory, Dictionary<int,List<StruckTableItemDropGroup>> pdropGroupDictionary, Dictionary<int,List<StruckTableMonsterDropRate>> pmonsterDropDictionary, TableItem ptableItem)
        {
            lastTestResult = new DropTestResult
            {
                MonsterUid = monsterUid,
                Iterations = iterations,
                DropRateCounts = new Dictionary<MonsterDropRateType, int>(),
                CategoryCounts = new Dictionary<ItemConstants.Category, int>(),
                SubCategoryCounts = new Dictionary<ItemConstants.SubCategory, int>(),
                TotalDrops = 0
            };
            
            dictionaryByCategory = pdictionaryByCategory;
            dictionaryBySubCategory = pdictionaryBySubCategory;
            dropGroupDictionary = pdropGroupDictionary;
            monsterDropDictionary = pmonsterDropDictionary;
            tableItem = ptableItem;
            
            foreach (MonsterDropRateType type in Enum.GetValues(typeof(MonsterDropRateType)))
            {
                lastTestResult.DropRateCounts[type] = 0;
            }
            foreach (ItemConstants.Category category in Enum.GetValues(typeof(ItemConstants.Category)))
            {
                lastTestResult.CategoryCounts[category] = 0;
            }
            foreach (ItemConstants.SubCategory subCategory in Enum.GetValues(typeof(ItemConstants.SubCategory)))
            {
                lastTestResult.SubCategoryCounts[subCategory] = 0;
            }

            for (int i = 0; i < iterations; i++)
            {
                int itemUid = GetDropItem(monsterUid);

                if (itemUid <= 0)
                {
                    lastTestResult.DropRateCounts[MonsterDropRateType.Nothing]++;
                }
                else
                {
                    lastTestResult.DropRateCounts[MonsterDropRateType.ItemDropGroupUid]++;
                    var info = tableItem.GetDataByUid(itemUid);
                    if (info == null) continue;

                    if (Enum.IsDefined(typeof(ItemConstants.Category), info.Category))
                    {
                        lastTestResult.CategoryCounts[info.Category]++;
                    }

                    if (Enum.IsDefined(typeof(ItemConstants.SubCategory), info.SubCategory))
                    {
                        lastTestResult.SubCategoryCounts[info.SubCategory]++;
                    }

                    lastTestResult.TotalDrops++;
                }
            }

            // GcLogger.Log($"테스트 완료: 몬스터 UID {monsterUid}, {iterations}회 실행됨.");
            return lastTestResult;
        }
#endif
        public void OnDestroy()
        {
        }
    }
}
