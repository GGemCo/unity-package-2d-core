using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 맵 매니저
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        // 타일맵이 들갈 grid 오브젝트
        private GameObject gridTileMap;
        // 페이드 인에 사용할 검정색 스프라이트 오브젝트
        private GameObject bgBlackForMapLoading;
        // 페이드 인 지속 시간
        private const float FADE_DURATION = 0.3f;

        // 맵 로드 상태
        private MapConstants.State currentState = MapConstants.State.None;
        // 현재 맵 uid
        private int currentMapUid;

        private SceneGame sceneGame;
        private SaveDataManager saveDataManager;
        private TableLoaderManager tableLoaderManager;
        
        // 현재 맵에서 플레이어가 스폰될 위치
        private Vector3 playSpawnPosition;

        // 맵 로드 완료되었을때 발생되는 이벤트
        private UnityEvent onLoadCompleteMap;
        
        // 현재 맵 테이블 데이터
        private StruckTableMap currentMapTableData;
        // 현재 타이맬 스크립트
        private MapTileCommon mapTileCommon;
        // 타일맵이 로드 완료되었을때 발생하는 이벤트
        private UnityEvent onLoadTileMap;
        // 몬스터 리젠 코루틴
        private Coroutine coroutineRegenMonster;
        // 캐릭터, 워프 스폰 매니저
        private MapLoadCharacters mapLoadCharacters;
        protected void Awake()
        {
            if (TableLoaderManager.Instance == null) return;
            tableLoaderManager = TableLoaderManager.Instance;
            
            mapLoadCharacters = new MapLoadCharacters();
            mapLoadCharacters.Initialize(this);
            
            CreateGrid();
        }
        /// <summary>
        /// 타일맵을 추가할 grid 오브젝트 만들기
        /// </summary>
        private void CreateGrid()
        {
            gridTileMap = new GameObject(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap))
            {
                tag = ConfigTags.GetValue(ConfigTags.Keys.GridTileMap)
            };
            Grid grid = gridTileMap.gameObject.AddComponent<Grid>();
            Vector2 tilemapGridSize = AddressableSettingsLoader.Instance.mapSettings.tilemapGridCellSize;
            if (tilemapGridSize == Vector2.zero)
            {
                GcLogger.LogError("타일맵 Grid 사이즈가 정해지지 않았습니다. GGemCoMapSettings 에 Tilemap Grid Cell Size 를 입력해주세요.");
                return;
            }
            grid.cellSize = new Vector3(tilemapGridSize.x, tilemapGridSize.y, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
        }
        protected void Start()
        {
            sceneGame = SceneGame.Instance;
            bgBlackForMapLoading = sceneGame.bgBlackForMapLoading;
            saveDataManager = sceneGame.saveDataManager;
            
            // 저장된 맵 불러오기
            int startMapUid = GetStartMapUid();

            LoadMap(startMapUid);
        }
        /// <summary>
        /// 게임 시작시 맵 불러오기
        /// </summary>
        /// <returns></returns>
        private int GetStartMapUid()
        {
            int startMapUid = saveDataManager.Player.CurrentMapUid;
            // 시작 맵 불러오기
            if (startMapUid <= 0)
            {
                startMapUid = AddressableSettingsLoader.Instance.mapSettings.startMapUid;
                if (startMapUid <= 0)
                {
                    GcLogger.LogError("시작 맵 고유번호가 잘 못 되었습니다. GGemCoMapSettins 에 startMapUid 를 입력해주세요.");
                    return 0;
                }

                var info = TableLoaderManager.Instance.TableMap.GetDataByUid(startMapUid);
                if (info == null)
                {
                    GcLogger.LogError("맵 테이블에 없는 고유번호 입니다. GGemCoMapSettins 에 startMapUid 를 확인해주세요.");
                    return 0;
                }
            }

            return startMapUid;
        }
        protected void Reset()
        {
            StopAllCoroutines();
            mapLoadCharacters.Reset();
        }
        /// <summary>
        /// 맵 불러오기
        /// </summary>
        /// <param name="mapUid"></param>
        private void LoadMap(int mapUid = 0)
        {
            if (IsPossibleLoad() != true)
            {
                // GcLogger.LogError($"map state: {currentState}");
                return;
            }

            if (mapUid <= 0)
            {
                GcLogger.LogError("맵 고유번호가 잘 못되었습니다.");
                return;
            }
            // GcLogger.Log("LoadMap start");
            Reset();
            currentState = MapConstants.State.FadeIn;
            currentMapUid = mapUid;
            
            StartCoroutine(UpdateState());
        }
        /// <summary>
        /// 맵 로드 상태별 처리 
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateState()
        {
            while (currentState != MapConstants.State.Complete && currentState != MapConstants.State.Failed)
            {
                switch (currentState)
                {
                    case MapConstants.State.FadeIn:
                        yield return StartCoroutineSafe(FadeIn());
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.UnloadPreviousStage;
                        break;

                    case MapConstants.State.UnloadPreviousStage:
                        yield return StartCoroutineSafe(UnloadPreviousStage());
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.LoadTilemapPrefab;
                        break;

                    case MapConstants.State.LoadTilemapPrefab:
                        yield return StartCoroutineSafe(LoadTilemap());
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.LoadPlayerPrefabs;
                        break;

                    case MapConstants.State.LoadPlayerPrefabs:
                        yield return StartCoroutineSafe(mapLoadCharacters.LoadPlayer(playSpawnPosition, currentMapTableData, mapTileCommon));
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.LoadMonsterPrefabs;
                        break;

                    case MapConstants.State.LoadMonsterPrefabs:
                        yield return StartCoroutineSafe(mapLoadCharacters.LoadMonsters(mapTileCommon));
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.LoadNpcPrefabs;
                        break;

                    case MapConstants.State.LoadNpcPrefabs:
                        yield return StartCoroutineSafe(mapLoadCharacters.LoadNpcs(mapTileCommon));
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.LoadWarpPrefabs;
                        break;

                    case MapConstants.State.LoadWarpPrefabs:
                        yield return StartCoroutineSafe(mapLoadCharacters.LoadWarps(mapTileCommon));
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.FadeOut;
                        break;

                    case MapConstants.State.FadeOut:
                        yield return StartCoroutineSafe(FadeOut());
                        if (currentState == MapConstants.State.Failed) yield break;
                        currentState = MapConstants.State.Complete;
                        break;
                }

                yield return null;
            }

            if (currentState == MapConstants.State.Complete)
            {
                OnMapLoadComplete();
            }
            else
            {
                Debug.LogError("맵 로드 실패");
            }
        }
        /// <summary>
        /// 실패 시 즉시 종료되는 안전한 코루틴 실행 함수
        /// </summary>
        private IEnumerator StartCoroutineSafe(IEnumerator routine)
        {
            yield return StartCoroutine(routine);

            if (currentState == MapConstants.State.Failed)
            {
                yield break;
            }
        }
        /// <summary>
        /// 실패 시 currentState를 Failed로 설정하고 코루틴 종료
        /// </summary>
        private void SetLoadFailed(string errorMessage)
        {
            Debug.LogError($"맵 로드 실패: {errorMessage}");
            StartCoroutine(FadeOut());
            currentState = MapConstants.State.Failed;
        }
        /// <summary>
        /// 로딩시 보여주는 검은 화면 fade in 처리 
        /// </summary>
        /// <returns></returns>
        IEnumerator FadeIn()
        {
            if (bgBlackForMapLoading == null)
            {
                GcLogger.LogError("Fade Sprite가 설정되지 않았습니다.");
                yield break;
            }
            // 이미 활성화 되어있으면 (인게임 처음 시작했을때) 건너뛰기.
            if (bgBlackForMapLoading.activeSelf)
            {
                yield break;
            }

            bgBlackForMapLoading.SetActive(true);
            Image spriteRenderer = bgBlackForMapLoading.GetComponent<Image>();
            spriteRenderer.color = new Color(0, 0, 0, 0);
            float elapsedTime = 0.0f;

            while (elapsedTime < FADE_DURATION)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FADE_DURATION);
                float alpha = Mathf.Lerp(0, 1, Easing.EaseOutQuintic(t));
                spriteRenderer.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // Fade in이 완료된 후에 완전히 불투명하게 설정
            spriteRenderer.color = new Color(0, 0, 0, 1);

            // Logger.Log("Fade In 완료");
        }
        /// <summary>
        /// tag 로 맵에 있는 오브젝트 지우기
        /// </summary>
        /// <param name="pTag"></param>
        private void DestroyByTag(string pTag)
        {
            GameObject[] maps = GameObject.FindGameObjectsWithTag(pTag);
            foreach (GameObject map in maps)
            {
                if (map == null) continue;
                Destroy(map);
            }
        }
        /// <summary>
        /// 맵 이동시 메모리 해제 처리
        /// </summary>
        /// <returns></returns>
        IEnumerator UnloadPreviousStage()
        {
            // 현재 씬에 있는 모든 몬스터 오브젝트를 삭제
            DestroyByTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            DestroyByTag(ConfigTags.GetValue(ConfigTags.Keys.Npc));
            // 드랍 아이템 지우기
            DestroyByTag(ConfigTags.GetValue(ConfigTags.Keys.DropItem));

            DestroyOthers();
            // 잠시 대기하여 오브젝트가 완전히 삭제되도록 보장
            yield return null;

            // 사용되지 않는 메모리 해제
            yield return Resources.UnloadUnusedAssets();

            // 필요시 가비지 컬렉션을 강제로 실행
            GC.Collect();

            // GcLogger.Log("이전 스테이지의 몬스터 프리팹이 메모리에서 해제되었습니다.");
        }

        private void DestroyOthers()
        {
            DestroyByTag(ConfigTags.GetValue(ConfigTags.Keys.Map));
        }

        /// <summary>
        /// MapEditor.cs:152
        /// </summary>
        private IEnumerator LoadTilemap()
        {
            if (gridTileMap == null)
            {
                SetLoadFailed($"Grid 오브젝트가 없습니다.");
                yield break;
            }
            // SceneGame.Instance.player?.GetComponent<Player>().Stop();

            if (currentMapUid == 0)
            {
                currentMapUid = saveDataManager.Player.CurrentMapUid;
            }
            if (tableLoaderManager.TableMap.GetCount() <= 0)
            {
                SetLoadFailed("맵 테이블에 내용이 없습니다.");
                yield break;
            }
            currentMapTableData = tableLoaderManager.TableMap.GetDataByUid(currentMapUid);
            if (currentMapTableData == null)
            {
                SetLoadFailed($"맵 테이블에서 찾을 수 없습니다. Uid: {currentMapUid}");
                yield break;
            }
            string path = GetFilePath(MapConstants.FileNameTilemap);
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                SetLoadFailed($"타일맵 prefab 이 없습니다. path: {path} / currentMapUid: {currentMapUid}");
                yield break;
            }
            // bgm 플레이
            if (currentMapTableData.BgmUid > 0)
            {
            }

            GameObject currentMap = Instantiate(prefab, gridTileMap.transform);
            mapTileCommon = currentMap.GetComponent<MapTileCommon>();
            mapTileCommon.Initialize(currentMapTableData.Uid, currentMapTableData.Name, currentMapTableData.Type, currentMapTableData.Subtype);
            var result = mapTileCommon.GetMapSize();

            // 로드된 맵에 맞게 맵 영역 사이즈 갱신하기 
            SceneGame.Instance.cameraManager?.ChangeMapSize(result.x, result.y);
            
            onLoadTileMap?.Invoke();
            // Logger.Log("타일맵 프리팹 로드 완료");
            yield return null;
        }
        IEnumerator FadeOut()
        {
            if (bgBlackForMapLoading == null)
            {
                GcLogger.LogError("Fade Sprite가 설정되지 않았습니다.");
                yield break;
            }

            Image spriteRenderer = bgBlackForMapLoading.GetComponent<Image>();
            spriteRenderer.color = new Color(0, 0, 0, 1);
            float elapsedTime = 0.0f;

            while (elapsedTime < FADE_DURATION)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FADE_DURATION);
                float alpha = Mathf.Lerp(1, 0, Easing.EaseInQuintic(t));
                spriteRenderer.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // Fade in이 완료된 후에 완전히 불투명하게 설정
            spriteRenderer.color = new Color(0, 0, 0, 0);
            bgBlackForMapLoading.SetActive(false);

            // Logger.Log("Fade Out 완료");
        }
        void OnMapLoadComplete()
        {
            StopAllCoroutines();

            sceneGame.saveDataManager.Player.CurrentMapUid = currentMapUid;
            playSpawnPosition = Vector3.zero;
            
            onLoadCompleteMap?.Invoke();
            // Logger.Log("맵 로드 완료");
        }
        private bool IsPossibleLoad()
        {
            return (currentState == MapConstants.State.Complete || currentState == MapConstants.State.None);
        }
        public string GetFilePath(string fileName)
        {
            return MapConstants.ResourceMapPath + currentMapTableData.FolderName + "/" + fileName;
        }

        private void SetPlaySpawnPosition(Vector3 position)
        {
            playSpawnPosition = position;
        }
        public Vector3 GetPlaySpawnPosition()
        {
            return playSpawnPosition;
        }
        /// <summary>
        /// 몬스터 죽었을때 리젠 처리 
        /// </summary>
        /// <param name="monsterVid"></param>
        /// <param name="monsterUid"></param>
        /// <param name="monsterObject"></param>
        public void OnDeadMonster(int monsterVid, int monsterUid, GameObject monsterObject)
        {
            if (monsterVid <= 0) return;
            coroutineRegenMonster = StartCoroutine(mapLoadCharacters.RegenMonster(monsterVid, currentMapUid, mapTileCommon));
        }
        /// <summary>
        /// 현재 맵 사이즈 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCurrentMapSize()
        {
            if (mapTileCommon == null) return Vector2.zero;
            return mapTileCommon.GetMapSize();
        }
        /// <summary>
        /// 워프로 맵 이동하기
        /// </summary>
        /// <param name="objectWarp"></param>
        public void LoadMapByWarp(ObjectWarp objectWarp)
        {
            if (objectWarp == null)
            {
                GcLogger.LogError("ObjectWarp.cs 가 없습니다.");
                return;
            }

            if (objectWarp.toMapUid <= 0)
            {
                GcLogger.LogError("이동할 워프 정보가 없습니다.");
                return;
            }
            SetPlaySpawnPosition(objectWarp.toMapPlayerSpawnPosition);
            LoadMap(objectWarp.toMapUid);
        }
        /// <summary>
        /// 플레이어가 죽었을때 다시 시작하기
        /// </summary>
        public void LoadMapByPlayerDead()
        {
            var info = tableLoaderManager.TableMap.GetDataByUid(currentMapUid);
            if (info == null) return;
            LoadMap(info.PlayerDeadSpawnUid);
        }
        /// <summary>
        /// 플레이어 기준 range 안에서 가장 가까운 몬스터 찾기
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public CharacterBase GetNearByMonsterDistance(int range)
        {
            return mapTileCommon?.GetNearByMonsterDistance(range);
        }

        public CharacterBase GetNpcByUid(int uid)
        {
            return mapTileCommon?.GetNpcByUid(uid);
        }

        public CharacterBase GetMonsterByUid(int uid)
        {
            return mapTileCommon?.GetMonsterByUid(uid);
        }

        public Transform GetCurrentMap()
        {
            return mapTileCommon.transform;
        }
        /// <summary>
        /// 모든 캐릭터 활성화
        /// 연출 시작시 사용
        /// </summary>
        public void ActiveAllCharacters()
        {
            if (mapTileCommon == null) return;
            mapTileCommon.ActiveAllCharacters();
        }
        public int GetCurrentMapUid() => currentMapUid;
        private void OnDestroy()
        {
            
        }
    }
}