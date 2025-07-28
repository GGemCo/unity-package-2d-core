using UnityEngine;

namespace GGemCo2DCore
{
    // 공통 이벤트 리스너 인터페이스
    public interface IAnimationEventListener
    {
        void OnAnimationEventEffect(string json, GameObject fromObject);
        void OnAnimationEventSound(int soundUid);
        void OnAnimationEventCameraShake(string json);
        void OnAnimationEventAttack(GameObject fromObject);
        void OnAnimationEventProjectile(int uid, GameObject fromObject);
        void OnAnimationEventSkill(string json, GameObject fromObject);
    }
}