using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GGemCo2DCore
{
    public class UIBlurRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("블러에 사용할 머티리얼입니다. GGemCo/UI/Blur/Kawase 셰이더를 사용하세요.")]
            public Material blurMaterial;

            [Min(1)]
            public int downsample = 2;

            [Range(1, 8)]
            public int iterations = 2;

            [Min(0f)]
            public float blurOffset = 1.25f;

            [Tooltip("블러를 요청한 UIBackdrop가 있을 때만 렌더링합니다.")]
            public bool renderOnlyWhenRequested = true;
        }

        public Settings settings = new Settings();

        private BlurPass _pass;

        private sealed class BlurPass : ScriptableRenderPass
        {
            private static readonly int BlurOffsetId = Shader.PropertyToID("_BlurOffset");
            private readonly Settings _settings;

            public BlurPass(Settings settings)
            {
                _settings = settings;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!Application.isPlaying)
                {
                    return;
                }

                if (_settings == null || _settings.blurMaterial == null)
                {
                    return;
                }

                if (_settings.renderOnlyWhenRequested && !UIBlurService.HasActiveRequestSafe)
                {
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer)
                {
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid())
                {
                    return;
                }

                TextureDesc blurTextureDescriptor = source.GetDescriptor(renderGraph);
                blurTextureDescriptor.name = "_UIBlurTempA";
                blurTextureDescriptor.clearBuffer = false;
                blurTextureDescriptor.depthBufferBits = DepthBits.None;
                blurTextureDescriptor.msaaSamples = MSAASamples.None;

                int downsample = Mathf.Max(1, _settings.downsample);
                blurTextureDescriptor.width = Mathf.Max(1, blurTextureDescriptor.width / downsample);
                blurTextureDescriptor.height = Mathf.Max(1, blurTextureDescriptor.height / downsample);

                TextureHandle tempA = renderGraph.CreateTexture(blurTextureDescriptor);
                blurTextureDescriptor.name = "_UIBlurTempB";
                TextureHandle tempB = renderGraph.CreateTexture(blurTextureDescriptor);

                _settings.blurMaterial.SetFloat(BlurOffsetId, _settings.blurOffset);

                TextureHandle current = source;
                int iterations = Mathf.Max(1, _settings.iterations);
                for (int i = 0; i < iterations; i++)
                {
                    TextureHandle verticalTarget = tempA;
                    TextureHandle horizontalTarget = tempB;

                    var verticalParams = new RenderGraphUtils.BlitMaterialParameters(current, verticalTarget, _settings.blurMaterial, 0);
                    renderGraph.AddBlitPass(verticalParams, $"UIBlur Vertical {i + 1}");

                    var horizontalParams = new RenderGraphUtils.BlitMaterialParameters(verticalTarget, horizontalTarget, _settings.blurMaterial, 1);
                    renderGraph.AddBlitPass(horizontalParams, $"UIBlur Horizontal {i + 1}");

                    current = horizontalTarget;
                }

                var outputDescriptor = cameraData.cameraTargetDescriptor;
                outputDescriptor.depthBufferBits = 0;
                outputDescriptor.msaaSamples = 1;

                if (!UIBlurService.EnsureOutput(outputDescriptor.width, outputDescriptor.height, outputDescriptor.graphicsFormat))
                {
                    return;
                }

                UIBlurService service = UIBlurService.Instance;
                if (service == null || service.OutputHandle == null)
                {
                    return;
                }

                TextureHandle output = renderGraph.ImportTexture(service.OutputHandle);
                if (!output.IsValid())
                {
                    return;
                }

                var outputParams = new RenderGraphUtils.BlitMaterialParameters(current, output, _settings.blurMaterial, 2);
                renderGraph.AddBlitPass(outputParams, "UIBlur Copy To Output");
            }
        }

        public override void Create()
        {
            _pass = new BlurPass(settings)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

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

            renderer.EnqueuePass(_pass);
        }
    }
}
