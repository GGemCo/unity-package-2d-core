using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브로 생성된 몬스터 VID와 웨이브 그룹 소유권을 연결해 관리합니다.
    /// </summary>
    public sealed class WaveMonsterOwnershipRegistry
    {
        private readonly Dictionary<int, WaveMonsterOwnership> _ownershipByMonsterVid =
            new Dictionary<int, WaveMonsterOwnership>();

        /// <summary>
        /// 웨이브 몬스터 VID를 소유 그룹에 등록합니다.
        /// </summary>
        /// <param name="monsterVid">생성된 몬스터 VID입니다.</param>
        /// <param name="ownership">몬스터가 속한 웨이브 소유권 정보입니다.</param>
        public void Register(int monsterVid, WaveMonsterOwnership ownership)
        {
            if (monsterVid <= 0)
            {
                return;
            }

            _ownershipByMonsterVid[monsterVid] = ownership;
        }

        /// <summary>
        /// 지정 몬스터 VID가 웨이브 소유 몬스터인지 확인합니다.
        /// </summary>
        /// <param name="monsterVid">조회할 몬스터 VID입니다.</param>
        /// <param name="ownership">조회된 웨이브 소유권 정보입니다.</param>
        /// <returns>웨이브 소유권이 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGet(int monsterVid, out WaveMonsterOwnership ownership)
        {
            return _ownershipByMonsterVid.TryGetValue(monsterVid, out ownership);
        }

        /// <summary>
        /// 지정 몬스터 VID의 웨이브 소유권 정보를 제거합니다.
        /// </summary>
        /// <param name="monsterVid">제거할 몬스터 VID입니다.</param>
        public void Unregister(int monsterVid)
        {
            if (monsterVid <= 0)
            {
                return;
            }

            _ownershipByMonsterVid.Remove(monsterVid);
        }

        /// <summary>
        /// 현재 맵의 모든 웨이브 몬스터 소유권 정보를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _ownershipByMonsterVid.Clear();
        }
    }
}
