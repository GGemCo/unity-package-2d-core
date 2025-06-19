using System.Collections;
using GGemCo.Scripts.UI;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 캐릭터 이동
    /// </summary>
    public class DialogueBalloonController : CutsceneDefaultController, ICutsceneController
    {
        private Camera cam;
        private string message;
        private float timer;
        private float duration;
        private bool isFollowTarget;
        private bool isBalloon;

        private Transform newTarget;
        private CharacterBase newTargetCharacter;
        private DialogueBalloonPool dialogueBalloonPool;
        private GameObject currentDialogueBalloon;

        public DialogueBalloonController(CutsceneManager manager, DialogueBalloonPool dialogueBalloonPool)
        {
            CutsceneManager = manager;
            this.dialogueBalloonPool = dialogueBalloonPool;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueBalloon) yield break;
            var data = evt.dialogueBalloon;
            
            // 캐릭터 타겟 찾기 같은 준비
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueBalloon) return;
            // 비정상적으로 종료될 경우가 있다
            if (isBalloon)
            {
                Stop();
            }
            var data = evt.dialogueBalloon;
            isFollowTarget = data.isFollowTarget;
            duration = evt.duration;
            // 말풍선을 붙일 캐릭터 
            newTarget = GetTargetTransform(data.characterType, data.characterUid);
            if (newTarget == null)
            {
                newTarget = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
            }
            if (newTarget == null)
            {
                GcLogger.LogError("대사를 하는 캐릭터가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                return;
            }
            newTargetCharacter = newTarget.GetComponent<CharacterBase>();
            if (newTargetCharacter == null)
            {
                GcLogger.LogError("CharacterBase 가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                return;
            }
            // 말풍선 
            currentDialogueBalloon = dialogueBalloonPool?.Get();
            if (currentDialogueBalloon == null)
            {
                GcLogger.LogError("말풍선이 만들어지지 않았습니다.");
                return; 
            }
            currentDialogueBalloon.GetComponent<UIDialogueBalloon>().Initialize(newTargetCharacter, data); // 텍스트 설정
            
            // 카메라가 따라가야하는 타겟 설정
            if (isFollowTarget)
            {
                SceneGame.Instance.cameraManager.SetFollowTarget(newTarget);
            }
            if (newTarget.gameObject.activeSelf == false)
            {
                newTarget.gameObject.SetActive(true);
            }
            timer = 0f;
            isBalloon = true;
        }
        public void Update()
        {
            if (!isBalloon) return;
            
            timer += Time.deltaTime;

            if (timer >= duration)
            {
                Stop();
            }
        }
        public void Stop()
        {
            timer = 0;
            isBalloon = false;
            dialogueBalloonPool?.Return(currentDialogueBalloon);
        }
        public void End()
        {
        }
    }
}
