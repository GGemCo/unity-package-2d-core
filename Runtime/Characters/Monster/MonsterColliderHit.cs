using UnityEngine;

namespace GGemCo2DCore
{
    public class MonsterColliderHit : MonoBehaviour
    {
        public CapsuleCollider2D capsuleCollider;

        private void Awake()
        {
            capsuleCollider = GetComponent<CapsuleCollider2D>();
        }
    }
}