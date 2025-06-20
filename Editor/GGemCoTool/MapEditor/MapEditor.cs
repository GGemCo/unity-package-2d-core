using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo.Editor
{
    public class MapExporter : DefaultEditorWindow
    {
        private List<CharacterRegenData> _npcList;
        private List<CharacterRegenData> _monsterDatas;
        private List<WarpData> _warpDatas;
        public MapTileCommon defaultMap;
        private static GameObject _gridTileMap;
        private static GameObject _player;

        public CharacterManager CharacterManager;
        private static TableMap _tableMap;
        private static TableNpc _tableNpc;
        public TableMonster TableMonster;
        public TableAnimation TableAnimation;

        private const string Title = "Map 배치툴";
        private readonly string _jsonFolderPath = Application.dataPath+"/Resources/Maps/";
        private const string ResourcesFolderPath = "Maps/";

        private const string NameTempTableLoaderManager = "TempTableLoaderManager";

        private const string JsonFileNameRegenNpc = MapConstants.FileNameRegenNpc + MapConstants.FileExt;
        private const string JsonFileNameRegenMonster = MapConstants.FileNameRegenMonster + MapConstants.FileExt;
        private const string JsonFileNameWarp = MapConstants.FileNameWarp + MapConstants.FileExt;

        private readonly NpcExporter _npcExporter = new NpcExporter();
        private readonly MonsterExporter _monsterExporter = new MonsterExporter();
        private readonly WarpExporter _warpExporter = new WarpExporter();
        
        // 이름 목록
        private static List<string> _nameNpc;
        private static List<string> _nameMonster;
        private static List<string> _nameMap;
        private static readonly Dictionary<int, StruckTableMap> StruckTableMaps = new Dictionary<int, StruckTableMap>();
        
        private int _loadMapUid;
        private int _previousIndexMap;
        private int _selectedIndexNpc;
        private int _selectedIndexMonster;
        private int _selectedIndexMap;
        private Vector2 _scrollPos = Vector2.zero;

        private bool _isLoadingCharacter;
        public AddressableMonsterLoader AddressableMonsterLoader;

        [MenuItem(ConfigEditor.NameToolMapExporter, false, (int)ConfigEditor.ToolOrdering.MapExporter)]
        public static void ShowWindow()
        {
            GetWindow<MapExporter>(Title);
        }

        private AddressableSettingsLoader _addressableSettingsLoader;
        protected override void OnEnable()
        {
            base.OnEnable();
            _isLoadingCharacter = false;
            _loadMapUid = 0;
            _previousIndexMap = 0;
            _selectedIndexNpc = 0;
            _selectedIndexMonster = 0;
            _selectedIndexMap = 0;
            
            _addressableSettingsLoader = new AddressableSettingsLoader();
            _ = _addressableSettingsLoader.InitializeAsync();
            _addressableSettingsLoader.OnLoadSettings += Initialize;
        }
        /// <summary>
        /// Addressable Settings 파일이 로드 되면 처리 하기   
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="playerSettings"></param>
        /// <param name="mapSettings"></param>
        /// <param name="saveSettings"></param>
        private void Initialize(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings)
        {
            // 타일맵을 추가할 grid
            _gridTileMap = GameObject.Find(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            if (!_gridTileMap)
            {
                _gridTileMap = new GameObject(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap))
                {
                    tag = ConfigTags.GetValue(ConfigTags.Keys.GridTileMap)
                };
                Grid grid = _gridTileMap.gameObject.AddComponent<Grid>();
                Vector2 tilemapGridSize = mapSettings.tilemapGridCellSize;
                if (tilemapGridSize == Vector2.zero)
                {
                    GcLogger.LogError(
                        "타일맵 Grid 사이즈가 정해지지 않았습니다. GGemCoMapSettings 에 Tilemap Grid Cell Size 를 입력해주세요.");
                    return;
                }
                grid.cellSize = new Vector3(tilemapGridSize.x, tilemapGridSize.y, 0);
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
            }

#if UNITY_6000_0_OR_NEWER
            DefaultMap defaultMap = FindFirstObjectByType<DefaultMap>();
#else
            DefaultMap defaultMap = FindObjectOfType<DefaultMap>();
#endif
            
            _ = LoadAsync(defaultMap);
        }

        private async Task LoadAsync(DefaultMap defaultMap)
        {
            try
            {
                var loadMapTask = TableLoaderManager.LoadMapTableAsync();
                var loadNpcTask = TableLoaderManager.LoadNpcTableAsync();
                var loadMonsterTask = TableLoaderManager.LoadMonsterTableAsync();
                var loadSpineTask = TableLoaderManager.LoadSpineTableAsync();
                await Task.WhenAll(loadMapTask, loadNpcTask, loadMonsterTask, loadSpineTask);

                _tableMap = loadMapTask.Result;
                _tableNpc = loadNpcTask.Result;
                TableMonster = loadMonsterTask.Result;
                TableAnimation = loadSpineTask.Result;
                
                CharacterManager = new CharacterManager();
                CharacterManager.Initialize(_tableNpc, TableMonster, TableAnimation);
            
                _npcExporter.Initialize(_tableNpc, TableAnimation, defaultMap, CharacterManager);
                _monsterExporter.Initialize(this);
                _warpExporter.Initialize(defaultMap);
            
                LoadInfoDataNpc();
                LoadInfoDataMonster();
                LoadInfoDataMap();
                
                AddressableMonsterLoader = new AddressableMonsterLoader();
                AddressableMonsterLoader.LoadAllMonster();
                
                isLoading = false;
                Repaint();
            }
            catch (System.Exception ex)
            {
                ShowLoadTableException(Title, ex);
            }
        }
        private void OnDestroy()
        {
            GameObject obj = GameObject.Find(NameTempTableLoaderManager);
            if (obj)
            {
                DestroyImmediate(obj);
            }

            obj = GameObject.FindWithTag(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            if (obj)
            {
                DestroyImmediate(obj);
            }
            
            _addressableSettingsLoader.OnLoadSettings -= Initialize;
        }

        private void OnGUI()
        {
            if (isLoading)
            {
                EditorGUILayout.LabelField("테이블 로딩 중...");
                return;
            }
            
            if (_nameNpc == null) return;
            
            if (_isLoadingCharacter)
            {
                EditorGUILayout.LabelField("json 로딩 중...");
                return;
            }
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            GUILayout.Label("* 맵 배치 불러오기", EditorStyles.whiteLargeLabel);
            // 파일 경로 및 파일명 입력
            _selectedIndexMap = EditorGUILayout.Popup("맵 선택", _selectedIndexMap, _nameMap.ToArray());
            _loadMapUid = StruckTableMaps.GetValueOrDefault(_selectedIndexMap)?.Uid ?? 0;
            if (_previousIndexMap != _selectedIndexMap)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                // if (_isLoadingCharacter)
                // {
                //     _previousIndexMap = _selectedIndexMap;
                // }
                // else
                // {
                //     _selectedIndexMap = _previousIndexMap;
                // }
                if (!_isLoadingCharacter)
                {
                    bool result = EditorUtility.DisplayDialog("불러오기", "현재 불러온 내용이 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
                    if (!result)
                    {
                        _selectedIndexMap = _previousIndexMap;
                    }
                    else 
                    {
                        _ = LoadJsonData();
                    }
                }
            }
            
            GUILayout.BeginHorizontal();
            // 불러오기 버튼
            if (GUILayout.Button("불러오기"))
            {
                _ = LoadJsonData();
            }
            if (GUILayout.Button("저장하기")) ExportDataToJson();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            // NPC 추가 섹션
            GUILayout.Label("* NPC 추가", EditorStyles.whiteLargeLabel);
            // NPC 드롭다운
            _selectedIndexNpc = EditorGUILayout.Popup("NPC 선택", _selectedIndexNpc, _nameNpc.ToArray());
            // NPC 추가 버튼
            if (GUILayout.Button("NPC 추가"))
            {
                _npcExporter.AddNpcToMap(_selectedIndexNpc);
            }
            
            GUILayout.Space(20);
            // 몬스터 추가 섹션
            GUILayout.Label("* 몬스터 추가", EditorStyles.whiteLargeLabel);
            // 몬스터 드롭다운
            _selectedIndexMonster = EditorGUILayout.Popup("몬스터 선택", _selectedIndexMonster, _nameMonster.ToArray());
            // 몬스터 추가 버튼
            if (GUILayout.Button("몬스터 추가"))
            {
                _monsterExporter.AddMonsterToMap(_selectedIndexMonster);
            }
            
            GUILayout.Space(20);
            // 워프 추가 섹션
            GUILayout.Label("* 워프 추가", EditorStyles.whiteLargeLabel);
            // 워프 추가 버튼
            if (GUILayout.Button("워프 추가"))
            {
                _warpExporter.AddWarpToMap();
            }
            
            EditorGUILayout.EndScrollView();
        }
        private void ExportDataToJson()
        {
            bool result = EditorUtility.DisplayDialog("저장하기", "현재 선택된 맵에 저장하시겠습니까?", "네", "아니요");
            if (!result) return;
            // 태그가 'Map'인 오브젝트를 찾습니다.
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
        
            if (!mapObject)
            {
                Debug.LogWarning("No GameObject with the tag 'Map' found in the scene.");
                return;
            }
            string currentJsonFolderPath = _jsonFolderPath + _tableMap.GetDataByUid(_loadMapUid).FolderName + "/";
            _npcExporter.ExportNpcDataToJson(currentJsonFolderPath, JsonFileNameRegenNpc, _loadMapUid);
            _monsterExporter.ExportMonsterDataToJson(currentJsonFolderPath, JsonFileNameRegenMonster, _loadMapUid);
            _warpExporter.ExportWarpDataToJson(currentJsonFolderPath, JsonFileNameWarp, _loadMapUid);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }

        private async Task LoadJsonData()
        {
            // bool result = EditorUtility.DisplayDialog("불러오기", "현재 불러온 내용이 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
            // if (!result)
            // {
            //     _selectedIndexMap = _previousIndexMap;
            //     return;
            // }
            var mapData = _tableMap.GetDataByUid(_loadMapUid);
            
            _isLoadingCharacter = true;
            
            await LoadTileData();
            // _npcExporter.LoadNpcData(ResourcesFolderPath + mapData.FolderName + "/" + MapConstants.FileNameRegenNpc);
            await _monsterExporter.LoadMonsterData($"{ConfigAddressables.LabelMap}_{mapData.FolderName}_{MapConstants.FileNameRegenMonster}");
            // _warpExporter.LoadWarpData(ResourcesFolderPath + mapData.FolderName + "/" + MapConstants.FileNameWarp);
            _isLoadingCharacter = false;
            _previousIndexMap = _selectedIndexMap;
        }
        /// <summary>
        /// MapManager.cs:25
        /// </summary>
        private async Task LoadTileData()
        {
            var mapData = _tableMap.GetDataByUid(_loadMapUid);
            if (mapData.Uid <= 0)
            {
                GcLogger.LogError("맵 데이터가 없거나 리젠 파일명이 없습니다.");
                return;
            }
            
            GameObject[] tilemap = GameObject.FindGameObjectsWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
            foreach (var map in tilemap)
            {
                DestroyImmediate(map);
            }

            // Addressables 키 생성 (예: "GGemCo_Map_Map1_Tilemap" 등으로 라벨링 했다면 해당 이름으로)
            string addressKey = $"{ConfigAddressables.LabelMap}_{mapData.FolderName}_{MapConstants.FileNameTilemap}";
    
            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(addressKey);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded || !handle.Result)
                {
                    GcLogger.LogError("맵 프리팹 로드 실패. addressKey: " + addressKey);
                    return;
                }

                GameObject prefab = handle.Result;

                if (!_gridTileMap)
                {
                    _gridTileMap = GameObject.Find(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
                }

                GameObject currentMap = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (_gridTileMap && currentMap)
                {
                    currentMap.transform.SetParent(_gridTileMap.transform, false);
                }

                defaultMap = currentMap?.GetComponent<MapTileCommon>();
                if (defaultMap)
                {
                    defaultMap.InitComponents();
                    defaultMap.InitTagSortingLayer();
                    defaultMap.Initialize(mapData.Uid, mapData.Name, mapData.Type, mapData.Subtype);

                    _npcExporter.SetDefaultMap(defaultMap);
                    _monsterExporter.SetDefaultMap(defaultMap);
                    _warpExporter.SetDefaultMap(defaultMap);
                }
                else
                {
                    Debug.LogError("MapTileCommon 컴포넌트를 찾을 수 없습니다.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"맵 프리팹 로드 중 예외 발생 {e.Message}");
            }
        }
        /// <summary>
        /// npc 정보 불러오기
        /// </summary>
        private void LoadInfoDataNpc()
        {
            Dictionary<int, Dictionary<string, string>> npcDictionary = _tableNpc.GetDatas();
             
            _nameNpc = new List<string>();
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in npcDictionary)
            {
                var info = _tableNpc.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _nameNpc.Add($"{info.Uid} - {info.Name}");
            }
        }
        /// <summary>
        ///  몬스터 정보 불러오기
        /// </summary>
        private void LoadInfoDataMonster()
        {
            Dictionary<int, Dictionary<string, string>> monsterDictionary = TableMonster.GetDatas();
             
            _nameMonster = new List<string>();
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = TableMonster.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _nameMonster.Add($"{info.Uid} - {info.Name}");
            }
        }

        private void LoadInfoDataMap()
        {
            Dictionary<int, Dictionary<string, string>> monsterDictionary = _tableMap.GetDatas();
             
            _nameMap = new List<string>();
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = _tableMap.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _nameMap.Add($"{info.Uid} - {info.Name}");
                StruckTableMaps.TryAdd(index++, info);
            }
        }
    }
}
