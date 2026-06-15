using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CrowdControl 종료 위치를 카메라 화면 안쪽으로 보정할 때 사용하는 계산 컨텍스트입니다.
    /// 캐릭터의 Transform 원점을 기준으로 이동 가능한 X/Y 범위를 보관합니다.
    /// </summary>
    internal readonly struct CrowdControlViewportClampContext
    {
        private readonly float _minAnchorX;
        private readonly float _maxAnchorX;
        private readonly float _minAnchorY;
        private readonly float _maxAnchorY;
        private readonly CrowdControlConstants.EndViewportClampAxis _clampAxis;

        /// <summary>
        /// 화면 경계와 캐릭터 Collider Bounds를 기준으로 보정 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="viewportRect">현재 카메라 화면의 월드 Rect입니다.</param>
        /// <param name="characterBounds">현재 캐릭터 Collider의 월드 Bounds입니다.</param>
        /// <param name="characterAnchor">캐릭터 이동 기준점인 Rigidbody2D 또는 Transform 위치입니다.</param>
        /// <param name="padding">화면 경계와 Collider 사이에 확보할 월드 단위 여백입니다.</param>
        /// <param name="clampAxis">보정할 축 정책입니다.</param>
        public CrowdControlViewportClampContext(
            Rect viewportRect,
            Bounds characterBounds,
            Vector2 characterAnchor,
            float padding,
            CrowdControlConstants.EndViewportClampAxis clampAxis)
        {
            float safePadding = Mathf.Max(0f, padding);
            float minOffsetX = characterBounds.min.x - characterAnchor.x;
            float maxOffsetX = characterBounds.max.x - characterAnchor.x;
            float minOffsetY = characterBounds.min.y - characterAnchor.y;
            float maxOffsetY = characterBounds.max.y - characterAnchor.y;

            ResolveAnchorRange(
                viewportRect.xMin,
                viewportRect.xMax,
                minOffsetX,
                maxOffsetX,
                safePadding,
                out float minAnchorX,
                out float maxAnchorX);

            ResolveAnchorRange(
                viewportRect.yMin,
                viewportRect.yMax,
                minOffsetY,
                maxOffsetY,
                safePadding,
                out float minAnchorY,
                out float maxAnchorY);

            _minAnchorX = minAnchorX;
            _maxAnchorX = maxAnchorX;
            _minAnchorY = minAnchorY;
            _maxAnchorY = maxAnchorY;
            _clampAxis = clampAxis;
        }

        /// <summary>
        /// 지정한 위치의 X 좌표를 캐릭터 Collider가 화면 안에 들어오는 범위로 보정합니다.
        /// </summary>
        /// <param name="position">보정할 CrowdControl 종료 위치입니다.</param>
        /// <returns>X축 보정이 반영된 위치입니다.</returns>
        public Vector2 ClampHorizontal(Vector2 position)
        {
            position.x = Mathf.Clamp(position.x, _minAnchorX, _maxAnchorX);
            return position;
        }

        /// <summary>
        /// 축 정책이 Both인 경우 지정한 위치의 Y 좌표를 화면 안쪽으로 보정합니다.
        /// </summary>
        /// <param name="position">보정할 CrowdControl 종료 위치입니다.</param>
        /// <returns>Y축 보정이 반영된 위치입니다.</returns>
        public Vector2 ClampVertical(Vector2 position)
        {
            if (_clampAxis == CrowdControlConstants.EndViewportClampAxis.Both)
                position.y = Mathf.Clamp(position.y, _minAnchorY, _maxAnchorY);

            return position;
        }

        /// <summary>
        /// 화면 범위와 Collider의 기준점 오프셋을 사용해 캐릭터 Anchor가 이동할 수 있는 범위를 계산합니다.
        /// 캐릭터가 화면보다 큰 경우에는 Collider 중심이 화면 중심에 오도록 단일 위치로 고정합니다.
        /// </summary>
        /// <param name="viewportMin">화면 축의 최소 월드 좌표입니다.</param>
        /// <param name="viewportMax">화면 축의 최대 월드 좌표입니다.</param>
        /// <param name="boundsMinOffset">캐릭터 Anchor에서 Collider 최소점까지의 오프셋입니다.</param>
        /// <param name="boundsMaxOffset">캐릭터 Anchor에서 Collider 최대점까지의 오프셋입니다.</param>
        /// <param name="padding">화면 경계 여백입니다.</param>
        /// <param name="minAnchor">계산된 Anchor 최소 좌표입니다.</param>
        /// <param name="maxAnchor">계산된 Anchor 최대 좌표입니다.</param>
        private static void ResolveAnchorRange(
            float viewportMin,
            float viewportMax,
            float boundsMinOffset,
            float boundsMaxOffset,
            float padding,
            out float minAnchor,
            out float maxAnchor)
        {
            minAnchor = viewportMin + padding - boundsMinOffset;
            maxAnchor = viewportMax - padding - boundsMaxOffset;

            if (minAnchor <= maxAnchor)
                return;

            float viewportCenter = (viewportMin + viewportMax) * 0.5f;
            float boundsCenterOffset = (boundsMinOffset + boundsMaxOffset) * 0.5f;
            float centeredAnchor = viewportCenter - boundsCenterOffset;
            minAnchor = centeredAnchor;
            maxAnchor = centeredAnchor;
        }
    }

    /// <summary>
    /// 플레이어 CrowdControl 종료 위치의 화면 경계 보정 정책을 해석합니다.
    /// </summary>
    internal static class CrowdControlEndViewportResolver
    {
        /// <summary>
        /// 현재 캐릭터, 맵, 카메라 상태를 기준으로 화면 경계 보정 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="character">CrowdControl이 적용되는 캐릭터입니다.</param>
        /// <param name="rigidbody2D">캐릭터의 이동 기준 Rigidbody2D입니다.</param>
        /// <param name="crowdControl">화면 경계 정책이 포함된 CrowdControl 런타임 데이터입니다.</param>
        /// <param name="context">생성된 화면 경계 보정 컨텍스트입니다.</param>
        /// <returns>화면 경계 보정을 적용할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        public static bool TryCreateContext(
            CharacterBase character,
            Rigidbody2D rigidbody2D,
            CrowdControlRuntimeData crowdControl,
            out CrowdControlViewportClampContext context)
        {
            context = default;

            if (character == null || crowdControl == null || !character.IsPlayer())
                return false;

            if (crowdControl.EndViewportPolicy == CrowdControlConstants.EndViewportPolicy.None)
                return false;

            SceneGame sceneGame = SceneGame.Instance;
            CameraManager cameraManager = sceneGame != null ? sceneGame.cameraManager : null;
            if (cameraManager == null)
                return false;

            if (ShouldSkipForFreeCameraFollow(character, crowdControl, sceneGame, cameraManager))
                return false;

            if (!cameraManager.TryGetBaseViewportWorldRect(out Rect viewportRect))
                return false;

            if (!CharacterGroundProbeUtility.TryGetCharacterWorldBounds(character, rigidbody2D, out Bounds characterBounds))
                return false;

            Vector2 characterAnchor = rigidbody2D != null
                ? rigidbody2D.position
                : (Vector2)character.transform.position;

            context = new CrowdControlViewportClampContext(
                viewportRect,
                characterBounds,
                characterAnchor,
                crowdControl.EndViewportPadding,
                crowdControl.EndViewportClampAxis);
            return true;
        }

        /// <summary>
        /// UseParallax 맵에서 게임 카메라가 플레이어를 정상 추적할 수 있는 경우
        /// 화면 경계 보정을 생략할지 확인합니다.
        /// </summary>
        /// <param name="character">CrowdControl이 적용되는 플레이어 캐릭터입니다.</param>
        /// <param name="crowdControl">화면 경계 예외 정책이 포함된 CrowdControl 데이터입니다.</param>
        /// <param name="sceneGame">현재 게임 씬 관리자입니다.</param>
        /// <param name="cameraManager">현재 게임 카메라 관리자입니다.</param>
        /// <returns>카메라 추적을 허용하기 위해 화면 보정을 생략해야 하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool ShouldSkipForFreeCameraFollow(
            CharacterBase character,
            CrowdControlRuntimeData crowdControl,
            SceneGame sceneGame,
            CameraManager cameraManager)
        {
            if (crowdControl.EndViewportPolicy !=
                CrowdControlConstants.EndViewportPolicy.ClampPlayerExceptFreeCameraFollow)
            {
                return false;
            }

            StruckTableMap mapData = sceneGame != null && sceneGame.mapManager != null
                ? sceneGame.mapManager.GetCurrentMapTableData()
                : null;

            return mapData != null &&
                   mapData.UseParallax &&
                   cameraManager.CanGameplayFollowTarget(character.transform);
        }
    }
}
