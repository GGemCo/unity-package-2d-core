using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class KeyboardManager
    {
        private List<IInputHandler> inputHandlers;
        private SceneGame sceneGame;

        public void Initialize(SceneGame psceneGame)
        {
            sceneGame = psceneGame;
            inputHandlers = new List<IInputHandler>();
        }

        // 의존성 주입을 통해 입력 처리기를 추가
        public void RegisterInputHandler(IInputHandler handler)
        {
            if (inputHandlers.Contains(handler)) return;
            inputHandlers.Add(handler);
        }
        public void RemoveInputHandler(IInputHandler handler)
        {
            inputHandlers.Remove(handler);
        }

        public void Update()
        {
            // 연출 중이면 
            if (sceneGame.CutsceneManager.IsPlaying())
            {
                return;
            }
            
            bool isInput = false;
            // 우선순위에 따라 정렬 후 입력 처리
            foreach (var handler in inputHandlers.OrderByDescending(h => h.Priority))
            {
                // 입력 처리 완료 시 이후 핸들러의 처리 중단
                if (handler.HandleInput())
                {
                    isInput = true;
                    break; // 처리 완료, 더 이상 핸들러 호출 안 함
                }
            }

            if (isInput) return;
            HandleInputCommon();
        }
        /// <summary>
        /// 공통 input 처리 
        /// </summary>
        private void HandleInputCommon()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnTriggerEscapeKeyDown();
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
            }
        }

        private void OnTriggerEscapeKeyDown()
        {
            // GcLogger.Log("OnTriggerEscapeKeyDown");
            if (sceneGame == null) return;
            
            if (sceneGame.InteractionManager != null && sceneGame.InteractionManager.IsInteractioning())
            {
                sceneGame.InteractionManager.EndInteraction();
                return;
            }
            sceneGame.uIWindowManager.CloseAll();
        }

        public void OnDestroy()
        {
            inputHandlers.Clear();
        }
    }
}