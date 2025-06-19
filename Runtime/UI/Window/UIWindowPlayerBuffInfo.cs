using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 버프 관리
    /// </summary>
    public class UIWindowPlayerBuffInfo : UIWindow
    {
        public GameObject prefabSlotBuff;
        private Dictionary<int, GameObject> slotBuffs = new Dictionary<int, GameObject>();
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowManager.WindowUid.PlayerBuffInfo;
            base.Awake();
            slotBuffs.Clear();
        }
        /// <summary>
        /// 버프 적용하기
        /// </summary>
        /// <param name="affectUid"></param>
        public void AddAffectIcon(int affectUid)
        {
            var info = TableLoaderManager.Instance.TableAffect.GetDataByUid(affectUid);
            if (info == null)
            {
                GcLogger.LogError("affect 테이블에 없는 어펙트 입니다. affect Uid: "+affectUid);
                return;
            }
            GameObject slotBuff;
            StopAllCoroutines();
            // 적용되어 있는 버프면, duration 초기화 하기
            if (slotBuffs.ContainsKey(affectUid))
            {
                slotBuff = slotBuffs.GetValueOrDefault(affectUid);
                slotBuff.GetComponentInChildren<UIIconBuff>()?.ReStartCoolTime();
            }
            else
            {
                // GcLogger.Log("RemoveBuffAfterDuration " + struckBuff.Uid + "/"+struckBuff.Name+"/"+struckBuff.Duration);
                slotBuff = Instantiate(prefabSlotBuff, containerIcon.transform);
                slotBuff.GetComponentInChildren<UIIconBuff>()?.Initialize(affectUid);
                slotBuffs.Add(affectUid, slotBuff);
            }
            StartCoroutine(RemoveBuffAfterDuration(affectUid, info.Duration, slotBuff));
        }
        /// <summary>
        /// 버프 제거 하기
        /// </summary>
        /// <param name="affectUid"></param>
        /// <param name="duration"></param>
        /// <param name="slotBuff"></param>
        /// <returns></returns>
        private IEnumerator RemoveBuffAfterDuration(int affectUid, float duration, GameObject slotBuff)
        {
            // GcLogger.Log("RemoveBuffAfterDuration " + struckBuff.Uid + "/"+struckBuff.Name+"/"+struckBuff.Duration);
            yield return new WaitForSeconds(duration);
            slotBuff.GetComponentInChildren<UIIconBuff>()?.RemoveCoolTime();
            
            slotBuffs.Remove(affectUid);
            Destroy(slotBuff);
        }
    }
}