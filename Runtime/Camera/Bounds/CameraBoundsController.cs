using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라 기본 위치를 현재 맵 경계 안으로 제한합니다.
    /// </summary>
    public sealed class CameraBoundsController
    {
        private Vector2 _mapSize;
        private bool _useLimitLeft = true;
        private bool _useLimitRight = true;
        private bool _useLimitTop = true;
        private bool _useLimitBottom = true;

        /// <summary>
        /// 현재 적용 중인 맵 크기입니다.
        /// </summary>
        public Vector2 MapSize => _mapSize;

        /// <summary>
        /// 유효한 맵 크기가 설정되어 있는지 반환합니다.
        /// </summary>
        public bool HasMapSize => _mapSize.x > 0f && _mapSize.y > 0f;

        /// <summary>
        /// 경계 제한 사용 여부를 갱신합니다.
        /// </summary>
        public void ConfigureLimits(bool useLimitLeft, bool useLimitRight, bool useLimitTop, bool useLimitBottom)
        {
            _useLimitLeft = useLimitLeft;
            _useLimitRight = useLimitRight;
            _useLimitTop = useLimitTop;
            _useLimitBottom = useLimitBottom;
        }

        /// <summary>
        /// 맵 크기를 갱신합니다.
        /// </summary>
        /// <param name="width">맵 월드 폭입니다.</param>
        /// <param name="height">맵 월드 높이입니다.</param>
        public void ChangeMapSize(float width, float height)
        {
            _mapSize.x = Mathf.Max(0f, width);
            _mapSize.y = Mathf.Max(0f, height);
        }

        /// <summary>
        /// 전달된 카메라 위치를 현재 경계 설정에 맞게 제한합니다.
        /// </summary>
        /// <param name="targetPosition">제한 전 목표 위치입니다.</param>
        /// <param name="camera">Orthographic 카메라입니다.</param>
        /// <returns>경계 제한이 반영된 카메라 위치입니다.</returns>
        public Vector3 Clamp(Vector3 targetPosition, Camera camera)
        {
            if (!HasMapSize || camera == null || !camera.orthographic)
            {
                return targetPosition;
            }

            float clampX = targetPosition.x;
            float clampY = targetPosition.y;
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;

            // 맵보다 화면이 큰 축은 화면 중심을 맵 중심으로 고정합니다.
            if (_useLimitLeft || _useLimitRight)
            {
                float minX = halfWidth;
                float maxX = _mapSize.x - halfWidth;
                if (maxX < minX)
                {
                    clampX = _mapSize.x * 0.5f;
                }
                else
                {
                    if (_useLimitLeft)
                    {
                        clampX = Mathf.Max(clampX, minX);
                    }

                    if (_useLimitRight)
                    {
                        clampX = Mathf.Min(clampX, maxX);
                    }
                }
            }

            if (_useLimitBottom || _useLimitTop)
            {
                float minY = halfHeight;
                float maxY = _mapSize.y - halfHeight;
                if (maxY < minY)
                {
                    clampY = _mapSize.y * 0.5f;
                }
                else
                {
                    if (_useLimitBottom)
                    {
                        clampY = Mathf.Max(clampY, minY);
                    }

                    if (_useLimitTop)
                    {
                        clampY = Mathf.Min(clampY, maxY);
                    }
                }
            }

            return new Vector3(clampX, clampY, targetPosition.z);
        }

        /// <summary>
        /// 카메라 기본 위치를 기준으로 Orthographic Viewport의 월드 Rect를 계산합니다.
        /// </summary>
        /// <param name="camera">계산에 사용할 Orthographic 카메라입니다.</param>
        /// <param name="basePosition">흔들림이 적용되기 전 기본 위치입니다.</param>
        /// <param name="worldRect">계산된 월드 Rect입니다.</param>
        /// <returns>계산에 성공하면 true를 반환합니다.</returns>
        public bool TryGetViewportWorldRect(Camera camera, Vector3 basePosition, out Rect worldRect)
        {
            worldRect = default;

            if (camera == null || !camera.isActiveAndEnabled || !camera.orthographic)
            {
                return false;
            }

            float halfHeight = camera.orthographicSize;
            float aspect = camera.aspect;
            if (halfHeight <= 0f || aspect <= 0f || float.IsNaN(aspect) || float.IsInfinity(aspect))
            {
                return false;
            }

            float halfWidth = halfHeight * aspect;
            worldRect = Rect.MinMaxRect(
                basePosition.x - halfWidth,
                basePosition.y - halfHeight,
                basePosition.x + halfWidth,
                basePosition.y + halfHeight);
            return worldRect.width > 0f && worldRect.height > 0f;
        }
    }
}
