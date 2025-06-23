using System;
using System.Collections;
using System.Threading.Tasks;
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
        private GameObject _gridTileMap;
        // 페이드 인에 사용할 검정색 스프라이트 오브젝트
        private GameObject _bgBlackForMapLoading;
        // 페이드 인 지속 시간
        private const float FadeDuration = 0.3f;

        // 맵 로드 상태
        private MapConstants.State _currentState = MapConstants.State.None;
        // 현재 맵 uid
        private int _currentMapUid;

        private SceneGame _sceneGame;
        private SaveDataManager _saveDataManager;
        private TableLoaderManager _tableLoaderManager;
        
        // 현재 맵에서 플레이어가 스폰될 위치
        private Vector3 _playSpawnPosition;

        // 맵 로드 완료되었을때 발생되는 이벤트
        private UnityEvent _onLoadCompleteMap;
        
        // 현재 맵 테이블 데이터
        private StruckTableMap _currentMapTableData;
        // 현재 타이맬 스크립트
        private MapTileCommon _mapTileCommon;
        // 타일맵이 로드 완료되었을때 발생하는 이벤트
        private UnityEvent _onLoadTileMap;
        // 캐릭터, 워프 스폰 매니저
        private MapLoadCharacters _mapLoadCharacters;
        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;
        
        protected void Awake()
        {
            if (!TableLoaderManager.Instance) return;
            _tableLoaderManager = TableLoaderManager.Instance;
            
            _mapLoadCharacters = new MapLoadCharacters();
            _mapLoadCharacters.Initialize(this);
            
            CreateGrid();
        }
        /// <summary>
        /// 타일맵을 추가할 grid 오브젝트 만들기
        /// </summary>
        private void CreateGrid()
        {
            _gridTileMap = new GameObject(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap))
            {
                tag = ConfigTags.GetValue(ConfigTags.Keys.GridTileMap)
            };
            Grid grid = _gridTileMap.gameObject.AddComponent<Grid>();
            Vector2 tilemapGridSize = AddressableLoaderSettings.Instance.mapSettings.tilemapGridCellSize;
            if (tilemapGridSize == Vector2.zero)
            {
                GcLogger.LogError($"타일맵 Grid 사이즈가 정해지지 않았습니다. {ConfigDefine.NameSDK}MapSettings 에 Tilemap Grid Cell Size 를 입력해주세요.");
                return;
            }
            grid.cellSize = new Vector3(tilemapGridSize.x, tilemapGridSize.y, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
        }
        protected void Start()
        {
            _sceneGame = SceneGame.Instance;
            _bgBlackForMapLoading = _sceneGame.bgBlackForMapLoading;
            _saveDataManager = _sceneGame.saveDataManager;
            _addressableLoaderPrefabCharacter = _sceneGame.AddressableLoaderPrefabCharacter;
            
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
            int startMapUid = _saveDataManager.Player.CurrentMapUid;
            // 시작 맵 불러오기
            if (startMapUid <= 0)
            {
                startMapUid = AddressableLoaderSettings.Instance.mapSettings.startMapUid;
                if (startMapUid <= 0)
                {
                    GcLogger.LogError($"시작 맵 고유번호가 잘 못 되었습니다. {ConfigDefine.NameSDK}MapSettins 에 startMapUid 를 입력해주세요.");
                    return 0;
                }

                var info = TableLoaderManager.Instance.TableMap.GetDataByUid(startMapUid);
                if (info == null)
                {
                    GcLogger.LogError($"맵 테이블에 없는 고유번호 입니다. {ConfigDefine.NameSDK}MapSettins 에 startMapUid 를 확인해주세요.");
                    return 0;
                }
            }

            return startMapUid;
        }
        protected void Reset()
        {
            StopAllCoroutines();
            _mapLoadCharacters?.Reset();
            _addressableLoaderPrefabCharacter?.Release();
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
            _currentState = MapConstants.State.FadeIn;
            _currentMapUid = mapUid;
            
            StartCoroutine(UpdateState());
        }
        private IEnumerator AwaitTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                Debug.LogException(task.Exception);
        }
        /// <summary>
        /// 맵 로드 상태별 처리 
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateState()
        {
            while (_currentState != MapConstants.State.Complete && _currentState != MapConstants.State.Failed)
            {
                switch (_currentState)
                {
                    case MapConstants.State.FadeIn:
                        yield return StartCoroutineSafe(FadeIn());
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.UnloadPreviousStage;
                        break;

                    case MapConstants.State.UnloadPreviousStage:
                        yield return StartCoroutineSafe(UnloadPreviousStage());
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.LoadTilemapPrefab;
                        break;
                    
                    case MapConstants.State.LoadTilemapPrefab:
                        yield return StartCoroutineSafe(AwaitTask(LoadTilemap()));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.LoadPlayerPrefabs;
                        break;
                        
                    case MapConstants.State.LoadPlayerPrefabs:
                        yield return StartCoroutineSafe(AwaitTask(
                            _mapLoadCharacters.LoadPlayer(_playSpawnPosition, _currentMapTableData, _mapTileCommon)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.LoadCharacterPrefabs;
                        break;
                    
                    case MapConstants.State.LoadCharacterPrefabs:
                        yield return StartCoroutineSafe(AwaitTask(
                            _addressableLoaderPrefabCharacter.LoadCharacterByMap(_currentMapTableData)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.CreateMonster;
                        break;

                    case MapConstants.State.CreateMonster:
                        yield return StartCoroutineSafe(
                            AwaitTask(_mapLoadCharacters.LoadMonsters(_mapTileCommon, _currentMapTableData)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.CreateNpc;
                        break;

                    case MapConstants.State.CreateNpc:
                        yield return StartCoroutineSafe(
                            AwaitTask(_mapLoadCharacters.LoadNpcs(_mapTileCommon, _currentMapTableData)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.CreateWarp;
                        break;

                    case MapConstants.State.CreateWarp:
                        yield return StartCoroutineSafe(AwaitTask(_mapLoadCharacters.LoadWarps(_mapTileCommon, _currentMapTableData)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.FadeOut;
                        break;

                    case MapConstants.State.FadeOut:
                        yield return StartCoroutineSafe(FadeOut());
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.Complete;
                        break;
                }

                yield return null;
            }

            if (_currentState == MapConstants.State.Complete)
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

            if (_currentState == MapConstants.State.Failed)
            {
            }
        }
        /// <summary>
        /// 실패 시 currentState를 Failed로 설정하고 코루틴 종료
        /// </summary>
        private void SetLoadFailed(string errorMessage)
        {
            Debug.LogError($"맵 로드 실패: {errorMessage}");
            StartCoroutine(FadeOut());
            _currentState = MapConstants.State.Failed;
        }
        /// <summary>
        /// 로딩시 보여주는 검은 화면 fade in 처리 
        /// </summary>
        /// <returns></returns>
        IEnumerator FadeIn()
        {
            if (!_bgBlackForMapLoading)
            {
                GcLogger.LogError("Fade Sprite가 설정되지 않았습니다.");
                yield break;
            }
            // 이미 활성화 되어있으면 (인게임 처음 시작했을때) 건너뛰기.
            if (_bgBlackForMapLoading.activeSelf)
            {
                yield break;
            }

            _bgBlackForMapLoading.SetActive(true);
            Image spriteRenderer = _bgBlackForMapLoading.GetComponent<Image>();
            spriteRenderer.color = new Color(0, 0, 0, 0);
            float elapsedTime = 0.0f;

            while (elapsedTime < FadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FadeDuration);
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
                if (!map) continue;
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
        private async Task LoadTilemap()
        {
            try
            {
                if (!_gridTileMap)
                {
                    SetLoadFailed($"Grid 오브젝트가 없습니다.");
                    return;
                }

                if (_currentMapUid == 0)
                {
                    _currentMapUid = _saveDataManager.Player.CurrentMapUid;
                }
                if (_tableLoaderManager.TableMap.GetCount() <= 0)
                {
                    SetLoadFailed("맵 테이블에 내용이 없습니다.");
                    return;
                }
                _currentMapTableData = _tableLoaderManager.TableMap.GetDataByUid(_currentMapUid);
                if (_currentMapTableData == null)
                {
                    SetLoadFailed($"맵 테이블에서 찾을 수 없습니다. Uid: {_currentMapUid}");
                    return;
                }
                string key = ConfigAddressableMap.GetKeyTileMap(_currentMapTableData.FolderName);
                GameObject prefab = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);
                if (!prefab)
                {
                    SetLoadFailed($"타일맵 prefab 이 없습니다. key: {key} / currentMapUid: {_currentMapUid}");
                    return;
                }
                // bgm 플레이
                if (_currentMapTableData.BgmUid > 0)
                {
                }

                GameObject currentMap = Instantiate(prefab, _gridTileMap.transform);
                _mapTileCommon = currentMap.GetComponent<MapTileCommon>();
                _mapTileCommon.Initialize(_currentMapTableData.Uid, _currentMapTableData.Name, _currentMapTableData.Type, _currentMapTableData.Subtype);
                var result = _mapTileCommon.GetMapSize();

                // 로드된 맵에 맞게 맵 영역 사이즈 갱신하기 
                SceneGame.Instance.cameraManager?.ChangeMapSize(result.x, result.y);
            
                _onLoadTileMap?.Invoke();
                
                _currentState = MapConstants.State.LoadPlayerPrefabs;
                // Logger.Log("타일맵 프리팹 로드 완료");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        IEnumerator FadeOut()
        {
            if (!_bgBlackForMapLoading)
            {
                GcLogger.LogError("Fade Sprite가 설정되지 않았습니다.");
                yield break;
            }

            Image spriteRenderer = _bgBlackForMapLoading.GetComponent<Image>();
            spriteRenderer.color = new Color(0, 0, 0, 1);
            float elapsedTime = 0.0f;

            while (elapsedTime < FadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FadeDuration);
                float alpha = Mathf.Lerp(1, 0, Easing.EaseInQuintic(t));
                spriteRenderer.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            // Fade in이 완료된 후에 완전히 불투명하게 설정
            spriteRenderer.color = new Color(0, 0, 0, 0);
            _bgBlackForMapLoading.SetActive(false);

            // Logger.Log("Fade Out 완료");
        }
        void OnMapLoadComplete()
        {
            StopAllCoroutines();

            _sceneGame.saveDataManager.Player.CurrentMapUid = _currentMapUid;
            _playSpawnPosition = Vector3.zero;
            
            _onLoadCompleteMap?.Invoke();
            // Logger.Log("맵 로드 완료");
        }
        private bool IsPossibleLoad()
        {
            return (_currentState == MapConstants.State.Complete || _currentState == MapConstants.State.None);
        }

        private void SetPlaySpawnPosition(Vector3 position)
        {
            _playSpawnPosition = position;
        }
        public Vector3 GetPlaySpawnPosition()
        {
            return _playSpawnPosition;
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
            StartCoroutine(_mapLoadCharacters.RegenMonster(monsterVid, _currentMapUid, _mapTileCommon));
        }
        /// <summary>
        /// 현재 맵 사이즈 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCurrentMapSize()
        {
            return !_mapTileCommon ? Vector2.zero : _mapTileCommon.GetMapSize();
        }
        /// <summary>
        /// 워프로 맵 이동하기
        /// </summary>
        /// <param name="objectWarp"></param>
        public void LoadMapByWarp(ObjectWarp objectWarp)
        {
            if (!objectWarp)
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
            var info = _tableLoaderManager.TableMap.GetDataByUid(_currentMapUid);
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
            return _mapTileCommon?.GetNearByMonsterDistance(range);
        }

        public CharacterBase GetNpcByUid(int uid)
        {
            return _mapTileCommon?.GetNpcByUid(uid);
        }

        public CharacterBase GetMonsterByUid(int uid)
        {
            return _mapTileCommon?.GetMonsterByUid(uid);
        }

        public Transform GetCurrentMap()
        {
            return _mapTileCommon.transform;
        }
        /// <summary>
        /// 모든 캐릭터 활성화
        /// 연출 시작시 사용
        /// </summary>
        public void ActiveAllCharacters()
        {
            if (!_mapTileCommon) return;
            _mapTileCommon.ActiveAllCharacters();
        }
        public int GetCurrentMapUid() => _currentMapUid;

    }
}