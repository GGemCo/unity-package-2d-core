using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신(Cutscene)의 로드, 준비, 재생, 종료 및 관련 런타임 상태를 관리합니다.
    /// 카메라, 캐릭터, 대사, UI, 시간 배율 등 연출에 필요한 컨트롤러를 생성하고 수명 주기를 조정합니다.
    /// </summary>
    public class CutsceneManager
    {
        /// <summary>
        /// 컷신 진행 상태를 나타냅니다.
        /// </summary>
        private enum State { Idle, Loading, Ready, Playing, Finished }

        private State _currentState;

        private CutsceneData _currentCutscene;
        private float _playTimer;
        private int _currentIndex;
        private DialogueBalloonPool _dialogueBalloonPool;

        // 연출 중 동적으로 생성된 캐릭터 인스턴스를 타입과 UID 기준으로 관리합니다.
        private readonly Dictionary<CharacterConstants.Type, Dictionary<int, GameObject>> _createCharacters =
            new Dictionary<CharacterConstants.Type, Dictionary<int, GameObject>>();

        // OverlayText에서 사용할 런타임 문자열 치환값을 보관합니다.
        private readonly Dictionary<CutsceneKeyTextOverlay, string> _overlayTextOverrides = new();

        // 캐릭터 대상 기반 컷신 이벤트에서 사용할 런타임 캐릭터 치환값을 보관합니다.
        private readonly Dictionary<CutsceneKeyCharacterTarget, CharacterBase> _characterTargetOverrides = new();
        
        // 현재 컷신에서 활성화된 컨트롤러 목록입니다.
        private readonly List<ICutsceneController> _activeControllers = new();

        // 컷신 타임라인 진행을 일시 정지시킨 요청자를 관리합니다.
        private readonly HashSet<object> _timelineProgressWaitOwners = new();
        
        private CameraMoveController _cameraMoveController;
        private CameraZoomController _cameraZoomController;
        private CameraShakeController _cameraShakeController;
        private CameraChangeTargetController _cameraChangeTargetController;
        
        private CharacterMoveController _characterMoveController;
        private CharacterAnimationController _characterAnimationController;
        
        private DialogueBalloonController _dialogueBalloonController;
        private ScreenFadeController _screenFadeController;
        private OverlayTextController _overlayTextController;
        private CharacterWhiteOverlayController _characterWhiteOverlayController;
        private UiPanelController _uiPanelController;
        private UiWindowVisibilityController _uiWindowVisibilityController;
        private TimeScaleController _timeScaleController;
        private SceneGame _sceneGame;
        private CutsceneOverlayPresenter _overlayPresenter;
        private CutsceneUiPanelPresenter _uiPanelPresenter;
        private ScreenFadePresenter _screenFadePresenter;

        private bool _hasCapturedTimeScaleState;
        private float _capturedTimeScale;
        private float _capturedFixedDeltaTime;
        private TimeScaleController _activeTimeScaleOwner;
        private bool _useUnscaledTimelineTime;

        // 캐릭터별 원본 애니메이션 TimeScale을 복원하기 위해 저장합니다.
        private readonly Dictionary<string, float> _capturedCharacterAnimationTimeScales = new(StringComparer.Ordinal);

        private GameObject _testTool;
        private bool _testToolActive;
        private AddressableLoaderSettings _settings;
        private bool _isCutsceneSessionActive;

        /// <summary>
        /// 컷신 세션이 시작되어 외부 시스템이 연출 상태를 반영해야 할 때 발생합니다.
        /// 로딩 또는 준비 단계에서 숨겨야 하는 UI가 있으므로 실제 Playing 전에도 호출될 수 있습니다.
        /// </summary>
        public event Action CutsceneStarted;

        /// <summary>
        /// 컷신 세션이 종료되어 외부 시스템이 연출 전 상태로 복원해야 할 때 발생합니다.
        /// 정상 종료, 로드 실패, 매니저 파괴 경로에서 모두 호출될 수 있습니다.
        /// </summary>
        public event Action CutsceneEnded;
        
        /// <summary>
        /// 컷신 매니저를 초기화하고 런타임 상태 및 참조를 기본값으로 되돌립니다.
        /// 대사 말풍선 풀과 테스트 도구 상태도 함께 준비합니다.
        /// </summary>
        /// <param name="scene">컷신이 동작할 현재 게임 씬 컨텍스트입니다.</param>
        public void Initialize(SceneGame scene)
        {
            _sceneGame = scene;
            _createCharacters.Clear();
            _overlayTextOverrides.Clear();
            _characterTargetOverrides.Clear();
            _playTimer = 0f;
            _currentIndex = 0;
            _currentState = State.Idle;
            _hasCapturedTimeScaleState = false;
            _capturedTimeScale = 1f;
            _capturedFixedDeltaTime = 0.02f;
            _activeTimeScaleOwner = null;
            _useUnscaledTimelineTime = false;
            _capturedCharacterAnimationTimeScales.Clear();
            _timelineProgressWaitOwners.Clear();
            _isCutsceneSessionActive = false;
            _settings = AddressableLoaderSettings.Instance;
            
            // 기존 컨트롤러 초기화 이후
            if (_sceneGame.containerDialogueBalloon)
            {
                _dialogueBalloonPool = new DialogueBalloonPool(_sceneGame.containerDialogueBalloon.transform);
            }

            _testTool = GameObject.Find("_TestTool");
            if (_testTool != null)
            {
                _testToolActive = _testTool.activeSelf;
            }
        }

        /// <summary>
        /// 현재 컷신이 재생 중인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 <see cref="State.Playing"/>이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsPlaying() => _currentState == State.Playing;

        /// <summary>
        /// 컷신 세션이 외부 UI를 연출 상태로 전환해야 하는 활성 구간인지 확인합니다.
        /// 로딩, 준비, 재생을 하나의 세션으로 보며 정상 종료 또는 실패 복구 시 비활성화됩니다.
        /// </summary>
        /// <returns>컷신 세션이 활성 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsSessionActive() => _isCutsceneSessionActive;

        /// <summary>
        /// Overlay 연출을 위한 Presenter를 반환하거나, 없으면 생성하여 초기화합니다.
        /// </summary>
        /// <returns>생성되었거나 기존에 존재하던 <see cref="CutsceneOverlayPresenter"/>입니다. 생성에 필요한 Canvas가 없으면 <see langword="null"/>을 반환합니다.</returns>
        public CutsceneOverlayPresenter GetOrCreateOverlayPresenter()
        {
            if (_overlayPresenter != null)
            {
                return _overlayPresenter;
            }

            var canvas = _sceneGame != null ? _sceneGame.canvasUI : null;
            if (canvas == null)
            {
                GcLogger.LogError("Cutscene overlay presenter를 만들기 위한 Canvas UI가 없습니다.");
                return null;
            }

            var root = new GameObject("CutsceneOverlayPresenter", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _overlayPresenter = root.AddComponent<CutsceneOverlayPresenter>();
            _overlayPresenter.Initialize();
            return _overlayPresenter;
        }

        /// <summary>
        /// 컷신 전용 UI 패널 Presenter를 반환하거나, 없으면 생성하여 초기화합니다.
        /// </summary>
        /// <returns>생성되었거나 기존에 존재하던 <see cref="CutsceneUiPanelPresenter"/>입니다. 생성에 필요한 Canvas가 없으면 <see langword="null"/>을 반환합니다.</returns>
        public CutsceneUiPanelPresenter GetOrCreateUiPanelPresenter()
        {
            if (_uiPanelPresenter != null)
            {
                return _uiPanelPresenter;
            }

            var canvas = _sceneGame != null ? _sceneGame.canvasUI : null;
            if (canvas == null)
            {
                GcLogger.LogError("Cutscene UI Panel presenter를 만들기 위한 Canvas UI가 없습니다.");
                return null;
            }

            var root = new GameObject("CutsceneUiPanelPresenter", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _uiPanelPresenter = root.AddComponent<CutsceneUiPanelPresenter>();
            _uiPanelPresenter.Initialize();
            return _uiPanelPresenter;
        }

        /// <summary>
        /// 화면 페이드 연출용 Presenter를 반환하거나, 없으면 생성한 뒤 전달된 데이터로 초기화합니다.
        /// </summary>
        /// <param name="data">초기화에 사용할 화면 페이드 설정 데이터입니다.</param>
        /// <returns>초기화된 <see cref="ScreenFadePresenter"/>입니다. 씬 참조가 없으면 <see langword="null"/>을 반환합니다.</returns>
        public ScreenFadePresenter GetOrCreateScreenFadePresenter(ScreenFadeData data)
        {
            if (_sceneGame == null)
            {
                GcLogger.LogError("Screen Fade Presenter를 만들기 위한 SceneGame 참조가 없습니다.");
                return null;
            }

            if (_screenFadePresenter == null)
            {
                var root = new GameObject("ScreenFadePresenter", typeof(RectTransform), typeof(Canvas));
                root.transform.SetParent(_sceneGame.transform, false);

                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _screenFadePresenter = root.AddComponent<ScreenFadePresenter>();
            }

            _screenFadePresenter.Initialize(data, _sceneGame);
            return _screenFadePresenter;
        }

        /// <summary>
        /// 현재 컷신 재생에 사용되는 내부 상태를 초기화합니다.
        /// 로드된 데이터 참조는 유지한 채 재생 진행 정보와 복원용 상태만 재설정합니다.
        /// </summary>
        private void Reset()
        {
            _createCharacters.Clear();
            _playTimer = 0f;
            _currentIndex = 0;
            _hasCapturedTimeScaleState = false;
            _activeTimeScaleOwner = null;
            _useUnscaledTimelineTime = false;
            _capturedCharacterAnimationTimeScales.Clear();
            _timelineProgressWaitOwners.Clear();
        }

        /// <summary>
        /// 컷신 데이터를 재생하기 전 로딩 세션을 시작하고 이전 재생 진행 상태를 초기화합니다.
        /// </summary>
        private void BeginCutsceneLoading()
        {
            Reset();
            _currentState = State.Loading;
            BeginCutsceneSession();
        }

        /// <summary>
        /// 현재 할당된 컷신 데이터를 씬 환경에 준비한 뒤 즉시 재생 또는 준비 코루틴을 시작합니다.
        /// </summary>
        private void PlayCurrentCutscene()
        {
            if (_currentCutscene == null)
            {
                GcLogger.LogError("재생할 연출 데이터가 없습니다.");
                FailCutsceneSession();
                return;
            }

            if (_sceneGame == null || _sceneGame.mapManager == null)
            {
                GcLogger.LogError("연출을 재생할 게임 씬 정보를 찾지 못했습니다.");
                FailCutsceneSession();
                return;
            }

            // 모든 캐릭터 활성화, 컬링 적용되지 않음
            _sceneGame.mapManager.ActiveAllCharacters();

            if (_testTool)
            {
                _testTool.SetActive(false);
            }

            // 즉시 준비 가능한 컷신은 현재 프레임에 바로 재생을 시작합니다.
            if (!TryPrepareAndPlayImmediate())
            {
                _sceneGame.StartCoroutine(PrepareAndPlay());
            }
        }

        /// <summary>
        /// 지정한 UID의 컷신을 재생합니다.
        /// 프리로드된 컷신은 즉시 준비하고, 프리로드되지 않은 컷신은 비동기 로드 후 재생합니다.
        /// </summary>
        /// <param name="uid">재생할 컷신 테이블 UID입니다.</param>
        public void PlayCutscene(int uid)
        {
            var info = TableLoaderManager.Instance.GetCutsceneData(uid);
            if (info == null)
            {
                return;
            }

            if (!info.PreLoad)
            {
                _ = PlayCutsceneAsync(uid);
                return;
            }
            string key = $"{ConfigAddressableKey.Cutscene}_{info.Uid}";
            _currentCutscene = AddressableLoaderCutscene.Instance.GetCutsceneDataByKey(key);
            if (GcLogger.IsNull(_currentCutscene, $"{nameof(TableCutscene)} 테이블에 정보가 없습니다. Uid: {info.Uid}")) return;

            BeginCutsceneLoading();
            PlayCurrentCutscene();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor 컷신 프리뷰에서 파일로부터 새로 읽은 컷신 데이터를 Addressables 캐시 없이 재생합니다.
        /// </summary>
        /// <param name="cutsceneData">Editor 툴이 최신 JSON 파일에서 역직렬화한 컷신 데이터입니다.</param>
        public void PlayCutsceneForEditorPreview(CutsceneData cutsceneData)
        {
            if (cutsceneData == null)
            {
                GcLogger.LogError("재생할 연출 데이터가 없습니다.");
                return;
            }

            _currentCutscene = cutsceneData;
            BeginCutsceneLoading();
            PlayCurrentCutscene();
        }
#endif
        
        /// <summary>
        /// 지정한 UID의 컷신 데이터를 로드하고 재생 준비를 시작합니다.
        /// Addressable에서 JSON 에셋을 불러온 뒤 파싱하여 준비 코루틴을 실행합니다.
        /// </summary>
        /// <param name="uid">재생할 컷신의 고유 식별자입니다.</param>
        /// <returns>컷신 로드 및 준비 시작이 완료될 때까지 비동기로 대기하는 작업입니다.</returns>
        /// <exception cref="Exception">컷신 로드 또는 파싱 과정에서 예외가 발생할 수 있으며, 내부에서 로그를 남깁니다.</exception>
        private async Task PlayCutsceneAsync(int uid)
        {
            try
            {
                var info = TableLoaderManager.Instance.GetCutsceneData(uid);
                if (info == null)
                {
                    return;
                }

                BeginCutsceneLoading();

                string key = $"{ConfigAddressableKey.Cutscene}_{info.Uid}";
                TextAsset asset = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);
            
                if (asset == null)
                {
                    GcLogger.LogError("연출 json 파일이 없습니다. " + info.FileName);
                    FailCutsceneSession();
                    return;
                }

                // json 파싱하기
                _currentCutscene = JsonConvert.DeserializeObject<CutsceneData>(asset.text);
                if (_currentCutscene == null)
                {
                    GcLogger.LogError("연출 json 파일을 파싱하지 못했습니다. " + info.FileName);
                    FailCutsceneSession();
                    return;
                }

                PlayCurrentCutscene();
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
                FailCutsceneSession();
            }
        }

        /// <summary>
        /// 컷신 로드 또는 준비 실패 시 내부 상태를 종료 상태로 전환하고 세션 종료 이벤트를 발행합니다.
        /// 실패 경로에서도 외부 UI가 컷신 중 숨김 상태로 남지 않도록 합니다.
        /// </summary>
        private void FailCutsceneSession()
        {
            _currentState = State.Finished;
            EndCutsceneSession();
        }

        /// <summary>
        /// 컷신 세션 시작 상태를 기록하고 시작 이벤트를 한 번만 발행합니다.
        /// 외부 UI는 이 이벤트를 기준으로 조작 UI, HUD 등을 연출 중 상태로 전환합니다.
        /// </summary>
        private void BeginCutsceneSession()
        {
            if (_isCutsceneSessionActive)
            {
                return;
            }

            _isCutsceneSessionActive = true;
            CutsceneStarted?.Invoke();
        }

        /// <summary>
        /// 컷신 세션 종료 상태를 기록하고 종료 이벤트를 한 번만 발행합니다.
        /// 로드 실패나 매니저 파괴처럼 정상 재생 완료가 아닌 경로에서도 외부 UI가 복원되도록 보장합니다.
        /// </summary>
        private void EndCutsceneSession()
        {
            if (!_isCutsceneSessionActive)
            {
                return;
            }

            _timelineProgressWaitOwners.Clear();
            _isCutsceneSessionActive = false;
            CutsceneEnded?.Invoke();
        }

        /// <summary>
        /// 컷신의 모든 이벤트 컨트롤러를 생성하고 사전 준비를 수행한 뒤 재생 상태로 전환합니다.
        /// </summary>
        /// <returns>각 컨트롤러의 준비가 완료될 때까지 순차적으로 진행하는 코루틴입니다.</returns>
        private IEnumerator PrepareAndPlay()
        {
            _currentState = State.Ready;

            foreach (var cutsceneEvent in _currentCutscene.events)
            {
                var controller = CreateController(cutsceneEvent.type);
                if (controller == null) continue;

                cutsceneEvent.Controller = controller;
                _activeControllers.Add(controller);
                yield return _sceneGame.StartCoroutine(controller.Ready(cutsceneEvent));
            }

            _currentState = State.Playing;
        }
        
        /// <summary>
        /// 현재 컷신의 모든 이벤트가 즉시 준비 가능한 경우 같은 프레임에 재생을 시작합니다.
        /// 하나라도 비동기 준비가 필요하면 <see langword="false"/>를 반환합니다.
        /// </summary>
        private bool TryPrepareAndPlayImmediate()
        {
            if (_currentCutscene == null)
            {
                return false;
            }

            _currentState = State.Ready;
            _activeControllers.Clear();

            for (int i = 0; i < _currentCutscene.events.Count; i++)
            {
                var cutsceneEvent = _currentCutscene.events[i];
                var controller = CreateController(cutsceneEvent.type);
                if (controller == null)
                {
                    continue;
                }

                if (!controller.SupportsImmediateReady)
                {
                    _activeControllers.Clear();
                    return false;
                }

                cutsceneEvent.Controller = controller;
                _activeControllers.Add(controller);
                controller.ReadyImmediate(cutsceneEvent);
            }

            StartPlaybackImmediate();
            return true;
        }
        
        
        /// <summary>
        /// 컷신 재생 상태를 즉시 시작 상태로 전환하고 0초 이벤트를 현재 프레임에서 바로 실행합니다.
        /// </summary>
        private void StartPlaybackImmediate()
        {
            _playTimer = 0f;
            _currentIndex = 0;
            _currentState = State.Playing;

            TriggerDueEvents();
        }
        
        /// <summary>
        /// 현재 재생 중인 컷신의 타임라인을 진행시키고, 실행 시점에 도달한 이벤트를 트리거합니다.
        /// 활성 컨트롤러의 프레임 업데이트도 함께 수행합니다.
        /// </summary>
        public void Update()
        {
            if (_currentState != State.Playing || _currentCutscene == null) return;

            if (!IsTimelineProgressWaiting())
            {
                _playTimer += GetTimelineDeltaTime();
                TriggerDueEvents();
            }

            foreach (var controller in _activeControllers)
            {
                controller.Update();
            }

            if (IsTimelineProgressWaiting()) return;
            if (!(_playTimer > _currentCutscene.duration)) return;
            OnCutsceneEnd();
        }

        /// <summary>
        /// 현재 재생 시간 이하인 이벤트를 모두 실행합니다.
        /// </summary>
        private void TriggerDueEvents()
        {
            while (_currentIndex < _currentCutscene.events.Count &&
                   _currentCutscene.events[_currentIndex].time <= _playTimer)
            {
                var evt = _currentCutscene.events[_currentIndex];
                evt.Controller?.Trigger(evt);
                _currentIndex++;

                if (IsTimelineProgressWaiting())
                {
                    break;
                }
            }
        }
        
        /// <summary>
        /// 컷신 종료 시 호출되어 컨트롤러, 생성 객체, UI 상태 및 카메라 상태를 정리하고 복원합니다.
        /// </summary>
        private void OnCutsceneEnd()
        {
            _currentState = State.Finished;

            ForceRestoreCharacterAnimationTimeScale();

            foreach (var controller in _activeControllers)
            {
                controller.End();
            }

            _activeControllers.Clear();

            // 만들었던 캐릭터 지우기
            foreach (var dic1 in _createCharacters)
            {
                foreach (var dic2 in dic1.Value)
                {
                    Object.Destroy(dic2.Value);
                }
            }
            
            ClearOverlayTextOverrides();
            _overlayPresenter?.ResetPresentation();
            _uiPanelPresenter?.ResetPresentation();
            _screenFadePresenter?.ResetPresentation();

            // 원래 카메라로 되돌리기
            SceneGame.Instance.cameraManager?.ReSetByCutscene();

            if (_testTool)
            {
                _testTool.SetActive(_testToolActive);
            }

            EndCutsceneSession();
        }

        /// <summary>
        /// 컷신 중 동적으로 생성한 캐릭터를 추적 목록에 등록합니다.
        /// </summary>
        /// <param name="type">캐릭터 분류 타입입니다.</param>
        /// <param name="characterUid">캐릭터의 고유 식별자입니다.</param>
        /// <param name="character">등록할 캐릭터 게임 오브젝트입니다.</param>
        public void AddCharacter(CharacterConstants.Type type, int characterUid, GameObject character)
        {
            if (!_createCharacters.ContainsKey(type))
            {
                _createCharacters.Add(type, new Dictionary<int, GameObject>());
            }

            _createCharacters[type].Add(characterUid, character);
        }

        /// <summary>
        /// 컷신 중 생성된 캐릭터 목록에서 대상 캐릭터의 Transform을 조회합니다.
        /// </summary>
        /// <param name="type">조회할 캐릭터 분류 타입입니다.</param>
        /// <param name="characterUid">조회할 캐릭터의 고유 식별자입니다.</param>
        /// <returns>해당 캐릭터가 존재하면 Transform을 반환하고, 없으면 <see langword="null"/>을 반환합니다.</returns>
        public Transform GetCharacter(CharacterConstants.Type type, int characterUid)
        {
            return _createCharacters.GetValueOrDefault(type)?.GetValueOrDefault(characterUid)?.transform;
        }

        /// <summary>
        /// OverlayText에서 사용할 치환 문자열을 등록하거나 갱신합니다.
        /// </summary>
        /// <param name="key">치환 항목을 식별하는 키입니다.</param>
        /// <param name="text">적용할 텍스트입니다. <see langword="null"/>이면 빈 문자열로 저장됩니다.</param>
        public void SetOverlayTextOverride(CutsceneKeyTextOverlay key, string text)
        {
            if (key == CutsceneKeyTextOverlay.None)
            {
                return;
            }

            _overlayTextOverrides[key] = text ?? string.Empty;
        }

        /// <summary>
        /// 등록된 OverlayText 치환 문자열을 조회합니다.
        /// </summary>
        /// <param name="key">조회할 치환 키입니다.</param>
        /// <param name="text">키가 존재할 경우 대응되는 치환 문자열을 반환합니다.</param>
        /// <returns>치환 문자열을 찾았으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        public bool TryGetOverlayTextOverride(CutsceneKeyTextOverlay key, out string text)
        {
            text = string.Empty;
            if (key == CutsceneKeyTextOverlay.None)
            {
                return false;
            }

            return _overlayTextOverrides.TryGetValue(key, out text);
        }

        /// <summary>
        /// 등록된 OverlayText 치환 문자열을 제거합니다.
        /// </summary>
        /// <param name="key">제거할 치환 키입니다.</param>
        public void RemoveOverlayTextOverride(CutsceneKeyTextOverlay key)
        {
            if (key == CutsceneKeyTextOverlay.None)
            {
                return;
            }

            _overlayTextOverrides.Remove(key);
        }

        /// <summary>
        /// 등록된 모든 OverlayText 치환 문자열을 제거합니다.
        /// </summary>
        public void ClearOverlayTextOverrides()
        {
            _overlayTextOverrides.Clear();
        }

        /// <summary>
        /// 캐릭터 대상 기반 컷신 이벤트에서 사용할 런타임 캐릭터를 등록하거나 갱신합니다.
        /// </summary>
        /// <param name="key">치환 항목을 식별하는 키입니다.</param>
        /// <param name="character">적용할 캐릭터입니다. <see langword="null"/>이면 등록을 제거합니다.</param>
        public void SetCharacterTargetOverride(CutsceneKeyCharacterTarget key, CharacterBase character)
        {
            if (key == CutsceneKeyCharacterTarget.None)
            {
                return;
            }

            if (character == null)
            {
                _characterTargetOverrides.Remove(key);
                return;
            }

            _characterTargetOverrides[key] = character;
        }

        /// <summary>
        /// 등록된 런타임 캐릭터 치환값을 조회합니다.
        /// </summary>
        /// <param name="key">조회할 치환 키입니다.</param>
        /// <param name="character">키가 존재할 경우 대응되는 캐릭터를 반환합니다.</param>
        /// <returns>치환 캐릭터를 찾았으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        public bool TryGetCharacterTargetOverride(CutsceneKeyCharacterTarget key, out CharacterBase character)
        {
            character = null;
            if (key == CutsceneKeyCharacterTarget.None)
            {
                return false;
            }

            return _characterTargetOverrides.TryGetValue(key, out character) && character != null;
        }

        /// <summary>
        /// 등록된 런타임 캐릭터 치환값을 제거합니다.
        /// </summary>
        /// <param name="key">제거할 치환 키입니다.</param>
        public void RemoveCharacterTargetOverride(CutsceneKeyCharacterTarget key)
        {
            if (key == CutsceneKeyCharacterTarget.None)
            {
                return;
            }

            _characterTargetOverrides.Remove(key);
        }

        /// <summary>
        /// 등록된 모든 런타임 캐릭터 치환값을 제거합니다.
        /// </summary>
        public void ClearCharacterTargetOverrides()
        {
            _characterTargetOverrides.Clear();
        }

        /// <summary>
        /// 캐릭터 애니메이션 TimeScale 저장용 키를 생성합니다.
        /// </summary>
        /// <param name="characterType">캐릭터 분류 타입입니다.</param>
        /// <param name="characterUid">캐릭터의 고유 식별자입니다.</param>
        /// <returns>타입과 UID를 결합한 고유 문자열 키입니다.</returns>
        public string BuildCharacterAnimationTimeScaleKey(CharacterConstants.Type characterType, int characterUid)
        {
            return $"{(int)characterType}:{characterUid}";
        }

        /// <summary>
        /// 저장된 캐릭터 애니메이션 TimeScale 값을 조회합니다.
        /// </summary>
        /// <param name="characterType">조회할 캐릭터 분류 타입입니다.</param>
        /// <param name="characterUid">조회할 캐릭터의 고유 식별자입니다.</param>
        /// <param name="timeScale">저장된 원본 TimeScale 값입니다.</param>
        /// <returns>저장된 값이 존재하면 <see langword="true"/>, 없으면 <see langword="false"/>를 반환합니다.</returns>
        public bool TryGetCapturedCharacterAnimationTimeScale(CharacterConstants.Type characterType, int characterUid, out float timeScale)
        {
            return _capturedCharacterAnimationTimeScales.TryGetValue(BuildCharacterAnimationTimeScaleKey(characterType, characterUid), out timeScale);
        }

        /// <summary>
        /// 캐릭터의 현재 애니메이션 TimeScale 값을 복원용 상태로 저장합니다.
        /// </summary>
        /// <param name="characterType">저장할 캐릭터 분류 타입입니다.</param>
        /// <param name="characterUid">저장할 캐릭터의 고유 식별자입니다.</param>
        /// <param name="timeScale">저장할 TimeScale 값입니다. 음수는 0으로 보정됩니다.</param>
        public void CaptureCharacterAnimationTimeScale(CharacterConstants.Type characterType, int characterUid, float timeScale)
        {
            _capturedCharacterAnimationTimeScales[BuildCharacterAnimationTimeScaleKey(characterType, characterUid)] = Mathf.Max(0f, timeScale);
        }

        /// <summary>
        /// 컷신 타임라인 진행에 사용할 델타 타임을 반환합니다.
        /// </summary>
        /// <returns>비율 무시 모드이면 <see cref="Time.unscaledDeltaTime"/>, 아니면 <see cref="Time.deltaTime"/>를 반환합니다.</returns>
        public float GetTimelineDeltaTime()
        {
            return _useUnscaledTimelineTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        /// <summary>
        /// 지정한 요청자가 컷신 타임라인 진행을 일시 대기하도록 등록합니다.
        /// 타임라인 시간만 멈추며, 이미 실행 중인 컨트롤러의 갱신은 계속 수행됩니다.
        /// </summary>
        /// <param name="owner">대기 요청을 소유하는 컨트롤러 또는 객체입니다.</param>
        public void RequestTimelineProgressWait(object owner)
        {
            if (owner == null)
            {
                return;
            }

            _timelineProgressWaitOwners.Add(owner);
        }

        /// <summary>
        /// 지정한 요청자가 등록했던 컷신 타임라인 진행 대기를 해제합니다.
        /// 모든 요청자가 해제되면 다음 프레임부터 타임라인 시간이 다시 흐릅니다.
        /// </summary>
        /// <param name="owner">대기 해제를 요청하는 컨트롤러 또는 객체입니다.</param>
        public void ReleaseTimelineProgressWait(object owner)
        {
            if (owner == null)
            {
                return;
            }

            _timelineProgressWaitOwners.Remove(owner);
        }

        /// <summary>
        /// 현재 컷신 타임라인 진행을 대기시키는 요청자가 있는지 확인합니다.
        /// </summary>
        /// <returns>하나 이상의 대기 요청자가 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsTimelineProgressWaiting()
        {
            return _timelineProgressWaitOwners.Count > 0;
        }

        /// <summary>
        /// 현재 TimeScale 컨트롤러가 타임라인에 사용할 시간 기준을 설정합니다.
        /// </summary>
        /// <param name="controller">설정을 요청한 TimeScale 컨트롤러입니다.</param>
        /// <param name="useUnscaledTimelineTime"><see langword="true"/>이면 비율 무시 시간 기준을 사용합니다.</param>
        public void SetTimeScaleTimelineMode(TimeScaleController controller, bool useUnscaledTimelineTime)
        {
            if (controller == null)
            {
                return;
            }

            if (_activeTimeScaleOwner != null && !ReferenceEquals(_activeTimeScaleOwner, controller))
            {
                return;
            }

            _useUnscaledTimelineTime = useUnscaledTimelineTime;
        }

        /// <summary>
        /// TimeScale 변경의 소유권을 등록하고, 최초 1회 원본 시간 관련 상태를 저장합니다.
        /// </summary>
        /// <param name="controller">현재 TimeScale 제어를 담당할 컨트롤러입니다.</param>
        public void RegisterTimeScaleOwner(TimeScaleController controller)
        {
            if (controller == null)
            {
                return;
            }

            if (!_hasCapturedTimeScaleState)
            {
                _capturedTimeScale = Time.timeScale;
                _capturedFixedDeltaTime = Time.fixedDeltaTime;
                _hasCapturedTimeScaleState = true;
            }

            _activeTimeScaleOwner = controller;
            _useUnscaledTimelineTime = false;
        }

        /// <summary>
        /// 저장된 원본 TimeScale 상태를 조회합니다.
        /// </summary>
        /// <param name="timeScale">저장된 원본 <see cref="Time.timeScale"/> 값입니다.</param>
        /// <param name="fixedDeltaTime">저장된 원본 <see cref="Time.fixedDeltaTime"/> 값입니다.</param>
        /// <returns>저장된 원본 상태가 있으면 <see langword="true"/>, 없으면 현재 엔진 값을 반환하며 <see langword="false"/>를 반환합니다.</returns>
        public bool TryGetCapturedTimeScaleState(out float timeScale, out float fixedDeltaTime)
        {
            if (_hasCapturedTimeScaleState)
            {
                timeScale = _capturedTimeScale;
                fixedDeltaTime = _capturedFixedDeltaTime;
                return true;
            }

            timeScale = Time.timeScale;
            fixedDeltaTime = Time.fixedDeltaTime;
            return false;
        }

        /// <summary>
        /// 전달된 컨트롤러가 현재 TimeScale 소유자인지 확인합니다.
        /// </summary>
        /// <param name="controller">확인할 TimeScale 컨트롤러입니다.</param>
        /// <returns>현재 등록된 소유자와 동일한 인스턴스이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsActiveTimeScaleOwner(TimeScaleController controller)
        {
            return controller != null && ReferenceEquals(_activeTimeScaleOwner, controller);
        }

        /// <summary>
        /// 현재 TimeScale 소유권을 해제하고 저장된 상태 플래그를 초기화합니다.
        /// 다른 소유자가 활성 상태인 경우에는 해제하지 않습니다.
        /// </summary>
        /// <param name="controller">해제를 요청한 TimeScale 컨트롤러입니다.</param>
        public void ClearTimeScaleOwner(TimeScaleController controller)
        {
            if (controller == null)
            {
                return;
            }

            if (_activeTimeScaleOwner != null && !ReferenceEquals(_activeTimeScaleOwner, controller))
            {
                return;
            }

            _activeTimeScaleOwner = null;
            _hasCapturedTimeScaleState = false;
            _useUnscaledTimelineTime = false;
        }

        /// <summary>
        /// 이벤트 타입에 맞는 컷신 컨트롤러 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="type">생성할 컨트롤러의 컷신 이벤트 타입입니다.</param>
        /// <returns>이벤트 타입에 대응되는 컨트롤러입니다. 지원하지 않는 타입이면 <see langword="null"/>을 반환합니다.</returns>
        private ICutsceneController CreateController(CutsceneEventType type)
        {
            return type switch
            {
                CutsceneEventType.CameraMove => new CameraMoveController(this),
                CutsceneEventType.CameraZoom => new CameraZoomController(this),
                CutsceneEventType.CameraShake => new CameraShakeController(this),
                CutsceneEventType.CameraChangeTarget => new CameraChangeTargetController(this),

                CutsceneEventType.CharacterMove => new CharacterMoveController(this),
                CutsceneEventType.CharacterAnimation => new CharacterAnimationController(this),
                CutsceneEventType.CharacterAnimationTimeScale => new CharacterAnimationTimeScaleController(this),

                CutsceneEventType.DialogueBalloon => new DialogueBalloonController(this, _dialogueBalloonPool),
                CutsceneEventType.ScreenFade => new ScreenFadeController(this),
                CutsceneEventType.OverlayText => new OverlayTextController(this),
                CutsceneEventType.CharacterWhiteOverlay => new CharacterWhiteOverlayController(this, _settings),
                CutsceneEventType.UiPanel => new UiPanelController(this),
                CutsceneEventType.UiWindowVisibility => new UiWindowVisibilityController(this),
                CutsceneEventType.TimeScale => new TimeScaleController(this),
                CutsceneEventType.WorldObjectVisibility => new WorldObjectVisibilityController(this),
                CutsceneEventType.CharacterControlLock => new CharacterControlLockController(this),
                CutsceneEventType.ScreenGlitch => new ScreenGlitchController(this),

                _ => null,
            };
        }

        /// <summary>
        /// 활성화된 모든 TimeScale 컨트롤러에 원본 시간 상태 복원을 강제합니다.
        /// </summary>
        private void ForceRestoreTimeScale()
        {
            foreach (var controller in _activeControllers)
            {
                if (controller is TimeScaleController timeScaleController)
                {
                    timeScaleController.ForceRestoreOriginalState();
                }
            }
        }

        /// <summary>
        /// 활성화된 모든 캐릭터 애니메이션 TimeScale 컨트롤러에 원본 상태 복원을 강제합니다.
        /// </summary>
        private void ForceRestoreCharacterAnimationTimeScale()
        {
            foreach (var controller in _activeControllers)
            {
                if (controller is CharacterAnimationTimeScaleController characterAnimationTimeScaleController)
                {
                    characterAnimationTimeScaleController.ForceRestoreOriginalState();
                }
            }
        }

        /// <summary>
        /// 매니저가 파괴될 때 활성 컨트롤러, 생성 오브젝트, Presenter 및 복원 상태를 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            ForceRestoreTimeScale();
            ForceRestoreCharacterAnimationTimeScale();

            foreach (var controller in _activeControllers)
            {
                controller.End();
            }

            _activeControllers.Clear();
            
            // 만들었던 캐릭터 지우기
            foreach (var dic1 in _createCharacters)
            {
                foreach (var dic2 in dic1.Value)
                {
                    Object.Destroy(dic2.Value);
                }
            }

            ClearOverlayTextOverrides();
            ClearCharacterTargetOverrides();

            if (_overlayPresenter != null)
            {
                Object.Destroy(_overlayPresenter.gameObject);
                _overlayPresenter = null;
            }

            if (_uiPanelPresenter != null)
            {
                Object.Destroy(_uiPanelPresenter.gameObject);
                _uiPanelPresenter = null;
            }

            if (_screenFadePresenter != null)
            {
                Object.Destroy(_screenFadePresenter.gameObject);
                _screenFadePresenter = null;
            }

            EndCutsceneSession();
        }
    }
}
