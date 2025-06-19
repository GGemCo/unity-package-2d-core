using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class MapExporter : EditorWindow
    {
        private List<CharacterRegenData> npcList;
        private List<CharacterRegenData> monsterDatas;
        private List<WarpData> warpDatas;
        private static MapTileCommon _defaultMap;
        private static GameObject _gridTileMap;
        private static GameObject _player;

        private CharacterManager characterManager;
        private TableLoaderManager tableLoaderManager;
        private static TableMap _tableMap;
        private static TableNpc _tableNpc;
        private static TableMonster _tableMonster;
        private static TableAnimation _tableAnimation;

        private const string Title = "Map 배치툴";
        private readonly string jsonFolderPath = Application.dataPath+"/Resources/Maps/";
        private const string RESOURCES_FOLDER_PATH = "Maps/";

        private const string NameTempTableLoaderManager = "TempTableLoaderManager";

        private readonly string jsonFileNameRegenNpc = MapConstants.FileNameRegenNpc+MapConstants.FileExt;
        private readonly string jsonFileNameRegenMonster = MapConstants.FileNameRegenMonster+MapConstants.FileExt;
        private readonly string jsonFileNameWarp = MapConstants.FileNameWarp+MapConstants.FileExt;
        
        private readonly NpcExporter npcExporter = new NpcExporter();
        private readonly MonsterExporter monsterExporter = new MonsterExporter();
        private readonly WarpExporter warpExporter = new WarpExporter();
        
        // 이름 목록
        private static List<string> _nameNpc;
        private static List<string> _nameMonster;
        private static List<string> _nameMap;
        private static Dictionary<int, StruckTableMap> struckTableMaps = new Dictionary<int, StruckTableMap>();
        
        private int loadMapUid = 0;
        private int previousIndexMap = 0;
        private int selectedIndexNpc;
        private int selectedIndexMonster;
        private int selectedIndexMap;
        private Vector2 scrollPos = Vector2.zero;

        [MenuItem(ConfigEditor.NameToolMapExporter, false, (int)ConfigEditor.ToolOrdering.MapExporter)]
        public static void ShowWindow()
        {
            GetWindow<MapExporter>(Title);
        }

        private AddressableSettingsLoader addressableSettingsLoader;
        private void OnEnable()
        {
            selectedIndexNpc = 0;
            selectedIndexMonster = 0;
            selectedIndexMap = 0;
            tableLoaderManager = new TableLoaderManager();
            _tableMap = tableLoaderManager.LoadMapTable();
            _tableNpc = tableLoaderManager.LoadNpcTable();
            _tableMonster = tableLoaderManager.LoadMonsterTable();
            _tableAnimation = tableLoaderManager.LoadSpineTable();

            addressableSettingsLoader = new AddressableSettingsLoader();
            _ = addressableSettingsLoader.InitializeAsync();
            addressableSettingsLoader.OnLoadSettings += Initialize;
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
            if (_gridTileMap == null)
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
            var defaultMap = FindFirstObjectByType<DefaultMap>();
#else
            var defaultMap = FindObjectOfType<DefaultMap>();
#endif
            
            characterManager = new CharacterManager();
            characterManager.Initialize(_tableNpc, _tableMonster, _tableAnimation);
            
            npcExporter.Initialize(_tableNpc, _tableAnimation, defaultMap, characterManager);
            monsterExporter.Initialize(_tableMonster, _tableAnimation, defaultMap, characterManager);
            warpExporter.Initialize(defaultMap);
            LoadInfoDataNpc();
            LoadInfoDataMonster();
            LoadInfoDataMap();
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
            
            addressableSettingsLoader.OnLoadSettings -= Initialize;
        }

        private void OnGUI()
        {
            if (_nameNpc == null) return;
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("* 맵 배치 불러오기", EditorStyles.whiteLargeLabel);
            // 파일 경로 및 파일명 입력
            selectedIndexMap = EditorGUILayout.Popup("맵 선택", selectedIndexMap, _nameMap.ToArray());
            loadMapUid = struckTableMaps.GetValueOrDefault(selectedIndexMap)?.Uid ?? 0;
            if (previousIndexMap != selectedIndexMap)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                if (LoadJsonData())
                {
                    previousIndexMap = selectedIndexMap;
                }
                else
                {
                    selectedIndexMap = previousIndexMap;
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
            selectedIndexNpc = EditorGUILayout.Popup("NPC 선택", selectedIndexNpc, _nameNpc.ToArray());
            // NPC 추가 버튼
            if (GUILayout.Button("NPC 추가"))
            {
                npcExporter.AddNpcToMap(selectedIndexNpc);
            }
            
            GUILayout.Space(20);
            // 몬스터 추가 섹션
            GUILayout.Label("* 몬스터 추가", EditorStyles.whiteLargeLabel);
            // 몬스터 드롭다운
            selectedIndexMonster = EditorGUILayout.Popup("몬스터 선택", selectedIndexMonster, _nameMonster.ToArray());
            // 몬스터 추가 버튼
            if (GUILayout.Button("몬스터 추가"))
            {
                monsterExporter.AddMonsterToMap(selectedIndexMonster);
            }
            
            GUILayout.Space(20);
            // 워프 추가 섹션
            GUILayout.Label("* 워프 추가", EditorStyles.whiteLargeLabel);
            // 워프 추가 버튼
            if (GUILayout.Button("워프 추가"))
            {
                warpExporter.AddWarpToMap();
            }
            
            EditorGUILayout.EndScrollView();
        }
        private void ExportDataToJson()
        {
            bool result = EditorUtility.DisplayDialog("저장하기", "현재 선택된 맵에 저장하시겠습니까?", "네", "아니요");
            if (!result) return;
            // 태그가 'Map'인 오브젝트를 찾습니다.
            GameObject mapObject = GameObject.FindGameObjectWithTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
        
            if (mapObject == null)
            {
                Debug.LogWarning("No GameObject with the tag 'Map' found in the scene.");
                return;
            }
            string currentJsonFolderPath = jsonFolderPath + _tableMap.GetDataByUid(loadMapUid).FolderName + "/";
            npcExporter.ExportNpcDataToJson(currentJsonFolderPath, jsonFileNameRegenNpc, loadMapUid);
            monsterExporter.ExportMonsterDataToJson(currentJsonFolderPath, jsonFileNameRegenMonster, loadMapUid);
            warpExporter.ExportWarpDataToJson(currentJsonFolderPath, jsonFileNameWarp, loadMapUid);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }

        private bool LoadJsonData()
        {
            bool result = EditorUtility.DisplayDialog("불러오기", "현재 불러온 내용이 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
            if (!result) return false;
            var mapData = _tableMap.GetDataByUid(loadMapUid);
            
            LoadTileData();
            npcExporter.LoadNpcData(RESOURCES_FOLDER_PATH + mapData.FolderName + "/" + MapConstants.FileNameRegenNpc);
            monsterExporter.LoadMonsterData(RESOURCES_FOLDER_PATH + mapData.FolderName + "/" + MapConstants.FileNameRegenMonster);
            warpExporter.LoadWarpData(RESOURCES_FOLDER_PATH + mapData.FolderName + "/" + MapConstants.FileNameWarp);
            return true;
        }
        /// <summary>
        /// MapManager.cs:25
        /// </summary>
        private void LoadTileData()
        {
            var mapData = _tableMap.GetDataByUid(loadMapUid);
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

            string tilemapPath = RESOURCES_FOLDER_PATH + mapData.FolderName + "/" + MapConstants.FileNameTilemap;
            GameObject prefab = Resources.Load<GameObject>(tilemapPath);
            if (prefab == null)
            {
                GcLogger.LogError("맵 프리팹이 없습니다. mapUid : " + loadMapUid + " / path : "+tilemapPath);
                return;
            }

            if (_gridTileMap == null)
            {
                _gridTileMap = GameObject.Find(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            }
            GameObject currentMap = Instantiate(prefab, _gridTileMap.transform);
            _defaultMap = currentMap.GetComponent<MapTileCommon>();
            _defaultMap.InitComponents();
            _defaultMap.InitTagSortingLayer();
            _defaultMap.Initialize(mapData.Uid, mapData.Name, mapData.Type, mapData.Subtype);
            npcExporter.SetDefaultMap(_defaultMap);
            monsterExporter.SetDefaultMap(_defaultMap);
            warpExporter.SetDefaultMap(_defaultMap);
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
                struckTableMaps.TryAdd(index++, info);
            }
        }
    }
}
