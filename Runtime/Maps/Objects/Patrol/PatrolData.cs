using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에 배치된 패트롤 또는 Encounter 활성화 볼륨의 직렬화 데이터입니다.
    /// </summary>
    [System.Serializable]
    public class PatrolData
    {
        public float X, Y, Z;
        public float RotationX, RotationY, RotationZ;
        public float BoxColliderOffsetX, BoxColliderOffsetY;
        public float BoxColliderSizeX, BoxColliderSizeY;

        /// <summary>
        /// 같은 값의 몬스터를 하나의 Encounter 그룹으로 묶는 맵 전용 ID입니다.
        /// 0 이하면 기존 단일 소유 몬스터 패트롤로 동작합니다.
        /// </summary>
        public int EncounterId;

        /// <summary>
        /// 플레이어가 Encounter 볼륨에서 나갔을 때 그룹의 Encounter Threat를 제거할지 여부입니다.
        /// 기본값 false에서는 진입 후 Leash 또는 전투 종료 정책이 관계를 정리합니다.
        /// </summary>
        public bool ReleaseEncounterThreatOnExit;

        /// <summary>
        /// 패트롤 또는 Encounter 활성화 볼륨 데이터를 생성합니다.
        /// </summary>
        public PatrolData(
            Vector3 position,
            Vector3 rotation,
            Vector2 boxColliderSize,
            Vector2 boxColliderOffset,
            int encounterId = 0,
            bool releaseEncounterThreatOnExit = false)
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
            EncounterId = Mathf.Max(0, encounterId);
            ReleaseEncounterThreatOnExit = releaseEncounterThreatOnExit;
        }
    }
}
