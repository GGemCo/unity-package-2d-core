using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과에 사용할 대상 참조를 캐싱하는 컴포넌트입니다.
    /// </summary>
    public sealed class UIEffectTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform moveTarget;
        [SerializeField] private RectTransform scaleTarget;
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField] private Graphic flashTargetGraphic;

        public RectTransform RootRectTransform => rootRectTransform;
        public CanvasGroup CanvasGroup => canvasGroup;
        public RectTransform MoveTarget => moveTarget;
        public RectTransform ScaleTarget => scaleTarget;
        public RectTransform ShakeTarget => shakeTarget;
        public Graphic FlashTargetGraphic => flashTargetGraphic;

        private void Reset()
        {
            AutoBind();
        }

        private void Awake()
        {
            AutoBind();
        }

        /// <summary>
        /// 비어 있는 참조를 현재 GameObject 기준으로 자동 바인딩합니다.
        /// </summary>
        public void AutoBind()
        {
            if (rootRectTransform == null)
                rootRectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (moveTarget == null)
                moveTarget = rootRectTransform;

            if (scaleTarget == null)
                scaleTarget = rootRectTransform;

            if (shakeTarget == null)
                shakeTarget = rootRectTransform;

            if (flashTargetGraphic == null)
                flashTargetGraphic = GetComponent<Graphic>();
        }

        /// <summary>
        /// 대상 GameObject에서 UIEffectTarget을 가져오거나 생성합니다.
        /// </summary>
        public static UIEffectTarget GetOrAdd(GameObject target)
        {
            if (target == null) return null;

            var effectTarget = target.GetComponent<UIEffectTarget>();
            if (effectTarget == null)
                effectTarget = target.AddComponent<UIEffectTarget>();

            effectTarget.AutoBind();
            return effectTarget;
        }
    }
}
