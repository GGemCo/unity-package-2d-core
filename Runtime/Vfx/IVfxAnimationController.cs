
using UnityEngine;

namespace GGemCo2DCore
{
    public interface IVfxAnimationController
    {
        // 이펙트 시작 애니 클립 이름
        public const string KeyClipNameStart = "start";
        // 루프 되는 클립 이름
        public const string KeyClipNamePlay = "play";
        // 없어지는 애니 클립 이름
        public const string KeyClipNameEnd = "end";
        /// <summary>
        /// VFX 애니메이션 렌더러에 적용할 색상을 설정합니다.
        /// </summary>
        /// <param name="colorHex">HTML 색상 문자열입니다.</param>
        void SetEffectColor(string colorHex);

        /// <summary>
        /// 종료 애니메이션 클립 보유 여부를 반환합니다.
        /// </summary>
        /// <returns>종료 클립이 있으면 true를 반환합니다.</returns>
        bool HasEndAnimation();

        /// <summary>
        /// VFX 시작 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="duration">재생 시간입니다. 0 이하는 1회 재생, 음수는 무제한 재생입니다.</param>
        /// <param name="timeScale">재생 속도입니다.</param>
        /// <param name="forceReset">true면 같은 상태라도 첫 프레임부터 다시 재생합니다.</param>
        /// <returns>재생 가능한 클립이 있으면 true를 반환합니다.</returns>
        bool Play(float duration, float timeScale = 1f, bool forceReset = false);

        /// <summary>
        /// VFX 종료 애니메이션을 재생합니다.
        /// </summary>
        void PlayEnd();

        /// <summary>
        /// AnimationEvent 완료 콜백을 VFX 애니메이션 컨트롤러에 전달합니다.
        /// </summary>
        /// <param name="struckAnimationEventComplete">애니메이션 완료 이벤트 데이터입니다.</param>
        void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete);
    }
}
