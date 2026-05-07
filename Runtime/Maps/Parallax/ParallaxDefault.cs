using UnityEngine;

namespace GGemCo2DCore
{
    public class ParallaxDefault : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float speed = 1f;
        [SerializeField] private Vector3 direction = Vector3.left;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private Camera debugCamera;
        [SerializeField] private float debugLogInterval = 1f;

        private float _debugElapsedTime;

        /// <summary>
        /// 매 프레임 패럴랙스 오브젝트를 이동시키고,
        /// 디버그 옵션이 켜져 있으면 주기적으로 현재 이동 속도 정보를 출력합니다.
        /// </summary>
        private void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime);

            if (!enableDebugLog)
            {
                return;
            }

            _debugElapsedTime += Time.deltaTime;
            if (_debugElapsedTime < debugLogInterval)
            {
                return;
            }

            _debugElapsedTime = 0f;
            LogDebugInfo();
        }

        /// <summary>
        /// 현재 설정 기준의 초당 월드 이동량(unit/s)과
        /// Orthographic 카메라 기준 화면 픽셀 이동량(px/s)을 로그로 출력합니다.
        /// </summary>
        [ContextMenu("Log Debug Info")]
        private void LogDebugInfo()
        {
            float worldUnitsPerSecond = direction.magnitude * speed;
            Camera targetCamera = debugCamera != null ? debugCamera : Camera.main;

            if (targetCamera == null)
            {
                Debug.Log(
                    $"[ParallaxDefault] {name} | " +
                    $"world: {worldUnitsPerSecond:F3} unit/s | " +
                    $"camera 가 없어 pixel 값을 계산할 수 없습니다.",
                    this);
                return;
            }

            if (!targetCamera.orthographic)
            {
                Debug.Log(
                    $"[ParallaxDefault] {name} | " +
                    $"world: {worldUnitsPerSecond:F3} unit/s | " +
                    $"Perspective 카메라는 화면 위치에 따라 픽셀 비율이 달라지므로 고정 px/s 계산이 어렵습니다.",
                    this);
                return;
            }

            float pixelsPerUnitOnScreen = Screen.height / (targetCamera.orthographicSize * 2f);
            float pixelsPerSecond = worldUnitsPerSecond * pixelsPerUnitOnScreen;

            Debug.Log(
                $"[ParallaxDefault] {name} | " +
                $"world: {worldUnitsPerSecond:F3} unit/s | " +
                $"screen: {pixelsPerSecond:F3} px/s | " +
                $"ppuOnScreen: {pixelsPerUnitOnScreen:F3} px/unit | " +
                $"screenHeight: {Screen.height} | " +
                $"orthoSize: {targetCamera.orthographicSize:F3}",
                this);
        }
    }
}