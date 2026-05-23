using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 Body Collider 사이의 이동 차단과 겹침 해소 정책을 정의하는 설정 자산입니다.
    /// </summary>
    /// <remarks>
    /// HitArea, AttackRange 같은 감지용 Collider가 아니라 실제 캐릭터 몸통 충돌에 사용할 Body Collider 정책만 관리합니다.
    /// </remarks>
    [CreateAssetMenu(
        fileName = ConfigScriptableObject.CharacterCollision.FileName,
        menuName = ConfigScriptableObject.CharacterCollision.MenuName,
        order = ConfigScriptableObject.CharacterCollision.Ordering)]
    public sealed class CharacterCollisionSettings : ScriptableObject
    {
        [Header("사용 여부")]
        [Tooltip("플레이어/몬스터/NPC Body Collider 사이의 충돌 정책을 사용할지 여부입니다.")]
        public bool useCharacterBodyCollision = true;

        [Header("관계별 충돌 정책")]
        [Tooltip("플레이어와 몬스터 사이의 Body Collider 충돌 정책입니다.")]
        public CharacterBodyCollisionPolicy playerMonsterPolicy = CharacterBodyCollisionPolicy.BlockAndSeparate;

        [Tooltip("플레이어와 NPC 사이의 Body Collider 충돌 정책입니다.")]
        public CharacterBodyCollisionPolicy playerNpcPolicy = CharacterBodyCollisionPolicy.BlockAndSeparate;

        [Tooltip("몬스터끼리의 Body Collider 충돌 정책입니다. 기본값은 군집 이동 정체를 피하기 위해 None입니다.")]
        public CharacterBodyCollisionPolicy monsterMonsterPolicy = CharacterBodyCollisionPolicy.None;

        [Tooltip("몬스터와 NPC 사이의 Body Collider 충돌 정책입니다.")]
        public CharacterBodyCollisionPolicy monsterNpcPolicy = CharacterBodyCollisionPolicy.None;

        [Tooltip("NPC끼리의 Body Collider 충돌 정책입니다.")]
        public CharacterBodyCollisionPolicy npcNpcPolicy = CharacterBodyCollisionPolicy.None;

        [Header("이동 전 차단")]
        [Tooltip("충돌 직전에서 멈출 때 Collider 사이에 남길 여유 거리입니다.")]
        [Min(0f)]
        public float collisionSkinWidth = 0.02f;

        [Header("겹침 해소")]
        [Tooltip("이미 겹친 Body Collider를 여러 프레임에 걸쳐 분리할지 여부입니다.")]
        public bool useCharacterBodySeparation = true;

        [Tooltip("겹침 해소 시 한 프레임에 적용할 최대 이동 거리입니다.")]
        [Min(0f)]
        public float separationMaxStep = 0.06f;

        [Tooltip("겹침 해소 후 남길 목표 여유 거리입니다.")]
        [Min(0f)]
        public float separationPadding = 0.03f;

        [Tooltip("수평 방향 분리 가중치입니다. 플레이어 착지 후 좌우로 빠져나오게 하려면 1 이상을 권장합니다.")]
        [Min(0f)]
        public float separationHorizontalBias = 1f;

        [Tooltip("수직 방향 분리 가중치입니다. 착지 후 위아래 튐을 줄이려면 낮은 값을 권장합니다.")]
        [Min(0f)]
        public float separationVerticalBias = 0.2f;

        [Header("점프 착지 보정")]
        [Tooltip("점프 착지 직후 겹침 해소를 더 강하게 적용할 시간입니다.")]
        [Min(0f)]
        public float landingSeparationDuration = 0.2f;

        [Tooltip("점프 착지 직후 겹침 해소에 곱할 배율입니다.")]
        [Min(1f)]
        public float landingSeparationMultiplier = 1.5f;

        [Header("사망 캐릭터 처리")]
        [Tooltip("사망 캐릭터의 Body 충돌 처리 방식입니다.")]
        public DeadCharacterBodyCollisionMode deadCharacterBodyCollisionMode =
            DeadCharacterBodyCollisionMode.IgnoreInCharacterCollision;

        [Tooltip("사망 확정 전 보류 상태인 캐릭터도 Body 충돌 검사에서 제외할지 여부입니다.")]
        public bool ignoreDeathPendingCharacters = true;

        /// <summary>
        /// 지정한 두 캐릭터 타입 사이의 Body 충돌 정책을 반환합니다.
        /// </summary>
        /// <param name="a">첫 번째 캐릭터 타입입니다.</param>
        /// <param name="b">두 번째 캐릭터 타입입니다.</param>
        /// <returns>설정된 Body 충돌 정책입니다.</returns>
        public CharacterBodyCollisionPolicy GetPolicy(CharacterConstants.Type a, CharacterConstants.Type b)
        {
            if (IsPair(a, b, CharacterConstants.Type.Player, CharacterConstants.Type.Monster))
                return playerMonsterPolicy;

            if (IsPair(a, b, CharacterConstants.Type.Player, CharacterConstants.Type.Npc))
                return playerNpcPolicy;

            if (IsPair(a, b, CharacterConstants.Type.Monster, CharacterConstants.Type.Monster))
                return monsterMonsterPolicy;

            if (IsPair(a, b, CharacterConstants.Type.Monster, CharacterConstants.Type.Npc))
                return monsterNpcPolicy;

            if (IsPair(a, b, CharacterConstants.Type.Npc, CharacterConstants.Type.Npc))
                return npcNpcPolicy;

            return CharacterBodyCollisionPolicy.None;
        }

        /// <summary>
        /// 두 타입이 순서와 무관하게 동일한 관계인지 검사합니다.
        /// </summary>
        /// <param name="a">첫 번째 실제 타입입니다.</param>
        /// <param name="b">두 번째 실제 타입입니다.</param>
        /// <param name="expectedA">기대 타입 A입니다.</param>
        /// <param name="expectedB">기대 타입 B입니다.</param>
        /// <returns>동일한 관계이면 true입니다.</returns>
        private static bool IsPair(
            CharacterConstants.Type a,
            CharacterConstants.Type b,
            CharacterConstants.Type expectedA,
            CharacterConstants.Type expectedB)
        {
            return (a == expectedA && b == expectedB) ||
                   (a == expectedB && b == expectedA);
        }
    }
}
