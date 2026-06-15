using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class MapEditor : DefaultEditorWindow
    {
        private const string Title = "Map 배치툴";

        // ---- Runtime refs (Editor session) ----
        private CharacterManager _characterManager;
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;

        private TableMap _tableMap;
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableAnimation _tableAnimation;

        private GameObject _gridTileMap;
        private MapTileCommon _defaultMap;

        private readonly NpcExporter _npcExporter = new NpcExporter();
        private readonly MonsterExporter _monsterExporter = new MonsterExporter();
        private readonly WarpExporter _warpExporter = new WarpExporter();
        // private readonly PatrolExporter _patrolExporter = new PatrolExporter();

        // ---- UI State ----
        private Vector2 _scrollPos;
        private bool _foldMap = true;
        private bool _foldNpc = true;
        private bool _foldNpcEdit = true;
        private bool _foldMonster = true;
        private bool _foldMonsterEdit = true;
        private bool _foldWarp = true;

        private int _selectedMapUid;
        private int _selectedNpcUid;
        private int _selectedMonsterUid;
        private bool _npcSpawnDefaultVisible;
        private MapCharacterVisibilityPolicy _npcSpawnMapVisibilityPolicy;
        private bool _editNpcDefaultVisible;
        private bool _editNpcFlip;
        private MapCharacterVisibilityPolicy _editNpcMapVisibilityPolicy;
        private bool _editNpcApplyToSameUid;
        private int _editNpcBoundInstanceId;
        private bool _usePatrolMonster;
        private MapCharacterVisibilityPolicy _monsterSpawnMapVisibilityPolicy;
        private MapCharacterVisibilityPolicy _editMonsterMapVisibilityPolicy;
        private bool _editMonsterApplyToSameUid;
        private int _editMonsterBoundInstanceId;

        // ---- Cached options (for SearchableDropdown) ----
        private readonly List<SearchableDropdownUtility.Option<int>> _mapOptions = new List<SearchableDropdownUtility.Option<int>>();
        private readonly List<SearchableDropdownUtility.Option<int>> _npcOptions = new List<SearchableDropdownUtility.Option<int>>();
        private readonly List<SearchableDropdownUtility.Option<int>> _monsterOptions = new List<SearchableDropdownUtility.Option<int>>();
        
        // MapEditor.cs 상단 필드 추가
        private bool _suppressSceneOpsThisEnable;

        [MenuItem(ConfigEditor.NameToolMapExporter, false, (int)ConfigEditor.ToolOrdering.MapExporter)]
        public static void ShowWindow()
        {
            GetWindow<MapEditor>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _selectedMapUid = 0;
            _selectedNpcUid = 0;
            _selectedMonsterUid = 0;
            _npcSpawnDefaultVisible = true;
            _npcSpawnMapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;
            _editNpcDefaultVisible = true;
            _editNpcFlip = false;
            _editNpcMapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;
            _editNpcApplyToSameUid = false;
            _editNpcBoundInstanceId = 0;
            _usePatrolMonster = false;
            _monsterSpawnMapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;
            _editMonsterMapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;
            _editMonsterApplyToSameUid = false;
            _editMonsterBoundInstanceId = 0;

            // 컴파일/도메인리로드 직후에는 씬 변경 작업을 막습니다.
            // (아래 2)에서 더 정교하게 처리)
            _suppressSceneOpsThisEnable = true;

            LoadTables();
            SetupServices();
            InitializeExporters();
            RebuildPopupCaches();

            // Grid 생성 등 씬 작업은 즉시 하지 않고 delayCall로 한 틱 뒤로 미룹니다.
            EditorApplication.delayCall += () =>
            {
                if (this == null) return; // 윈도우가 이미 닫혔을 수 있음
                _suppressSceneOpsThisEnable = false;

                // 여기서 Grid를 보장하는 것은 OK (삭제는 X)
                EnsureGridObject();
            };
        }

        private void OnDisable()
        {
            // DestroyGridObjectIfExists();
        }

        private void LoadTables()
        {
            _tableMap = TableLoaderManager.LoadMapTable();
            _tableNpc = TableLoaderManager.LoadNpcTable();
            _tableMonster = TableLoaderManager.LoadMonsterTable();
            _tableAnimation = TableLoaderManager.LoadSpineTable();
        }

        private void SetupServices()
        {
            _addressableLoaderPrefabCharacter = new AddressableLoaderPrefabCharacter();

            _characterManager = new CharacterManager();
            if (_tableNpc != null && _tableMonster != null && _tableAnimation != null)
            {
                _characterManager.Initialize(_tableNpc, _tableMonster, _tableAnimation, _addressableLoaderPrefabCharacter);
            }
        }

        private void EnsureGridObject()
        {
            _gridTileMap = GameObject.Find(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            if (_gridTileMap != null) return;

            _gridTileMap = new GameObject(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap))
            {
                tag = ConfigTags.GetValue(ConfigTags.Keys.GridTileMap)
            };

            var grid = _gridTileMap.AddComponent<Grid>();

            GGemCoMapSettings mapSettings =
                AssetDatabaseLoaderManager.LoadAsset<GGemCoMapSettings>(ConfigAddressableSetting.MapSettings.Path);

            if (mapSettings == null)
            {
                Debug.LogError($"MapSettings 로드 실패: {ConfigAddressableSetting.MapSettings.Path}");
                return;
            }

            Vector2 cellSize = mapSettings.tilemapGridCellSize;
            if (cellSize == Vector2.zero)
            {
                Debug.LogError($"타일맵 Grid 사이즈가 정해지지 않았습니다. {ConfigDefine.NameSDK}MapSettings 의 Tilemap Grid Cell Size 를 입력해주세요.");
                return;
            }

            grid.cellSize = new Vector3(cellSize.x, cellSize.y, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
        }

        private void DestroyGridObjectIfExists()
        {
            var obj = GameObject.FindWithTag(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            if (obj == null) return;

            // Grid가 선택되어 있거나, Grid 하위가 선택되어 있으면 Selection 정리
            ClearSelectionIfInSubtree(obj.transform);

            SafeDestroyImmediate(obj);
        }

        private void InitializeExporters()
        {
            var defaultMap = CompatObjectFind.FindFirst<DefaultMap>();
            _npcExporter.Initialize(_tableNpc, _tableAnimation, defaultMap, _characterManager);
            _monsterExporter.Initialize(_tableMonster, _tableAnimation, defaultMap, _characterManager);
            _warpExporter.Initialize(defaultMap);
            // _patrolExporter.Initialize(defaultMap);
        }

        private void RebuildPopupCaches()
        {
            int previousMapUid = _selectedMapUid;
            int previousNpcUid = _selectedNpcUid;
            int previousMonsterUid = _selectedMonsterUid;

            _mapOptions.Clear();
            _npcOptions.Clear();
            _monsterOptions.Clear();

            if (_tableMap != null)
            {
                var mapDict = _tableMap.GetDatas();
                foreach (var kv in mapDict)
                {
                    var info = kv.Value;
                    if (info == null || info.Uid <= 0) continue;
                    _mapOptions.Add(new SearchableDropdownUtility.Option<int>(
                        info.Uid.ToString(),
                        info.Name,
                        info.Uid));
                }
            }

            if (_tableNpc != null)
            {
                var npcDict = _tableNpc.GetDatas();
                foreach (var kv in npcDict)
                {
                    var info = kv.Value;
                    if (info == null || info.Uid <= 0) continue;
                    _npcOptions.Add(new SearchableDropdownUtility.Option<int>(
                        info.Uid.ToString(),
                        info.Name,
                        info.Uid));
                }
            }

            if (_tableMonster != null)
            {
                var monsterDict = _tableMonster.GetDatas();
                foreach (var kv in monsterDict)
                {
                    var info = kv.Value;
                    if (info == null || info.Uid <= 0) continue;
                    _monsterOptions.Add(new SearchableDropdownUtility.Option<int>(
                        info.Uid.ToString(),
                        info.Name,
                        info.Uid));
                }
            }

            _selectedMapUid = TryGetPreservedUid(_mapOptions, previousMapUid);
            _selectedNpcUid = TryGetPreservedUid(_npcOptions, previousNpcUid);
            _selectedMonsterUid = TryGetPreservedUid(_monsterOptions, previousMonsterUid);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!IsDataReady())
            {
                DrawDataNotReady();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos); // :contentReference[oaicite:3]{index=3}
            {
                DrawMapSection();
                GUILayout.Space(12);
                DrawNpcSection();
                GUILayout.Space(12);
                DrawNpcEditSection();
                GUILayout.Space(12);
                DrawMonsterSection();
                GUILayout.Space(12);
                DrawMonsterEditSection();
                GUILayout.Space(12);
                DrawWarpSection();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Title, EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton))
                {
                    AssetDatabaseLoaderManager.ClearCache();
                    LoadTables();
                    SetupServices();
                    InitializeExporters();
                    RebuildPopupCaches();
                    Repaint();
                }
            }
        }

        private bool IsDataReady()
        {
            return _tableMap != null
                   && _tableNpc != null
                   && _tableMonster != null
                   && _tableAnimation != null
                   && _mapOptions.Count > 0;
        }

        private void DrawDataNotReady()
        {
            EditorGUILayout.HelpBox(
                "테이블 또는 목록 데이터가 준비되지 않았습니다.\n" +
                "- Map/Npc/Monster/Animation 테이블 로드 여부\n" +
                "- 테이블의 Uid > 0 데이터 존재 여부\n" +
                "상단 '데이터 새로고침'으로 재시도 해주세요.",
                MessageType.Warning); // :contentReference[oaicite:4]{index=4}
        }

        private static int FindOptionIndexByUid(IReadOnlyList<SearchableDropdownUtility.Option<int>> options, int selectedUid)
        {
            if (options == null || options.Count == 0 || selectedUid <= 0)
            {
                return -1;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Data == selectedUid)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int TryGetPreservedUid(IReadOnlyList<SearchableDropdownUtility.Option<int>> options, int previousUid)
        {
            if (options == null || options.Count == 0)
            {
                return 0;
            }

            return FindOptionIndexByUid(options, previousUid) >= 0
                ? previousUid
                : options[0].Data;
        }

        private int GetSelectedMapUid()
        {
            return _selectedMapUid;
        }

        private void DrawMapSection()
        {
            _foldMap = EditorGUILayout.Foldout(_foldMap, "1) 맵 불러오기 / 저장", true);
            if (!_foldMap) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "맵을 선택한 뒤 '불러오기'로 배치 데이터를 로드합니다.\n" +
                    "NPC/몬스터/워프/패트롤 배치 후 '저장하기'로 Json을 갱신합니다.",
                    MessageType.Info);
                if (_suppressSceneOpsThisEnable)
                {
                    EditorGUILayout.HelpBox(
                        "스크립트 리컴파일 직후에는 자동 로드를 수행하지 않습니다. '불러오기' 버튼으로 진행해주세요.",
                        MessageType.Info);
                }

                int selectedMapIndex = FindOptionIndexByUid(_mapOptions, _selectedMapUid);
                SearchableDropdownUtility.DrawLabeledFieldAndShow(
                    "맵 선택",
                    _mapOptions,
                    selectedMapIndex,
                    (_, option) =>
                    {
                        _selectedMapUid = option.Data;

                        // 선택 변경 시 즉시 로드(기존 동작 유지)
                        if (_suppressSceneOpsThisEnable)
                        {
                            // 컴파일 직후에는 자동 로드 금지 (사용자 명시 액션으로만)
                            Repaint();
                            return;
                        }

                        TryLoadSelectedMapWithConfirm();
                    },
                    noneText: "(맵 선택)");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = GetSelectedMapUid() > 0;

                    if (GUILayout.Button("불러오기", GUILayout.Height(28)))
                    {
                        TryLoadSelectedMapWithConfirm(force: true);
                    }

                    if (GUILayout.Button("저장하기", GUILayout.Height(28)))
                    {
                        ExportDataToJsonWithConfirm();
                    }

                    GUI.enabled = true;
                }
            }
        }

        /// <summary>
        /// 캐릭터 배치에 사용할 맵 표시 정책 선택 필드를 그립니다.
        /// </summary>
        /// <param name="label">필드에 표시할 라벨입니다.</param>
        /// <param name="currentValue">현재 선택된 정책입니다.</param>
        /// <returns>사용자가 선택한 맵 표시 정책입니다.</returns>
        private static MapCharacterVisibilityPolicy DrawMapVisibilityPolicyField(
            string label,
            MapCharacterVisibilityPolicy currentValue)
        {
            GUIContent content = new GUIContent(
                label,
                "DefaultCulling은 카메라 컬링을 따르고, KeepVisible/KeepHidden은 명시 상태를 우선합니다.");
            return (MapCharacterVisibilityPolicy)EditorGUILayout.EnumPopup(content, currentValue);
        }

        /// <summary>
        /// NPC 배치 섹션을 그립니다.
        /// 배치 시점에 기본 보임 값과 맵 표시 정책을 함께 저장할 수 있도록 UI를 제공합니다.
        /// </summary>
        private void DrawNpcSection()
        {
            _foldNpc = EditorGUILayout.Foldout(_foldNpc, "2) NPC 추가", true);
            if (!_foldNpc) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int selectedNpcIndex = FindOptionIndexByUid(_npcOptions, _selectedNpcUid);
                SearchableDropdownUtility.DrawLabeledFieldAndShow(
                    "NPC 선택",
                    _npcOptions,
                    selectedNpcIndex,
                    (_, option) => _selectedNpcUid = option.Data,
                    noneText: "(NPC 선택)");

                _npcSpawnMapVisibilityPolicy = DrawMapVisibilityPolicyField(
                    "맵 표시 정책",
                    _npcSpawnMapVisibilityPolicy);

                bool usesDefaultVisible =
                    _npcSpawnMapVisibilityPolicy == MapCharacterVisibilityPolicy.DefaultCulling;
                using (new EditorGUI.DisabledScope(!usesDefaultVisible))
                {
                    _npcSpawnDefaultVisible = HelperEditorUI.ToggleLeft(
                        "스폰 후 기본 보임",
                        _npcSpawnDefaultVisible,
                        "DefaultCulling 정책에서 런타임 스폰 직후 기본 표시 여부를 결정합니다.");
                }

                if (!usesDefaultVisible)
                {
                    EditorGUILayout.HelpBox(
                        "KeepVisible/KeepHidden 정책에서는 명시 표시 정책이 DefaultVisible보다 우선합니다.",
                        MessageType.Info);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = GetSelectedMapUid() > 0;

                    if (GUILayout.Button("NPC 추가", GUILayout.Height(26)))
                    {
                        _npcExporter.AddNpcToMap(
                            _selectedNpcUid,
                            _npcSpawnDefaultVisible,
                            _npcSpawnMapVisibilityPolicy);
                    }

                    GUI.enabled = true;
                }
            }
        }

        /// <summary>
        /// 현재 맵에 배치된 NPC의 DefaultVisible/Flip/MapVisibilityPolicy를 편집하는 섹션을 그립니다.
        /// Hierarchy에서 선택한 NPC를 기준으로 편집하며, 필요 시 동일 UID 대상 일괄 적용을 지원합니다.
        /// </summary>
        private void DrawNpcEditSection()
        {
            _foldNpcEdit = EditorGUILayout.Foldout(_foldNpcEdit, "3) 배치된 NPC 편집", true);
            if (!_foldNpcEdit) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_defaultMap == null || GetSelectedMapUid() <= 0)
                {
                    EditorGUILayout.HelpBox("맵을 먼저 불러온 뒤 배치된 NPC를 선택해주세요.", MessageType.Info);
                    return;
                }

                if (!TryGetSelectedNpcInCurrentMap(out Npc selectedNpc))
                {
                    _editNpcBoundInstanceId = 0;
                    EditorGUILayout.HelpBox("Hierarchy에서 현재 맵 하위의 NPC 오브젝트를 선택해주세요.", MessageType.Info);
                    return;
                }

                BindNpcEditDraftIfNeeded(selectedNpc);
                DrawSelectedNpcInfo(selectedNpc);

                _editNpcMapVisibilityPolicy = DrawMapVisibilityPolicyField(
                    "맵 표시 정책",
                    _editNpcMapVisibilityPolicy);

                bool usesDefaultVisible =
                    _editNpcMapVisibilityPolicy == MapCharacterVisibilityPolicy.DefaultCulling;
                using (new EditorGUI.DisabledScope(!usesDefaultVisible))
                {
                    _editNpcDefaultVisible = HelperEditorUI.ToggleLeft(
                        "기본 보임(DefaultVisible)",
                        _editNpcDefaultVisible,
                        "DefaultCulling 정책에서 저장 후 런타임 스폰 시 기본 표시 여부를 결정합니다.");
                }

                if (!usesDefaultVisible)
                {
                    EditorGUILayout.HelpBox(
                        "KeepVisible/KeepHidden 정책에서는 명시 표시 정책이 DefaultVisible보다 우선합니다.",
                        MessageType.Info);
                }

                _editNpcFlip = HelperEditorUI.ToggleLeft(
                    "좌우 반전(Flip)",
                    _editNpcFlip,
                    "NPC.SetFlip 경로로 적용하며 CharacterRegenData.IsFlip 값도 동기화합니다.");

                _editNpcApplyToSameUid = HelperEditorUI.ToggleLeft(
                    "동일 UID 일괄 적용",
                    _editNpcApplyToSameUid,
                    "체크하면 현재 맵에서 같은 UID를 가진 모든 NPC에 함께 적용합니다.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("정책 적용", GUILayout.Height(26)))
                    {
                        ApplyNpcEditPolicy(selectedNpc);
                    }

                    if (GUILayout.Button("값 다시읽기", GUILayout.Height(26)))
                    {
                        ForceBindNpcEditDraft(selectedNpc);
                    }
                }
            }
        }

        /// <summary>
        /// 선택된 NPC의 편집 대상 요약 정보를 표시합니다.
        /// </summary>
        /// <param name="selectedNpc">현재 선택된 NPC</param>
        private static void DrawSelectedNpcInfo(Npc selectedNpc)
        {
            if (!selectedNpc)
            {
                return;
            }

            Vector3 position = selectedNpc.transform.position;
            EditorGUILayout.LabelField("선택 NPC", $"{selectedNpc.name} (Uid: {selectedNpc.uid})");
            EditorGUILayout.LabelField("위치", $"({position.x:F2}, {position.y:F2}, {position.z:F2})");
        }

        /// <summary>
        /// 현재 Selection에서 편집 가능한 NPC를 찾습니다.
        /// 선택된 오브젝트가 NPC의 자식인 경우 부모 NPC를 함께 탐색합니다.
        /// </summary>
        /// <param name="selectedNpc">현재 맵 하위에 존재하는 선택 NPC</param>
        /// <returns>편집 가능한 NPC를 찾았으면 true</returns>
        private bool TryGetSelectedNpcInCurrentMap(out Npc selectedNpc)
        {
            selectedNpc = null;
            if (_defaultMap == null)
            {
                return false;
            }

            GameObject selectedObject = Selection.activeGameObject;
            if (!selectedObject)
            {
                return false;
            }

            Npc npc = selectedObject.GetComponent<Npc>();
            if (!npc)
            {
                npc = selectedObject.GetComponentInParent<Npc>();
            }

            if (!npc)
            {
                return false;
            }

            if (!npc.transform.IsChildOf(_defaultMap.transform))
            {
                return false;
            }

            selectedNpc = npc;
            return true;
        }

        /// <summary>
        /// 선택 대상이 바뀌었을 때만 편집 드래프트를 새 대상의 배치 정책 값으로 동기화합니다.
        /// 사용자가 편집 중인 값을 프레임마다 덮어쓰지 않도록 InstanceID 기반으로 바인딩합니다.
        /// </summary>
        /// <param name="selectedNpc">현재 선택된 NPC</param>
        private void BindNpcEditDraftIfNeeded(Npc selectedNpc)
        {
            if (!selectedNpc)
            {
                _editNpcBoundInstanceId = 0;
                return;
            }

            int instanceId = selectedNpc.GetInstanceID();
            if (_editNpcBoundInstanceId == instanceId)
            {
                return;
            }

            ForceBindNpcEditDraft(selectedNpc);
        }

        /// <summary>
        /// 선택된 NPC의 현재 정책값을 편집 드래프트로 강제 동기화합니다.
        /// </summary>
        /// <param name="selectedNpc">동기화할 NPC</param>
        private void ForceBindNpcEditDraft(Npc selectedNpc)
        {
            if (!selectedNpc)
            {
                return;
            }

            int mapUid = GetSelectedMapUid();
            _editNpcDefaultVisible = NpcPlacementEditorUtility.GetDefaultVisible(selectedNpc, mapUid);
            _editNpcFlip = NpcPlacementEditorUtility.GetFlip(selectedNpc, mapUid);
            _editNpcMapVisibilityPolicy =
                NpcPlacementEditorUtility.GetMapVisibilityPolicy(selectedNpc, mapUid);
            _editNpcBoundInstanceId = selectedNpc.GetInstanceID();
        }

        /// <summary>
        /// 편집 드래프트 값을 선택 NPC(또는 동일 UID 집합)에 적용합니다.
        /// Undo/Dirty/Prefab Override 기록을 함께 처리하여 에디터 편집 이력을 보존합니다.
        /// </summary>
        /// <param name="selectedNpc">기준이 되는 선택 NPC</param>
        private void ApplyNpcEditPolicy(Npc selectedNpc)
        {
            if (!selectedNpc)
            {
                return;
            }

            List<Npc> targets = CollectNpcEditTargets(selectedNpc, _editNpcApplyToSameUid);
            if (targets.Count <= 0)
            {
                Debug.LogWarning("적용할 NPC를 찾지 못했습니다.");
                return;
            }

            int mapUid = GetSelectedMapUid();
            int appliedCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Npc targetNpc = targets[i];
                if (!targetNpc)
                {
                    continue;
                }

                Undo.RecordObject(targetNpc, "Edit NPC Placement Policy");
                Undo.RecordObject(targetNpc.transform, "Edit NPC Placement Policy");

                NpcPlacementEditorUtility.ApplyPlacementPolicy(
                    targetNpc,
                    mapUid,
                    _editNpcDefaultVisible,
                    _editNpcFlip,
                    _editNpcMapVisibilityPolicy);

                PrefabUtility.RecordPrefabInstancePropertyModifications(targetNpc);
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetNpc.transform);
                EditorUtility.SetDirty(targetNpc);
                EditorUtility.SetDirty(targetNpc.transform);
                appliedCount++;
            }

            if (appliedCount > 0)
            {
                Debug.Log($"NPC 정책 적용 완료: {appliedCount}개 / Uid:{selectedNpc.uid}");
            }
        }

        /// <summary>
        /// 정책 적용 대상 NPC 목록을 구성합니다.
        /// 일괄 옵션이 켜지면 현재 맵 하위에서 동일 UID의 NPC를 모두 수집합니다.
        /// </summary>
        /// <param name="selectedNpc">기준 NPC</param>
        /// <param name="applyToSameUid">동일 UID 일괄 적용 여부</param>
        /// <returns>정책 적용 대상 목록</returns>
        private List<Npc> CollectNpcEditTargets(Npc selectedNpc, bool applyToSameUid)
        {
            List<Npc> targets = new List<Npc>();
            if (!selectedNpc)
            {
                return targets;
            }

            if (!applyToSameUid || _defaultMap == null)
            {
                targets.Add(selectedNpc);
                return targets;
            }

            Npc[] mapNpcs = _defaultMap.GetComponentsInChildren<Npc>(true);
            for (int i = 0; i < mapNpcs.Length; i++)
            {
                Npc mapNpc = mapNpcs[i];
                if (!mapNpc) continue;
                if (mapNpc.uid != selectedNpc.uid) continue;
                targets.Add(mapNpc);
            }

            return targets;
        }

        /// <summary>
        /// 몬스터 배치 섹션을 그리고 신규 몬스터의 맵 표시 정책을 설정합니다.
        /// </summary>
        private void DrawMonsterSection()
        {
            _foldMonster = EditorGUILayout.Foldout(_foldMonster, "4) 몬스터 추가", true);
            if (!_foldMonster) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int selectedMonsterIndex = FindOptionIndexByUid(_monsterOptions, _selectedMonsterUid);
                SearchableDropdownUtility.DrawLabeledFieldAndShow(
                    "몬스터 선택",
                    _monsterOptions,
                    selectedMonsterIndex,
                    (_, option) => _selectedMonsterUid = option.Data,
                    noneText: "(몬스터 선택)");

                _monsterSpawnMapVisibilityPolicy = DrawMapVisibilityPolicyField(
                    "맵 표시 정책",
                    _monsterSpawnMapVisibilityPolicy);

                _usePatrolMonster = HelperEditorUI.ToggleLeft(
                    "패트롤 영역 생성",
                    _usePatrolMonster,
                    "몬스터 추가와 함께 패트롤 영역 오브젝트를 생성합니다.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = GetSelectedMapUid() > 0;

                    if (GUILayout.Button("몬스터 추가", GUILayout.Height(26)))
                    {
                        _monsterExporter.AddMonsterToMap(
                            _selectedMonsterUid,
                            _usePatrolMonster,
                            _monsterSpawnMapVisibilityPolicy);
                    }

                    GUI.enabled = true;
                }
            }
        }

        /// <summary>
        /// 현재 맵에 배치된 몬스터의 맵 표시 정책을 편집하는 섹션을 그립니다.
        /// Hierarchy에서 선택한 몬스터를 기준으로 동일 UID 일괄 적용을 지원합니다.
        /// </summary>
        private void DrawMonsterEditSection()
        {
            _foldMonsterEdit = EditorGUILayout.Foldout(_foldMonsterEdit, "5) 배치된 몬스터 편집", true);
            if (!_foldMonsterEdit) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_defaultMap == null || GetSelectedMapUid() <= 0)
                {
                    EditorGUILayout.HelpBox("맵을 먼저 불러온 뒤 배치된 몬스터를 선택해주세요.", MessageType.Info);
                    return;
                }

                if (!TryGetSelectedMonsterInCurrentMap(out Monster selectedMonster))
                {
                    _editMonsterBoundInstanceId = 0;
                    EditorGUILayout.HelpBox(
                        "Hierarchy에서 현재 맵 하위의 몬스터 오브젝트를 선택해주세요.",
                        MessageType.Info);
                    return;
                }

                BindMonsterEditDraftIfNeeded(selectedMonster);
                DrawSelectedMonsterInfo(selectedMonster);

                _editMonsterMapVisibilityPolicy = DrawMapVisibilityPolicyField(
                    "맵 표시 정책",
                    _editMonsterMapVisibilityPolicy);

                _editMonsterApplyToSameUid = HelperEditorUI.ToggleLeft(
                    "동일 UID 일괄 적용",
                    _editMonsterApplyToSameUid,
                    "체크하면 현재 맵에서 같은 UID를 가진 모든 몬스터에 함께 적용합니다.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("정책 적용", GUILayout.Height(26)))
                    {
                        ApplyMonsterEditPolicy(selectedMonster);
                    }

                    if (GUILayout.Button("값 다시읽기", GUILayout.Height(26)))
                    {
                        ForceBindMonsterEditDraft(selectedMonster);
                    }
                }
            }
        }

        /// <summary>
        /// 선택된 몬스터의 편집 대상 요약 정보를 표시합니다.
        /// </summary>
        /// <param name="selectedMonster">현재 선택된 몬스터입니다.</param>
        private static void DrawSelectedMonsterInfo(Monster selectedMonster)
        {
            if (!selectedMonster)
            {
                return;
            }

            Vector3 position = selectedMonster.transform.position;
            EditorGUILayout.LabelField(
                "선택 몬스터",
                $"{selectedMonster.name} (Uid: {selectedMonster.uid})");
            EditorGUILayout.LabelField(
                "위치",
                $"({position.x:F2}, {position.y:F2}, {position.z:F2})");
        }

        /// <summary>
        /// 현재 Selection에서 편집 가능한 몬스터를 찾습니다.
        /// 선택된 오브젝트가 몬스터의 자식인 경우 부모 몬스터를 함께 탐색합니다.
        /// </summary>
        /// <param name="selectedMonster">현재 맵 하위에 존재하는 선택 몬스터입니다.</param>
        /// <returns>편집 가능한 몬스터를 찾았으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryGetSelectedMonsterInCurrentMap(out Monster selectedMonster)
        {
            selectedMonster = null;
            if (_defaultMap == null)
            {
                return false;
            }

            GameObject selectedObject = Selection.activeGameObject;
            if (!selectedObject)
            {
                return false;
            }

            Monster monster = selectedObject.GetComponent<Monster>();
            if (!monster)
            {
                monster = selectedObject.GetComponentInParent<Monster>();
            }

            if (!monster || !monster.transform.IsChildOf(_defaultMap.transform))
            {
                return false;
            }

            selectedMonster = monster;
            return true;
        }

        /// <summary>
        /// 선택 대상이 바뀌었을 때만 몬스터 편집 드래프트를 현재 값으로 동기화합니다.
        /// </summary>
        /// <param name="selectedMonster">현재 선택된 몬스터입니다.</param>
        private void BindMonsterEditDraftIfNeeded(Monster selectedMonster)
        {
            if (!selectedMonster)
            {
                _editMonsterBoundInstanceId = 0;
                return;
            }

            int instanceId = selectedMonster.GetInstanceID();
            if (_editMonsterBoundInstanceId == instanceId)
            {
                return;
            }

            ForceBindMonsterEditDraft(selectedMonster);
        }

        /// <summary>
        /// 선택된 몬스터의 현재 맵 표시 정책을 편집 드래프트로 강제 동기화합니다.
        /// </summary>
        /// <param name="selectedMonster">동기화할 몬스터입니다.</param>
        private void ForceBindMonsterEditDraft(Monster selectedMonster)
        {
            if (!selectedMonster)
            {
                return;
            }

            _editMonsterMapVisibilityPolicy =
                MonsterPlacementEditorUtility.GetMapVisibilityPolicy(
                    selectedMonster,
                    GetSelectedMapUid());
            _editMonsterBoundInstanceId = selectedMonster.GetInstanceID();
        }

        /// <summary>
        /// 편집 드래프트 값을 선택 몬스터 또는 같은 UID의 몬스터 집합에 적용합니다.
        /// </summary>
        /// <param name="selectedMonster">기준이 되는 선택 몬스터입니다.</param>
        private void ApplyMonsterEditPolicy(Monster selectedMonster)
        {
            if (!selectedMonster)
            {
                return;
            }

            List<Monster> targets = CollectMonsterEditTargets(
                selectedMonster,
                _editMonsterApplyToSameUid);
            if (targets.Count <= 0)
            {
                Debug.LogWarning("적용할 몬스터를 찾지 못했습니다.");
                return;
            }

            int mapUid = GetSelectedMapUid();
            int appliedCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Monster targetMonster = targets[i];
                if (!targetMonster)
                {
                    continue;
                }

                Undo.RecordObject(targetMonster, "Edit Monster Map Visibility Policy");
                MonsterPlacementEditorUtility.ApplyMapVisibilityPolicy(
                    targetMonster,
                    mapUid,
                    _editMonsterMapVisibilityPolicy);

                PrefabUtility.RecordPrefabInstancePropertyModifications(targetMonster);
                EditorUtility.SetDirty(targetMonster);
                appliedCount++;
            }

            if (appliedCount > 0)
            {
                Debug.Log($"몬스터 표시 정책 적용 완료: {appliedCount}개 / Uid:{selectedMonster.uid}");
            }
        }

        /// <summary>
        /// 몬스터 표시 정책 적용 대상 목록을 구성합니다.
        /// </summary>
        /// <param name="selectedMonster">기준 몬스터입니다.</param>
        /// <param name="applyToSameUid">동일 UID 일괄 적용 여부입니다.</param>
        /// <returns>정책을 적용할 몬스터 목록입니다.</returns>
        private List<Monster> CollectMonsterEditTargets(
            Monster selectedMonster,
            bool applyToSameUid)
        {
            List<Monster> targets = new List<Monster>();
            if (!selectedMonster)
            {
                return targets;
            }

            if (!applyToSameUid || _defaultMap == null)
            {
                targets.Add(selectedMonster);
                return targets;
            }

            Monster[] mapMonsters = _defaultMap.GetComponentsInChildren<Monster>(true);
            for (int i = 0; i < mapMonsters.Length; i++)
            {
                Monster mapMonster = mapMonsters[i];
                if (!mapMonster || mapMonster.uid != selectedMonster.uid)
                {
                    continue;
                }

                targets.Add(mapMonster);
            }

            return targets;
        }

        /// <summary>
        /// 워프 배치 섹션을 그립니다.
        /// </summary>
        private void DrawWarpSection()
        {
            _foldWarp = EditorGUILayout.Foldout(_foldWarp, "6) 워프 추가", true);
            if (!_foldWarp) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = GetSelectedMapUid() > 0;

                    if (GUILayout.Button("워프 추가", GUILayout.Height(26)))
                    {
                        _warpExporter.AddWarpToMap();
                    }

                    GUI.enabled = true;
                }
            }
        }

        private bool TryLoadSelectedMapWithConfirm(bool force = false)
        {
            if (GetSelectedMapUid() <= 0) return false;

            if (!force)
            {
                bool ok = EditorUtility.DisplayDialog("불러오기",
                    "현재 불러온 내용이 초기화 됩니다.\n계속 진행할까요?",
                    "네", "아니요");
                if (!ok) return false;
            }
            else
            {
                bool ok = EditorUtility.DisplayDialog("불러오기",
                    "현재 불러온 내용이 초기화 됩니다.\n계속 진행할까요?",
                    "네", "아니요");
                if (!ok) return false;
            }

            return LoadJsonDataInternal();
        }

        private void ExportDataToJsonWithConfirm()
        {
            if (GetSelectedMapUid() <= 0) return;

            bool ok = EditorUtility.DisplayDialog("저장하기", "현재 선택된 맵에 저장하시겠습니까?", "네", "아니요");
            if (!ok) return;

            ExportDataToJsonInternal();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }

        private bool LoadJsonDataInternal()
        {
            int mapUid = GetSelectedMapUid();
            var mapData = _tableMap.GetDataByUid(mapUid);
            if (mapData == null || mapData.Uid <= 0)
            {
                Debug.LogError("맵 데이터가 없거나 유효하지 않습니다.");
                return false;
            }

            LoadTileData(mapData);

            _npcExporter.LoadNpcData(ConfigAddressableMap.GetAssetPathRegenNpc(mapData.FolderName));
            _monsterExporter.LoadMonsterData(ConfigAddressableMap.GetAssetPathRegenMonster(mapData.FolderName));
            _warpExporter.LoadWarpData(ConfigAddressableMap.GetAssetPathWarp(mapData.FolderName));
            // _patrolExporter.LoadJsonData(ConfigAddressableMap.GetAssetPathPatrol(mapData.FolderName));

            return true;
        }

        private void ExportDataToJsonInternal()
        {
            int mapUid = GetSelectedMapUid();

            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            if (!mapObject)
            {
                Debug.LogWarning("Scene에서 Map 태그 오브젝트를 찾을 수 없습니다.");
                return;
            }

            var mapInfo = _tableMap.GetDataByUid(mapUid);
            if (mapInfo == null || mapInfo.Uid <= 0)
            {
                Debug.LogError("맵 데이터가 없거나 유효하지 않습니다.");
                return;
            }

            string folderName = mapInfo.FolderName;
            string jsonFolderPath = ConfigAddressablePath.Maps.Folder(folderName);

            // monster, npc 의 label 업데이트 해주기
            string labelName = ConfigAddressableMap.GetLabel(folderName);
            RemoveCharacterMapLabel(labelName);

            _npcExporter.ExportNpcDataToJson(jsonFolderPath, ConfigAddressableMap.GetFileName(MapAssetType.RegenNpcJson), mapUid, mapInfo);
            _monsterExporter.ExportMonsterDataToJson(jsonFolderPath, ConfigAddressableMap.GetFileName(MapAssetType.RegenMonsterJson), mapUid, mapInfo);
            _warpExporter.ExportWarpDataToJson(jsonFolderPath, ConfigAddressableMap.GetFileName(MapAssetType.WarpJson), mapUid);
            // _patrolExporter.ExportPatrolDataToJson(jsonFolderPath, ConfigAddressableMap.GetFileName(MapAssetType.PatrolJson), mapUid);

            AssetDatabase.Refresh();
        }

        private void RemoveCharacterMapLabel(string labelName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다.");
                return;
            }

            // Monster labels
            var monsters = _tableMonster.GetDatas();
            foreach (var kv in monsters)
            {
                var info = kv.Value;
                if (info == null) continue;

                var anim = _tableAnimation.GetDataByUid(info.AnimationUid);
                if (anim == null) continue;

                string assetPath = ConfigAddressableMap.GetPathCharacter(anim, true);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                var entry = settings.FindAssetEntry(guid);
                // SetLabel: enable=false => remove (Addressables API) :contentReference[oaicite:6]{index=6}
                entry?.SetLabel(labelName, false, true);
            }

            // NPC labels
            var npcs = _tableNpc.GetDatas();
            foreach (var kv in npcs)
            {
                var info = kv.Value;
                if (info == null) continue;

                var anim = _tableAnimation.GetDataByUid(info.AnimationUid);
                if (anim == null) continue;

                string assetPath = ConfigAddressableMap.GetPathCharacter(anim, true);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                var entry = settings.FindAssetEntry(guid);
                entry?.SetLabel(labelName, false, true);
            }
        }

        private void LoadTileData(StruckTableMap mapData)
        {
            // 1) GridTileMap 확보
            if (!_gridTileMap)
            {
                _gridTileMap = GameObject.Find(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
                if (!_gridTileMap)
                {
                    Debug.LogError("GridTileMap 오브젝트가 없습니다. 다시 생성 또는 씬 상태를 확인해주세요.");
                    return;
                }
            }

            // 2) Selection 안전 처리 (Grid 하위에 선택된 것이 있으면 먼저 정리)
            ClearSelectionIfInSubtree(_gridTileMap.transform);

            // 3) 기존에 툴이 로드했던 맵(=Grid 하위)만 제거
            //    ※ 태그 기반 전체 삭제를 피합니다.
            for (int i = _gridTileMap.transform.childCount - 1; i >= 0; i--)
            {
                var child = _gridTileMap.transform.GetChild(i);
                if (child != null)
                {
                    SafeDestroyImmediate(child.gameObject);
                }
            }

            // 4) 프리팹 로드/생성
            string tilemapPath = ConfigAddressableMap.GetAssetPathTileMap(mapData.FolderName);
            GameObject prefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(tilemapPath);
            if (!prefab)
            {
                Debug.LogError($"맵 프리팹이 없습니다. mapUid:{mapData.Uid} / path:{tilemapPath}");
                return;
            }

            GameObject currentMap = Instantiate(prefab, _gridTileMap.transform);
            Selection.activeGameObject = currentMap;
            EditorGUIUtility.PingObject(currentMap);

            _defaultMap = currentMap.GetComponent<MapTileCommon>();
            if (_defaultMap == null)
            {
                Debug.LogError("MapTileCommon 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            _defaultMap.InitComponents();
            _defaultMap.InitTagSortingLayer();
            _defaultMap.Initialize(mapData);
            _editNpcBoundInstanceId = 0;

            _npcExporter.SetDefaultMap(_defaultMap);
            _monsterExporter.SetDefaultMap(_defaultMap);
            _warpExporter.SetDefaultMap(_defaultMap);
            // _patrolExporter.SetDefaultMap(_defaultMap);
        }

        private static bool IsSelectionInSubtree(Transform root)
        {
            if (root == null) return false;

            // Selection.gameObjects는 내부적으로 null을 필터링해주지 않는 케이스가 있어
            // 방어적으로 Object 배열로 확인합니다.
            var selected = Selection.objects;
            if (selected == null || selected.Length == 0) return false;

            for (int i = 0; i < selected.Length; i++)
            {
                var obj = selected[i];
                if (obj == null) continue; // 이미 파괴되어 null일 수 있음

                // 게임오브젝트/컴포넌트 둘 다 대응
                GameObject go = null;
                if (obj is GameObject g) go = g;
                else if (obj is Component c) go = c.gameObject;

                if (go == null) continue;

                if (go.transform == root || go.transform.IsChildOf(root))
                    return true;
            }

            return false;
        }

        private static void ClearSelectionIfInSubtree(Transform root)
        {
            if (!IsSelectionInSubtree(root)) return;

            // 인스펙터가 null Selection을 잡고 SerializedObject 만들기 전에 Selection 정리
            Selection.objects = Array.Empty<UnityEngine.Object>();

            // 안전하게 인스펙터/하이라키 리페인트
            EditorApplication.delayCall += () =>
            {
                EditorApplication.RepaintHierarchyWindow();
                // InspectorWindow는 내부 타입이라 직접 접근이 까다롭지만,
                // Selection 변경으로 대부분 갱신됩니다.
            };
        }

        private static void SafeDestroyImmediate(GameObject go)
        {
            if (!go) return;

            // 혹시 그 오브젝트 자체가 선택되어 있는 경우도 대비
            var selected = Selection.objects;
            if (selected != null)
            {
                for (int i = 0; i < selected.Length; i++)
                {
                    if (selected[i] == go)
                    {
                        Selection.objects = Array.Empty<UnityEngine.Object>();
                        break;
                    }
                }
            }

            DestroyImmediate(go);
        }
    }
}
