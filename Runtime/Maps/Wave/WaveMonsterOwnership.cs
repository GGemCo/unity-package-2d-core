namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브로 생성된 몬스터가 어떤 시나리오와 그룹 인스턴스에 속하는지 나타냅니다.
    /// </summary>
    public readonly struct WaveMonsterOwnership
    {
        /// <summary>
        /// 몬스터를 생성한 웨이브 시나리오 UID입니다.
        /// </summary>
        public readonly int ScenarioUid;

        /// <summary>
        /// 몬스터를 생성한 웨이브 그룹 UID입니다.
        /// </summary>
        public readonly int GroupUid;

        /// <summary>
        /// 같은 그룹이 반복 실행될 때 각 실행 회차를 구분하는 런타임 인스턴스 ID입니다.
        /// </summary>
        public readonly int GroupInstanceId;

        /// <summary>
        /// 웨이브 몬스터 소유권 정보를 생성합니다.
        /// </summary>
        /// <param name="scenarioUid">웨이브 시나리오 UID입니다.</param>
        /// <param name="groupUid">웨이브 그룹 UID입니다.</param>
        /// <param name="groupInstanceId">웨이브 그룹 런타임 인스턴스 ID입니다.</param>
        public WaveMonsterOwnership(int scenarioUid, int groupUid, int groupInstanceId)
        {
            ScenarioUid = scenarioUid;
            GroupUid = groupUid;
            GroupInstanceId = groupInstanceId;
        }
    }
}
