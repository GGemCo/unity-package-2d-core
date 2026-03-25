using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public sealed class SpriteWhiteOverlayController : MonoBehaviour
    {
        private static readonly int OverlayStrengthId = Shader.PropertyToID("_OverlayStrength");
        private static readonly int OverlayColorId = Shader.PropertyToID("_OverlayColor");

        [Header("Targets")]
        [SerializeField] private SpriteRenderer[] targetRenderers;

        [Header("Default")]
        [SerializeField, Range(0f, 1f)] private float overlayStrength = 0f;
        
        [SerializeField] private Color overlayColor = Color.white;

        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashRoutine;

        public float OverlayStrength => overlayStrength;
        public Color OverlayColor => overlayColor;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            ApplyOverlay();
        }


        /// <summary>
        /// 오버레이 기본 설정을 적용합니다.
        /// 필요 시 대상 SpriteRenderer 목록도 다시 수집합니다.
        /// </summary>
        public void Configure(Color color, bool refreshTargets = false)
        {
            SetOverlayColor(color);

            if (refreshTargets)
            {
                RefreshTargets();
            }
        }

        /// <summary>
        /// 오버레이 색상을 즉시 적용합니다.
        /// </summary>
        public void SetOverlayColor(Color color)
        {
            if (overlayColor == color)
            {
                return;
            }

            overlayColor = color;
            ApplyOverlay();
        }

        /// <summary>
        /// 오버레이 강도를 즉시 적용합니다.
        /// 0 = 원본 색상, 1 = 완전 흰색.
        /// </summary>
        public void SetOverlay(float strength)
        {
            strength = Mathf.Clamp01(strength);

            if (Mathf.Approximately(overlayStrength, strength))
            {
                return;
            }

            overlayStrength = strength;
            ApplyOverlay();
        }

        /// <summary>
        /// 오버레이를 제거합니다.
        /// </summary>
        public void ClearOverlay()
        {
            SetOverlay(0f);
        }

        /// <summary>
        /// 즉시 흰색으로 올렸다가 duration 동안 유지 후 제거합니다.
        /// 가장 단순한 피격 플래시에 적합합니다.
        /// </summary>
        public void Flash(float duration)
        {
            Flash(duration, null);
        }

        /// <summary>
        /// curve를 사용해 0~1 시간 구간 동안 오버레이 강도를 제어합니다.
        /// curve X축: 정규화 시간(0~1)
        /// curve Y축: 오버레이 강도(0~1)
        /// </summary>
        public void Flash(float duration, AnimationCurve curve)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            _flashRoutine = StartCoroutine(CoFlash(duration, curve));
        }

        /// <summary>
        /// 진행 중인 플래시를 중단하고 즉시 오버레이를 제거합니다.
        /// </summary>
        public void StopFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            ClearOverlay();
        }

        /// <summary>
        /// 대상 SpriteRenderer 목록을 다시 수집합니다.
        /// 런타임 중 파츠가 동적으로 추가되는 경우에 호출합니다.
        /// </summary>
        public void RefreshTargets()
        {
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            ApplyOverlay();
        }

        private IEnumerator CoFlash(float duration, AnimationCurve curve)
        {
            duration = Mathf.Max(0.01f, duration);

            if (curve == null)
            {
                SetOverlay(1f);
                yield return new WaitForSeconds(duration);
                SetOverlay(0f);
                _flashRoutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Clamp01(curve.Evaluate(normalized));
                SetOverlay(value);
                yield return null;
            }

            SetOverlay(0f);
            _flashRoutine = null;
        }

        private void ApplyOverlay()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                // GetPropertyBlock은 전달한 블록 내용을 덮어쓰므로,
                // 기존 블록을 읽은 뒤 필요한 값만 갱신하는 패턴을 유지합니다.
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(OverlayStrengthId, overlayStrength);
                _propertyBlock.SetColor(OverlayColorId, overlayColor);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void OnDisable()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            ClearOverlay();
        }
    }
}