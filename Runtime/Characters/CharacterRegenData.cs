using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에 배치되는 캐릭터의 리젠 및 초기 배치 정보를 보관합니다.
    /// </summary>
    [System.Serializable]
    public class CharacterRegenData
    {
        public int Uid;
        public int MapUid;
        public float x, y, z;
        public bool IsFlip;
        public bool DefaultVisible;
        public float MoveStep;
        public float MoveSpeed;
        public bool CanMoveX;
        public bool CanMoveY;
        public PatrolData patrolData;

        /// <summary>
        /// 맵 배치 AttackType Override 값이 설정되었는지 여부입니다.
        /// </summary>
        public bool HasAttackTypeOverride;

        /// <summary>
        /// 몬스터 테이블의 기본 공격 성향 대신 사용할 맵 배치 Override 값입니다.
        /// <see cref="HasAttackTypeOverride"/>가 false이면 이 값은 사용하지 않습니다.
        /// </summary>
        public CharacterConstants.AttackType AttackTypeOverride;

        /// <summary>
        /// 맵 배치 CombatProfileUid Override 값이 설정되었는지 여부입니다.
        /// </summary>
        public bool HasCombatProfileUidOverride;

        /// <summary>
        /// monster 테이블의 기본 CombatProfileUid 대신 사용할 맵 배치 Override 값입니다.
        /// 0이면 전투 프로필을 명시적으로 사용하지 않습니다.
        /// <see cref="HasCombatProfileUidOverride"/>가 false이면 이 값은 사용하지 않습니다.
        /// </summary>
        public int CombatProfileUidOverride;

        /// <summary>
        /// 카메라 컬링보다 우선 적용할 맵 표시 정책입니다.
        /// </summary>
        public MapCharacterVisibilityPolicy MapVisibilityPolicy;

        /// <summary>
        /// 캐릭터 리젠 데이터를 생성합니다.
        /// </summary>
        /// <param name="uid">캐릭터 테이블 UID입니다.</param>
        /// <param name="position">맵 배치 위치입니다.</param>
        /// <param name="flip">초기 좌우 반전 여부입니다.</param>
        /// <param name="mapUid">배치된 맵 UID입니다.</param>
        /// <param name="defaultVisible">기본 표시 여부입니다.</param>
        /// <param name="moveStep">초기 이동 스텝 값입니다.</param>
        /// <param name="moveSpeed">초기 이동 속도 값입니다.</param>
        /// <param name="canMoveX">X축 이동 가능 여부입니다.</param>
        /// <param name="canMoveY">Y축 이동 가능 여부입니다.</param>
        /// <param name="patrolData">순찰 데이터입니다.</param>
        /// <param name="mapVisibilityPolicy">맵 컬링 및 표시 정책입니다.</param>
        /// <param name="attackTypeOverride">몬스터 테이블의 기본 공격 성향을 덮어쓸 값입니다. null이면 테이블 값을 사용합니다.</param>
        /// <param name="combatProfileUidOverride">몬스터 테이블의 CombatProfileUid를 덮어쓸 값입니다. null이면 테이블 값을 사용하고 0이면 프로필을 사용하지 않습니다.</param>
        public CharacterRegenData(
            int uid,
            Vector3 position,
            bool flip,
            int mapUid,
            bool defaultVisible,
            float moveStep = 0,
            float moveSpeed = 0,
            bool canMoveX = true,
            bool canMoveY = true,
            PatrolData patrolData = null,
            MapCharacterVisibilityPolicy mapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling,
            CharacterConstants.AttackType? attackTypeOverride = null,
            int? combatProfileUidOverride = null)
        {
            Uid = uid;
            MapUid = mapUid;
            x = position.x;
            y = position.y;
            z = position.z;
            IsFlip = flip;
            DefaultVisible = defaultVisible;
            MoveStep = moveStep;
            MoveSpeed = moveSpeed;
            CanMoveX = canMoveX;
            CanMoveY = canMoveY;
            this.patrolData = patrolData;
            MapVisibilityPolicy = mapVisibilityPolicy;
            HasAttackTypeOverride = attackTypeOverride.HasValue;
            AttackTypeOverride = attackTypeOverride.GetValueOrDefault();
            HasCombatProfileUidOverride = combatProfileUidOverride.HasValue;
            CombatProfileUidOverride = Mathf.Max(
                0,
                combatProfileUidOverride.GetValueOrDefault());
        }

        /// <summary>
        /// JSON 저장 시 AttackType Override 사용 여부를 기록할지 결정합니다.
        /// </summary>
        /// <returns>배치별 AttackType Override가 설정되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool ShouldSerializeHasAttackTypeOverride()
        {
            return HasAttackTypeOverride;
        }

        /// <summary>
        /// JSON 저장 시 AttackType Override 값을 기록할지 결정합니다.
        /// </summary>
        /// <returns>배치별 AttackType Override가 설정되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool ShouldSerializeAttackTypeOverride()
        {
            return HasAttackTypeOverride;
        }

        /// <summary>
        /// JSON 저장 시 CombatProfileUid Override 사용 여부를 기록할지 결정합니다.
        /// </summary>
        /// <returns>배치별 CombatProfileUid Override가 설정되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool ShouldSerializeHasCombatProfileUidOverride()
        {
            return HasCombatProfileUidOverride;
        }

        /// <summary>
        /// JSON 저장 시 CombatProfileUid Override 값을 기록할지 결정합니다.
        /// </summary>
        /// <returns>배치별 CombatProfileUid Override가 설정되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool ShouldSerializeCombatProfileUidOverride()
        {
            return HasCombatProfileUidOverride;
        }
    }
    
    /// <summary>
    /// 맵 캐릭터 리젠 데이터 목록입니다.
    /// </summary>
    [System.Serializable]
    public class CharacterRegenDataList
    {
        public List<CharacterRegenData> CharacterRegenDatas = new List<CharacterRegenData>();
    }
}
