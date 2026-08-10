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
        /// 설정된 축 정책에 따라 지정한 위치를 화면 안쪽의 유효한 Anchor 범위로 보정합니다.
        /// </summary>
        /// <param name="position">보정할 캐릭터 Anchor 위치입니다.</param>
        /// <returns>화면 경계 보정이 반영된 위치입니다.</returns>
        public Vector2 Clamp(Vector2 position)
        {
            return ClampVertical(ClampHorizontal(position));
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
    /// 플레이어와 몬스터 CrowdControl의 화면 경계 보정 정책을 해석합니다.
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

            if (character == null || crowdControl == null || !IsPolicyApplicableToCharacter(character, crowdControl.EndViewportPolicy))
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
        /// 지정한 화면 경계 정책이 대상 캐릭터 종류에 적용되는지 확인합니다.
        /// </summary>
        /// <param name="character">CrowdControl 대상 캐릭터입니다.</param>
        /// <param name="policy">검사할 화면 경계 정책입니다.</param>
        /// <returns>정책이 대상 캐릭터에 적용되면 <see langword="true"/>입니다.</returns>
        public static bool IsPolicyApplicableToCharacter(
            CharacterBase character,
            CrowdControlConstants.EndViewportPolicy policy)
        {
            if (character == null)
                return false;

            switch (policy)
            {
                case CrowdControlConstants.EndViewportPolicy.ClampPlayerToViewport:
                case CrowdControlConstants.EndViewportPolicy.ClampPlayerExceptFreeCameraFollow:
                    return character.IsPlayer();

                case CrowdControlConstants.EndViewportPolicy.ClampCombatCharacterToViewport:
                case CrowdControlConstants.EndViewportPolicy.ClampCombatCharacterExceptFreeCameraFollow:
                    return character.IsPlayer() || character.IsMonster();

                case CrowdControlConstants.EndViewportPolicy.None:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 지정한 정책이 플레이어와 몬스터를 함께 제한하는 신규 공통 정책인지 확인합니다.
        /// </summary>
        /// <param name="policy">검사할 화면 경계 정책입니다.</param>
        /// <returns>공통 전투 캐릭터 정책이면 <see langword="true"/>입니다.</returns>
        public static bool IsCombatCharacterPolicy(CrowdControlConstants.EndViewportPolicy policy)
        {
            return policy == CrowdControlConstants.EndViewportPolicy.ClampCombatCharacterToViewport ||
                   policy == CrowdControlConstants.EndViewportPolicy.ClampCombatCharacterExceptFreeCameraFollow;
        }

        /// <summary>
        /// 캐릭터 Collider 전체가 현재 기본 카메라 Viewport 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="character">검사할 캐릭터입니다.</param>
        /// <param name="rigidbody2D">캐릭터 이동 기준 Rigidbody2D입니다.</param>
        /// <returns>Collider Bounds 전체가 화면 안에 있으면 <see langword="true"/>입니다.</returns>
        public static bool IsCharacterFullyInsideViewport(CharacterBase character, Rigidbody2D rigidbody2D)
        {
            SceneGame sceneGame = SceneGame.Instance;
            CameraManager cameraManager = sceneGame != null ? sceneGame.cameraManager : null;
            if (character == null || cameraManager == null ||
                !cameraManager.TryGetBaseViewportWorldRect(out Rect viewportRect) ||
                !CharacterGroundProbeUtility.TryGetCharacterWorldBounds(character, rigidbody2D, out Bounds characterBounds))
            {
                return false;
            }

            return characterBounds.min.x >= viewportRect.xMin &&
                   characterBounds.max.x <= viewportRect.xMax &&
                   characterBounds.min.y >= viewportRect.yMin &&
                   characterBounds.max.y <= viewportRect.yMax;
        }

        /// <summary>
        /// UseParallax 맵에서 게임 카메라가 대상 캐릭터를 정상 추적할 수 있는 경우
        /// 화면 경계 보정을 생략할지 확인합니다.
        /// </summary>
        /// <param name="character">CrowdControl이 적용되는 대상 캐릭터입니다.</param>
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
                    CrowdControlConstants.EndViewportPolicy.ClampPlayerExceptFreeCameraFollow &&
                crowdControl.EndViewportPolicy !=
                    CrowdControlConstants.EndViewportPolicy.ClampCombatCharacterExceptFreeCameraFollow)
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

    /// <summary>
    /// CrowdControl 종료 위치와 이동 중 증분 이동량에 동일한 화면 경계 정책을 적용합니다.
    /// </summary>
    /// <remarks>
    /// 인스턴스는 캐릭터 컨트롤러가 재사용하며, 모션 Tick 중에는 객체나 컬렉션을 새로 생성하지 않습니다.
    /// 신규 전투 캐릭터 정책은 CC 시작 시 화면 안에 있던 대상만 제한하여 화면 밖 몬스터가 순간이동하지 않도록 합니다.
    /// </remarks>
    internal sealed class CrowdControlViewportMotionConstraint : ICharacterMotionPositionConstraint2D
    {
        private CharacterBase _character;
        private Rigidbody2D _rigidbody2D;
        private CrowdControlRuntimeData _crowdControl;
        private bool _isEligible;

        /// <summary>
        /// 현재 CrowdControl과 캐릭터 상태를 기준으로 재사용 가능한 화면 경계 제약을 구성합니다.
        /// </summary>
        /// <param name="character">CrowdControl 대상 캐릭터입니다.</param>
        /// <param name="rigidbody2D">캐릭터 이동 기준 Rigidbody2D입니다.</param>
        /// <param name="crowdControl">화면 경계 정책을 포함한 CrowdControl 데이터입니다.</param>
        public void Configure(
            CharacterBase character,
            Rigidbody2D rigidbody2D,
            CrowdControlRuntimeData crowdControl)
        {
            _character = character;
            _rigidbody2D = rigidbody2D;
            _crowdControl = crowdControl;
            _isEligible = CrowdControlEndViewportResolver.TryCreateContext(
                _character,
                _rigidbody2D,
                _crowdControl,
                out _);

            if (!_isEligible || !CrowdControlEndViewportResolver.IsCombatCharacterPolicy(crowdControl.EndViewportPolicy))
                return;

            // 화면 밖에서 대기 중인 몬스터를 CC 적용과 동시에 화면 안으로 순간이동시키지 않습니다.
            // 초기 진입 판정에는 정책 padding을 적용하지 않아 화면 가장자리의 정상 캐릭터도 보호합니다.
            _isEligible = CrowdControlEndViewportResolver.IsCharacterFullyInsideViewport(_character, _rigidbody2D);
        }

        /// <summary>
        /// 현재 CC의 종료 위치 보정에 사용할 최신 Viewport 컨텍스트를 반환합니다.
        /// </summary>
        /// <param name="context">현재 카메라와 Collider 기준 화면 경계 컨텍스트입니다.</param>
        /// <returns>이 CC에 화면 경계 정책을 적용할 수 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetCurrentContext(out CrowdControlViewportClampContext context)
        {
            context = default;
            return _isEligible &&
                   CrowdControlEndViewportResolver.TryCreateContext(
                       _character,
                       _rigidbody2D,
                       _crowdControl,
                       out context);
        }

        /// <summary>
        /// 현재 CC가 이동 중 화면 경계 제약을 사용하도록 설정되었는지 확인합니다.
        /// </summary>
        /// <returns>이동 중과 종료 시 모두 보정해야 하면 <see langword="true"/>입니다.</returns>
        public bool ShouldConstrainDuringMotion()
        {
            return _isEligible &&
                   _crowdControl != null &&
                   _crowdControl.ViewportConstraintPhase == CrowdControlConstants.ViewportConstraintPhase.DuringAndEnd;
        }

        /// <inheritdoc />
        public bool TryConstrain(
            Vector2 currentPosition,
            Vector2 requestedDelta,
            out MotionPositionConstraintResult result)
        {
            result = new MotionPositionConstraintResult(requestedDelta, false, false);
            if (!ShouldConstrainDuringMotion() || !TryGetCurrentContext(out CrowdControlViewportClampContext context))
                return false;

            Vector2 requestedPosition = currentPosition + requestedDelta;
            Vector2 constrainedPosition = context.Clamp(requestedPosition);
            Vector2 constrainedDelta = constrainedPosition - currentPosition;
            bool horizontalConstrained = Mathf.Abs(constrainedDelta.x - requestedDelta.x) > CharacterCrowdControlController.Epsilon;
            bool verticalConstrained = Mathf.Abs(constrainedDelta.y - requestedDelta.y) > CharacterCrowdControlController.Epsilon;

            result = new MotionPositionConstraintResult(
                constrainedDelta,
                horizontalConstrained,
                verticalConstrained);
            return true;
        }
    }
}
