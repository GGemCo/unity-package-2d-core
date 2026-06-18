namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 그룹에서 생성할 몬스터와 배치 보정값을 정의합니다.
    /// </summary>
    [System.Serializable]
    public sealed class MapWaveMonsterSpawnData
    {
        /// <summary>
        /// 생성할 몬스터 테이블 UID입니다.
        /// </summary>
        public int MonsterUid;

        /// <summary>
        /// 생성 기준으로 사용할 스폰 포인트 ID입니다.
        /// </summary>
        public int SpawnPointId;

        /// <summary>
        /// 생성할 몬스터 수입니다. 기본값은 1마리입니다.
        /// </summary>
        public int Count = 1;

        /// <summary>
        /// 여러 마리를 생성할 때 각 몬스터 사이에 둘 스폰 간격입니다.
        /// </summary>
        public float SpawnIntervalSeconds;

        /// <summary>
        /// 스폰 포인트 기준 X 오프셋입니다.
        /// </summary>
        public float OffsetX;

        /// <summary>
        /// 스폰 포인트 기준 Y 오프셋입니다.
        /// </summary>
        public float OffsetY;

        /// <summary>
        /// 스폰 포인트 기준 Z 오프셋입니다.
        /// </summary>
        public float OffsetZ;

        /// <summary>
        /// 생성 시 좌우 반전 여부입니다.
        /// </summary>
        public bool IsFlip;

        /// <summary>
        /// 생성 직후 기본 표시 여부입니다.
        /// </summary>
        public bool DefaultVisible = true;

        /// <summary>
        /// 몬스터에 적용할 초기 이동 스텝 값입니다. 0이면 테이블/애니메이션 기본값을 사용합니다.
        /// </summary>
        public float MoveStep;

        /// <summary>
        /// 몬스터에 적용할 초기 이동 속도 값입니다. 0이면 테이블 기본값을 사용합니다.
        /// </summary>
        public float MoveSpeed;

        /// <summary>
        /// X축 이동 가능 여부입니다.
        /// </summary>
        public bool CanMoveX = true;

        /// <summary>
        /// Y축 이동 가능 여부입니다.
        /// </summary>
        public bool CanMoveY = true;

        /// <summary>
        /// 웨이브 몬스터에 적용할 맵 표시 정책입니다.
        /// </summary>
        public MapCharacterVisibilityPolicy MapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;
    }
}
