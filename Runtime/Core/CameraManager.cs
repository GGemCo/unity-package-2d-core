using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo.Scripts
{
    public class CameraManager : MonoBehaviour
    {
        private float originalOrthographicSize;
        private Vector3 originCameraPosition;
        private Camera currentCamera;
        
        private Vector3 cameraPosition;
        private Vector2 center;
        private Vector2 mapSize;
        private Vector2 monsterSpawnPositionBoxSize;
        private Transform followTarget;
        
        [SerializeField] float cameraMoveSpeed;
        float width;
        float height;

        // 흔들림 효과 관련 변수
        private Vector3 originalPos;
        private bool isShaking;
        
        // 줌 관련 처리 
        private bool isZooming;
        private float zoomTimer;
        private float zoomDuration;
        private float zoomStartSize;
        private float zoomEndSize;
        private Easing.EaseType zoomEasing;

        private void Awake()
        {
            isShaking = false;
            isZooming = false;
            zoomTimer = 0;
            zoomDuration = 0;
            zoomStartSize = 0;
            zoomEndSize = 0;
            zoomEasing = Easing.EaseType.Linear;
            originCameraPosition = Vector3.zero;
            
            currentCamera = GetComponent<Camera>();
            originalOrthographicSize = currentCamera.orthographicSize;
            height = originalOrthographicSize;
            width = height * Screen.width / Screen.height;
            originalPos = transform.localPosition;
        }
        private void Update()
        {
            LimitCameraArea();
        }
        private void LimitCameraArea()
        {
            if (followTarget == null || mapSize.x == 0) return;
            // 플레이어를 따라가는 카메라 위치 계산
            Vector3 targetPos = followTarget.position + cameraPosition;
            targetPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);
    
            // 맵의 좌측 상단을 기준으로 경계 내로 제한
            float clampX = Mathf.Clamp(targetPos.x, width, mapSize.x - width); // 좌측 상단 기준 X 좌표 제한
            float clampY = Mathf.Clamp(targetPos.y, height, mapSize.y - height); // 좌측 상단 기준 Y 좌표 제한

            // 맵의 가운데 기준으로 경계 내로 제한
            // float lx = mapSize.x - width;
            // float clampX = Mathf.Clamp(targetPos.x, -lx + center.x, lx + center.x);
            //
            // float ly = mapSize.y - height;
            // float clampY = Mathf.Clamp(targetPos.y, -ly + center.y, ly + center.y);
            
            // 흔들림이 적용된 최종 위치 설정
            if (isShaking)
            {
                transform.position = new Vector3(clampX, clampY, -10f) + (Vector3)Random.insideUnitCircle * 0.1f;
            }
            else
            {
                transform.position = new Vector3(clampX, clampY, -10f);
                originalPos = transform.position;
            }

            if (isZooming)
            {
                zoomTimer += Time.deltaTime;
                float t = Mathf.Clamp01(zoomTimer / zoomDuration);
                float easedT = Easing.Apply(t, zoomEasing);
                float zoom = Mathf.Lerp(zoomStartSize, zoomEndSize, easedT);
                currentCamera.orthographicSize = zoom;
                
                height = zoom;
                width = height * Screen.width / Screen.height;
                if (t >= 1f)
                {
                    isZooming = false;
                }
            }
        }
        private IEnumerator Shake(float duration, float magnitude)
        {
            isShaking = true;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                transform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            isShaking = false;
            transform.localPosition = originalPos;
        }
        /// <summary>
        /// 카메라 흔들림 효과 주기
        /// </summary>
        /// <param name="shakeDuration"></param>
        /// <param name="shakeMagnitude"></param>
        public void StartShake(float shakeDuration, float shakeMagnitude)
        {
            if (shakeDuration <= 0 || shakeMagnitude <= 0) return;
            StartCoroutine(Shake(shakeDuration, shakeMagnitude));
        }
        /// <summary>
        /// 맵 경계선 사이즈 변경하기
        /// </summary>
        /// <param name="pWidth"></param>
        /// <param name="pHeight"></param>
        public void ChangeMapSize(float pWidth, float pHeight)
        {
            mapSize.x = pWidth;
            mapSize.y = pHeight;
        }
        /// <summary>
        /// 카메라 강제로 이동시키기
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void MoveCameraPosition(float x, float y)
        {
            transform.position = new Vector3(x, y, -10f) + cameraPosition;
            originalPos = transform.position;
        }
        /// <summary>
        /// 플레이어 기준에서의 카메라 위치 값 바꾸기 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void ChangeCameraPositionValue(float x, float y)
        {
            originCameraPosition.x = cameraPosition.x;
            originCameraPosition.y = cameraPosition.y;
            cameraPosition.x = x;
            cameraPosition.y = y;
        }
        public void ResetCameraPositionValue()
        {
            cameraPosition.x = originCameraPosition.x;
            cameraPosition.y = originCameraPosition.y;
        }
        /// <summary>
        /// 카메라가 따라가는 캐릭터 지우기
        /// </summary>
        public void RemoveFollowTarget()
        {
            followTarget = null;
        }
        /// <summary>
        /// 따라가는 캐릭터 변경
        /// </summary>
        /// <param name="target"></param>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target == null ? SceneGame.Instance.player.transform : target;
        }
        /// <summary>
        /// player 따라가도록 설정
        /// </summary>
        public void SetFollowPlayer()
        {
            if (SceneGame.Instance == null || SceneGame.Instance.player == null) return;
            SetFollowTarget(SceneGame.Instance.player.transform);
        }
        /// <summary>
        /// orthographicSize 변경하기
        /// </summary>
        /// <param name="endSize"></param>
        /// <param name="duration"></param>
        /// <param name="easeType"></param>
        public void StartZoom(float endSize, float duration = 1f, Easing.EaseType easeType = Easing.EaseType.EaseOutQuad)
        {
            zoomTimer = 0;
            zoomStartSize = currentCamera.orthographicSize;
            zoomEndSize = endSize;
            zoomDuration = duration;
            zoomEasing = easeType;
            isZooming = true;
        }
        /// <summary>
        /// orthographicSize 초기화
        /// </summary>
        private void ReSetZoom()
        {
            zoomTimer = 0;
            zoomStartSize = currentCamera.orthographicSize;
            zoomEndSize = originalOrthographicSize;
            zoomDuration = 1f;
            zoomEasing = Easing.EaseType.EaseOutQuad;
            isZooming = true;
        }
        /// <summary>
        /// 연출 종료시 호출
        /// </summary>
        public void ReSetByCutscene()
        {
            // 카메라 player 따라가
            SetFollowPlayer();
            ReSetZoom();
        }
        private void OnDrawGizmos()
        {
            // Gizmos.color = Color.red;
            // Gizmos.DrawWireCube(center, mapSize * 2);
            //
            // Gizmos.color = Color.blue;
            // Gizmos.DrawWireCube(center, monsterSpawnPositionBoxSize * 2);
        }
        private void OnDestroy()
        {
            
        }
    }
}
