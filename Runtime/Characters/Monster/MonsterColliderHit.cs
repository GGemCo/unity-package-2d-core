using UnityEngine;

namespace GGemCo.Scripts
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