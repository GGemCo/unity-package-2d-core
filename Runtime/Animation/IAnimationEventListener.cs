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
    }
}