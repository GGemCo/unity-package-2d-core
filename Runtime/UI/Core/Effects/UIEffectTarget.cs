using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 실행 시 반복 조회되는 참조를 캐싱하는 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIEffectTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform scaleTarget;
        [SerializeField] private RectTransform shakeTarget;

        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = transform as RectTransform;
                return rectTransform;
            }
        }

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                    canvasGroup = GetComponent<CanvasGroup>();
                return canvasGroup;
            }
        }

        public Transform ScaleTarget => scaleTarget != null ? scaleTarget : transform;
        public RectTransform ShakeTarget => shakeTarget != null ? shakeTarget : RectTransform;

        private void Reset()
        {
            rectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            scaleTarget = transform;
            shakeTarget = transform as RectTransform;
        }

        public static UIEffectTarget GetOrAdd(GameObject target)
        {
            if (target == null) return null;
            var result = target.GetComponent<UIEffectTarget>();
            if (result != null) return result;
            return target.AddComponent<UIEffectTarget>();
        }
    }
}
