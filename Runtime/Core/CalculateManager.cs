using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 최종 수치 계산 값 관리 매니저
    /// </summary>
    public class CalculateManager : MonoBehaviour
    {
        /// <summary>
        /// 플레이어 공격력 구하기
        /// </summary>
        /// <returns></returns>
        public long GetPlayerTotalAtk()
        {
            return SceneGame.Instance.player.GetComponent<Player>().TotalAtk.Value;
        }
        /// <summary>
        /// 몬스터 공격력 구하기
        /// </summary>
        /// <param name="monsterUid"></param>
        /// <returns></returns>
        public long GetMonsterTotalAtk(int monsterUid)
        {
            if (monsterUid < 0)
            {
                GcLogger.LogError("몬스터 고유번호가 없습니다. Uid:"+monsterUid);
                return 0;
            }
            var info = TableLoaderManager.Instance.TableMonster.GetDataByUid(monsterUid);
            if (info == null || info.Uid <= 0)
            {
                GcLogger.LogError("몬스터 테이블에 정보가 없습니다. Uid:"+monsterUid);
                return 0;
            }

            return info.StatAtk;
        }
        private void OnDestroy()
        {
            
        }
    }
}