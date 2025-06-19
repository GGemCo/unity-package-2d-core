using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo.Scripts
{
    public abstract class ComponentController
    {
        /// <summary>
        /// Rigidbody2D 컴포넌트 추가하기
        /// </summary>
        public static Rigidbody2D AddRigidbody2D(GameObject gameObject)
        {
            Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
            if (body == null) return null;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.freezeRotation = true;
            return body;
        }

        /// <summary>
        /// CapsuleCollider2D 컴포넌트 추가하기
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="isTrigger"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="includeLayers">include layer index 값의 비트 연산 값</param>
        /// <param name="excludeLayers">exclude layer index 값의 비트 연산 값</param>
        /// <param name="dir"></param>
        public static CapsuleCollider2D AddCapsuleCollider2D(GameObject gameObject, bool isTrigger, Vector2 offset,
            Vector2 size, int includeLayers = 0, int excludeLayers = 0,
            CapsuleDirection2D dir = CapsuleDirection2D.Horizontal)
        {
            CapsuleCollider2D capsuleCollider2D = gameObject.AddComponent<CapsuleCollider2D>();
            if (capsuleCollider2D == null) return null;
            capsuleCollider2D.isTrigger = isTrigger;
            capsuleCollider2D.includeLayers = includeLayers;
            capsuleCollider2D.excludeLayers = excludeLayers;
            capsuleCollider2D.direction = dir;
            capsuleCollider2D.offset = offset;
            capsuleCollider2D.size = size;
            return capsuleCollider2D;
        }

        /// <summary>
        /// BoxCollider2D 컴포넌트 추가하기
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="isTrigger"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="includeLayers">각 레이어 index 값의 비트 연산 값</param>
        /// <param name="excludeLayers">각 레이어 index 값의 비트 연산 값</param>
        public static BoxCollider2D AddBoxCollider2D(GameObject gameObject, bool isTrigger, Vector2 offset, Vector2 size, int includeLayers = 0, int excludeLayers = 0)
        {
            BoxCollider2D boxCollider2D = gameObject.AddComponent<BoxCollider2D>();
            if (boxCollider2D == null) return null;
            boxCollider2D.isTrigger = isTrigger;
            boxCollider2D.includeLayers = includeLayers;
            boxCollider2D.excludeLayers = excludeLayers;
            boxCollider2D.offset = offset;
            boxCollider2D.size = size;
            return boxCollider2D;
        }
        /// <summary>
        /// TilemapCollider2D 컴포넌트 추가하기
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="isTrigger"></param>
        /// <param name="offset"></param>
        /// <param name="includeLayers">각 레이어 index 값의 비트 연산 값</param>
        /// <param name="excludeLayers">각 레이어 index 값의 비트 연산 값</param>
        public static TilemapCollider2D AddTileMapCollider2D(GameObject gameObject, bool isTrigger, Vector2 offset, int includeLayers = 0, int excludeLayers = 0)
        {
            TilemapCollider2D tileMapCollider2D = gameObject.AddComponent<TilemapCollider2D>();
            if (tileMapCollider2D == null) return null;
            tileMapCollider2D.isTrigger = isTrigger;
            tileMapCollider2D.includeLayers = includeLayers;
            tileMapCollider2D.excludeLayers = excludeLayers;
            tileMapCollider2D.offset = offset;
            return tileMapCollider2D;
        }
        public static PolygonCollider2D AddPolygonCollider2D(GameObject gameObject, bool isTrigger, Vector2 offset, Vector2[] points, int includeLayers = 0, int excludeLayers = 0)
        {
            PolygonCollider2D polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();
            if (polygonCollider2D == null) return null;
            polygonCollider2D.isTrigger = isTrigger;
            polygonCollider2D.includeLayers = includeLayers;
            polygonCollider2D.excludeLayers = excludeLayers;
            polygonCollider2D.offset = offset;
            polygonCollider2D.points = points;
            return polygonCollider2D;
        }
        
    }
}