using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class MapEditor : DefaultEditorWindow
    {
        private List<CharacterRegenData> _npcList;
        private List<CharacterRegenData> _monsterDatas;
        private List<WarpData> _warpDatas;
        private static MapTileCommon _defaultMap;
        private static GameObject _gridTileMap;
        private static GameObject _player;

        private CharacterManager _characterManager;
        private static TableMap _tableMap;
        private static TableNpc _tableNpc;
        private static TableMonster _tableMonster;
        private static TableAnimation _tableAnimation;

        private const string Title = "Map 배치툴";

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
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;

        [MenuItem(ConfigEditor.NameToolMapExporter, false, (int)ConfigEditor.ToolOrdering.MapExporter)]
        public static void ShowWindow()
        {
            GetWindow<MapEditor>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _loadMapUid = 0;
            _previousIndexMap = 0;
            _selectedIndexNpc = 0;
            _selectedIndexMonster = 0;
            _selectedIndexMap = 0;
            _tableMap = TableLoaderManager.LoadMapTable();
            _tableNpc = TableLoaderManager.LoadNpcTable();
            _tableMonster = TableLoaderManager.LoadMonsterTable();
            _tableAnimation = TableLoaderManager.LoadSpineTable();

            _addressableLoaderPrefabCharacter = new AddressableLoaderPrefabCharacter();
            
            CreateGirdObject();
        }
        private void CreateGirdObject()
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

                GGemCoMapSettings mapSettings = AssetDatabaseLoaderManager.LoadAsset<GGemCoMapSettings>(ConfigAddressableSetting.MapSettings.Path);
                    
                Vector2 tilemapGridSize = mapSettings.tilemapGridCellSize;
                if (tilemapGridSize == Vector2.zero)
                {
                    GcLogger.LogError(
                        $"타일맵 Grid 사이즈가 정해지지 않았습니다. {ConfigDefine.NameSDK}MapSettings 에 Tilemap Grid Cell Size 를 입력해주세요.");
                    return;
                }
                grid.cellSize = new Vector3(tilemapGridSize.x, tilemapGridSize.y, 0);
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
            }

#if UNITY_6000_0_OR_NEWER
            var defaultMap = FindFirstObjectByType<DefaultMap>();
#else
            var defaultMap = FindObjectOfType<DefaultMap>();
#endif
            
            _characterManager = new CharacterManager();
            _characterManager.Initialize(_tableNpc, _tableMonster, _tableAnimation, _addressableLoaderPrefabCharacter);
            
            _npcExporter.Initialize(_tableNpc, _tableAnimation, defaultMap, _characterManager);
            _monsterExporter.Initialize(_tableMonster, _tableAnimation, defaultMap, _characterManager);
            _warpExporter.Initialize(defaultMap);
            LoadInfoDataNpc();
            LoadInfoDataMonster();
            LoadInfoDataMap();
        }

        private void OnDestroy()
        {
            GameObject obj = GameObject.FindWithTag(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            if (obj)
            {
                DestroyImmediate(obj);
            }
        }

        private void OnGUI()
        {
            if (_nameNpc == null) return;
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            GUILayout.Label("* 맵 배치 불러오기", EditorStyles.whiteLargeLabel);
            // 파일 경로 및 파일명 입력
            _selectedIndexMap = EditorGUILayout.Popup("맵 선택", _selectedIndexMap, _nameMap.ToArray());
            _loadMapUid = StruckTableMaps.GetValueOrDefault(_selectedIndexMap)?.Uid ?? 0;
            if (_previousIndexMap != _selectedIndexMap)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                if (LoadJsonData())
                {
                    _previousIndexMap = _selectedIndexMap;
                }
                else
                {
                    _selectedIndexMap = _previousIndexMap;
                }
            }
            
            GUILayout.BeginHorizontal();
            // 불러오기 버튼
            if (GUILayout.Button("불러오기")) LoadJsonData();
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
            string currentJsonFolderPath = ConfigAddressableMap.GetPathJson(_tableMap.GetDataByUid(_loadMapUid).FolderName);
            
            _npcExporter.ExportNpcDataToJson(currentJsonFolderPath, ConfigAddressableMap.FileNameRegenNpc, _loadMapUid);
            _monsterExporter.ExportMonsterDataToJson(currentJsonFolderPath, ConfigAddressableMap.FileNameRegenMonster, _loadMapUid);
            _warpExporter.ExportWarpDataToJson(currentJsonFolderPath, ConfigAddressableMap.FileNameWarp, _loadMapUid);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }

        private bool LoadJsonData()
        {
            bool result = EditorUtility.DisplayDialog("불러오기", "현재 불러온 내용이 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
            if (!result) return false;
            var mapData = _tableMap.GetDataByUid(_loadMapUid);
            
            LoadTileData();
            _npcExporter.LoadNpcData($"{ConfigAddressableMap.GetAssetPathRegenNpc(mapData.FolderName)}");
            _monsterExporter.LoadMonsterData($"{ConfigAddressableMap.GetAssetPathRegenMonster(mapData.FolderName)}");
            _warpExporter.LoadWarpData($"{ConfigAddressableMap.GetAssetPathWarp(mapData.FolderName)}");
            return true;
        }
        /// <summary>
        /// MapManager.cs:25
        /// </summary>
        private void LoadTileData()
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

            string tilemapPath = ConfigAddressableMap.GetAssetPathTileMap(mapData.FolderName);
            GameObject prefab = AssetDatabaseLoaderManager.LoadAsset<GameObject>(tilemapPath);
            if (!prefab)
            {
                GcLogger.LogError("맵 프리팹이 없습니다. mapUid : " + _loadMapUid + " / path : "+tilemapPath);
                return;
            }

            if (!_gridTileMap)
            {
                _gridTileMap = GameObject.Find(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            }
            GameObject currentMap = Instantiate(prefab, _gridTileMap.transform);
            _defaultMap = currentMap.GetComponent<MapTileCommon>();
            _defaultMap.InitComponents();
            _defaultMap.InitTagSortingLayer();
            _defaultMap.Initialize(mapData.Uid, mapData.Name, mapData.Type, mapData.Subtype);
            _npcExporter.SetDefaultMap(_defaultMap);
            _monsterExporter.SetDefaultMap(_defaultMap);
            _warpExporter.SetDefaultMap(_defaultMap);
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
            Dictionary<int, Dictionary<string, string>> monsterDictionary = _tableMonster.GetDatas();
             
            _nameMonster = new List<string>();
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = _tableMonster.GetDataByUid(outerPair.Key);
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
