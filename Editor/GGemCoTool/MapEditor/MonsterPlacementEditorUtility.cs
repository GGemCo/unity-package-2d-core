using GGemCo2DCore;
using TMPro;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 배치툴에서 몬스터의 표시 및 전투 Override 정책을 리젠 데이터에 일관되게 적용합니다.
    /// </summary>
    public static class MonsterPlacementEditorUtility
    {
        /// <summary>
        /// 몬스터에 리젠 데이터가 없으면 현재 배치 상태를 기반으로 생성하여 연결합니다.
        /// </summary>
        /// <param name="monster">대상 몬스터입니다.</param>
        /// <param name="fallbackMapUid">맵 UID 추론 실패 시 사용할 대체 맵 UID입니다.</param>
        /// <returns>보정된 리젠 데이터이며, 대상이 없으면 <see langword="null"/>입니다.</returns>
        public static CharacterRegenData EnsureRegenData(Monster monster, int fallbackMapUid)
        {
            if (!monster)
            {
                return null;
            }

            if (monster.CharacterRegenData != null)
            {
                return monster.CharacterRegenData;
            }

            CharacterRegenData regenData = new CharacterRegenData(
                monster.uid,
                monster.transform.position,
                monster.isFlip,
                ResolveMapUid(monster, fallbackMapUid),
                defaultVisible: true,
                canMoveX: monster.canMoveX,
                canMoveY: monster.canMoveY,
                mapVisibilityPolicy: monster.MapVisibilityPolicy,
                combatProfileUidOverride: null);

            monster.CharacterRegenData = regenData;
            return regenData;
        }

        /// <summary>
        /// 몬스터의 현재 맵 표시 정책을 조회합니다.
        /// </summary>
        /// <param name="monster">조회할 몬스터입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <returns>현재 맵 표시 정책입니다.</returns>
        public static MapCharacterVisibilityPolicy GetMapVisibilityPolicy(Monster monster, int fallbackMapUid)
        {
            CharacterRegenData regenData = EnsureRegenData(monster, fallbackMapUid);
            if (regenData == null)
            {
                return MapCharacterVisibilityPolicy.DefaultCulling;
            }

            return regenData.MapVisibilityPolicy;
        }

        /// <summary>
        /// 몬스터의 현재 맵 배치 AttackType Override 값을 조회합니다.
        /// </summary>
        /// <param name="monster">조회할 몬스터입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <returns>배치별 Override 값이며, 값이 없으면 monster 테이블의 기본값을 사용합니다.</returns>
        public static CharacterConstants.AttackType? GetAttackTypeOverride(
            Monster monster,
            int fallbackMapUid)
        {
            CharacterRegenData regenData = EnsureRegenData(monster, fallbackMapUid);
            if (regenData == null || !regenData.HasAttackTypeOverride)
            {
                return null;
            }

            return regenData.AttackTypeOverride;
        }

        /// <summary>
        /// 몬스터의 현재 맵 배치 CombatProfileUid Override 값을 조회합니다.
        /// </summary>
        /// <param name="monster">조회할 몬스터입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <returns>배치별 Override 값이며, null이면 monster 테이블의 기본값을 사용합니다.</returns>
        public static int? GetCombatProfileUidOverride(
            Monster monster,
            int fallbackMapUid)
        {
            CharacterRegenData regenData = EnsureRegenData(
                monster,
                fallbackMapUid);
            if (regenData == null || !regenData.HasCombatProfileUidOverride)
            {
                return null;
            }

            return Mathf.Max(0, regenData.CombatProfileUidOverride);
        }

        /// <summary>
        /// 몬스터의 맵 표시 정책을 리젠 데이터와 런타임 상태에 함께 적용합니다.
        /// </summary>
        /// <param name="monster">적용 대상 몬스터입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <param name="mapVisibilityPolicy">적용할 맵 표시 정책입니다.</param>
        public static void ApplyMapVisibilityPolicy(
            Monster monster,
            int fallbackMapUid,
            MapCharacterVisibilityPolicy mapVisibilityPolicy)
        {
            if (!monster)
            {
                return;
            }

            CharacterRegenData regenData = EnsureRegenData(monster, fallbackMapUid);
            if (regenData == null)
            {
                return;
            }

            regenData.MapUid = ResolveMapUid(monster, fallbackMapUid);
            regenData.x = monster.transform.position.x;
            regenData.y = monster.transform.position.y;
            regenData.z = monster.transform.position.z;
            regenData.IsFlip = monster.isFlip;
            regenData.CanMoveX = monster.canMoveX;
            regenData.CanMoveY = monster.canMoveY;
            regenData.MapVisibilityPolicy = mapVisibilityPolicy;

            monster.SetMapVisibilityPolicy(mapVisibilityPolicy);
            UpdateInfoText(monster);
        }

        /// <summary>
        /// 몬스터의 배치별 AttackType Override를 리젠 데이터와 현재 런타임 상태에 함께 적용합니다.
        /// </summary>
        /// <param name="monster">적용 대상 몬스터입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <param name="attackTypeOverride">배치별 Override 값입니다. null이면 테이블 기본값을 사용합니다.</param>
        /// <param name="tableAttackType">Override가 없을 때 사용할 monster 테이블의 기본값입니다.</param>
        public static void ApplyAttackTypeOverride(
            Monster monster,
            int fallbackMapUid,
            CharacterConstants.AttackType? attackTypeOverride,
            CharacterConstants.AttackType tableAttackType)
        {
            if (!monster)
            {
                return;
            }

            CharacterRegenData regenData = EnsureRegenData(monster, fallbackMapUid);
            if (regenData == null)
            {
                return;
            }

            regenData.HasAttackTypeOverride = attackTypeOverride.HasValue;
            regenData.AttackTypeOverride = attackTypeOverride.GetValueOrDefault();
            monster.ApplyAttackTypeOverride(attackTypeOverride, tableAttackType);
            UpdateInfoText(monster);
        }

        /// <summary>
        /// 몬스터의 배치별 CombatProfileUid Override를 리젠 데이터에 적용합니다.
        /// 실제 전투 프로필 객체 구성은 런타임 스폰 초기화 시 이 값을 기준으로 수행합니다.
        /// </summary>
        /// <param name="monster">적용 대상 몬스터입니다.</param>
        /// <param name="fallbackMapUid">리젠 데이터 보정 시 사용할 대체 맵 UID입니다.</param>
        /// <param name="combatProfileUidOverride">배치별 Override 값입니다. null이면 테이블 기본값을 사용하고 0이면 프로필을 사용하지 않습니다.</param>
        public static void ApplyCombatProfileUidOverride(
            Monster monster,
            int fallbackMapUid,
            int? combatProfileUidOverride)
        {
            if (!monster)
            {
                return;
            }

            CharacterRegenData regenData = EnsureRegenData(
                monster,
                fallbackMapUid);
            if (regenData == null)
            {
                return;
            }

            regenData.HasCombatProfileUidOverride =
                combatProfileUidOverride.HasValue;
            regenData.CombatProfileUidOverride = Mathf.Max(
                0,
                combatProfileUidOverride.GetValueOrDefault());
            UpdateInfoText(monster);
        }

        /// <summary>
        /// 에디터에서 표시하는 몬스터 오버레이 텍스트를 현재 맵 표시 정책으로 갱신합니다.
        /// </summary>
        /// <param name="monster">텍스트를 갱신할 몬스터입니다.</param>
        public static void UpdateInfoText(Monster monster)
        {
            if (!monster)
            {
                return;
            }

            TextMeshProUGUI text = monster.GetComponentInChildren<TextMeshProUGUI>();
            if (!text)
            {
                return;
            }

            CharacterRegenData regenData = monster.CharacterRegenData;
            MapCharacterVisibilityPolicy mapVisibilityPolicy = regenData != null
                ? regenData.MapVisibilityPolicy
                : monster.MapVisibilityPolicy;
            string attackTypeText = regenData != null && regenData.HasAttackTypeOverride
                ? $"{regenData.AttackTypeOverride} (Override)"
                : $"{monster.GetAttackType()} (Table)";
            string combatProfileText =
                regenData != null && regenData.HasCombatProfileUidOverride
                    ? regenData.CombatProfileUidOverride > 0
                        ? $"{regenData.CombatProfileUidOverride} (Override)"
                        : "None (Override)"
                    : "Table";
            Vector3 pos = monster.transform.position;
            float scaleX = Mathf.Abs(monster.transform.localScale.x);
            text.text =
                $"Uid: {monster.uid}\nPos: ({pos.x:F2}, {pos.y:F2})\nScale: {scaleX:F2}\n" +
                $"AttackType: {attackTypeText}\nCombatProfile: {combatProfileText}\n" +
                $"VisibilityPolicy: {mapVisibilityPolicy}";
        }

        /// <summary>
        /// 몬스터가 속한 맵 UID를 안전하게 계산합니다.
        /// </summary>
        /// <param name="monster">대상 몬스터입니다.</param>
        /// <param name="fallbackMapUid">대체 맵 UID입니다.</param>
        /// <returns>확정된 맵 UID입니다.</returns>
        private static int ResolveMapUid(Monster monster, int fallbackMapUid)
        {
            if (!monster)
            {
                return fallbackMapUid;
            }

            CharacterRegenData regenData = monster.CharacterRegenData;
            if (regenData != null && regenData.MapUid > 0)
            {
                return regenData.MapUid;
            }

            DefaultMap map = monster.GetComponentInParent<DefaultMap>();
            if (map)
            {
                int mapUid = map.GetChapterNumber();
                if (mapUid > 0)
                {
                    return mapUid;
                }
            }

            return fallbackMapUid;
        }
    }
}
