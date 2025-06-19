using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이콘 쿨타임 관리
    /// </summary>
    public class UICoolTimeHandler
    {
        // 현재 쿨타임 시간
        private float currentCoolTime;
        // true : 시계 반대 방향으로 쿨타임이 표시된다. 
        private bool isReverseFillAmount;
        // true : 현재 쿨타임이 진행중이다.
        private bool isPlayingCoolTime;
        // 쿨타임 시간 (초)
        private float coolTimeDuration;

        private UIIcon icon;

        public void Initialize(UIIcon picon)
        {
            icon = picon;

            if (icon.imageCoolTimeGauge != null)
            {
                icon.imageCoolTimeGauge.gameObject.SetActive(false);
            }
        }

        public bool ReStartCoolTime(float coolTime, UIIcon picon)
        {
            ResetCoolTime();
            icon = picon;
            return StartCoolTime(coolTime);
        }
        public bool StartCoolTime(float coolTime = 0)
        {
            if (coolTime > 0)
            {
                SetCoolTime(coolTime);
            }

            if (isPlayingCoolTime || coolTimeDuration <= 0) return false;

            isPlayingCoolTime = true;
            currentCoolTime = coolTimeDuration;

            if (icon.imageCoolTimeGauge != null)
            {
                icon.imageCoolTimeGauge.fillAmount = isReverseFillAmount ? 0 : 1;
                icon.imageCoolTimeGauge.gameObject.SetActive(true);
            }
            return true;
        }

        public void UpdateCoolTime()
        {
            if (!isPlayingCoolTime) return;

            currentCoolTime -= Time.deltaTime;
            if (currentCoolTime <= 0)
            {
                ResetCoolTime();
                return;
            }

            if (icon.imageCoolTimeGauge != null)
            {
                icon.imageCoolTimeGauge.fillAmount = isReverseFillAmount
                    ? 1 - currentCoolTime / coolTimeDuration
                    : currentCoolTime / coolTimeDuration;
            }
        }
        public void ResetCoolTime()
        {
            isPlayingCoolTime = false;
            currentCoolTime = 0;

            if (icon.imageCoolTimeGauge != null)
            {
                icon.imageCoolTimeGauge.gameObject.SetActive(false);
            }
        }

        private void SetCoolTime(float time) => coolTimeDuration = time;

        public float GetCurrentCoolTime() => currentCoolTime;

        public void SetRemainCoolTime(float remainCoolTime)
        {
            if (remainCoolTime <= 0) return;

            isPlayingCoolTime = true;
            currentCoolTime = remainCoolTime;

            if (icon.imageCoolTimeGauge != null)
            {
                icon.imageCoolTimeGauge.gameObject.SetActive(true);
            }
        }
    }
}
