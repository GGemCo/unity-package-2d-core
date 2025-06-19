using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 매니저
    /// </summary>
    public class CutsceneManager
    {
        private enum State { Idle, Loading, Ready, Playing, Finished }
        private State currentState;

        private CutsceneData currentCutscene;
        private float playTimer;
        private int currentIndex;
        private float originalOrthographicSize;
        private DialogueBalloonPool dialogueBalloonPool;

        // 연출중 생성된 캐릭터 관리
        private readonly Dictionary<CharacterConstants.Type, Dictionary<int, GameObject>> createCharacters =
            new Dictionary<CharacterConstants.Type, Dictionary<int, GameObject>>();
        
        // 연출 컨트롤러
        private readonly List<ICutsceneController> activeControllers = new();
        
        private CameraMoveController cameraMoveController;
        private CameraZoomController cameraZoomController;
        private CameraShakeController cameraShakeController;
        private CameraChangeTargetController cameraChangeTargetController;
        
        private CharacterMoveController characterMoveController;
        private CharacterAnimationController characterAnimationController;
        
        private DialogueBalloonController dialogueBalloonController;
        private SceneGame sceneGame;
        
        public void Initialize(SceneGame scene)
        {
            sceneGame = scene;
            createCharacters.Clear();
            playTimer = 0f;
            currentIndex = 0;
            currentState = State.Idle;
            
            // 기존 컨트롤러 초기화 이후
            if (sceneGame.containerDialogueBalloon)
            {
                dialogueBalloonPool = new DialogueBalloonPool(sceneGame.containerDialogueBalloon.transform); // 부모는 선택
            }
        }
        public bool IsPlaying() => currentState == State.Playing;
        /// <summary>
        /// 초기화
        /// </summary>
        private void Reset()
        {
            createCharacters.Clear();
            playTimer = 0f;
            currentIndex = 0;
        }
        /// <summary>
        /// 연출 플레이
        /// </summary>
        /// <param name="uid"></param>
        public void PlayCutscene(int uid)
        {
            var info = TableLoaderManager.Instance.TableCutscene.GetDataByUid(uid);
            if (info == null)
            {
                return;
            }
            Reset();
            currentState = State.Loading;

            TextAsset asset = Resources.Load<TextAsset>($"Cutscene/{info.FileName}");
            if (asset == null)
            {
                GcLogger.LogError("연출 json 파일이 없습니다. " + info.FileName);
                return;
            }
            // 카메라 원본 size 저장 
            originalOrthographicSize = SceneGame.Instance.mainCamera.orthographicSize;
            // 모든 캐릭터 활성화, 컬링 적용되지 않음
            sceneGame.mapManager.ActiveAllCharacters();
            // json 파싱하기
            currentCutscene = JsonConvert.DeserializeObject<CutsceneData>(asset.text);    
            // 리소스 생성, 프리팹 로딩, 사운드 등 선행 처리
            sceneGame.StartCoroutine(PrepareAndPlay());
        }
        /// <summary>
        /// 연출 준비
        /// </summary>
        /// <returns></returns>
        private IEnumerator PrepareAndPlay()
        {
            currentState = State.Ready;

            foreach (var cutsceneEvent in currentCutscene.events)
            {
                var controller = CreateController(cutsceneEvent.type);
                if (controller == null) continue;
                cutsceneEvent.Controller = controller; // 저장
                activeControllers.Add(controller);
                yield return sceneGame.StartCoroutine(controller.Ready(cutsceneEvent));
            }

            // GcLogger.Log("모든 컨트롤러 준비 완료 → 연출 시작");
            currentState = State.Playing;
        }

        public void Update()
        {
            if (currentState != State.Playing || currentCutscene == null) return;

            playTimer += Time.deltaTime;

            while (currentIndex < currentCutscene.events.Count &&
                   currentCutscene.events[currentIndex].time <= playTimer)
            {
                var evt = currentCutscene.events[currentIndex];
                evt.Controller?.Trigger(evt); // 재사용
                currentIndex++;
            }

            foreach (var controller in activeControllers)
            {
                controller.Update();
            }

            if (!(playTimer > currentCutscene.duration)) return;
            OnCutsceneEnd();
            
        }
        /// <summary>
        /// 연출 종료
        /// </summary>
        private void OnCutsceneEnd()
        {
            // GcLogger.Log("연출 종료");
            currentState = State.Finished;
            
            foreach (var controller in activeControllers)
            {
                controller.End();
            }
            activeControllers.Clear(); // 메모리 정리
            
            // 만들었던 캐릭터 지우기
            foreach (var dic1 in createCharacters)
            {
                foreach (var dic2 in dic1.Value)
                {
                    Object.Destroy(dic2.Value);
                }
            }
            
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
            if (!createCharacters.ContainsKey(type))
            {
                createCharacters.Add(type, new Dictionary<int, GameObject>());
            }
            createCharacters[type].Add(characterUid, character);
        }
        /// <summary>
        /// 연출 중 생성된 캐릭터에서 찾기
        /// </summary>
        /// <param name="type"></param>
        /// <param name="characterUid"></param>
        /// <returns></returns>
        public Transform GetCharacter(CharacterConstants.Type type, int characterUid)
        {
            return createCharacters.GetValueOrDefault(type)?.GetValueOrDefault(characterUid)?.transform;
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

                CutsceneEventType.DialogueBalloon => new DialogueBalloonController(this, dialogueBalloonPool),

                _ => null,
            };
        }

        public void OnDestroy()
        {
            foreach (var controller in activeControllers)
            {
                controller.End();
            }
            activeControllers.Clear(); // 메모리 정리
            
            // 만들었던 캐릭터 지우기
            foreach (var dic1 in createCharacters)
            {
                foreach (var dic2 in dic1.Value)
                {
                    Object.Destroy(dic2.Value);
                }
            }
        }
    }
}
