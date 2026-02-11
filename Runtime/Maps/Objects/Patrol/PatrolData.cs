using UnityEngine;

namespace GGemCo2DCore
{
    public class PatrolData
    {
        public int MapUid;
        public int MonsterUid;
        public float X, Y, Z;
        public float RotationX, RotationY, RotationZ;
        public float BoxColliderSizeX, BoxColliderSizeY;
        public float BoxColliderOffsetX, BoxColliderOffsetY;

        public PatrolData(int mapUid, Vector3 position, int monsterUid, Vector3 rotation, Vector2 boxColliderSize, Vector2 boxColliderOffset)
        {
            MapUid = mapUid;
            X = position.x;
            Y = position.y;
            Z = position.z;
            RotationX = rotation.x;
            RotationY = rotation.y;
            RotationZ = rotation.z;
            MonsterUid = monsterUid;
            BoxColliderSizeX = boxColliderSize.x;
            BoxColliderSizeY = boxColliderSize.y;
            BoxColliderOffsetX = boxColliderOffset.x;
            BoxColliderOffsetY = boxColliderOffset.y;
        }
    }
}