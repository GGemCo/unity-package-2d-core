using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드 위치를 지면에 투영할 때 사용할 레이어 선택 정책입니다.
    /// </summary>
    public enum GroundSurfaceLayerPolicy
    {
        /// <summary>일반 지면과 원웨이 플랫폼을 모두 탐색합니다.</summary>
        DefaultGroundAndOneWay = 0,

        /// <summary>일반 지면만 탐색하고 원웨이 플랫폼은 제외합니다.</summary>
        SolidGroundOnly = 1,

        /// <summary>호출자가 전달한 사용자 정의 레이어 마스크를 사용합니다.</summary>
        Custom = 2,
    }

    /// <summary>
    /// 임의 월드 위치에서 아래 방향으로 지면을 탐색하기 위한 옵션입니다.
    /// </summary>
    public readonly struct GroundSurfaceProbeOptions
    {
        /// <summary>지면 탐색에 사용할 레이어 정책입니다.</summary>
        public GroundSurfaceLayerPolicy LayerPolicy { get; }

        /// <summary><see cref="GroundSurfaceLayerPolicy.Custom"/> 정책에서 사용할 레이어 마스크입니다.</summary>
        public int CustomLayerMask { get; }

        /// <summary>입력 위치보다 Ray 시작점을 위로 올릴 거리입니다.</summary>
        public float ProbeStartUpOffset { get; }

        /// <summary>입력 위치에서 아래쪽으로 탐색할 최대 거리입니다.</summary>
        public float MaxProbeDistance { get; }

        /// <summary>탐색된 표면의 Normal 방향으로 결과 위치를 띄울 거리입니다.</summary>
        public float SurfaceNormalOffset { get; }

        /// <summary>
        /// 지면 표면 탐색 옵션을 생성합니다.
        /// </summary>
        /// <param name="layerPolicy">지면 탐색에 사용할 레이어 정책입니다.</param>
        /// <param name="customLayerMask">사용자 정의 레이어 마스크입니다.</param>
        /// <param name="probeStartUpOffset">Ray 시작점을 위로 올릴 거리입니다.</param>
        /// <param name="maxProbeDistance">입력 위치에서 아래쪽으로 탐색할 최대 거리입니다.</param>
        /// <param name="surfaceNormalOffset">표면 Normal 방향으로 결과 위치를 띄울 거리입니다.</param>
        public GroundSurfaceProbeOptions(
            GroundSurfaceLayerPolicy layerPolicy,
            int customLayerMask,
            float probeStartUpOffset,
            float maxProbeDistance,
            float surfaceNormalOffset)
        {
            LayerPolicy = layerPolicy;
            CustomLayerMask = customLayerMask;
            ProbeStartUpOffset = Mathf.Max(0f, probeStartUpOffset);
            MaxProbeDistance = Mathf.Max(0f, maxProbeDistance);
            SurfaceNormalOffset = surfaceNormalOffset;
        }
    }

    /// <summary>
    /// 지면 표면 탐색에 성공했을 때 반환되는 결과입니다.
    /// </summary>
    public readonly struct GroundSurfaceProbeHit
    {
        /// <summary>표면 Normal 오프셋까지 반영된 최종 월드 위치입니다.</summary>
        public Vector3 Position { get; }

        /// <summary>Physics2D가 탐색한 원본 표면 접점입니다.</summary>
        public Vector2 Point { get; }

        /// <summary>탐색된 지면 표면의 정규화된 Normal입니다.</summary>
        public Vector2 Normal { get; }

        /// <summary>탐색된 지면 Collider입니다.</summary>
        public Collider2D Collider { get; }

        /// <summary>입력 위치에서 원본 표면 접점까지의 수직 하강 거리입니다.</summary>
        public float VerticalDistance { get; }

        /// <summary>탐색된 표면이 프로젝트의 원웨이 플랫폼 레이어인지 여부입니다.</summary>
        public bool IsOneWayPlatform { get; }

        /// <summary>
        /// 지면 표면 탐색 결과를 생성합니다.
        /// </summary>
        /// <param name="position">표면 오프셋까지 반영된 최종 월드 위치입니다.</param>
        /// <param name="point">원본 표면 접점입니다.</param>
        /// <param name="normal">표면 Normal입니다.</param>
        /// <param name="collider">탐색된 Collider입니다.</param>
        /// <param name="verticalDistance">입력 위치에서 표면까지의 수직 하강 거리입니다.</param>
        /// <param name="isOneWayPlatform">원웨이 플랫폼 여부입니다.</param>
        public GroundSurfaceProbeHit(
            Vector3 position,
            Vector2 point,
            Vector2 normal,
            Collider2D collider,
            float verticalDistance,
            bool isOneWayPlatform)
        {
            Position = position;
            Point = point;
            Normal = normal;
            Collider = collider;
            VerticalDistance = verticalDistance;
            IsOneWayPlatform = isOneWayPlatform;
        }
    }

    /// <summary>
    /// 캐릭터에 종속되지 않은 임의 월드 위치를 실제 2D 지면 표면으로 투영합니다.
    /// </summary>
    public static class GroundSurfaceProbeUtility
    {
        private const float NormalSqrMagnitudeEpsilon = 0.000001f;

        /// <summary>
        /// 지정된 월드 위치에서 아래 방향으로 가장 가까운 지면을 탐색하고 표면 위치를 반환합니다.
        /// </summary>
        /// <param name="sourcePosition">지면으로 투영할 원본 월드 위치입니다.</param>
        /// <param name="options">레이어, 탐색 거리 및 표면 오프셋 설정입니다.</param>
        /// <param name="result">탐색된 지면 표면 정보입니다.</param>
        /// <returns>유효한 지면 표면을 찾았으면 <see langword="true"/>입니다.</returns>
        public static bool TryProjectToGround(
            Vector3 sourcePosition,
            in GroundSurfaceProbeOptions options,
            out GroundSurfaceProbeHit result)
        {
            result = default;

            int layerMask = ResolveLayerMask(options.LayerPolicy, options.CustomLayerMask);
            if (layerMask == 0)
                return false;

            float startUpOffset = Mathf.Max(0f, options.ProbeStartUpOffset);
            float probeDistance = Mathf.Max(0f, options.MaxProbeDistance);
            float rayDistance = startUpOffset + probeDistance;
            if (rayDistance <= 0f)
                return false;

            Vector2 origin = new Vector2(sourcePosition.x, sourcePosition.y + startUpOffset);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayDistance, layerMask);
            if (hit.collider == null || hit.collider.isTrigger)
                return false;

            Vector2 normal = hit.normal.sqrMagnitude > NormalSqrMagnitudeEpsilon
                ? hit.normal.normalized
                : Vector2.up;

            // Z축은 2D 물리 탐색에 포함되지 않으므로 호출자가 전달한 원본 값을 유지합니다.
            Vector3 projectedPosition = new Vector3(hit.point.x, hit.point.y, sourcePosition.z);
            projectedPosition += (Vector3)(normal * options.SurfaceNormalOffset);

            int oneWayLayer = LayerMask.NameToLayer(
                ConfigLayer.GetValue(ConfigLayer.Keys.TileMapOneWayPlatform));
            bool isOneWayPlatform = oneWayLayer >= 0 && hit.collider.gameObject.layer == oneWayLayer;
            float verticalDistance = Mathf.Max(0f, sourcePosition.y - hit.point.y);

            result = new GroundSurfaceProbeHit(
                projectedPosition,
                hit.point,
                normal,
                hit.collider,
                verticalDistance,
                isOneWayPlatform);
            return true;
        }

        /// <summary>
        /// 지면 레이어 정책을 실제 Physics2D 레이어 마스크로 변환합니다.
        /// </summary>
        /// <param name="layerPolicy">적용할 지면 레이어 정책입니다.</param>
        /// <param name="customLayerMask">사용자 정의 정책에서 사용할 레이어 마스크입니다.</param>
        /// <returns>Physics2D 지면 탐색에 사용할 레이어 마스크입니다.</returns>
        public static int ResolveLayerMask(
            GroundSurfaceLayerPolicy layerPolicy,
            int customLayerMask = 0)
        {
            switch (layerPolicy)
            {
                case GroundSurfaceLayerPolicy.SolidGroundOnly:
                    return LayerMask.GetMask(ConfigLayer.GetValue(ConfigLayer.Keys.TileMapGround));

                case GroundSurfaceLayerPolicy.Custom:
                    return customLayerMask;

                case GroundSurfaceLayerPolicy.DefaultGroundAndOneWay:
                default:
                    return CharacterGroundProbeUtility.GetDefaultGroundProbeMask();
            }
        }
    }
}
