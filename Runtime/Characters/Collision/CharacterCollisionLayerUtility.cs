using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 Body Collider 레이어와 충돌체 탐색을 공통으로 처리하는 유틸리티입니다.
    /// </summary>
    public static class CharacterCollisionLayerUtility
    {
        /// <summary>
        /// 캐릭터 타입에 대응하는 Body Collider 레이어 이름을 반환합니다.
        /// </summary>
        /// <param name="characterType">조회할 캐릭터 타입입니다.</param>
        /// <returns>캐릭터 Body 레이어 이름입니다. 지원하지 않는 타입이면 빈 문자열입니다.</returns>
        public static string GetBodyLayerName(CharacterConstants.Type characterType)
        {
            return characterType switch
            {
                CharacterConstants.Type.Player => ConfigLayer.GetValue(ConfigLayer.Keys.CharacterBodyPlayer),
                CharacterConstants.Type.Monster => ConfigLayer.GetValue(ConfigLayer.Keys.CharacterBodyMonster),
                CharacterConstants.Type.Npc => ConfigLayer.GetValue(ConfigLayer.Keys.CharacterBodyNpc),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 캐릭터 타입에 대응하는 Body Collider 레이어 인덱스를 반환합니다.
        /// </summary>
        /// <param name="characterType">조회할 캐릭터 타입입니다.</param>
        /// <returns>Unity 레이어 인덱스입니다. 레이어가 없거나 지원하지 않는 타입이면 -1입니다.</returns>
        public static int GetBodyLayer(CharacterConstants.Type characterType)
        {
            string layerName = GetBodyLayerName(characterType);
            return string.IsNullOrEmpty(layerName) ? -1 : LayerMask.NameToLayer(layerName);
        }

        /// <summary>
        /// 캐릭터 타입에 대응하는 Body Collider 레이어 마스크를 반환합니다.
        /// </summary>
        /// <param name="characterType">조회할 캐릭터 타입입니다.</param>
        /// <returns>유효한 레이어이면 해당 레이어 마스크, 아니면 0입니다.</returns>
        public static int GetBodyLayerMask(CharacterConstants.Type characterType)
        {
            int layer = GetBodyLayer(characterType);
            return layer >= 0 ? 1 << layer : 0;
        }


        /// <summary>
        /// ConfigLayer 키에 대응하는 Unity 레이어 인덱스를 반환합니다.
        /// </summary>
        /// <param name="layerKey">조회할 ConfigLayer 키입니다.</param>
        /// <returns>Unity 레이어 인덱스입니다. 레이어가 없으면 -1입니다.</returns>
        public static int GetLayer(ConfigLayer.Keys layerKey)
        {
            string layerName = ConfigLayer.GetValue(layerKey);
            return string.IsNullOrEmpty(layerName) ? -1 : LayerMask.NameToLayer(layerName);
        }

        /// <summary>
        /// ConfigLayer 키에 대응하는 Unity 레이어 마스크를 반환합니다.
        /// </summary>
        /// <param name="layerKey">조회할 ConfigLayer 키입니다.</param>
        /// <returns>유효한 레이어이면 해당 레이어 마스크, 아니면 0입니다.</returns>
        public static int GetLayerMask(ConfigLayer.Keys layerKey)
        {
            int layer = GetLayer(layerKey);
            return layer >= 0 ? 1 << layer : 0;
        }

        /// <summary>
        /// 캐릭터 오브젝트 안에서 이동 차단용 Body Collider를 찾습니다.
        /// </summary>
        /// <param name="character">대상 캐릭터입니다.</param>
        /// <returns>이동 차단에 사용할 CapsuleCollider2D입니다. 없으면 null입니다.</returns>
        /// <remarks>
        /// HitArea와 AttackRange는 Trigger 기반 감지용이므로 Body Collider 후보에서 제외합니다.
        /// </remarks>
        public static CapsuleCollider2D FindBodyCollider(CharacterBase character)
        {
            if (character == null)
                return null;

            CapsuleCollider2D[] colliders = character.GetComponentsInChildren<CapsuleCollider2D>(true);
            CapsuleCollider2D fallback = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                CapsuleCollider2D collider = colliders[i];
                if (collider == null)
                    continue;

                if (IsSensorCollider(collider))
                    continue;

                if (!collider.isTrigger)
                    return collider;

                fallback ??= collider;
            }

            return fallback;
        }

        /// <summary>
        /// 전달된 Collider가 데미지/공격 범위 감지용 센서인지 확인합니다.
        /// </summary>
        /// <param name="collider">검사할 Collider입니다.</param>
        /// <returns>HitArea 또는 AttackRange이면 true를 반환합니다.</returns>
        public static bool IsSensorCollider(Collider2D collider)
        {
            if (collider == null)
                return true;

            return collider.GetComponent<CharacterHitArea>() != null ||
                   collider.GetComponent<CharacterAttackRange>() != null;
        }
    }
}
