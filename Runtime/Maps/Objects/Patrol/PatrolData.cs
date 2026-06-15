using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에 배치된 패트롤 영역과 Encounter 그룹 메타데이터입니다.
    /// </summary>
    [System.Serializable]
    public class PatrolData
    {
        public float x, y, z;
        public float rotationX, rotationY, rotationZ;
        public float boxColliderOffsetX, boxColliderOffsetY;
        public float boxColliderSizeX, boxColliderSizeY;

        /// <summary>
        /// 같은 값의 몬스터를 하나의 Encounter 그룹으로 묶는 맵 전용 ID입니다.
        /// 0 이하면 기존 단일 소유 몬스터 패트롤로 동작합니다.
        /// </summary>
        public int encounterId;

        /// <summary>
        /// 패트롤 영역 데이터를 생성합니다.
        /// </summary>
        /// <param name="position">패트롤 영역의 월드 위치입니다.</param>
        /// <param name="rotation">패트롤 영역의 회전값입니다.</param>
        /// <param name="boxColliderSize">패트롤 영역 Collider 크기입니다.</param>
        /// <param name="boxColliderOffset">패트롤 영역 Collider 오프셋입니다.</param>
        /// <param name="encounterId">같은 Encounter 그룹으로 묶을 맵 전용 ID입니다.</param>
        public PatrolData(
            Vector3 position,
            Vector3 rotation,
            Vector2 boxColliderSize,
            Vector2 boxColliderOffset,
            int encounterId = 0)
        {
            x = position.x;
            y = position.y;
            z = position.z;
            rotationX = rotation.x;
            rotationY = rotation.y;
            rotationZ = rotation.z;
            boxColliderOffsetX = boxColliderOffset.x;
            boxColliderOffsetY = boxColliderOffset.y;
            boxColliderSizeX = boxColliderSize.x;
            boxColliderSizeY = boxColliderSize.y;
            this.encounterId = Mathf.Max(0, encounterId);
        }

        public PatrolData(float rotationX)
        {
            this.rotationX = rotationX;
        }
    }
}
