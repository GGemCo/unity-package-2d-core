using UnityEngine;

namespace GGemCo2DCore
{
    public static class CompatRigidbody2D
    {
        public static float GetLinearDamping(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearDamping;
#else
            return rb.drag;
#endif
        }

        public static void SetLinearDamping(this Rigidbody2D rb, float value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = value;
#else
            rb.drag = value;
#endif
        }

        public static Vector2 GetLinearVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetLinearVelocity(this Rigidbody2D rb, Vector2 value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }
}