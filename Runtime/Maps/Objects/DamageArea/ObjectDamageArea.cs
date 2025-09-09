using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 데미지를 주는 오브젝트
    /// </summary>
    public class ObjectDamageArea : DefaultMapObject
    {
        public SkillConstants.DamageType damageType;
        public long damage;
        public int affectUid;

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterHitArea hitArea = other.GetComponentInChildren<CharacterHitArea>();
            if (!hitArea) return;
            // GcLogger.Log($"death zone. OnTriggerEnter2D. damage:{damage}");
            MetadataDamage metadataDamage = new MetadataDamage
            {
                damage = damage,
                damageType = damageType,
                affectUid = affectUid
            };
            hitArea.target.TakeDamage(metadataDamage);
        }
    }
}