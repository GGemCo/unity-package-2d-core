using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// UI 아이콘 쿨타임 관리
    /// 아이템, 스킬, 버프 아이콘
    /// </summary>
    public class UIIconCoolTimeManager : MonoBehaviour
    {
        private readonly Dictionary<UIWindowManager.WindowUid, Dictionary<int, UICoolTimeHandler>> dictionaryCoolTime = new Dictionary<UIWindowManager.WindowUid, Dictionary<int, UICoolTimeHandler>>();

        private void AddCoolTime(UIWindowManager.WindowUid windowUid, UIIcon icon, float coolTime)
        {
            dictionaryCoolTime.TryAdd(windowUid, new Dictionary<int, UICoolTimeHandler>());
            var handler = new UICoolTimeHandler();
            handler.Initialize(icon);
            if (handler.StartCoolTime(coolTime) == false) return;
            if (!dictionaryCoolTime[windowUid].TryAdd(icon.uid, handler))
            {
                dictionaryCoolTime[windowUid][icon.uid] = handler;
            }
        }
        private void Update()
        {
            foreach (var datas in dictionaryCoolTime)
            {
                foreach (var data in datas.Value)
                {
                    data.Value?.UpdateCoolTime();
                }
            }
        }
        private UICoolTimeHandler GetCoolTimeHandler(UIWindowManager.WindowUid windowUid, int iconUid)
        {
            return dictionaryCoolTime.TryGetValue(windowUid, out Dictionary<int, UICoolTimeHandler> handlers) ? handlers.GetValueOrDefault(iconUid) : null;
        }
        /// <summary>
        /// 남은 쿨타임 셋팅하기
        /// </summary>
        /// <param name="windowUid"></param>
        /// <param name="iconUid"></param>
        /// <param name="remainTime"></param>
        public void SetRemainCoolTime(UIWindowManager.WindowUid windowUid, int iconUid, float remainTime)
        {
            if (remainTime <= 0) return;
            var handler = GetCoolTimeHandler(windowUid, iconUid);
            if (handler == null)
            {
                GcLogger.LogError("존재하지 않는 쿨타임 입니다. window Uid: " + windowUid + " / icon Uid: " + iconUid);
                return;
            }
            handler.SetRemainCoolTime(remainTime);
        }
        /// <summary>
        /// 쿨타임 리셋하기
        /// </summary>
        /// <param name="windowUid"></param>
        /// <param name="iconUid"></param>
        public void ResetCoolTime(UIWindowManager.WindowUid windowUid, int iconUid)
        {
            var handler = GetCoolTimeHandler(windowUid, iconUid);
            if (handler == null)
            {
                // GcLogger.LogError("존재하지 않는 쿨타임 입니다. window Uid: " + windowUid + " / icon Uid: " + iconUid);
                return;
            }
            handler.ResetCoolTime();
        }
        /// <summary>
        /// 남아있는 쿨타임 시간 가져오기
        /// </summary>
        /// <param name="windowUid"></param>
        /// <param name="iconUid"></param>
        /// <returns></returns>
        public float GetCurrentCoolTime(UIWindowManager.WindowUid windowUid, int iconUid)
        {
            var handler = GetCoolTimeHandler(windowUid, iconUid);
            if (handler == null)
            {
                // GcLogger.LogError("존재하지 않는 쿨타임 입니다. window Uid: " + windowUid + " / icon Uid: " + iconUid);
                return 0;
            }
            return handler.GetCurrentCoolTime();
        }
        /// <summary>
        /// 쿨타임 실행하기
        /// </summary>
        /// <param name="windowUid"></param>
        /// <param name="icon"></param>
        /// <param name="coolTime"></param>
        /// <returns></returns>
        public bool StartHandler(UIWindowManager.WindowUid windowUid, UIIcon icon, float coolTime)
        {
            var handler = GetCoolTimeHandler(windowUid, icon.uid);
            // 기존 handle 가 있으면 cool time 만 업데이트 
            if (handler != null)
            {
                return handler.ReStartCoolTime(coolTime, icon);
            }
            AddCoolTime(windowUid, icon, coolTime);
            return true;
        }
        private void OnDestroy()
        {
            
        }
    }
}