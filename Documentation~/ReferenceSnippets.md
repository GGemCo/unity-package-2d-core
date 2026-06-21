# Core ReferenceSnippets

작성일: 2026-02-18

목적:
- Core 패키지에서 AI가 **동일한 스타일/구조**로 코드를 생성하도록, 핵심 레퍼런스(스니펫)를 제공

우선순위:
1) `Docs/ReferenceSnippets.md`
2) `Docs/STYLE_CONTRACT.md`
3) `Docs/GOLDEN_REFERENCES.md`
4) `Docs/GGemCoPatterns/*`
5) Core `CONVENTIONS/ARCHITECTURE/PLAYBOOK`

---

## Projectile 정책 확장 기준(컨트롤러 진입점)

- 경로: `Projectile/ProjectileController.cs`
- 포인트:
  - 발사체 초기화/틱/충돌 처리의 중심 진입점
  - Boundary/반사 같은 정책은 컨트롤러에 직접 누적하지 말고, 정책(enum)+헬퍼/핸들러로 분리

```csharp
    /// - 테이블 조회 → 발사 횟수/지연/타겟 유형 처리
    /// - 코루틴으로 다발사 타이밍 관리
    /// </summary>
    public class ProjectileController
    {
        private CharacterBase _character;
        private ProjectileManager _projectileManager;

        private CharacterBase _target;

        public void Initialize(CharacterBase characterBase)
        {
            _character = characterBase;
            _projectileManager = SceneGame.Instance.ProjectileManager;
        }

        /// <summary>
        /// 메타데이터를 받아 지정 발사체를 발사합니다.
        /// - Fixed: 오브젝트 타겟
        /// - Area/None: 좌표 타겟
        /// </summary>
        public void Launch(MetadataProjectile metadataProjectile)
        {
            if (metadataProjectile == null) return;

            _target = metadataProjectile.Target;

            var info = TableLoaderManager.Instance.GetProjectileData(metadataProjectile.Uid);
            if (info == null) return;

            // owner가 비어 있으면 이 캐릭터를 owner로 사용(기본 정책)
            var meta = metadataProjectile.Owner == null
                ? new MetadataProjectile(
                    uid: metadataProjectile.Uid,
                    damageType: metadataProjectile.DamageType,
                    damage: metadataProjectile.Damage,
                    target: metadataProjectile.Target,
                    owner: _character,
                    speedMultiplier: metadataProjectile.SpeedMultiplier,
                    scaleMultiplier: metadataProjectile.ScaleMultiplier,
                    visualType: metadataProjectile.VisualType,
                    visualSprite: metadataProjectile.VisualSprite,
                    visualAnimatorController: metadataProjectile.VisualAnimatorController,
                    visualEffectUidOverride: metadataProjectile.VisualEffectUidOverride)
                : metadataProjectile;

            _character.StartCoroutine(CreateProjectileBurst(info, meta));
        }

        private IEnumerator CreateProjectileBurst(StruckTableProjectile info, MetadataProjectile meta)
        {
            // 목표가 필요한 타입인데 타겟이 없다면 중단
            if (info.TargetType == ProjectileConstants.TargetType.Fixed && !_target)
                yield break;

            int count = Mathf.Max(1, info.Count);
            for (int i = 0; i < count; i++)
            {
                var proj = _projectileManager.CreateProjectile(meta);
                if (proj != null)
                {
                    // 좌표 산출
                    if (info.TargetType == ProjectileConstants.TargetType.Fixed)
                    {
                        proj.Launch(_target);
                    }
                    else
                    {
                        // Area/None: 좌표 기반
                        // 직선형은 X를 고정, 곡선형은 X를 범위에서 샘플
                        float x = _target
                            ? _target.transform.position.x
                            : _character.transform.position.x;

                        bool isArc = (info.ArcHeightMin > 0) || (info.ArcHeightMax > 0);
                        if (isArc && _target)
                        {
                            x = Random.Range(_target.transform.position.x - info.TargetPositionRangeX,
                                             _target.transform.position.x + info.TargetPositionRangeX);
                        }

                        float y = _target
                            ? _target.GetRandomPositionYInHitArea()
                            : _character.transform.position.y;

                        proj.Launch(new Vector2(x, y));
                    }
                }

                float delay = Mathf.Max(0f, info.SecDelayByOne);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }
}
```
## TableLoader 기반(테이블 파싱/캐시 파이프라인)

- 경로: `TableLoader/TableLoaderBase.cs`
- 포인트:
  - 테이블 로딩/파싱의 기본 골격
  - 테이블 변경 시: Struct/Parser/Loader/EditorTool/Export를 함께 갱신

```csharp

namespace GGemCo2DCore
{
    public class TableLoaderBase : MonoBehaviour
    {
        protected TableRegistry registry;
        
        private bool EnsureInitialized()
        {
            if (registry != null) return true;

            var manager = FindFirstObjectByType<TableLoaderManager>();
            if (manager == null)
            {
                GcLogger.LogWarning("[TableLoaderManager] Instance not found.");
                return false;
            }

            registry ??= new TableRegistry();
            return true;
        }

        public bool RegistryTable(ITableParser tableParser)
        {
            if (!EnsureInitialized())
                return false;

            registry.Register(tableParser);
            return true;
        }
        public bool TryLoadTable(string key, string content)
        {
            return registry.TryLoad(key, content);
        }
        /// <summary>
        /// 제네릭을 사용하여 Addressables에서 설정을 로드하는 함수
        /// </summary>
        private async Task<string> LoadTextAsync(string key)
        {
            // 키가 Addressables에 등록되어 있는지 확인
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            await locationsHandle.Task;

            if (!locationsHandle.Status.Equals(AsyncOperationStatus.Succeeded) || locationsHandle.Result.Count == 0)
            {
                GcLogger.LogError($"[AddressableSettingsLoader] '{key}' 가 Addressables에 등록되지 않았습니다. '{key}' 를 생성한 후 {ConfigDefine.NameSDK}Tool > 기본 셋팅하기 메뉴를 열고 Addressable 추가하기 버튼을 클릭해주세요.");
                Addressables.Release(locationsHandle);
                return null;
            }

            var handle = Addressables.LoadAssetAsync<TextAsset>(key);
            var asset = await handle.Task;
            Addressables.Release(locationsHandle);

            string content = asset != null ? asset.text : null;

            // 여기가 핵심: 사용 직후 해제
            Addressables.Release(handle);

            return content;
        }
        public async Task LoadDataFile(AddressableAssetInfo info)
        {
            var content = await LoadTextAsync(info.Key);
            if (string.IsNullOrEmpty(content)) return;

            if (!TryLoadTable(info.Etc1, content))
                GcLogger.LogWarning($"[TableLoader] Unregistered table key: {info.Etc1}");
        }
        // ===============================
        // Generic Helper (공통 로깅/널 처리)
        // ===============================
        protected TRow GetData<TTable, TRow>(
            TTable table,
            int uid,
            string label,
            Func<TTable, int, TRow> getFunc,
            bool logIfMissing = true)
            where TRow : class
        {
            if (table == null)
            {
                if (logIfMissing)
                    GcLogger.LogWarning($"[Table] {label} table is null.");
                return null;
            }

            var row = getFunc(table, uid);
            if (row == null && logIfMissing)
                GcLogger.LogWarning($"[Table] {label} not found. uid={uid}");
            return row;
        }

        protected bool TryGetData<TTable, TRow>(
            TTable table,
            int uid,
            out TRow row,
            string label,
            Func<TTable, int, TRow> getFunc,
            bool logIfMissing = false)
            where TRow : class
        {
            row = GetData(table, uid, label, getFunc, logIfMissing);
            return row != null;
        }
    }
}
```
## TableLoaderManager(테이블 로딩 오케스트레이션)

- 경로: `TableLoader/TableLoaderManager.cs`
- 포인트:
  - 여러 테이블의 로딩 순서/초기화 흐름
  - Reload 시 캐시 갱신과 의존 순서를 유지

```csharp
    /// <summary>
    /// 데이터 테이블 Loader
    /// </summary>
    public class TableLoaderManager : TableLoaderBase
    {
        public static TableLoaderManager Instance;

        public TableNpc TableNpc { get; private set; } = new TableNpc();
        public TableMap TableMap { get; private set; } = new TableMap();
        public TableMonster TableMonster { get; private set; } = new TableMonster();
        public TableAnimation TableAnimation { get; private set; } = new TableAnimation();
        public TableItem TableItem { get; private set; } = new TableItem();
        // Item option tables
        public TableItemBaseOption TableItemBaseOption { get; private set; } = new TableItemBaseOption();
        public TableItemAffixDef TableItemAffixDef { get; private set; } = new TableItemAffixDef();
        public TableItemAffixPool TableItemAffixPool { get; private set; } = new TableItemAffixPool();
        public TableItemRollRule TableItemRollRule { get; private set; } = new TableItemRollRule();
        public TableMonsterDropRate TableMonsterDropRate { get; private set; } = new TableMonsterDropRate();
        public TableNpcDropRate TableNpcDropRate { get; private set; } = new TableNpcDropRate();
        public TableItemDropGroup TableItemDropGroup { get; private set; } = new TableItemDropGroup();
        public TableExp TableExp { get; private set; } = new TableExp();
        public TableWindow TableWindow { get; private set; } = new TableWindow();
        public TableStat TableStat { get; private set; } = new TableStat();
        public TableDamageType TableDamageType { get; private set; } = new TableDamageType();
        public TableState TableState { get; private set; } = new TableState();
        public TableCrowdControl TableCrowdControl { get; private set; } = new TableCrowdControl();
        public TableEffect TableEffect { get; private set; } = new TableEffect();
        public TableInteraction TableInteraction { get; private set; } = new TableInteraction();
        public TableShop TableShop { get; private set; } = new TableShop();
        public TableItemUpgrade TableItemUpgrade { get; private set; } = new TableItemUpgrade();
        public TableItemSalvage TableItemSalvage { get; private set; } = new TableItemSalvage();
        public TableItemCraft TableItemCraft { get; private set; } = new TableItemCraft();
        public TableCutscene TableCutscene { get; private set; } = new TableCutscene();
        public TableDialogue TableDialogue { get; private set; } = new TableDialogue();
        public TableProjectile TableProjectile { get; private set; } = new TableProjectile();
        public TableSound TableSound { get; private set; } = new TableSound();
        public TableSimulationTool TableSimulationTool { get; private set; } = new TableSimulationTool();
        public TableSimulationGrowth TableSimulationGrowth { get; private set; } = new TableSimulationGrowth();
        public TableItemUse TableItemUse { get; private set; } = new TableItemUse();
        public TableItemUseAction TableItemUseAction { get; private set; } = new TableItemUseAction();
        
        protected void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }

                registry = new TableRegistry();
                registry.Register(TableAnimation);
                registry.Register(TableMonster);
                registry.Register(TableNpc);
                registry.Register(TableMap);
                registry.Register(TableItem);
                registry.Register(TableItemBaseOption);
                registry.Register(TableItemAffixDef);
                registry.Register(TableItemAffixPool);
                registry.Register(TableItemRollRule);
                registry.Register(TableMonsterDropRate);
                registry.Register(TableNpcDropRate);
                registry.Register(TableItemDropGroup);
                registry.Register(TableExp);
                registry.Register(TableWindow);
                registry.Register(TableStat);
                registry.Register(TableDamageType);
                registry.Register(TableState);
                registry.Register(TableCrowdControl);
                registry.Register(TableEffect);
                registry.Register(TableInteraction);
                registry.Register(TableShop);
                registry.Register(TableItemUpgrade);
                registry.Register(TableItemSalvage);
                registry.Register(TableItemCraft);
                registry.Register(TableCutscene);
                registry.Register(TableDialogue);
                registry.Register(TableProjectile);
                registry.Register(TableSound);
                registry.Register(TableSimulationTool);
                registry.Register(TableSimulationGrowth);
                registry.Register(TableItemUse);
                registry.Register(TableItemUseAction);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private float GetNpcMoveStep(int npcUid)
        {
            var info = TableNpc.GetDataByUid(npcUid);
            if (info == null) return 0;
            var info2 = GetAnimationData(info.AnimationUid);
            if (info2 is { MoveStep: > 0 })
            {
                return info2.MoveStep;
            }
            return 0;
        }

        private float GetMonsterMoveStep(int monsterUid)
        {
            var info = TableMonster.GetDataByUid(monsterUid);
            if (info == null) return 0;
            var info2 = GetAnimationData(info.AnimationUid);
            if (info2 is { MoveStep: > 0 })
            {
                return info2.MoveStep;
            }
            return 0;
        }

        

        /// <summary>
        /// Locale 변경 등으로 인해, 로드 시점에 캐시된 표시용 Name 필드를 다시 로컬라이즈합니다.
        /// - Stat/DamageType/State 테이블은 로드 시점에 Name을 덮어쓰므로, Locale 변경 시 재적용이 필요합니다.
        /// </summary>
        public void RefreshStatusNames()
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return;

            TableStat.RefreshLocalizedNames(loc);
            TableDamageType.RefreshLocalizedNames(loc);
            TableState.RefreshLocalizedNames(loc);
        }

        public float GetCharacterMoveStep(CharacterConstants.Type type, int characterUid)
        {
            if (type == CharacterConstants.Type.Npc)
            {
                return GetNpcMoveStep(characterUid);
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                return GetMonsterMoveStep(characterUid);
            }

            return 0;
        }

        // =======================================
        // Facade Accessors for ALL Tables (Get/Try)
        // =======================================

        // Npc
        public StruckTableNpc GetNpcData(int uid, bool logIfMissing = true)
            => GetData(TableNpc, uid, "NPC", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetNpcData(int uid, out StruckTableNpc data, bool logIfMissing = false)
            => TryGetData(TableNpc, uid, out data, "NPC", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Map
        public StruckTableMap GetMapData(int uid, bool logIfMissing = true)
            => GetData(TableMap, uid, "Map", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetMapData(int uid, out StruckTableMap data, bool logIfMissing = false)
            => TryGetData(TableMap, uid, out data, "Map", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Monster
        public StruckTableMonster GetMonsterData(int uid, bool logIfMissing = true)
            => GetData(TableMonster, uid, "Monster", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetMonsterData(int uid, out StruckTableMonster data, bool logIfMissing = false)
            => TryGetData(TableMonster, uid, out data, "Monster", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Animation
        public StruckTableAnimation GetAnimationData(int uid, bool logIfMissing = true)
            => GetData(TableAnimation, uid, "Animation", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetAnimationData(int uid, out StruckTableAnimation data, bool logIfMissing = false)
            => TryGetData(TableAnimation, uid, out data, "Animation", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Item
        public StruckTableItem GetItemData(int uid, bool logIfMissing = true)
            => GetData(TableItem, uid, "Item", (t, i) => t.GetDataByUid(i), logIfMissing);
        public bool TryGetItemData(int uid, out StruckTableItem data, bool logIfMissing = false)
            => TryGetData(TableItem, uid, out data, "Item", (t, i) => t.GetDataByUid(i), logIfMissing);

        // Window
        public StruckTableWindow GetWindowData(int uid, bool logIfMissing = true)
            => GetData(TableWindow, uid, "Window", (t, i) => t.GetDataByUid(i), logIfMissing);
```
## Addressables 로딩/해제 기준(컨트롤러)

- 경로: `AddressableLoader/AddressableLoaderController.cs`
- 포인트:
  - Load/Release 정책의 중심 진입점
  - 키 문자열 분산 금지(Keys/Config로 중앙화)
  - 씬 전환/반복 로딩 시 누수 방지

```csharp

namespace GGemCo2DCore
{
    public static class AddressableLoaderController
    {
        private static readonly Dictionary<object, AsyncOperationHandle> LoadedResources = new Dictionary<object, AsyncOperationHandle>();
        private static readonly HashSet<AsyncOperationHandle> ActiveHandles = new HashSet<AsyncOperationHandle>();

        /// <summary>
        /// key를 통해 단일 리소스를 비동기로 로드합니다.
        /// </summary>
        public static async Task<T> LoadByKeyAsync<T>(string key) where T : Object
        {
            // 이미 로드된 리소스가 있는 경우 반환
            foreach (var pair in LoadedResources)
            {
                if (pair.Value.IsValid() && pair.Value.DebugName == key && pair.Key is T loaded)
                {
                    return loaded;
                }
            }
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var result = handle.Result;
                LoadedResources[result] = handle;
                ActiveHandles.Add(handle);
                return result;
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to load asset by key: {key}");
            return null;
        }

        /// <summary>
        /// label을 통해 여러 리소스를 비동기로 로드합니다.
        /// </summary>
        public static async Task<Dictionary<string, T>> LoadByLabelAsync<T>(string label) where T : Object
        {
            // 리소스 위치를 먼저 가져옵니다.
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            await locationsHandle.Task;

            if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableLoaderManager] Failed to get resource locations for label: {label}");
                return null;
            }

            var result = new Dictionary<string, T>();
            var locations = locationsHandle.Result;

            foreach (var location in locations)
            {
                string key = location.PrimaryKey;
                
                // 이미 로드된 오브젝트가 있는 경우 캐시에서 꺼냄
                bool alreadyLoaded = false;
                foreach (var pair in LoadedResources)
                {
                    if (pair.Value.IsValid() && pair.Value.DebugName == key && pair.Key is T cachedObj)
                    {
                        result[key] = cachedObj;
                        alreadyLoaded = true;
                        break;
                    }
                }
                if (alreadyLoaded) continue;
                
                var handle = Addressables.LoadAssetAsync<T>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    T obj = handle.Result;
                    result[key] = obj;

                    LoadedResources[obj] = handle;
                    ActiveHandles.Add(handle);
                }
                else
                {
                    Debug.LogWarning($"[AddressableLoaderManager] Failed to load: {key}");
                }
            }

            Addressables.Release(locationsHandle);
            return result;
        }

        /// <summary>
        /// Addressables.InstantiateAsync 를 사용하여 프리팹 인스턴스를 생성합니다.
        /// 자동으로 해제 추적에 포함됩니다.
        /// </summary>
        public static async Task<GameObject> InstantiateAsync(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            var handle = Addressables.InstantiateAsync(key, position, rotation, parent);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject instance = handle.Result;
                LoadedResources[instance] = handle;
                ActiveHandles.Add(handle);
                return instance;
            }

            Debug.LogError($"[AddressableLoaderManager] Failed to instantiate prefab with key: {key}");
            return null;
        }

        /// <summary>
        /// 개별 리소스를 해제합니다.
        /// </summary>
        public static void Release(object obj)
        {
            if (obj == null) return;

            if (LoadedResources.TryGetValue(obj, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                LoadedResources.Remove(obj);
                ActiveHandles.Remove(handle);
            }
            else
            {
                Debug.LogWarning($"[AddressableLoaderManager] Tried to release unknown object: {obj}");
            }
        }

        /// <summary>
        /// 모든 로드된 리소스를 해제합니다.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var handle in ActiveHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            ActiveHandles.Clear();
            LoadedResources.Clear();
        }
        public static void ReleaseByHandles(HashSet<AsyncOperationHandle> handles)
        {
            foreach (var handle in handles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            handles.Clear();
        }
    }
}
```
