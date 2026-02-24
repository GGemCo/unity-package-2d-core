using UnityEngine;

namespace GGemCo2DCore
{
    // 공통 이벤트 리스너 인터페이스
    public interface IAnimationEventListener
    {
        void OnAnimationEventEffect(string json, GameObject fromObject);
        void OnAnimationEventSound(string json);
        void OnAnimationEventCameraShake(string json);
        void OnAnimationEventAttack(string json, GameObject fromObject);
        void OnAnimationEventSkill(string json, GameObject fromObject);
        void OnAnimationEventJump(GameObject fromObject, string eventName);
        void OnAnimationEventDash(GameObject fromObject, string eventName);
        void OnAnimationEventUseTool(string json, GameObject fromObject);
        void OnAnimationEventUseSeed(string json, GameObject fromObject);
        void OnAnimationEventGuardEnd(GameObject gameObject);

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 시작.
        /// - AnimationEvent의 string 파라미터에 JSON을 전달하여 런타임 설정을 오버라이드할 수 있습니다.
        /// </summary>
        void OnAnimationEventStartBackstepTrail(string json, GameObject fromObject);

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 종료.
        /// </summary>
        void OnAnimationEventStopBackstepTrail(string json, GameObject fromObject);
    }
}