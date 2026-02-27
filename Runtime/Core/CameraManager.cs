using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    public class CameraManager : MonoBehaviour
    {
        [Tooltip("왼쪽 경계 제한 여부")] 
        public bool useLimitLeft = true;
        [Tooltip("오른쪽 경계 제한 여부")] 
        public bool useLimitRight = true;
        [Tooltip("위쪽 경계 제한 여부")] 
        public bool useLimitTop = true;
        [Tooltip("아래쪽 경계 제한 여부")] 
        public bool useLimitBottom = true;
        [Tooltip("타겟을 따라다니는 속도")]
        [SerializeField] private float cameraMoveSpeed;
        
        [Header("Camera Offset")]
        [Tooltip("타겟(플레이어) 기준 카메라 기본 오프셋(월드 단위). 예) x>0이면 캐릭터 오른쪽을 더 보여줍니다.")]
        [SerializeField]
        private Vector2 followOffset = Vector2.zero;

        private float _originalOrthographicSize;
        private Vector3 _originCameraPosition;
        private Camera _currentCamera;

        private Vector3 _cameraPosition; // (legacy) runtime override
        private Vector3 _basePosition;
        private Vector3 _shakeOffset;
        private Vector2 _center;
        private Vector2 _mapSize;
        private Vector2 _monsterSpawnPositionBoxSize;
        private Transform _followTarget;
        
        private float _width;
        private float _height;

        // 흔들림 효과 관련 변수
        private bool _isShaking;
        
        // 줌 관련 처리 
        private bool _isZooming;
        private float _zoomTimer;
        private float _zoomDuration;
        private float _zoomStartSize;
        private float _zoomEndSize;
        private Easing.EaseType _zoomEasing;

        private void Awake()
        {
            _isShaking = false;
            _isZooming = false;
            _zoomTimer = 0;
            _zoomDuration = 0;
            _zoomStartSize = 0;
            _zoomEndSize = 0;
            _zoomEasing = Easing.EaseType.Linear;
            _originCameraPosition = Vector3.zero;
            _cameraPosition = new Vector3(followOffset.x, followOffset.y, 0f);
            _basePosition = transform.position;
            _shakeOffset = Vector3.zero;

            _currentCamera = GetComponent<Camera>();
            _originalOrthographicSize = _currentCamera.orthographicSize;
            _height = _originalOrthographicSize;
            _width = _height * Screen.width / Screen.height;
        }

        private void Update()
        {
            LimitCameraArea();
        }

        private void LimitCameraArea()
        {
            if (_followTarget == null || _mapSize.x == 0) return;

            // 플레이어를 따라가는 카메라 위치 계산
            Vector3 targetPos = _followTarget.position + _cameraPosition;
            targetPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * cameraMoveSpeed);

            float clampX = targetPos.x;
            float clampY = targetPos.y;

            // 카메라 half extents (Orthographic)
            float halfH = _currentCamera.orthographicSize;
            float halfW = halfH * _currentCamera.aspect;

            // --- 좌우 제한 ---
            if (useLimitLeft || useLimitRight)
            {
                float minX = halfW;
                float maxX = _mapSize.x - halfW;

                // 맵이 화면보다 작아 clamp 구간이 뒤집히는 경우 -> 중앙 고정
                if (maxX < minX)
                {
                    clampX = _mapSize.x * 0.5f;
                }
                else
                {
                    if (useLimitLeft) clampX = Mathf.Max(clampX, minX);
                    if (useLimitRight) clampX = Mathf.Min(clampX, maxX);
                }
            }

            // --- 상하 제한 ---
            if (useLimitBottom || useLimitTop)
            {
                float minY = halfH;
                float maxY = _mapSize.y - halfH;

                if (maxY < minY)
                {
                    clampY = _mapSize.y * 0.5f;
                }
                else
                {
                    if (useLimitBottom) clampY = Mathf.Max(clampY, minY);
                    if (useLimitTop) clampY = Mathf.Min(clampY, maxY);
                }
            }

            // 최종 위치 적용 (Shake는 "기본 위치"를 기준으로 오프셋으로만 적용)
            _basePosition = new Vector3(clampX, clampY, -10f);
            transform.position = _basePosition + _shakeOffset;

            // 줌 처리
            if (_isZooming)
            {
                _zoomTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_zoomTimer / _zoomDuration);
                float easedT = Easing.Apply(t, _zoomEasing);
                float zoom = Mathf.Lerp(_zoomStartSize, _zoomEndSize, easedT);
                _currentCamera.orthographicSize = zoom;

                _height = zoom;
                _width = _height * Screen.width / Screen.height;
                if (t >= 1f) _isZooming = false;
            }
        }

        private IEnumerator Shake(float duration, float magnitude)
        {
            _isShaking = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                _shakeOffset = new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _isShaking = false;
        }

        /// <summary>
        /// 카메라 흔들림 효과 주기
        /// </summary>
        public void StartShake(float shakeDuration, float shakeMagnitude)
        {
            if (shakeDuration <= 0 || shakeMagnitude <= 0) return;
            StartCoroutine(Shake(shakeDuration, shakeMagnitude));
        }

        /// <summary>
        /// 맵 경계선 사이즈 변경하기
        /// </summary>
        public void ChangeMapSize(float pWidth, float pHeight)
        {
            _mapSize.x = pWidth;
            _mapSize.y = pHeight;
        }

        /// <summary>
        /// 카메라 강제로 이동시키기
        /// </summary>
        public void MoveCameraPosition(float x, float y)
        {
            transform.position = new Vector3(x, y, -10f) + _cameraPosition;
            _basePosition = transform.position;
        }

        /// <summary>
        /// 플레이어 기준에서의 카메라 위치 값 바꾸기
        /// </summary>
        public void ChangeCameraPositionValue(float x, float y)
        {
            _originCameraPosition.x = _cameraPosition.x;
            _originCameraPosition.y = _cameraPosition.y;
            _cameraPosition.x = x;
            _cameraPosition.y = y;
        }

        public void ResetCameraPositionValue()
        {
            _cameraPosition.x = _originCameraPosition.x;
            _cameraPosition.y = _originCameraPosition.y;
        }

        /// <summary>
        /// 카메라가 따라가는 캐릭터 지우기
        /// </summary>
        public void RemoveFollowTarget()
        {
            _followTarget = null;
        }

        /// <summary>
        /// 따라가는 캐릭터 변경
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target == null ? SceneGame.Instance.player.transform : target;
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
        public void StartZoom(float endSize, float duration = 1f, Easing.EaseType easeType = Easing.EaseType.EaseOutQuad)
        {
            _zoomTimer = 0;
            _zoomStartSize = _currentCamera.orthographicSize;
            _zoomEndSize = endSize;
            _zoomDuration = duration;
            _zoomEasing = easeType;
            _isZooming = true;
        }

        /// <summary>
        /// orthographicSize 초기화
        /// </summary>
        private void ReSetZoom()
        {
            _zoomTimer = 0;
            _zoomStartSize = _currentCamera.orthographicSize;
            _zoomEndSize = _originalOrthographicSize;
            _zoomDuration = 1f;
            _zoomEasing = Easing.EaseType.EaseOutQuad;
            _isZooming = true;
        }

        /// <summary>
        /// 연출 종료시 호출
        /// </summary>
        public void ReSetByCutscene()
        {
            SetFollowPlayer();
            ReSetZoom();
        }

        /// <summary>
        /// 타겟(플레이어) 기준 카메라 기본 오프셋(월드 단위).
        /// Inspector에서 설정한 값은 시작 시 기본값으로 사용되며, 흔들림(Shake)은 이 기본 위치를 기준으로 적용됩니다.
        /// </summary>
        public Vector2 FollowOffset
        {
            get => new Vector2(_cameraPosition.x, _cameraPosition.y);
            set => _cameraPosition = new Vector3(value.x, value.y, 0f);
        }

        public void SetCameraMoveSpeed(float speed)
        {
            cameraMoveSpeed = speed;
        }

        /// <summary>
        /// 카메라 위치 가져오기 (Z 값은 제외)
        /// </summary>
        public Vector2 GetPositionCenter()
        {
            return transform.position;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Gizmos.color = Color.red;
            // Gizmos.DrawWireCube(center, mapSize * 2);
            //
            // Gizmos.color = Color.blue;
            // Gizmos.DrawWireCube(center, monsterSpawnPositionBoxSize * 2);
        }
#endif
    }
}