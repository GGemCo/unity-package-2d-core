using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 카메라 이동하기
    /// </summary>
    public class CameraMoveController : CutsceneDefaultController, ICutsceneController
    {
        private Camera cam;
        private Vector2 startPosition, endPosition;
        private float duration;
        private float timer;
        private bool isMoving;
        private bool endTargetPlayer;
        private Easing.EaseType easing;

        public CameraMoveController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            cam = SceneGame.Instance.mainCamera;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            // 캐릭터 타겟 찾기 같은 준비
            yield return null;
        }
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraMove) return;
            duration = evt.duration;
            var data = evt.cameraMove;

            startPosition = data.startPosition.ToVector2();
            if (startPosition == Vector2.zero)
            {
                startPosition = cam.transform.position;
            }
            endPosition = data.endPosition.ToVector2();
            easing = data.easing;
            endTargetPlayer = data.endTargetPlayer;

            timer = 0f;
            isMoving = true;
            SceneGame.Instance.cameraManager.RemoveFollowTarget();
        }

        public void Update()
        {
            if (!isMoving) return;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float easedT = Easing.Apply(t, easing);

            Vector2 basePos = Vector2.Lerp(startPosition, endPosition, easedT);
            cam.transform.position= new Vector3(basePos.x, basePos.y, cam.transform.position.z);

            if (t >= 1f)
            {
                Stop();
            }
        }

        public void Stop()
        {
            isMoving = false;
            if (endTargetPlayer)
            {
                SceneGame.Instance.cameraManager.SetFollowPlayer();
            }
        }
        public void End()
        {
            isMoving = false;
        }
    }
}
