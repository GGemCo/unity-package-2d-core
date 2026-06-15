using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵에 배치되는 캐릭터의 리젠 및 초기 배치 정보를 보관합니다.
    /// </summary>
    public class CharacterRegenData
    {
        public int Uid;
        public int MapUid;
        public float x, y, z;
        public bool IsFlip;
        public bool DefaultVisible;
        public float MoveStep;
        public float MoveSpeed;
        public bool CanMoveX;
        public bool CanMoveY;
        public PatrolData patrolData;

        /// <summary>
        /// 카메라 컬링보다 우선 적용할 맵 표시 정책입니다.
        /// </summary>
        public MapCharacterVisibilityPolicy MapVisibilityPolicy;

        /// <summary>
        /// 캐릭터 리젠 데이터를 생성합니다.
        /// </summary>
        /// <param name="uid">캐릭터 테이블 UID입니다.</param>
        /// <param name="position">맵 배치 위치입니다.</param>
        /// <param name="flip">초기 좌우 반전 여부입니다.</param>
        /// <param name="mapUid">배치된 맵 UID입니다.</param>
        /// <param name="defaultVisible">기본 표시 여부입니다.</param>
        /// <param name="moveStep">초기 이동 스텝 값입니다.</param>
        /// <param name="moveSpeed">초기 이동 속도 값입니다.</param>
        /// <param name="canMoveX">X축 이동 가능 여부입니다.</param>
        /// <param name="canMoveY">Y축 이동 가능 여부입니다.</param>
        /// <param name="patrolData">순찰 데이터입니다.</param>
        /// <param name="mapVisibilityPolicy">맵 컬링 및 표시 정책입니다.</param>
        public CharacterRegenData(
            int uid,
            Vector3 position,
            bool flip,
            int mapUid,
            bool defaultVisible,
            float moveStep = 0,
            float moveSpeed = 0,
            bool canMoveX = true,
            bool canMoveY = true,
            PatrolData patrolData = null,
            MapCharacterVisibilityPolicy mapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling)
        {
            Uid = uid;
            MapUid = mapUid;
            x = position.x;
            y = position.y;
            z = position.z;
            IsFlip = flip;
            DefaultVisible = defaultVisible;
            MoveStep = moveStep;
            MoveSpeed = moveSpeed;
            CanMoveX = canMoveX;
            CanMoveY = canMoveY;
            this.patrolData = patrolData;
            MapVisibilityPolicy = mapVisibilityPolicy;
        }
    }
    
    /// <summary>
    /// 맵 캐릭터 리젠 데이터 목록입니다.
    /// </summary>
    [System.Serializable]
    public class CharacterRegenDataList
    {
        public List<CharacterRegenData> CharacterRegenDatas = new List<CharacterRegenData>();
    }
}
