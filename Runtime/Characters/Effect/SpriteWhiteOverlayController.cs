using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private Material overlayMaterial;

        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _flashRoutine;
        private CharacterBase _ownerCharacter;
        private readonly List<SpriteRenderer> _targetRendererBuffer = new();

        public float OverlayStrength => overlayStrength;
        public Color OverlayColor => overlayColor;
        public Material OverlayMaterial => overlayMaterial;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _ownerCharacter = GetComponentInParent<CharacterBase>();

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                RefreshTargets();
            }
            else
            {
                FilterTargetRenderers(targetRenderers);
            }

            ApplyOverlay();
        }


        /// <summary>
        /// 오버레이 기본 설정을 적용합니다.
        /// 필요 시 대상 SpriteRenderer 목록도 다시 수집합니다.
        /// </summary>
        public void Configure(Color color, bool refreshTargets = false)
        {
            Configure(color, null, refreshTargets);
        }

        /// <summary>
        /// 오버레이 기본 설정을 적용합니다.
        /// 필요 시 대상 SpriteRenderer 목록을 다시 수집하고,
        /// Material이 지정되면 각 SpriteRenderer의 sharedMaterial에 적용합니다.
        /// </summary>
        public void Configure(Color color, Material material, bool refreshTargets = false)
        {
            SetOverlayColor(color);

            if (material != null)
            {
                ApplyOverlayMaterial(material, refreshTargets);
                return;
            }

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
        /// Sprite White Overlay 호환 Material을 각 대상 SpriteRenderer에 적용합니다.
        /// 런타임 중 Material asset 자체의 속성은 수정하지 않고 sharedMaterial 참조만 교체합니다.
        /// </summary>
        public void ApplyOverlayMaterial(Material material, bool refreshTargets = false)
        {
            if (material == null)
            {
                if (refreshTargets)
                {
                    RefreshTargets();
                }

                return;
            }

            overlayMaterial = material;

            if (refreshTargets || targetRenderers == null || targetRenderers.Length == 0)
            {
                RefreshTargets();
            }

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

                renderer.sharedMaterial = overlayMaterial;
            }

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
            FilterTargetRenderers(GetComponentsInChildren<SpriteRenderer>(true));
            ApplyOverlay();
        }

        /// <summary>
        /// 전달된 후보 중 현재 캐릭터가 직접 소유한 스프라이트 렌더러만 대상으로 보관합니다.
        /// </summary>
        /// <param name="candidates">오버레이 적용 대상 후보입니다.</param>
        private void FilterTargetRenderers(SpriteRenderer[] candidates)
        {
            _targetRendererBuffer.Clear();

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    SpriteRenderer candidate = candidates[i];
                    if (!IsValidOverlayTarget(candidate))
                        continue;

                    _targetRendererBuffer.Add(candidate);
                }
            }

            targetRenderers = _targetRendererBuffer.ToArray();
        }

        /// <summary>
        /// 캐릭터에 부착된 VFX와 하위 캐릭터의 렌더러를 오버레이 대상에서 제외합니다.
        /// </summary>
        /// <param name="candidate">검사할 스프라이트 렌더러입니다.</param>
        /// <returns>현재 캐릭터의 외형 렌더러이면 <c>true</c>를 반환합니다.</returns>
        private bool IsValidOverlayTarget(SpriteRenderer candidate)
        {
            if (candidate == null)
                return false;

            // Attach 방식으로 캐릭터 하위에 생성된 VFX는 캐릭터 오버레이의 영향을 받지 않아야 합니다.
            if (candidate.GetComponentInParent<VfxBehaviourBase>() != null)
                return false;

            if (_ownerCharacter == null)
                _ownerCharacter = GetComponentInParent<CharacterBase>();

            CharacterBase rendererOwner = candidate.GetComponentInParent<CharacterBase>();
            return _ownerCharacter == null || rendererOwner == _ownerCharacter;
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
