using UnityEngine;

namespace GGemCo.Scripts
{
    public class WarpData
    {
        public int MapUid;
        public int ToMapUid; // 이동할 map uid
        public float x, y, z;
        public float RotationX, RotationY, RotationZ;
        public float ToX, ToY, ToZ; // 이동했을때 플레이어 스폰되는 위치
        public float BoxColliderSizeX, BoxColliderSizeY;
        public float BoxColliderOffsetX, BoxColliderOffsetY;

        public WarpData(int mapUid, Vector3 position, int toMapUid, Vector3 toSpawnPosition, Vector3 rotation, Vector2 boxColliderSize, Vector2 boxColliderOffset)
        {
            MapUid = mapUid;
            ToMapUid = toMapUid;
            x = position.x;
            y = position.y;
            z = position.z;
            ToX = toSpawnPosition.x;
            ToY = toSpawnPosition.y;
            ToZ = toSpawnPosition.z;
            RotationX = rotation.x;
            RotationY = rotation.y;
            RotationZ = rotation.z;
            BoxColliderSizeX = boxColliderSize.x;
            BoxColliderSizeY = boxColliderSize.y;
            BoxColliderOffsetX = boxColliderOffset.x;
            BoxColliderOffsetY = boxColliderOffset.y;
        }
    }
}