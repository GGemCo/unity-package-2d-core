using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 실행 시 반복 조회되는 참조를 캐싱하는 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIEffectTarget : MonoBehaviour
    {
        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform moveTarget;
        [SerializeField] private RectTransform shakeTarget;
        [SerializeField] private Transform scaleTarget;
        [SerializeField] private Graphic flashTargetGraphic;

        public RectTransform RootRectTransform
        {
            get
            {
                if (rootRectTransform == null)
                    rootRectTransform = transform as RectTransform;
                return rootRectTransform;
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

        public RectTransform MoveTarget => moveTarget != null ? moveTarget : RootRectTransform;
        public RectTransform ShakeTarget => shakeTarget != null ? shakeTarget : RootRectTransform;
        public Transform ScaleTarget => scaleTarget != null ? scaleTarget : transform;
        public Graphic FlashTargetGraphic => flashTargetGraphic;

        private void Reset()
        {
            rootRectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            moveTarget = transform as RectTransform;
            shakeTarget = transform as RectTransform;
            scaleTarget = transform;
        }

        public static UIEffectTarget GetOrAdd(GameObject target)
        {
            if (target == null)
                return null;

            var result = target.GetComponent<UIEffectTarget>();
            if (result != null)
                return result;

            return target.AddComponent<UIEffectTarget>();
        }
    }
}
