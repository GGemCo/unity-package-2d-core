using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 파랄럭스 대상 레이어의 기준 위치와 카메라 영향 비율을 관리합니다.
    /// </summary>
    public class ParallaxLayer2D : MonoBehaviour
    {
        [Header("축별 적용")]
        [SerializeField] private bool useHorizontalParallax = true;
        [SerializeField] private bool useVerticalParallax = true;

        [Header("카메라 영향 비율")]
        [Tooltip("0이면 일반 월드 오브젝트처럼 동작하고, 1이면 카메라를 완전히 따라가 화면상 위치가 거의 고정됩니다.")]
        [SerializeField] private float horizontalCameraInfluence = 0.75f;
        [Tooltip("0이면 일반 월드 오브젝트처럼 동작하고, 1이면 카메라를 완전히 따라가 화면상 위치가 거의 고정됩니다.")]
        [SerializeField] private float verticalCameraInfluence = 0.5f;

        [Header("기준 좌표")]
        [Tooltip("True 이면 localPosition 기준으로, False 이면 world position 기준으로 파랄럭스를 적용합니다.")]
        [SerializeField] private bool useLocalPosition = true;

        private Vector3 _baselineLocalPosition;
        private Vector3 _baselineWorldPosition;
        private bool _hasBaseline;

        /// <summary>
        /// 현재 위치를 파랄럭스 기준 위치로 저장합니다.
        /// </summary>
        public void CaptureBaseline()
        {
            _baselineLocalPosition = transform.localPosition;
            _baselineWorldPosition = transform.position;
            _hasBaseline = true;
        }

        /// <summary>
        /// 저장된 기준 위치로 레이어를 복원합니다.
        /// </summary>
        public void ResetToBaseline()
        {
            if (!_hasBaseline)
            {
                CaptureBaseline();
            }

            if (useLocalPosition)
            {
                transform.localPosition = _baselineLocalPosition;
                return;
            }

            transform.position = _baselineWorldPosition;
        }

        /// <summary>
        /// 카메라 이동량을 기준으로 레이어의 파랄럭스 위치를 계산하여 적용합니다.
        /// </summary>
        /// <param name="cameraDelta">기준 시점 대비 카메라 이동량입니다.</param>
        public void ApplyParallax(Vector3 cameraDelta)
        {
            if (!_hasBaseline)
            {
                CaptureBaseline();
            }

            Vector3 offset = new Vector3(
                useHorizontalParallax ? cameraDelta.x * horizontalCameraInfluence : 0f,
                useVerticalParallax ? cameraDelta.y * verticalCameraInfluence : 0f,
                0f);

            if (useLocalPosition)
            {
                Vector3 nextLocalPosition = _baselineLocalPosition + offset;
                nextLocalPosition.z = _baselineLocalPosition.z;
                transform.localPosition = nextLocalPosition;
                return;
            }

            Vector3 nextWorldPosition = _baselineWorldPosition + offset;
            nextWorldPosition.z = _baselineWorldPosition.z;
            transform.position = nextWorldPosition;
        }

        /// <summary>
        /// 현재 레이어가 기준 위치를 이미 저장했는지 반환합니다.
        /// </summary>
        public bool HasBaseline => _hasBaseline;

        /// <summary>
        /// 수평 카메라 영향 비율을 반환합니다.
        /// </summary>
        public float HorizontalCameraInfluence => horizontalCameraInfluence;

        /// <summary>
        /// 수직 카메라 영향 비율을 반환합니다.
        /// </summary>
        public float VerticalCameraInfluence => verticalCameraInfluence;
    }
}
