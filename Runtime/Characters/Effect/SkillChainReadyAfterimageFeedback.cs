using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 체인 가능 상태가 열렸을 때 <see cref="CharacterAfterimageTrail"/>을 사용해 잔상 피드백을 재생하는 브리지 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillChainReadyAfterimageFeedback : MonoBehaviour, ISkillChainReadyFeedback
    {
        [Header("References")]
        [SerializeField] private CharacterAfterimageTrail trail;

        [Tooltip("잔상 수명(0이면 컴포넌트 기본값 사용).")]
        [SerializeField] private float ghostLifetimeSeconds = 0.2f;

        [Tooltip("잔상 색상 HTML Hex. 예) \"4AA3FF\" 또는 \"#4AA3FF\"")]
        [SerializeField] private string colorHex = string.Empty;

        [Tooltip("잔상 알파(0~1). 음수면 색상에 포함된 알파 또는 컴포넌트 기본값 사용.")]
        [SerializeField] private float alpha = 0.45f;

        [Tooltip("원본 SpriteRenderer 대비 sortingOrder 보정값.")]
        [SerializeField] private int sortingOrderOffset = -1;
        
        private void Awake()
        {
            if (trail == null)
                trail = GetComponentInChildren<CharacterAfterimageTrail>(true);
        }

        public void PlaySkillChainReady()
        {
            if (trail == null)
                return;

            var data = new StruckAnimationEventAfterimageSnapshot
            {
                GhostLifetimeSeconds = ghostLifetimeSeconds,
                ColorHex = colorHex,
                Alpha = alpha,
                SortingOrderOffset = sortingOrderOffset
            };

            trail.CaptureOnce(data);
        }

        public void StopSkillChainReady()
        {
            if (trail == null)
                return;

            trail.StopTrail();
        }
    }
}
