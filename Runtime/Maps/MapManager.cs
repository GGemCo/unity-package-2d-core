using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 매니저
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        // 타일맵이 들갈 grid 오브젝트
        private GameObject _grid;

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

        // 맵 로드 시작되었을때 발생되는 이벤트
        public event Action OnLoadStartMap;
        public static event Action<MapTileCommon, GameObject> OnLoadCompleteMap;

        /// <summary>
        /// 맵 로드 화면 페이드 아웃이 완료되어 플레이 화면이 다시 노출되었을 때 발생합니다.
        /// 자동 이동처럼 플레이어가 화면에 보인 뒤 시작해야 하는 시스템에서 사용합니다.
        /// </summary>
        public static event Action<MapTileCommon, GameObject> OnMapRevealComplete;

        public static event Action<MapTileCommon, GameObject> OnLoadTilemapCompleteMap;
        public static event Action<MapTileCommon, GameObject> OnLoadCompletePlayer;
        public static event Action<MapTileCommon, GameObject> OnLoadCompleteNpc;

        // 현재 맵 테이블 데이터
        private StruckTableMap _currentMapTableData;

        // 현재 타이맬 스크립트
        private MapTileCommon _mapTileCommon;

        // 타일맵이 로드 완료되었을때 발생하는 이벤트
        // 캐릭터, 워프 스폰 매니저
        private MapLoadCharacters _mapLoadCharacters;

        // 맵 웨이브 스폰 컨트롤러
        private MapWaveSpawnController _mapWaveSpawnController;

        private AddressableLoaderPrefabCharacter _addressableLoaderPrefabCharacter;
        private MapEntryRuleResolver _mapEntryRuleResolver;
        private GGemCoMapSettings _mapSettings;
        private MapAutoMoveLifecycleController _autoMoveLifecycleController;
        private MapSoundScopeController _mapSoundScopeController;

        protected void Awake()
        {
            if (!TableLoaderManager.Instance) return;
            _tableLoaderManager = TableLoaderManager.Instance;

            _mapLoadCharacters = new MapLoadCharacters();
            _mapLoadCharacters.Initialize(this);
            _mapWaveSpawnController = new MapWaveSpawnController();
            _mapWaveSpawnController.Initialize(this, _mapLoadCharacters);
            _autoMoveLifecycleController = new MapAutoMoveLifecycleController();

            CreateGrid();
        }

        /// <summary>
        /// 타일맵을 추가할 grid 오브젝트 만들기
        /// </summary>
        private void CreateGrid()
        {
            GameObject exitsGrid = GameObject.FindWithTag(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap));
            if (exitsGrid != null)
            {
                Destroy(exitsGrid.gameObject);
            }

            _grid = new GameObject(ConfigTags.GetValue(ConfigTags.Keys.GridTileMap))
            {
                tag = ConfigTags.GetValue(ConfigTags.Keys.GridTileMap)
            };
            Grid grid = _grid.gameObject.AddComponent<Grid>();
            Vector2 tilemapGridSize = AddressableLoaderSettings.Instance.mapSettings.tilemapGridCellSize;
            if (tilemapGridSize == Vector2.zero)
            {
                GcLogger.LogError(
                    $"타일맵 Grid 사이즈가 정해지지 않았습니다. {ConfigDefine.NameSDK}MapSettings 에 Tilemap Grid Cell Size 를 입력해주세요.");
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
            _mapEntryRuleResolver = new MapEntryRuleResolver(_tableLoaderManager, _saveDataManager?.LicenseManager);
            _mapSettings = AddressableLoaderSettings.Instance.mapSettings;
            _autoMoveLifecycleController?.Register(this);
            _mapSoundScopeController = new MapSoundScopeController(_tableLoaderManager, AddressableLoaderSound.Instance);

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
                    GcLogger.LogError(
                        $"시작 맵 고유번호가 잘 못 되었습니다. {ConfigDefine.NameSDK}MapSettins 에 startMapUid 를 입력해주세요.");
                    return 0;
                }

                var info = TableLoaderManager.Instance.GetMapData(startMapUid);
                if (info == null)
                {
                    GcLogger.LogError($"맵 테이블에 없는 고유번호 입니다. {ConfigDefine.NameSDK}MapSettins 에 startMapUid 를 확인해주세요.");
                    return 0;
                }
            }
            else
            {
                var info = TableLoaderManager.Instance.GetMapData(startMapUid);
                if (GcLogger.IsNull(info,
                        $"맵 테이블에 없는 고유번호 입니다. mapUid:{startMapUid}")) return 0;
                // 마을 타입에서 시작하는 설정이 되어있으면
                if (_mapSettings != null && _mapSettings.useStartMapTown && info.Type != _mapSettings.typeMapTown &&
                    _saveDataManager.MapProgress.LastTownMapUid > 0)
                {
                    info = TableLoaderManager.Instance.GetMapData(_saveDataManager.MapProgress.LastTownMapUid);
                    if (info != null && info.Type != _mapSettings.typeMapTown)
                    {
                        GcLogger.LogWarning(
                            $"마지막으로 저장된 마을 타입 번호의 데이터가 마을 타입으로 지정되어 있지 않습니다." +
                            $"mapUid: {_saveDataManager.MapProgress.LastTownMapUid}, type: {info.Type}");
                    }
                    else
                    {
                        startMapUid = _saveDataManager.MapProgress.LastTownMapUid;
                    }
                }
            }

            return startMapUid;
        }

        /// <summary>
        /// 맵 매니저가 제거될 때 자동 이동 수명주기 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            _autoMoveLifecycleController?.Unregister(this);
            _mapSoundScopeController?.Dispose();
            _mapSoundScopeController = null;
        }

        protected void Reset()
        {
            _mapWaveSpawnController?.Reset();
            StopAllCoroutines();
            // 맵 언로드 시점에 외부 패키지가 Addressables 핸들을 해제할 수 있도록 알림.
            CharacterSpawnHooks.NotifyMapUnload();
            _mapLoadCharacters?.Reset();
            _addressableLoaderPrefabCharacter?.Release();
        }

        private bool HasValidCurrentMap()
        {
            return _mapTileCommon != null;
        }

        private MapTileCommon DetachCurrentMapReference()
        {
            var previousMap = _mapTileCommon;
            _mapTileCommon = null;
            return previousMap;
        }

        /// <summary>
        /// 맵 불러오기
        /// </summary>
        /// <param name="mapUid"></param>
        public void LoadMap(int mapUid = 0)
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

            mapUid = ResolveMapEntryTargetMapUid(mapUid);
            CancelCutsceneBeforeMapLoad();
            // GcLogger.Log("LoadMap start");
            Reset();
            _currentState = MapConstants.State.FadeIn;
            _currentMapUid = mapUid;

            OnLoadStartMap?.Invoke();

            StartCoroutine(UpdateState());
        }

        /// <summary>
        /// 맵 로드가 확정되었을 때 이전 맵에서 진행 중이던 컷신과 대화 연출을 정리합니다.
        /// 맵 오브젝트를 제거하기 전에 호출하여 컷신 컨트롤러가 기존 대상 참조를 사용해 복원 처리를 완료할 수 있게 합니다.
        /// </summary>
        private void CancelCutsceneBeforeMapLoad()
        {
            CutsceneManager cutsceneManager = _sceneGame != null
                ? _sceneGame.CutsceneManager
                : SceneGame.Instance?.CutsceneManager;

            cutsceneManager?.CancelCurrentCutsceneForMapTransition();
        }

        /// <summary>
        /// map_entry_rule 테이블을 적용하여 실제로 로드할 맵 UID를 결정합니다.
        /// </summary>
        /// <param name="requestMapUid">플레이어가 원래 입장하려던 맵 UID입니다.</param>
        /// <returns>규칙이 매칭되면 대상 맵 UID를, 없으면 요청 맵 UID를 반환합니다.</returns>
        private int ResolveMapEntryTargetMapUid(int requestMapUid)
        {
            _mapEntryRuleResolver ??= new MapEntryRuleResolver(
                _tableLoaderManager,
                _saveDataManager?.LicenseManager ?? SceneGame.Instance?.saveDataManager?.LicenseManager);
            return _mapEntryRuleResolver.ResolveTargetMapUid(requestMapUid);
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
                        _currentState = MapConstants.State.LoadSoundScope;
                        break;

                    case MapConstants.State.LoadSoundScope:
                        yield return StartCoroutineSafe(AwaitTask(PrepareCurrentMapSoundScopeAsync()));
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
                        OnLoadCompletePlayer?.Invoke(_mapTileCommon, _grid);
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
                        OnLoadCompleteNpc?.Invoke(_mapTileCommon, _grid);
                        _currentState = MapConstants.State.CreateWarp;
                        break;

                    case MapConstants.State.CreateWarp:
                        yield return StartCoroutineSafe(
                            AwaitTask(_mapLoadCharacters.LoadWarps(_mapTileCommon, _currentMapTableData)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        yield return StartCoroutineSafe(
                            AwaitTask(_mapWaveSpawnController.LoadWaveSpawnAsync(_mapTileCommon, _currentMapTableData)));
                        if (_currentState == MapConstants.State.Failed) yield break;
                        _currentState = MapConstants.State.FadeOut;
                        break;

                    case MapConstants.State.FadeOut:
                        yield return StartCoroutineSafe(FadeOut());
                        if (_currentState == MapConstants.State.Failed) yield break;
                        NotifyMapRevealComplete();
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
        /// 맵 화면이 다시 노출된 직후 필요한 후처리 이벤트를 발행합니다.
        /// </summary>
        private void NotifyMapRevealComplete()
        {
            OnMapRevealComplete?.Invoke(_mapTileCommon, _grid);
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
            _mapSoundScopeController?.CancelPending();
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
        /// 현재 로드 대상 맵의 map_sound 및 기존 BgmUid를 기준으로 다음 맵 사운드 범위를 준비합니다.
        /// 타일맵과 캐릭터를 제거하기 전에 로드를 시작하여 맵 전환 중 필요한 AudioClip을 확보합니다.
        /// </summary>
        private async Task PrepareCurrentMapSoundScopeAsync()
        {
            if (_currentMapUid <= 0 || _tableLoaderManager == null)
                return;

            StruckTableMap targetMapData = _tableLoaderManager.GetMapData(_currentMapUid);
            if (targetMapData == null)
            {
                SetLoadFailed($"맵 테이블에서 찾을 수 없습니다. Uid: {_currentMapUid}");
                return;
            }

            if (_mapSoundScopeController == null && AddressableLoaderSound.Instance != null)
            {
                _mapSoundScopeController = new MapSoundScopeController(
                    _tableLoaderManager,
                    AddressableLoaderSound.Instance);
            }

            if (_mapSoundScopeController != null)
                await _mapSoundScopeController.PrepareAsync(_currentMapUid, targetMapData.BgmUid);
        }

        /// <summary>
        /// 맵 이동시 메모리 해제 처리
        /// </summary>
        /// <returns></returns>
        IEnumerator UnloadPreviousStage()
        {
            var previousMap = DetachCurrentMapReference();

            // 현재 씬에 있는 몬스터는 Destroy 대신 Pool로 반환한다.
            _mapLoadCharacters?.ReturnAllMonstersToPool(previousMap);
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
                if (!_grid)
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

                _currentMapTableData = _tableLoaderManager.GetMapData(_currentMapUid);
                if (_currentMapTableData == null)
                {
                    SetLoadFailed($"맵 테이블에서 찾을 수 없습니다. Uid: {_currentMapUid}");
                    return;
                }

                ApplyCameraOverrideFromCurrentMapData();

                string key = ConfigAddressableMap.GetKeyTileMap(_currentMapTableData.FolderName);
                GameObject prefab = await AddressableLoaderController.LoadByKeyAsync<GameObject>(key);
                if (!prefab)
                {
                    SetLoadFailed($"타일맵 prefab 이 없습니다. key: {key} / currentMapUid: {_currentMapUid}");
                    return;
                }

                GameObject currentMap = Instantiate(prefab, _grid.transform);
                _mapTileCommon = currentMap.GetComponent<MapTileCommon>();
                if (_mapTileCommon == null)
                {
                    SetLoadFailed($"MapTileCommon 컴포넌트가 없습니다. key: {key} / currentMapUid: {_currentMapUid}");
                    Destroy(currentMap);
                    return;
                }

                _mapTileCommon.Initialize(_currentMapTableData);
                var result = GetMapSize();

                // 로드된 맵에 맞게 맵 영역 사이즈 갱신하기
                SceneGame.Instance.cameraManager?.ChangeMapSize(result.x, result.y);

                // 타일맵 검증과 초기화가 끝난 뒤 새 맵 사운드를 활성화하고 이전 맵 범위를 해제합니다.
                if (_mapSoundScopeController != null)
                {
                    _mapSoundScopeController.Activate(
                        _sceneGame.soundManager,
                        _currentMapUid,
                        _currentMapTableData.BgmUid);
                }
                else if (_currentMapTableData.BgmUid > 0)
                {
                    // AddressableLoaderSound가 없는 특수 테스트 환경에서는 기존 BGM 동작을 유지합니다.
                    _sceneGame.soundManager?.PlayByUid(_currentMapTableData.BgmUid);
                }
                else
                {
                    _sceneGame.soundManager?.StopBgm();
                    _sceneGame.soundManager?.StopAmbient();
                }

                OnLoadTilemapCompleteMap?.Invoke(_mapTileCommon, _grid);


                // Logger.Log("타일맵 프리팹 로드 완료");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 현재 로드 대상 맵의 테이블 설정을 기준으로 카메라 오버라이드 값을 적용합니다.
        /// 맵에서 값을 지정하지 않은 항목은 CameraManager 내부 기본값을 유지합니다.
        /// </summary>
        private void ApplyCameraOverrideFromCurrentMapData()
        {
            if (_sceneGame?.cameraManager == null)
            {
                return;
            }

            _sceneGame.cameraManager.ApplyMapCameraOverrides(_currentMapTableData);
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

        /// <summary>
        /// 맵 로드 완료 후 저장 데이터 갱신, 기존 완료 콜백, 맵 입장 이벤트 발행을 처리합니다.
        /// </summary>
        void OnMapLoadComplete()
        {
            StopAllCoroutines();

            _sceneGame.saveDataManager.Player.CurrentMapUid = _currentMapUid;
            // 마지막으로 있었던 마을 저장
            if (_mapSettings != null && _mapSettings.useStartMapTown && _currentMapTableData.Type == _mapSettings.typeMapTown)
            {
                _sceneGame.saveDataManager.MapProgress.SaveLastTownMapUid(_currentMapUid);
            }
            _playSpawnPosition = Vector3.zero;
            // 맵 이동 후 한번 저장
            _saveDataManager.SaveData();

            OnLoadCompleteMap?.Invoke(_mapTileCommon, _grid);
            GameEventManager.MapEntered(new MapEnteredEventData(_currentMapUid,
                _mapTileCommon != null ? _mapTileCommon.gameObject : null));
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
        /// 몬스터 사망 이벤트를 웨이브 소유권과 일반 배치 리젠 정책으로 분기 처리합니다.
        /// 웨이브 몬스터는 웨이브 컨트롤러가 그룹 클리어와 다음 그룹 전환을 담당하므로 기본 개별 리젠 예약을 걸지 않습니다.
        /// </summary>
        /// <param name="monsterVid">사망한 몬스터의 런타임 VID입니다.</param>
        public void OnDeadMonster(int monsterVid)
        {
            if (monsterVid <= 0) return;
            if (!HasValidCurrentMap()) return;

            if (TryHandleWaveMonsterDead(monsterVid))
            {
                return;
            }

            if (ShouldSuppressMonsterRespawn()) return;

            _mapLoadCharacters?.MarkMonsterDead(monsterVid);
            StartCoroutine(_mapLoadCharacters.RegenMonster(monsterVid, _currentMapUid, _mapTileCommon));
        }

        /// <summary>
        /// 사망한 몬스터가 웨이브 소유 몬스터인지 확인하고 웨이브 컨트롤러에 사망 처리를 위임합니다.
        /// 웨이브 몬스터로 확인되면 일반 배치 리젠 로직을 실행하지 않도록 true를 반환합니다.
        /// </summary>
        /// <param name="monsterVid">사망한 몬스터의 런타임 VID입니다.</param>
        /// <returns>웨이브 소유 몬스터로 처리했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryHandleWaveMonsterDead(int monsterVid)
        {
            return _mapWaveSpawnController?.TryHandleWaveMonsterDead(monsterVid) == true;
        }

        /// <summary>
        /// 웨이브 진행, 맵 전체 몬스터 처치 목표 또는 맵 종료 정책이 진행 중이면 몬스터 리젠 예약을 막아야 하는지 확인합니다.
        /// </summary>
        /// <returns>현재 맵에서 몬스터 리젠을 막아야 하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldSuppressMonsterRespawn()
        {
            if (_mapWaveSpawnController?.ShouldSuppressNormalMonsterRespawn() == true)
            {
                return true;
            }

            if (SceneGame.Instance?.QuestManager?.HasActiveObjective(
                    _currentMapUid,
                    QuestConstants.ObjectiveType.KillMonsterInMap) == true)
            {
                return true;
            }

            return SceneGame.Instance?.mapClearExitPolicyController
                ?.ShouldSuppressMonsterRespawn(_currentMapUid) == true;
        }

        public void OnMonsterReturnedToPool(int monsterVid)
        {
            if (monsterVid <= 0) return;
            if (!HasValidCurrentMap()) return;

            _mapLoadCharacters?.OnMonsterReturnedToPool(monsterVid, _mapTileCommon);
        }

        /// <summary>
        /// 현재 맵 사이즈 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCurrentMapSize()
        {
            return GetMapSize();
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

            var info = _tableLoaderManager.TableMap.GetDataByUid(objectWarp.toMapUid);
            if (info == null) return;
            // 맵 이동 전 저장
            _saveDataManager.SaveData();
            SetPlaySpawnPosition(objectWarp.toMapPlayerSpawnPosition);
            LoadMap(objectWarp.toMapUid);
        }

        /// <summary>
        /// 플레이어가 죽었을때 다시 시작하기
        /// </summary>
        public void LoadMapByPlayerDead()
        {
            var info = _tableLoaderManager.GetMapData(_currentMapUid);
            if (info == null) return;
            if (_sceneGame.player)
            {
                _sceneGame.player.gameObject.GetComponent<Player>().ResetStatsByDead();
            }

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

        /// <summary>
        /// 현재 맵에 등록되어 있고 활성화된 NPC 목록을 반환합니다.
        /// Intro 종료 후 상호작용 재검사처럼 현재 맵 기준 NPC 후보가 필요한 시스템에서 사용합니다.
        /// </summary>
        /// <returns>현재 맵에 등록된 활성 NPC 열거 결과입니다.</returns>
        public IEnumerable<Npc> GetActiveNpcs()
        {
            if (_mapTileCommon == null)
            {
                yield break;
            }

            Dictionary<int, GameObject> npcs = _mapTileCommon.GetNpcs();
            if (npcs == null || npcs.Count == 0)
            {
                yield break;
            }

            foreach (GameObject npcObject in npcs.Values)
            {
                if (npcObject == null || !npcObject.activeInHierarchy)
                {
                    continue;
                }

                Npc npc = npcObject.GetComponent<Npc>();
                if (npc == null || !npc.isActiveAndEnabled || npc.IsStatusDead())
                {
                    continue;
                }

                yield return npc;
            }
        }

        public CharacterBase GetNpcByUid(int uid)
        {
            return _mapTileCommon?.GetNpcByUid(uid);
        }

        public CharacterBase GetMonsterByUid(int uid)
        {
            return _mapTileCommon?.GetMonsterByUid(uid);
        }

        /// <summary>
        /// 컷신에서 동적으로 생성한 캐릭터를 현재 맵의 상주 캐릭터로 등록합니다.
        /// 등록 후에는 컷신 추적 목록과 무관하게 맵 컬링/조회 로직의 관리 대상이 됩니다.
        /// </summary>
        /// <param name="character">맵에 정착시킬 캐릭터입니다.</param>
        /// <returns>정상 등록되면 <see langword="true"/>를 반환합니다.</returns>
        public bool RegisterCutsceneSpawnedCharacter(CharacterBase character)
        {
            if (character == null || _mapTileCommon == null)
            {
                return false;
            }

            if (!IsSupportedMapResidentType(character.type))
            {
                return false;
            }

            if (character.uid <= 0)
            {
                GcLogger.LogWarning(
                    $"맵 정착 등록을 건너뜁니다. uid가 유효하지 않습니다. type={character.type}, uid={character.uid}");
                return false;
            }

            Dictionary<int, GameObject> targetMap =
                character.type == CharacterConstants.Type.Npc
                    ? _mapTileCommon.GetNpcs()
                    : _mapTileCommon.GetMonsters();

            if (IsCharacterAlreadyRegistered(targetMap, character.gameObject))
            {
                EnsureCharacterRegenDataForMapSettlement(character);
                ApplyMonsterMapBoundaryOverrides(character);
                RefreshNpcQuestInfoIfNeeded(character);
                return true;
            }

            CharacterBase existingByUid = character.type == CharacterConstants.Type.Npc
                ? _mapTileCommon.GetNpcByUid(character.uid)
                : _mapTileCommon.GetMonsterByUid(character.uid);

            if (existingByUid != null && !ReferenceEquals(existingByUid, character))
            {
                GcLogger.LogWarning(
                    $"맵 정착 등록을 건너뜁니다. 동일 uid 캐릭터가 이미 맵에 존재합니다. type={character.type}, uid={character.uid}");
                return false;
            }

            Transform mapRoot = _mapTileCommon.transform;
            if (character.transform.parent != mapRoot)
            {
                character.transform.SetParent(mapRoot, true);
            }

            EnsureCharacterRegenDataForMapSettlement(character);
            ApplyMonsterMapBoundaryOverrides(character);

            int registrationVid = ResolveRegistrationVid(character);
            character.vid = registrationVid;

            if (character.type == CharacterConstants.Type.Npc)
            {
                _mapTileCommon.AddNpc(registrationVid, character.gameObject);
                RefreshNpcQuestInfoIfNeeded(character);
                return true;
            }

            _mapTileCommon.AddMonster(registrationVid, character.gameObject);
            return true;
        }

        /// <summary>
        /// 맵에 등록되는 몬스터에게 현재 맵의 이동 경계 정책을 적용합니다.
        /// </summary>
        /// <param name="character">맵 등록 대상 캐릭터입니다.</param>
        private void ApplyMonsterMapBoundaryOverrides(CharacterBase character)
        {
            Monster monster = character as Monster;
            if (monster == null)
            {
                return;
            }

            // 런타임 동적 스폰 몬스터도 일반 맵 로드 몬스터와 동일하게 Parallax 경계 해제 정책을 따릅니다.
            monster.ApplyMapBoundaryOverrides(_currentMapTableData);
        }

        public Transform GetCurrentMap()
        {
            return _mapTileCommon != null ? _mapTileCommon.transform : null;
        }

        /// <summary>
        /// 맵 상주 캐릭터 등록을 지원하는 타입인지 확인합니다.
        /// </summary>
        /// <param name="type">검사할 캐릭터 타입입니다.</param>
        /// <returns>Npc 또는 Monster면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsSupportedMapResidentType(CharacterConstants.Type type)
        {
            return type == CharacterConstants.Type.Npc ||
                   type == CharacterConstants.Type.Monster;
        }

        /// <summary>
        /// 등록 대상 캐릭터의 RegenData를 현재 맵 상태 기준으로 보정합니다.
        /// NPC 퀘스트 아이콘, 몬스터 사망 이벤트 등 맵 로직이 필요한 최소 필드를 보장합니다.
        /// </summary>
        /// <param name="character">보정할 캐릭터입니다.</param>
        private void EnsureCharacterRegenDataForMapSettlement(CharacterBase character)
        {
            if (character == null)
            {
                return;
            }

            int mapUid = _currentMapUid;
            Vector3 position = character.transform.position;
            bool defaultVisible = character.gameObject.activeSelf;
            float moveStep = character.GetCurrentMoveStep();
            float moveSpeed = character.GetCurrentMoveSpeed(isPercent: false);

            bool canMoveX = true;
            bool canMoveY = true;

            if (character is Monster monster)
            {
                canMoveX = monster.canMoveX;
                canMoveY = monster.canMoveY;
            }

            if (character.CharacterRegenData == null)
            {
                character.CharacterRegenData = new CharacterRegenData(
                    character.uid,
                    position,
                    character.isFlip,
                    mapUid,
                    defaultVisible,
                    moveStep,
                    moveSpeed,
                    canMoveX,
                    canMoveY,
                    mapVisibilityPolicy: character.MapVisibilityPolicy);
                return;
            }

            CharacterRegenData regenData = character.CharacterRegenData;
            regenData.Uid = character.uid;
            regenData.MapUid = mapUid;
            regenData.x = position.x;
            regenData.y = position.y;
            regenData.z = position.z;
            regenData.IsFlip = character.isFlip;
            regenData.DefaultVisible = defaultVisible;
            regenData.MoveStep = moveStep;
            regenData.MoveSpeed = moveSpeed;
            regenData.CanMoveX = canMoveX;
            regenData.CanMoveY = canMoveY;
            regenData.MapVisibilityPolicy = character.MapVisibilityPolicy;
        }

        /// <summary>
        /// 맵 딕셔너리에서 사용할 캐릭터 VID를 계산합니다.
        /// 기존 VID가 비어 있으면 재사용하고, 충돌하면 다음 가용 값을 할당합니다.
        /// </summary>
        /// <param name="character">등록할 캐릭터입니다.</param>
        /// <returns>등록 가능한 VID 값입니다.</returns>
        private int ResolveRegistrationVid(CharacterBase character)
        {
            int preferredVid = character != null ? character.vid : 0;
            if (preferredVid > 0 && IsCharacterVidAvailable(preferredVid, character != null ? character.gameObject : null))
            {
                return preferredVid;
            }

            return GetNextAvailableCharacterVid();
        }

        /// <summary>
        /// 지정한 VID를 맵 캐릭터 딕셔너리에서 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="vid">확인할 VID입니다.</param>
        /// <param name="ownerObject">현재 등록하려는 캐릭터 오브젝트입니다.</param>
        /// <returns>충돌 없이 사용할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsCharacterVidAvailable(int vid, GameObject ownerObject)
        {
            if (_mapTileCommon == null || vid <= 0)
            {
                return false;
            }

            if (IsVidReservedByAnotherCharacter(_mapTileCommon.GetNpcs(), vid, ownerObject))
            {
                return false;
            }

            if (IsVidReservedByAnotherCharacter(_mapTileCommon.GetMonsters(), vid, ownerObject))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 맵의 NPC/몬스터 딕셔너리를 스캔해 다음으로 사용할 VID를 반환합니다.
        /// </summary>
        /// <returns>현재 맵에서 비어 있는 다음 VID입니다.</returns>
        private int GetNextAvailableCharacterVid()
        {
            if (_mapTileCommon == null)
            {
                return 1;
            }

            int nextVid = 1;

            nextVid = Mathf.Max(nextVid, GetNextVidFromMap(_mapTileCommon.GetNpcs()));
            nextVid = Mathf.Max(nextVid, GetNextVidFromMap(_mapTileCommon.GetMonsters()));

            while (!IsCharacterVidAvailable(nextVid, null))
            {
                nextVid++;
            }

            return nextVid;
        }

        /// <summary>
        /// 딕셔너리 키 기준으로 다음 후보 VID를 계산합니다.
        /// </summary>
        /// <param name="map">VID를 키로 사용하는 캐릭터 맵입니다.</param>
        /// <returns>해당 맵에서 사용할 다음 후보 VID입니다.</returns>
        private static int GetNextVidFromMap(Dictionary<int, GameObject> map)
        {
            if (map == null || map.Count == 0)
            {
                return 1;
            }

            int maxVid = 0;
            foreach (int vid in map.Keys)
            {
                if (vid > maxVid)
                {
                    maxVid = vid;
                }
            }

            return maxVid + 1;
        }

        /// <summary>
        /// 지정한 VID가 다른 오브젝트에 의해 이미 점유됐는지 확인합니다.
        /// </summary>
        /// <param name="map">점유 상태를 확인할 딕셔너리입니다.</param>
        /// <param name="vid">확인할 VID입니다.</param>
        /// <param name="ownerObject">현재 등록하려는 오브젝트입니다.</param>
        /// <returns>다른 오브젝트가 점유 중이면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsVidReservedByAnotherCharacter(
            Dictionary<int, GameObject> map,
            int vid,
            GameObject ownerObject)
        {
            if (map == null || !map.TryGetValue(vid, out GameObject existingObject))
            {
                return false;
            }

            if (existingObject == null)
            {
                return true;
            }

            return !ReferenceEquals(existingObject, ownerObject);
        }

        /// <summary>
        /// 지정 캐릭터가 이미 해당 딕셔너리에 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="map">검사할 캐릭터 딕셔너리입니다.</param>
        /// <param name="characterObject">검사할 캐릭터 오브젝트입니다.</param>
        /// <returns>이미 등록되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsCharacterAlreadyRegistered(
            Dictionary<int, GameObject> map,
            GameObject characterObject)
        {
            if (map == null || characterObject == null)
            {
                return false;
            }

            foreach (GameObject mapCharacter in map.Values)
            {
                if (ReferenceEquals(mapCharacter, characterObject))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// NPC 등록 직후 퀘스트 상태 아이콘 갱신을 즉시 반영합니다.
        /// </summary>
        /// <param name="character">등록된 캐릭터입니다.</param>
        private static void RefreshNpcQuestInfoIfNeeded(CharacterBase character)
        {
            if (character is Npc npc)
            {
                npc.UpdateQuestInfo();
            }
        }

        /// <summary>
        /// 현재 로드된 맵에 배치된 몬스터 항목 목록을 반환합니다.
        /// </summary>
        /// <returns>몬스터 VID와 게임 오브젝트 쌍 목록입니다.</returns>
        public List<KeyValuePair<int, GameObject>> GetCurrentMapMonsterEntries()
        {
            return _mapTileCommon != null
                ? _mapTileCommon.GetMonsterEntries()
                : new List<KeyValuePair<int, GameObject>>();
        }

        /// <summary>
        /// 현재 맵에 살아있는 몬스터 수를 계산합니다.
        /// 비활성화된 몬스터도 사망 상태가 아니면 맵에 남아있는 대상으로 취급합니다.
        /// </summary>
        /// <returns>현재 맵의 살아있는 몬스터 수입니다.</returns>
        public int CountCurrentMapAliveMonsters()
        {
            if (_mapTileCommon == null)
            {
                return 0;
            }

            int count = 0;
            List<KeyValuePair<int, GameObject>> monsterEntries = _mapTileCommon.GetMonsterEntries();
            foreach (KeyValuePair<int, GameObject> entry in monsterEntries)
            {
                GameObject monsterObject = entry.Value;
                if (monsterObject == null)
                {
                    continue;
                }

                Monster monster = monsterObject.GetComponent<Monster>();
                if (monster == null || monster.IsStatusDead())
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        /// <summary>
        /// 현재 맵에 배치된 생존 몬스터 중 지정 위치에서 가장 가까운 몬스터를 검색합니다.
        /// </summary>
        /// <param name="origin">검색 기준 위치입니다.</param>
        /// <param name="includeInactive">Culling 등으로 비활성화된 몬스터를 포함할지 여부입니다.</param>
        /// <param name="maxDistance">검색 최대 거리입니다. 0 이하이면 거리 제한 없이 검색합니다.</param>
        /// <param name="monster">검색된 가장 가까운 생존 몬스터입니다.</param>
        /// <returns>조건에 맞는 몬스터를 찾으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryFindNearestAliveMonster(
            Vector2 origin,
            bool includeInactive,
            float maxDistance,
            out Monster monster)
        {
            monster = null;
            return _mapTileCommon != null &&
                   _mapTileCommon.TryFindNearestAliveMonster(origin, includeInactive, maxDistance, out monster);
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

        /// <summary>
        /// 현재 로드 중이거나 로드된 맵 테이블 데이터를 반환합니다.
        /// 맵 로드 중 자동 이동, 카메라, 맵별 정책을 조회해야 하는 시스템에서 사용합니다.
        /// </summary>
        /// <returns>현재 맵 테이블 데이터입니다. 아직 로드되지 않았으면 null을 반환합니다.</returns>
        public StruckTableMap GetCurrentMapTableData() => _currentMapTableData;

        public bool IsStateComplete()
        {
            return _currentState == MapConstants.State.Complete;
        }

        /// <summary>
        /// 현재 맵의 월드 경계 크기(X, Y)를 반환합니다.
        /// 경계를 계산할 수 없으면 <see cref="Vector2.zero"/>를 반환합니다.
        /// </summary>
        /// <returns>현재 맵의 월드 경계 크기입니다.</returns>
        public Vector2 GetMapSize()
        {
            if (!TryGetCurrentMapWorldBounds(out Bounds bounds))
            {
                return Vector2.zero;
            }

            return new Vector2(bounds.size.x, bounds.size.y);
        }

        /// <summary>
        /// 현재 로드된 맵의 월드 경계(Bounds)를 반환합니다.
        /// </summary>
        /// <param name="bounds">현재 맵의 월드 경계입니다.</param>
        /// <returns>경계 계산에 성공하면 true를 반환합니다.</returns>
        public bool TryGetCurrentMapWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (!HasValidCurrentMap())
            {
                return false;
            }

            return TryGetMapWorldBounds(out bounds);
        }

        /// <summary>
        /// 현재 로드된 맵의 하단 월드 경계값(minY)을 반환합니다.
        /// </summary>
        /// <param name="bottomY">현재 맵 월드 경계의 최하단 Y 값입니다.</param>
        /// <returns>하단 경계 계산에 성공하면 true를 반환합니다.</returns>
        public bool TryGetCurrentMapBottomY(out float bottomY)
        {
            bottomY = 0f;
            if (!TryGetCurrentMapWorldBounds(out Bounds bounds))
            {
                return false;
            }

            bottomY = bounds.min.y;
            return true;
        }

        private bool TryGetMapWorldBounds(out Bounds totalBounds)
        {
            totalBounds = default;
            bool hasBounds = false;

            AppendTilemapBounds(ref totalBounds, ref hasBounds);
            AppendSpriteRendererBounds(ref totalBounds, ref hasBounds);

            return hasBounds;
        }

        private void AppendTilemapBounds(ref Bounds totalBounds, ref bool hasBounds)
        {
            Tilemap[] tilemaps = _mapTileCommon.GetComponentsInChildren<Tilemap>();

            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap == null)
                {
                    continue;
                }

                if (!TryGetTilemapWorldBounds(tilemap, out Bounds tileBounds))
                {
                    continue;
                }

                EncapsulateBounds(ref totalBounds, ref hasBounds, tileBounds);
            }
        }

        private void AppendSpriteRendererBounds(ref Bounds totalBounds, ref bool hasBounds)
        {
            SpriteRenderer[] spriteRenderers = _mapTileCommon.GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer == null || spriteRenderer.sprite == null)
                {
                    continue;
                }

                EncapsulateBounds(ref totalBounds, ref hasBounds, spriteRenderer.bounds);
            }
        }

        private static bool TryGetTilemapWorldBounds(Tilemap tilemap, out Bounds bounds)
        {
            Vector3Int minCell = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int maxCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(pos))
                {
                    continue;
                }

                minCell = Vector3Int.Min(minCell, pos);
                maxCell = Vector3Int.Max(maxCell, pos);
            }

            if (minCell.x == int.MaxValue)
            {
                bounds = default;
                return false;
            }

            Vector3 minWorldPos = tilemap.CellToWorld(minCell);
            Vector3 maxWorldPos = tilemap.CellToWorld(maxCell + Vector3Int.one);

            bounds = new Bounds();
            bounds.SetMinMax(
                Vector3.Min(minWorldPos, maxWorldPos),
                Vector3.Max(minWorldPos, maxWorldPos));

            return true;
        }

        private static void EncapsulateBounds(ref Bounds totalBounds, ref bool hasBounds, Bounds bounds)
        {
            if (!hasBounds)
            {
                totalBounds = bounds;
                hasBounds = true;
                return;
            }

            totalBounds.Encapsulate(bounds.min);
            totalBounds.Encapsulate(bounds.max);
        }

        public GridInformation GetGridInformation()
        {
            return _grid != null ? _grid.GetComponent<GridInformation>() : null;
        }

        public Grid GetGrid()
        {
            return _grid != null ? _grid.GetComponent<Grid>() : null;
        }

        public string GetCurrentMapName()
        {
            return _currentMapTableData != null ? _currentMapTableData.Name : "";
        }
    }
}
