using UnityEngine;

namespace GGemCo2DCore
{
    public class PatrolData
    {
        public float X, Y, Z;
        public float RotationX, RotationY, RotationZ;
        public float BoxColliderOffsetX, BoxColliderOffsetY;
        public float BoxColliderSizeX, BoxColliderSizeY;

        public PatrolData(Vector3 position, Vector3 rotation, Vector2 boxColliderSize, Vector2 boxColliderOffset)
        {
            X = position.x;
            Y = position.y;
            Z = position.z;
            RotationX = rotation.x;
            RotationY = rotation.y;
            RotationZ = rotation.z;
            BoxColliderOffsetX = boxColliderOffset.x;
            BoxColliderOffsetY = boxColliderOffset.y;
            BoxColliderSizeX = boxColliderSize.x;
            BoxColliderSizeY = boxColliderSize.y;
        }
    }
}