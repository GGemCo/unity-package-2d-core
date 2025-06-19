using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 카메라 이동하기
    /// </summary>
    public class CameraChangeTargetController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterConstants.Type characterType;
        private int characterUid;
        private float timer;
        private float duration;

        private Transform newTarget;
        private bool isChange;

        public CameraChangeTargetController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            // 캐릭터 타겟 찾기 같은 준비
            yield return null;
        }
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraChangeTarget) return;
            duration = evt.duration;
            var data = evt.cameraChangeTarget;
            newTarget = GetTargetTransform(data.characterType, data.characterUid);

            if (newTarget != null)
            {
                SceneGame.Instance.cameraManager.SetFollowTarget(newTarget);
            }

            timer = 0f;
            isChange = true;
        }
        public void Update()
        {
            if (!isChange) return;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            
            if (timer > duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            isChange = false;
        }
        public void End()
        {
            SceneGame.Instance.cameraManager.SetFollowPlayer();
        }
    }
}
