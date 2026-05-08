using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 글리치 상태가 활성화되어 있을 때 카메라 컬러 텍스처에 전체 화면 글리치 셰이더를 적용하는 URP RenderFeature입니다.
    /// Renderer Data에 이 Feature를 추가하면 <see cref="CutsceneGlitchService"/>의 상태를 읽어 컷신 중에만 동작합니다.
    /// </summary>
    public sealed class CutsceneGlitchRenderFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// RenderFeature에서 사용하는 설정 값입니다.
        /// </summary>
        [System.Serializable]
        public class Settings
        {
            [Tooltip("글리치 효과에 사용할 머티리얼입니다. 비어 있으면 GGemCo/Cutscene/ScreenGlitch 셰이더로 런타임 머티리얼을 생성합니다.")]
            public Material glitchMaterial;

            [Tooltip("글리치 요청이 있을 때만 렌더 패스를 실행할지 여부입니다.")]
            public bool renderOnlyWhenRequested = true;

            [Tooltip("렌더 패스가 실행될 URP 타이밍입니다.")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        private const string ShaderName = "GGemCo/Cutscene/ScreenGlitch";

        public Settings settings = new Settings();

        private GlitchPass _pass;
        private Material _runtimeMaterial;

        /// <summary>
        /// ScriptableRendererFeature가 생성되거나 설정이 변경될 때 렌더 패스를 준비합니다.
        /// </summary>
        public override void Create()
        {
            EnsureMaterial();

            _pass = new GlitchPass(settings)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        /// <summary>
        /// 게임 카메라에 글리치 렌더 패스를 등록합니다.
        /// 에디터 미리보기나 씬 뷰 카메라에는 적용하지 않습니다.
        /// </summary>
        /// <param name="renderer">현재 카메라를 렌더링하는 ScriptableRenderer입니다.</param>
        /// <param name="renderingData">현재 렌더링 컨텍스트 데이터입니다.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            EnsureMaterial();
            if (settings.glitchMaterial == null)
            {
                return;
            }

            _pass.renderPassEvent = settings.renderPassEvent;
            renderer.EnqueuePass(_pass);
        }

        /// <summary>
        /// Feature가 제거되거나 파이프라인이 정리될 때 런타임 생성 머티리얼을 해제합니다.
        /// </summary>
        /// <param name="disposing">관리 리소스를 함께 해제할지 여부입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (_runtimeMaterial != null)
            {
                CoreUtils.Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }

        /// <summary>
        /// 명시적으로 지정된 머티리얼이 없으면 셰이더 이름으로 런타임 머티리얼을 생성합니다.
        /// 생성된 머티리얼은 Feature가 보유하며 Dispose 시 해제됩니다.
        /// </summary>
        private void EnsureMaterial()
        {
            if (settings == null)
            {
                settings = new Settings();
            }

            if (settings.glitchMaterial != null)
            {
                return;
            }

            if (_runtimeMaterial != null)
            {
                settings.glitchMaterial = _runtimeMaterial;
                return;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                return;
            }

            _runtimeMaterial = CoreUtils.CreateEngineMaterial(shader);
            settings.glitchMaterial = _runtimeMaterial;
        }

        /// <summary>
        /// 실제 RenderGraph 패스를 기록하고 글리치 셰이더 파라미터를 적용하는 렌더 패스입니다.
        /// </summary>
        private sealed class GlitchPass : ScriptableRenderPass
        {
            private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
            private static readonly int RgbSplitId = Shader.PropertyToID("_RgbSplit");
            private static readonly int HorizontalJitterId = Shader.PropertyToID("_HorizontalJitter");
            private static readonly int VerticalJumpId = Shader.PropertyToID("_VerticalJump");
            private static readonly int BlockNoiseId = Shader.PropertyToID("_BlockNoise");
            private static readonly int ScanlineStrengthId = Shader.PropertyToID("_ScanlineStrength");
            private static readonly int ColorDriftId = Shader.PropertyToID("_ColorDrift");
            private static readonly int GlitchTimeId = Shader.PropertyToID("_GlitchTime");
            private static readonly int SeedId = Shader.PropertyToID("_Seed");

            private readonly Settings _settings;

            /// <summary>
            /// 글리치 렌더 패스를 생성합니다.
            /// </summary>
            /// <param name="settings">렌더링에 사용할 Feature 설정입니다.</param>
            public GlitchPass(Settings settings)
            {
                _settings = settings;
            }

            /// <summary>
            /// RenderGraph에 글리치 블릿 패스를 기록합니다.
            /// 활성 요청이 없거나 타겟이 백버퍼인 경우에는 렌더링을 건너뜁니다.
            /// </summary>
            /// <param name="renderGraph">현재 프레임의 RenderGraph입니다.</param>
            /// <param name="frameData">URP가 제공하는 프레임별 렌더링 데이터입니다.</param>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!Application.isPlaying)
                {
                    return;
                }

                if (_settings == null || _settings.glitchMaterial == null)
                {
                    return;
                }

                if (_settings.renderOnlyWhenRequested && !CutsceneGlitchService.HasActiveGlitchSafe)
                {
                    return;
                }

                ScreenGlitchState state = CutsceneGlitchService.CurrentStateSafe;
                if (!state.IsActive())
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid())
                {
                    return;
                }

                ApplyMaterialState(_settings.glitchMaterial, state);

                TextureDesc descriptor = source.GetDescriptor(renderGraph);
                descriptor.name = "_CutsceneGlitchTemp";
                descriptor.clearBuffer = false;
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;

                TextureHandle temp = renderGraph.CreateTexture(descriptor);

                var glitchParams = new RenderGraphUtils.BlitMaterialParameters(source, temp, _settings.glitchMaterial, 0);
                renderGraph.AddBlitPass(glitchParams, "Cutscene Glitch");

                var copyParams = new RenderGraphUtils.BlitMaterialParameters(temp, source, _settings.glitchMaterial, 1);
                renderGraph.AddBlitPass(copyParams, "Cutscene Glitch Copy");
            }

            /// <summary>
            /// 현재 컷신 글리치 상태를 셰이더 프로퍼티로 변환해 머티리얼에 반영합니다.
            /// </summary>
            /// <param name="material">글리치 셰이더를 사용하는 머티리얼입니다.</param>
            /// <param name="state">이번 프레임에 적용할 글리치 상태입니다.</param>
            private static void ApplyMaterialState(Material material, ScreenGlitchState state)
            {
                float intensity = Mathf.Clamp01(state.Intensity);

                material.SetFloat(IntensityId, intensity);
                material.SetFloat(RgbSplitId, Mathf.Max(0f, state.RgbSplit));
                material.SetFloat(HorizontalJitterId, Mathf.Max(0f, state.HorizontalJitter));
                material.SetFloat(VerticalJumpId, Mathf.Max(0f, state.VerticalJump));
                material.SetFloat(BlockNoiseId, Mathf.Clamp01(state.BlockNoise));
                material.SetFloat(ScanlineStrengthId, Mathf.Clamp01(state.ScanlineStrength));
                material.SetFloat(ColorDriftId, Mathf.Clamp01(state.ColorDrift));
                material.SetFloat(GlitchTimeId, Time.unscaledTime * Mathf.Max(0f, state.NoiseSpeed) + state.Seed);
                material.SetFloat(SeedId, state.Seed);
            }
        }
    }
}
