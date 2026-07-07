using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 무한 스크롤 배경의 개별 반복 레이어를 처리합니다.
    /// </summary>
    public sealed class InfiniteScrollingBackgroundLayer : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private bool repeatHorizontal = true;
        [SerializeField] private bool repeatVertical;
        [SerializeField] private Vector2 cameraInfluence = Vector2.one;

        private Vector3 _baselinePosition;
        private Vector3 _baselineCameraPosition;
        private Vector2 _tileSize;
        private bool _hasBaseline;

        /// <summary>
        /// 현재 위치와 카메라 위치를 기준점으로 저장합니다.
        /// </summary>
        /// <param name="cameraPosition">기준 카메라 위치입니다.</param>
        public void CaptureBaseline(Vector3 cameraPosition)
        {
            _baselinePosition = transform.position;
            _baselineCameraPosition = cameraPosition;
            _tileSize = ResolveTileSize();
            _hasBaseline = true;
        }

        /// <summary>
        /// 카메라 이동량에 맞춰 배경 위치를 갱신하고 화면 밖으로 벗어난 타일을 이어 붙입니다.
        /// </summary>
        /// <param name="cameraPosition">현재 기준 카메라 위치입니다.</param>
        public void Tick(Vector3 cameraPosition)
        {
            if (!_hasBaseline)
            {
                CaptureBaseline(cameraPosition);
            }

            Vector3 cameraDelta = cameraPosition - _baselineCameraPosition;
            Vector3 nextPosition = _baselinePosition + new Vector3(
                cameraDelta.x * cameraInfluence.x,
                cameraDelta.y * cameraInfluence.y,
                0f);

            if (repeatHorizontal && _tileSize.x > 0.0001f)
            {
                nextPosition.x = WrapAxis(nextPosition.x, cameraPosition.x, _tileSize.x);
            }

            if (repeatVertical && _tileSize.y > 0.0001f)
            {
                nextPosition.y = WrapAxis(nextPosition.y, cameraPosition.y, _tileSize.y);
            }

            nextPosition.z = _baselinePosition.z;
            transform.position = nextPosition;
        }

        private static float WrapAxis(float position, float cameraPosition, float tileSize)
        {
            float delta = position - cameraPosition;
            if (delta > tileSize)
            {
                position -= tileSize * 2f;
            }
            else if (delta < -tileSize)
            {
                position += tileSize * 2f;
            }

            return position;
        }

        private Vector2 ResolveTileSize()
        {
            Renderer resolvedRenderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (resolvedRenderer == null)
            {
                return Vector2.zero;
            }

            Bounds bounds = resolvedRenderer.bounds;
            return new Vector2(bounds.size.x, bounds.size.y);
        }
    }
}
