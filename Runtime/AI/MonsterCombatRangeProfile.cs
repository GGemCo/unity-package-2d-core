using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 전투에서 서로 다른 책임을 가진 범위 값을 런타임용으로 정규화한 불변 프로필입니다.
    /// </summary>
    /// <remarks>
    /// 실제 피해 판정 영역은 <see cref="CharacterBase.colliderAttackRange"/> Collider가 담당하고,
    /// 이 프로필은 감지, 기본 공격 시작, 선호 거리, 추적 한계와 리시 정책의 논리 범위만 담당합니다.
    /// </remarks>
    public readonly struct MonsterCombatRangeProfile
    {
        private const float MinimumRange = 0.01f;
        private const float DefaultFallbackRange = 1f;

        /// <summary>몬스터 중심 기준 선공 감지 X축 반경입니다.</summary>
        public float DetectionRangeX { get; }

        /// <summary>몬스터 중심 기준 선공 감지 Y축 반경입니다.</summary>
        public float DetectionRangeY { get; }

        /// <summary>감지 해제 X축 반경입니다.</summary>
        public float DetectionExitRangeX { get; }

        /// <summary>감지 해제 Y축 반경입니다.</summary>
        public float DetectionExitRangeY { get; }

        /// <summary>기본 공격을 시작할 수 있는 X축 거리입니다.</summary>
        public float BasicAttackRangeX { get; }

        /// <summary>기본 공격을 시작할 수 있는 Y축 거리입니다.</summary>
        public float BasicAttackRangeY { get; }

        /// <summary>몬스터가 유지하려는 최소 전투 거리입니다.</summary>
        public float PreferredRangeMin { get; }

        /// <summary>몬스터가 유지하려는 최대 전투 거리입니다.</summary>
        public float PreferredRangeMax { get; }

        /// <summary>타겟 추적을 포기할 2D 거리입니다. 0 이하면 별도 추적 확장 범위를 사용하지 않습니다.</summary>
        public float ChaseRange { get; }

        /// <summary>홈 위치 기준 소프트 리시 거리입니다. 0 이하면 비활성입니다.</summary>
        public float SoftLeashRange { get; }

        /// <summary>홈 위치 기준 하드 리시 거리입니다. 0 이하면 비활성입니다.</summary>
        public float HardLeashRange { get; }

        /// <summary>monster_combat_profile 테이블 행을 명시적으로 적용했는지 여부입니다.</summary>
        public bool IsConfigured { get; }

        /// <summary>감지 범위가 유효하게 설정되었는지 여부입니다.</summary>
        public bool IsDetectionEnabled => DetectionRangeX > 0f && DetectionRangeY > 0f;

        /// <summary>추적 거리 제한을 사용하는지 여부입니다.</summary>
        public bool HasChaseLimit => ChaseRange > 0f;

        /// <summary>
        /// 정규화된 몬스터 전투 범위 프로필을 생성합니다.
        /// </summary>
        private MonsterCombatRangeProfile(
            bool isConfigured,
            float detectionRangeX,
            float detectionRangeY,
            float detectionExitRangeX,
            float detectionExitRangeY,
            float basicAttackRangeX,
            float basicAttackRangeY,
            float preferredRangeMin,
            float preferredRangeMax,
            float chaseRange,
            float softLeashRange,
            float hardLeashRange)
        {
            IsConfigured = isConfigured;
            DetectionRangeX = detectionRangeX;
            DetectionRangeY = detectionRangeY;
            DetectionExitRangeX = detectionExitRangeX;
            DetectionExitRangeY = detectionExitRangeY;
            BasicAttackRangeX = basicAttackRangeX;
            BasicAttackRangeY = basicAttackRangeY;
            PreferredRangeMin = preferredRangeMin;
            PreferredRangeMax = preferredRangeMax;
            ChaseRange = chaseRange;
            SoftLeashRange = softLeashRange;
            HardLeashRange = hardLeashRange;
        }

        /// <summary>
        /// 테이블 데이터와 기존 공격 판정 Collider를 조합하여 런타임 프로필을 생성합니다.
        /// </summary>
        /// <param name="tableData">선택한 monster_combat_profile 테이블 행입니다.</param>
        /// <param name="actualHitArea">기존 일반 공격의 실제 피해 판정 Collider입니다.</param>
        /// <returns>기존 데이터와 신규 데이터를 모두 지원하는 정규화된 범위 프로필입니다.</returns>
        /// <remarks>
        /// 신규 프로필이 없거나 특정 값이 0 이하이면 기존 공격 Collider의 월드 반경을 호환값으로 사용합니다.
        /// Chase/Leash 값은 0을 명시적인 비활성 값으로 유지합니다.
        /// </remarks>
        public static MonsterCombatRangeProfile Create(
            StruckTableMonsterCombatProfile tableData,
            CapsuleCollider2D actualHitArea)
        {
            Vector2 legacyExtents = ResolveLegacyHitAreaExtents(actualHitArea);
            float fallbackX = Mathf.Max(MinimumRange, legacyExtents.x);
            float fallbackY = Mathf.Max(MinimumRange, legacyExtents.y);

            float detectionRangeX = ResolvePositive(tableData?.DetectionRangeX ?? 0f, fallbackX);
            float detectionRangeY = ResolvePositive(tableData?.DetectionRangeY ?? 0f, fallbackY);
            float detectionExitRangeX = Mathf.Max(
                detectionRangeX,
                ResolvePositive(tableData?.DetectionExitRangeX ?? 0f, detectionRangeX));
            float detectionExitRangeY = Mathf.Max(
                detectionRangeY,
                ResolvePositive(tableData?.DetectionExitRangeY ?? 0f, detectionRangeY));

            float basicAttackRangeX = ResolvePositive(tableData?.BasicAttackRangeX ?? 0f, fallbackX);
            float basicAttackRangeY = ResolvePositive(tableData?.BasicAttackRangeY ?? 0f, fallbackY);
            float preferredRangeMax = ResolvePositive(tableData?.PreferredRangeMax ?? 0f, basicAttackRangeX);
            float preferredRangeMin = Mathf.Clamp(tableData?.PreferredRangeMin ?? 0f, 0f, preferredRangeMax);

            float chaseRange = Mathf.Max(0f, tableData?.ChaseRange ?? 0f);
            float softLeashRange = Mathf.Max(0f, tableData?.SoftLeashRange ?? 0f);
            float hardLeashRange = Mathf.Max(0f, tableData?.HardLeashRange ?? 0f);
            if (softLeashRange > 0f && hardLeashRange > 0f)
            {
                hardLeashRange = Mathf.Max(softLeashRange, hardLeashRange);
            }

            return new MonsterCombatRangeProfile(
                tableData != null,
                detectionRangeX,
                detectionRangeY,
                detectionExitRangeX,
                detectionExitRangeY,
                basicAttackRangeX,
                basicAttackRangeY,
                preferredRangeMin,
                preferredRangeMax,
                chaseRange,
                softLeashRange,
                hardLeashRange);
        }

        /// <summary>
        /// X/Y축 거리가 기본 공격 시작 범위 안인지 확인합니다.
        /// </summary>
        /// <param name="horizontalDistance">타겟 HitArea 가장자리까지의 X축 거리입니다.</param>
        /// <param name="verticalDistance">타겟 HitArea 가장자리까지의 Y축 거리입니다.</param>
        /// <returns>기본 공격을 시작할 수 있으면 <see langword="true"/>입니다.</returns>
        public bool IsWithinBasicAttackRange(float horizontalDistance, float verticalDistance)
        {
            return horizontalDistance >= 0f &&
                   verticalDistance >= 0f &&
                   horizontalDistance <= BasicAttackRangeX &&
                   verticalDistance <= BasicAttackRangeY;
        }

        /// <summary>
        /// X축 거리가 선호 전투 거리 구간 안이고 Y축 거리가 기본 공격 허용 범위 안인지 확인합니다.
        /// </summary>
        /// <param name="horizontalDistance">타겟 HitArea 가장자리까지의 X축 거리입니다.</param>
        /// <param name="verticalDistance">타겟 HitArea 가장자리까지의 Y축 거리입니다.</param>
        /// <returns>선호 거리 구간과 수직 허용 범위를 모두 만족하면 <see langword="true"/>입니다.</returns>
        public bool IsWithinPreferredRange(float horizontalDistance, float verticalDistance)
        {
            return horizontalDistance >= PreferredRangeMin &&
                   horizontalDistance <= PreferredRangeMax &&
                   verticalDistance >= 0f &&
                   verticalDistance <= BasicAttackRangeY;
        }

        /// <summary>
        /// 2D 거리가 추적 한계를 초과했는지 확인합니다.
        /// </summary>
        /// <param name="distance2D">몬스터와 타겟 사이의 2D 거리입니다.</param>
        /// <returns>추적 한계가 활성화되어 있고 거리를 초과했으면 <see langword="true"/>입니다.</returns>
        public bool IsBeyondChaseRange(float distance2D)
        {
            return HasChaseLimit && distance2D > ChaseRange;
        }

        /// <summary>
        /// 기존 실제 공격 Collider의 월드 공간 반경을 계산합니다.
        /// </summary>
        /// <param name="actualHitArea">실제 공격 판정용 CapsuleCollider2D입니다.</param>
        /// <returns>Collider 중심 오프셋을 포함한 X/Y 반경입니다.</returns>
        private static Vector2 ResolveLegacyHitAreaExtents(CapsuleCollider2D actualHitArea)
        {
            if (actualHitArea == null)
            {
                return Vector2.one * DefaultFallbackRange;
            }

            Vector3 scale = actualHitArea.transform.lossyScale;
            float scaleX = Mathf.Abs(scale.x);
            float scaleY = Mathf.Abs(scale.y);
            float extentX = actualHitArea.size.x * scaleX * 0.5f + Mathf.Abs(actualHitArea.offset.x * scaleX);
            float extentY = actualHitArea.size.y * scaleY * 0.5f + Mathf.Abs(actualHitArea.offset.y * scaleY);
            return new Vector2(extentX, extentY);
        }

        /// <summary>
        /// 0보다 큰 값을 우선 사용하고, 유효하지 않으면 호환 기본값을 반환합니다.
        /// </summary>
        /// <param name="value">우선 적용할 설정값입니다.</param>
        /// <param name="fallback">설정값이 유효하지 않을 때 사용할 호환값입니다.</param>
        /// <returns>0보다 큰 정규화된 범위 값입니다.</returns>
        private static float ResolvePositive(float value, float fallback)
        {
            return value > 0f ? value : Mathf.Max(MinimumRange, fallback);
        }
    }

    /// <summary>
    /// 몬스터와 현재 타겟 사이의 거리 계산을 공통화하는 유틸리티입니다.
    /// </summary>
    public static class MonsterCombatRangeMath
    {
        /// <summary>
        /// 몬스터와 타겟 사이의 X/Y축 거리와 2D 중심 거리를 한 번에 계산합니다.
        /// </summary>
        /// <param name="owner">거리 기준이 되는 몬스터 Transform입니다.</param>
        /// <param name="target">현재 전투 타겟 Transform입니다.</param>
        /// <param name="horizontalDistance">타겟 HitArea 가장자리까지의 X축 거리입니다.</param>
        /// <param name="verticalDistance">타겟 HitArea 가장자리까지의 Y축 거리입니다.</param>
        /// <param name="distance2D">몬스터와 타겟 중심 사이의 2D 거리입니다.</param>
        /// <returns>유효한 Transform 두 개로 거리를 계산했으면 <see langword="true"/>입니다.</returns>
        public static bool TryGetDistances(
            Transform owner,
            Transform target,
            out float horizontalDistance,
            out float verticalDistance,
            out float distance2D)
        {
            horizontalDistance = -1f;
            verticalDistance = -1f;
            distance2D = -1f;
            if (owner == null || target == null)
            {
                return false;
            }

            Vector3 ownerPosition = owner.position;
            Vector3 targetPosition = target.position;
            Vector2 centerDelta = targetPosition - ownerPosition;
            distance2D = centerDelta.magnitude;

            if (TryResolveTargetHitArea(target, out Collider2D hitArea) &&
                hitArea.enabled &&
                hitArea.gameObject.activeInHierarchy)
            {
                Bounds bounds = hitArea.bounds;
                horizontalDistance = GetDistanceToBoundsAxis(ownerPosition.x, bounds.min.x, bounds.max.x);
                verticalDistance = GetDistanceToBoundsAxis(ownerPosition.y, bounds.min.y, bounds.max.y);
                return true;
            }

            horizontalDistance = Mathf.Abs(targetPosition.x - ownerPosition.x);
            verticalDistance = Mathf.Abs(targetPosition.y - ownerPosition.y);
            return true;
        }

        /// <summary>
        /// 몬스터 원점에서 타겟 HitArea 가장자리까지의 수평 거리를 계산합니다.
        /// </summary>
        /// <param name="owner">거리 기준이 되는 몬스터 Transform입니다.</param>
        /// <param name="target">현재 전투 타겟 Transform입니다.</param>
        /// <returns>수평 거리입니다. 유효한 대상이 없으면 -1을 반환합니다.</returns>
        public static float GetHorizontalDistance(Transform owner, Transform target)
        {
            if (owner == null || target == null)
            {
                return -1f;
            }

            if (TryResolveTargetHitArea(target, out Collider2D hitArea) && hitArea.enabled && hitArea.gameObject.activeInHierarchy)
            {
                Bounds bounds = hitArea.bounds;
                return GetDistanceToBoundsAxis(owner.position.x, bounds.min.x, bounds.max.x);
            }

            return Mathf.Abs(target.position.x - owner.position.x);
        }

        /// <summary>
        /// 몬스터 원점에서 타겟 HitArea 가장자리까지의 수직 거리를 계산합니다.
        /// </summary>
        /// <param name="owner">거리 기준이 되는 몬스터 Transform입니다.</param>
        /// <param name="target">현재 전투 타겟 Transform입니다.</param>
        /// <returns>수직 거리입니다. 유효한 대상이 없으면 -1을 반환합니다.</returns>
        public static float GetVerticalDistance(Transform owner, Transform target)
        {
            if (owner == null || target == null)
            {
                return -1f;
            }

            if (TryResolveTargetHitArea(target, out Collider2D hitArea) && hitArea.enabled && hitArea.gameObject.activeInHierarchy)
            {
                Bounds bounds = hitArea.bounds;
                return GetDistanceToBoundsAxis(owner.position.y, bounds.min.y, bounds.max.y);
            }

            return Mathf.Abs(target.position.y - owner.position.y);
        }

        /// <summary>
        /// 몬스터와 타겟 사이의 2D 중심 거리를 계산합니다.
        /// </summary>
        /// <param name="owner">거리 기준이 되는 몬스터 Transform입니다.</param>
        /// <param name="target">현재 전투 타겟 Transform입니다.</param>
        /// <returns>2D 거리입니다. 유효한 대상이 없으면 -1을 반환합니다.</returns>
        public static float GetDistance2D(Transform owner, Transform target)
        {
            if (owner == null || target == null)
            {
                return -1f;
            }

            Vector2 delta = target.position - owner.position;
            return delta.magnitude;
        }

        /// <summary>
        /// 타겟이 몬스터 중심 기준 X/Y 감지 반경 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="owner">감지 주체인 몬스터 Transform입니다.</param>
        /// <param name="target">확인할 타겟 Transform입니다.</param>
        /// <param name="rangeX">X축 반경입니다.</param>
        /// <param name="rangeY">Y축 반경입니다.</param>
        /// <returns>감지 범위 안이면 <see langword="true"/>입니다.</returns>
        public static bool IsWithinAxisAlignedRange(
            Transform owner,
            Transform target,
            float rangeX,
            float rangeY)
        {
            if (owner == null || target == null || rangeX <= 0f || rangeY <= 0f)
            {
                return false;
            }

            Vector2 ownerPosition = owner.position;
            Vector2 targetPoint = target.position;
            if (TryResolveTargetHitArea(target, out Collider2D hitArea) && hitArea.enabled && hitArea.gameObject.activeInHierarchy)
            {
                targetPoint = hitArea.ClosestPoint(ownerPosition);
            }

            Vector2 delta = targetPoint - ownerPosition;
            return Mathf.Abs(delta.x) <= rangeX && Mathf.Abs(delta.y) <= rangeY;
        }

        /// <summary>
        /// 한 축의 기준 좌표에서 Bounds 구간까지의 최단 거리를 계산합니다.
        /// </summary>
        /// <param name="origin">거리 기준 축 좌표입니다.</param>
        /// <param name="minimum">Bounds의 최소 축 좌표입니다.</param>
        /// <param name="maximum">Bounds의 최대 축 좌표입니다.</param>
        /// <returns>Bounds 구간까지의 최단 거리입니다.</returns>
        private static float GetDistanceToBoundsAxis(float origin, float minimum, float maximum)
        {
            if (origin < minimum)
            {
                return minimum - origin;
            }

            if (origin > maximum)
            {
                return origin - maximum;
            }

            return 0f;
        }

        /// <summary>
        /// 타겟 Transform 또는 부모 계층에서 캐릭터 HitArea Collider를 찾습니다.
        /// </summary>
        /// <param name="target">HitArea를 찾을 대상 Transform입니다.</param>
        /// <param name="hitArea">찾은 HitArea Collider입니다.</param>
        /// <returns>유효한 HitArea Collider를 찾으면 <see langword="true"/>입니다.</returns>
        private static bool TryResolveTargetHitArea(Transform target, out Collider2D hitArea)
        {
            hitArea = null;
            if (target == null)
            {
                return false;
            }

            CharacterBase character = target.GetComponent<CharacterBase>() ?? target.GetComponentInParent<CharacterBase>();
            if (character != null && character.colliderHitArea != null)
            {
                hitArea = character.colliderHitArea;
                return true;
            }

            CharacterHitArea area = target.GetComponent<CharacterHitArea>() ?? target.GetComponentInChildren<CharacterHitArea>();
            if (area == null)
            {
                return false;
            }

            hitArea = area.GetComponent<Collider2D>();
            return hitArea != null;
        }
    }
}
