using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 투사체, 레이저 등 원거리 공격 시스템이 공통으로 사용하는 피격 대상 해석 유틸리티입니다.
    /// - Collider → 실제 피격 캐릭터 해석
    /// - HitArea 해석
    /// - 플레이어/몬스터 간 적대 관계 판정
    /// </summary>
    public static class CombatHitTargetUtility
    {
        /// <summary>
        /// 충돌 Collider에서 실제 데미지를 받을 캐릭터를 해석합니다.
        /// - CharacterHitArea가 있으면 HitArea.target을 우선 사용합니다.
        /// - 없으면 Collider 또는 상위 오브젝트의 CharacterBase를 사용합니다.
        /// </summary>
        /// <param name="other">해석할 충돌 Collider입니다.</param>
        /// <returns>해석된 타겟 캐릭터이며, 찾지 못하면 null을 반환합니다.</returns>
        public static CharacterBase ResolveTargetCharacter(Collider2D other)
        {
            if (!other)
                return null;

            CharacterHitArea hitArea = other.GetComponent<CharacterHitArea>();
            if (hitArea && hitArea.target)
                return hitArea.target;

            CharacterBase direct = other.GetComponent<CharacterBase>();
            if (direct)
                return direct;

            return other.GetComponentInParent<CharacterBase>();
        }

        /// <summary>
        /// 충돌 Collider에서 CharacterHitArea를 해석합니다.
        /// - Collider에 직접 붙은 HitArea를 우선 사용합니다.
        /// - 없으면 타겟 캐릭터 하위에서 탐색합니다.
        /// </summary>
        /// <param name="other">충돌 Collider입니다.</param>
        /// <param name="target">이미 해석된 타겟 캐릭터입니다.</param>
        /// <returns>해석된 HitArea이며, 찾지 못하면 null을 반환합니다.</returns>
        public static CharacterHitArea ResolveHitArea(Collider2D other, CharacterBase target)
        {
            if (!other)
                return null;

            CharacterHitArea area = other.GetComponent<CharacterHitArea>();
            if (area)
                return area;

            return target ? target.GetComponentInChildren<CharacterHitArea>() : null;
        }

        /// <summary>
        /// 시전자와 충돌 Collider의 관계를 검사하여 실제로 데미지를 적용할 수 있는 대상인지 판정합니다.
        /// </summary>
        /// <param name="attacker">공격 시전자입니다.</param>
        /// <param name="other">충돌 Collider입니다.</param>
        /// <param name="target">해석된 타겟 캐릭터를 반환합니다.</param>
        /// <returns>적대 관계의 유효한 피격 대상이면 true를 반환합니다.</returns>
        public static bool TryResolveHostileTarget(CharacterBase attacker, Collider2D other, out CharacterBase target)
        {
            target = null;

            if (!attacker || !other)
                return false;

            if (other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
                return false;

            target = ResolveTargetCharacter(other);
            if (!target)
                return false;

            return IsHostilePair(attacker, target);
        }

        /// <summary>
        /// 두 캐릭터가 플레이어↔몬스터 적대 관계인지 판정합니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <returns>공격 가능 관계이면 true를 반환합니다.</returns>
        public static bool IsHostilePair(CharacterBase attacker, CharacterBase target)
        {
            if (!attacker || !target)
                return false;

            bool fromMonster = attacker.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool fromPlayer = attacker.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster = target.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer = target.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));

            return (fromMonster && toPlayer) || (fromPlayer && toMonster);
        }
    }
}
