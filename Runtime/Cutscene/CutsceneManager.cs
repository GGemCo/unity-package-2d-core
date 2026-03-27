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
    /// 연출 매니저
    /// </summary>
    public class CutsceneManager
    {
        private enum State { Idle, Loading, Ready, Playing, Finished }
        private State _currentState;

        private CutsceneData _currentCutscene;
        private float _playTimer;
        private int _currentIndex;
        private float _originalOrthographicSize;
        private DialogueBalloonPool _dialogueBalloonPool;

        // 연출중 생성된 캐릭터 관리
        private readonly Dictionary<CharacterConstants.Type, Dictionary<int, GameObject>> _createCharacters =
            new Dictionary<CharacterConstants.Type, Dictionary<int, GameObject>>();

        // OverlayText 런타임 문자열 치환값
        private readonly Dictionary<string, string> _overlayTextOverrides = new(StringComparer.Ordinal);
        
        // 연출 컨트롤러
        private readonly List<ICutsceneController> _activeControllers = new();
        
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
        private SceneGame _sceneGame;
        private CutsceneOverlayPresenter _overlayPresenter;
        private CutsceneUiPanelPresenter _uiPanelPresenter;
        private ScreenFadePresenter _screenFadePresenter;
        
        public void Initialize(SceneGame scene)
        {
            _sceneGame = scene;
            _createCharacters.Clear();
            _overlayTextOverrides.Clear();
            _playTimer = 0f;
            _currentIndex = 0;
            _currentState = State.Idle;
            
            // 기존 컨트롤러 초기화 이후
            if (_sceneGame.containerDialogueBalloon)
            {
                _dialogueBalloonPool = new DialogueBalloonPool(_sceneGame.containerDialogueBalloon.transform); // 부모는 선택
            }
        }
        public bool IsPlaying() => _currentState == State.Playing;

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
        /// 초기화
        /// </summary>
        private void Reset()
        {
            _createCharacters.Clear();
            _playTimer = 0f;
            _currentIndex = 0;
        }
        /// <summary>
        /// 연출 플레이
        /// </summary>
        /// <param name="uid"></param>
        public async Task PlayCutscene(int uid)
        {
            try
            {
                var info = TableLoaderManager.Instance.GetCutsceneData(uid);
                if (info == null)
                {
                    return;
                }
                Reset();
                _currentState = State.Loading;

                string key = $"{ConfigAddressableKey.Cutscene}_{info.Uid}";
                TextAsset asset = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);
            
                if (asset == null)
                {
                    GcLogger.LogError("연출 json 파일이 없습니다. " + info.FileName);
                    return;
                }
                // 카메라 원본 size 저장 
                _originalOrthographicSize = SceneGame.Instance.mainCamera.orthographicSize;
                // 모든 캐릭터 활성화, 컬링 적용되지 않음
                _sceneGame.mapManager.ActiveAllCharacters();
                // json 파싱하기
                _currentCutscene = JsonConvert.DeserializeObject<CutsceneData>(asset.text);    
                // 리소스 생성, 프리팹 로딩, 사운드 등 선행 처리
                _sceneGame.StartCoroutine(PrepareAndPlay());
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }
        /// <summary>
        /// 연출 준비
        /// </summary>
        /// <returns></returns>
        private IEnumerator PrepareAndPlay()
        {
            _currentState = State.Ready;

            foreach (var cutsceneEvent in _currentCutscene.events)
            {
                var controller = CreateController(cutsceneEvent.type);
                if (controller == null) continue;
                cutsceneEvent.Controller = controller; // 저장
                _activeControllers.Add(controller);
                yield return _sceneGame.StartCoroutine(controller.Ready(cutsceneEvent));
            }

            // GcLogger.Log("모든 컨트롤러 준비 완료 → 연출 시작");
            _currentState = State.Playing;
        }

        public void Update()
        {
            if (_currentState != State.Playing || _currentCutscene == null) return;

            _playTimer += Time.deltaTime;

            while (_currentIndex < _currentCutscene.events.Count &&
                   _currentCutscene.events[_currentIndex].time <= _playTimer)
            {
                var evt = _currentCutscene.events[_currentIndex];
                evt.Controller?.Trigger(evt); // 재사용
                _currentIndex++;
            }

            foreach (var controller in _activeControllers)
            {
                controller.Update();
            }

            if (!(_playTimer > _currentCutscene.duration)) return;
            OnCutsceneEnd();
            
        }
        /// <summary>
        /// 연출 종료
        /// </summary>
        private void OnCutsceneEnd()
        {
            // GcLogger.Log("연출 종료");
            _currentState = State.Finished;
            
            foreach (var controller in _activeControllers)
            {
                controller.End();
            }
            _activeControllers.Clear(); // 메모리 정리
            
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
        }
        /// <summary>
        /// 연출에 필요한 캐릭터 생성 후 추가
        /// </summary>
        /// <param name="type"></param>
        /// <param name="characterUid"></param>
        /// <param name="character"></param>
        public void AddCharacter(CharacterConstants.Type type, int characterUid, GameObject character)
        {
            if (!_createCharacters.ContainsKey(type))
            {
                _createCharacters.Add(type, new Dictionary<int, GameObject>());
            }
            _createCharacters[type].Add(characterUid, character);
        }
        /// <summary>
        /// 연출 중 생성된 캐릭터에서 찾기
        /// </summary>
        /// <param name="type"></param>
        /// <param name="characterUid"></param>
        /// <returns></returns>
        public Transform GetCharacter(CharacterConstants.Type type, int characterUid)
        {
            return _createCharacters.GetValueOrDefault(type)?.GetValueOrDefault(characterUid)?.transform;
        }

        public void SetOverlayTextOverride(string key, string text)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _overlayTextOverrides[key] = text ?? string.Empty;
        }

        public bool TryGetOverlayTextOverride(string key, out string text)
        {
            text = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _overlayTextOverrides.TryGetValue(key, out text);
        }

        public void RemoveOverlayTextOverride(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _overlayTextOverrides.Remove(key);
        }

        public void ClearOverlayTextOverrides()
        {
            _overlayTextOverrides.Clear();
        }
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

                CutsceneEventType.DialogueBalloon => new DialogueBalloonController(this, _dialogueBalloonPool),
                CutsceneEventType.ScreenFade => new ScreenFadeController(this),
                CutsceneEventType.OverlayText => new OverlayTextController(this),
                CutsceneEventType.CharacterWhiteOverlay => new CharacterWhiteOverlayController(this),
                CutsceneEventType.UiPanel => new UiPanelController(this),

                _ => null,
            };
        }

        public void OnDestroy()
        {
            foreach (var controller in _activeControllers)
            {
                controller.End();
            }
            _activeControllers.Clear(); // 메모리 정리
            
            // 만들었던 캐릭터 지우기
            foreach (var dic1 in _createCharacters)
            {
                foreach (var dic2 in dic1.Value)
                {
                    Object.Destroy(dic2.Value);
                }
            }

            ClearOverlayTextOverrides();

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
        }
    }
}
