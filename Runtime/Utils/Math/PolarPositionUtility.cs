using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 각도(도/deg)와 거리로 2D(XY) 기준의 월드 좌표를 계산하는 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// - 2D 기준: 0° = +X, 90° = +Y, 반시계 방향이 +입니다.
    /// - Z축 회전(즉, XY 평면) 기준으로 방향 벡터를 구성합니다.
    /// </remarks>
    public static class PolarPositionUtility
    {
        /// <summary>
        /// 월드 원점에서 angleDeg 방향으로 distance만큼 이동한 월드 좌표(Vector2)를 반환합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="angleDeg">각도(도)입니다. 0° = +X, 90° = +Y 입니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <returns>계산된 월드 좌표(Vector2)입니다.</returns>
        public static Vector2 WorldFromAngleDistance(Vector2 origin, float angleDeg, float distance)
        {
            float rad = angleDeg * Mathf.Deg2Rad; // deg -> rad
            float x = Mathf.Cos(rad) * distance;
            float y = Mathf.Sin(rad) * distance;
            return origin + new Vector2(x, y);
        }

        /// <summary>
        /// 월드 원점에서 angleDeg 방향으로 distance만큼 이동한 월드 좌표(Vector3)를 반환합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="angleDeg">각도(도)입니다. 0° = +X, 90° = +Y 입니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <returns>Z는 유지되고, XY만 이동한 월드 좌표(Vector3)입니다.</returns>
        /// <remarks>
        /// 반환값은 <c>origin + new Vector3(x, y, 0)</c> 형태이므로 origin.z는 그대로 유지됩니다.
        /// </remarks>
        public static Vector3 WorldFromAngleDistance(Vector3 origin, float angleDeg, float distance)
        {
            float rad = angleDeg * Mathf.Deg2Rad; // deg -> rad
            float x = Mathf.Cos(rad) * distance;
            float y = Mathf.Sin(rad) * distance;
            return origin + new Vector3(x, y, 0f);
        }

        /// <summary>
        /// 기준 Transform의 로컬 각도를 "로컬 +X 축 기준"으로 해석하여, 월드 방향으로 변환한 뒤 위치를 계산합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="reference">로컬 방향을 월드 방향으로 변환할 기준 Transform입니다.</param>
        /// <param name="localAngleDeg">reference의 로컬 +X 축 기준 각도(도)입니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <returns>reference의 방향을 반영하여 계산된 월드 좌표(Vector3)입니다.</returns>
        /// <remarks>
        /// 예: 총구/캐릭터의 바라보는 방향을 기준으로 로컬 오프셋 각도를 주고 싶을 때 사용합니다.
        /// </remarks>
        public static Vector3 WorldFromLocalAngleDistance(Vector3 origin, Transform reference, float localAngleDeg, float distance)
        {
            // 로컬 +X를 기준으로 각도 벡터 생성
            float rad = localAngleDeg * Mathf.Deg2Rad;
            var localDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            // 로컬 방향 -> 월드 방향 (스케일/포지션 영향 없음)
            Vector3 worldDir = reference.TransformDirection(localDir);

            return origin + (worldDir * distance);
        }

        /// <summary>
        /// Quaternion.AngleAxis로 방향 벡터를 만든 뒤 distance를 곱하여 월드 좌표를 계산합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="angleDeg">각도(도)입니다. Z축(Vector3.forward) 기준으로 회전합니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <returns>계산된 월드 좌표(Vector3)입니다.</returns>
        public static Vector3 WorldFromAngleAxis(Vector3 origin, float angleDeg, float distance)
        {
            // 2D(XY)에서 회전축은 Z
            Vector3 dir = Quaternion.AngleAxis(angleDeg, Vector3.forward) * Vector3.right;
            return origin + (dir * distance);
        }

        /// <summary>
        /// 월드 원점 기준으로 angleDeg 방향에 sideX(+1/-1) 보정을 적용하여 distance만큼 이동한 좌표(Vector2)를 반환합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="angleDeg">각도(도)입니다. 0° = +X 입니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <param name="sideX">X방향 부호입니다. +1은 오른쪽, -1은 왼쪽이며 0은 +1로 보정됩니다.</param>
        /// <returns>계산된 월드 좌표(Vector2)입니다.</returns>
        public static Vector2 WorldFromAngleDistance(
            Vector2 origin,
            float angleDeg,
            float distance,
            int sideX)
        {
            sideX = Mathf.Clamp(sideX, -1, 1);
            if (sideX == 0)
                sideX = 1;

            float rad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * distance * sideX;
            float y = Mathf.Sin(rad) * distance;

            return origin + new Vector2(x, y);
        }

        /// <summary>
        /// reference의 로컬 +X 기준 각도에 sideX(+1/-1) 보정을 적용한 로컬 방향을 월드로 변환하여 위치를 계산합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="reference">로컬 방향을 월드 방향으로 변환할 기준 Transform입니다.</param>
        /// <param name="angleDeg">reference의 로컬 +X 축 기준 각도(도)입니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <param name="sideX">X방향 부호입니다. +1은 오른쪽, -1은 왼쪽이며 0은 +1로 보정됩니다.</param>
        /// <returns>reference의 방향을 반영하여 계산된 월드 좌표(Vector3)입니다.</returns>
        public static Vector3 WorldFromLocalAngleDistance(
            Vector3 origin,
            Transform reference,
            float angleDeg,
            float distance,
            int sideX)
        {
            sideX = Mathf.Clamp(sideX, -1, 1);
            if (sideX == 0)
                sideX = 1;

            float rad = angleDeg * Mathf.Deg2Rad;

            // 로컬 기준 +X 방향
            Vector3 localDir = new Vector3(
                Mathf.Cos(rad) * sideX,
                Mathf.Sin(rad),
                0f
            );

            // 로컬 → 월드 방향 변환
            Vector3 worldDir = reference.TransformDirection(localDir);

            return origin + (worldDir * distance);
        }

        /// <summary>
        /// Quaternion.AngleAxis 기반 계산에 sideX(+1/-1) 보정을 적용하여 월드 좌표를 계산합니다.
        /// </summary>
        /// <param name="origin">기준 월드 좌표입니다.</param>
        /// <param name="angleDeg">각도(도)입니다. Z축(Vector3.forward) 기준으로 회전합니다.</param>
        /// <param name="distance">이동 거리입니다.</param>
        /// <param name="sideX">X방향 부호입니다. +1은 오른쪽, -1은 왼쪽이며 0은 +1로 보정됩니다.</param>
        /// <returns>계산된 월드 좌표(Vector3)입니다.</returns>
        public static Vector3 WorldFromAngleAxis(
            Vector3 origin,
            float angleDeg,
            float distance,
            int sideX)
        {
            sideX = Mathf.Clamp(sideX, -1, 1);
            if (sideX == 0)
                sideX = 1;

            Vector3 baseDir = Vector3.right * sideX;
            Vector3 dir = Quaternion.AngleAxis(angleDeg, Vector3.forward) * baseDir;

            return origin + (dir * distance);
        }

        /// <summary>
        /// 두 Vector2 좌표 사이의 실제 거리(유클리드 거리)를 반환합니다.
        /// </summary>
        /// <param name="a">첫 번째 좌표입니다.</param>
        /// <param name="b">두 번째 좌표입니다.</param>
        /// <returns>두 점 사이의 거리입니다.</returns>
        public static float Distance(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        /// <summary>
        /// 두 Vector2 좌표 사이의 제곱 거리(squared distance)를 반환합니다.
        /// </summary>
        /// <param name="a">첫 번째 좌표입니다.</param>
        /// <param name="b">두 번째 좌표입니다.</param>
        /// <returns>두 점 사이의 제곱 거리입니다. (sqrt를 하지 않아 Update/FixedUpdate 등 빈번 호출에 유리)</returns>
        public static float SqrDistance(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude;
        }
    }
}
