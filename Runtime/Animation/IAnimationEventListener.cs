using UnityEngine;

namespace GGemCo2DCore
{
    // 공통 이벤트 리스너 인터페이스
    public interface IAnimationEventListener
    {
        void OnAnimationEventComplete(string json, GameObject fromObject);
        void OnAnimationEventVfx(string json, GameObject fromObject);
        void OnAnimationEventSound(string json);
        void OnAnimationEventCameraShake(string json);
        void OnAnimationEventAttack(string json, GameObject fromObject);
        void OnAnimationEventSkill(string json, GameObject fromObject);
        void OnAnimationEventJump(GameObject fromObject, string eventName);
        void OnAnimationEventDash(GameObject fromObject, string eventName);
        void OnAnimationEventMotion(string json, GameObject fromObject);
        void OnAnimationEventCrowdControl(string json, GameObject fromObject);
        void OnAnimationEventUseTool(string json, GameObject fromObject);
        void OnAnimationEventUseSeed(string json, GameObject fromObject);
        void OnAnimationEventGuardEnd(GameObject gameObject);

        /// <summary>
        /// 플레이어 피격 연출용 애니메이션 이벤트를 전달합니다.
        /// </summary>
        /// <param name="fromObject">이벤트를 발생시킨 오브젝트입니다.</param>
        void OnAnimationEventPlayerHit(GameObject fromObject);

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 시작.
        /// - AnimationEvent의 string 파라미터에 JSON을 전달하여 런타임 설정을 오버라이드할 수 있습니다.
        /// </summary>
        void OnAnimationEventStartBackstepTrail(string json, GameObject fromObject);

        /// <summary>
        /// 백스탭(또는 대시/회피) 트레일 종료.
        /// </summary>
        void OnAnimationEventStopBackstepTrail(string json, GameObject fromObject);

        /// <summary>
        /// 현재 프레임의 Sprite를 단발 잔상으로 1회 캡처합니다.
        /// </summary>
        void OnAnimationEventCaptureAfterimageSnapshot(string json, GameObject fromObject);
        
        /// <summary>
        /// 사망 애니메이션 마지막 프레임에서 호출되는 이벤트 입니다.
        /// </summary>
        void OnAnimationEventDead(string json, GameObject fromObject);
    }
}
