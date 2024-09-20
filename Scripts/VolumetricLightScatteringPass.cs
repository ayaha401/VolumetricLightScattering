using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AyahaGraphicComponents.VolumetricLighting
{
    internal class VolumetricLightScatteringPass : ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler = new ProfilingSampler("VolumetricLightScattering");

        private Material _occluderMaterial;
        private Material _radialBlurMaterial;

        private RTHandle _occluderTarget;

        private VolumetricLightScattering.VolumetricLightScatteringSettings _settings;
        private readonly List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("LightweightForward"),
            new ShaderTagId("SRPDefaultUnlit")
        };
        private FilteringSettings _filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        public VolumetricLightScatteringPass(Material occludersMaterial, Material radialBlurMaterial, VolumetricLightScattering.VolumetricLightScatteringSettings settings)
        {
            _occluderMaterial = occludersMaterial;
            _radialBlurMaterial = radialBlurMaterial;
            _settings = settings;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDescriptor.depthBufferBits = 0;
            cameraTextureDescriptor.width = Mathf.RoundToInt(cameraTextureDescriptor.width * _settings.resolutionScale);
            cameraTextureDescriptor.height = Mathf.RoundToInt(cameraTextureDescriptor.height * _settings.resolutionScale);

            RenderingUtils.ReAllocateIfNeeded(ref _occluderTarget, cameraTextureDescriptor, name: "_RadialTexture");
            ConfigureTarget(_occluderTarget);
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_occluderMaterial == null || _radialBlurMaterial == null) return;
            if (RenderSettings.sun == null || !RenderSettings.sun.enabled) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                context.DrawSkybox(camera);

                DrawingSettings drawSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque);
                drawSettings.overrideMaterial = _occluderMaterial;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);

                cmd.SetGlobalTexture("_RadialTexture", _occluderTarget);

                Vector3 sunDirectionWorldSpace = RenderSettings.sun.transform.forward;
                Vector3 cameraDirectionWorldSpace = camera.transform.forward;
                Vector3 cameraPositionWorldSpace = camera.transform.position;
                Vector3 sunPositionWorldSpace = cameraPositionWorldSpace + sunDirectionWorldSpace;
                Vector3 sunPositionViewportSpace = camera.WorldToViewportPoint(sunPositionWorldSpace);

                float dotProd = Vector3.Dot(-cameraDirectionWorldSpace, sunDirectionWorldSpace);
                dotProd -= Vector3.Dot(cameraDirectionWorldSpace, Vector3.down);
                float intensityFader = dotProd / _settings.fadeRange;
                intensityFader = Mathf.Clamp(intensityFader, 0.0f, 1.0f);

                Color sunColor = RenderSettings.sun.color;
                if (RenderSettings.sun.useColorTemperature)
                {
                    sunColor *= Mathf.CorrelatedColorTemperatureToRGB(RenderSettings.sun.colorTemperature);
                }

                _radialBlurMaterial.SetColor("_Color", sunColor);
                _radialBlurMaterial.SetVector("_Center", sunPositionViewportSpace);
                _radialBlurMaterial.SetFloat("_BlurWidth", _settings.blurWidth);
                _radialBlurMaterial.SetFloat("_NumSamples", _settings.numSamples);
                _radialBlurMaterial.SetFloat("_Intensity", _settings.intensity * intensityFader);

                RTHandle camTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                Blitter.BlitCameraTexture(cmd, camTarget, camTarget, _radialBlurMaterial, 0);
            }
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _occluderTarget?.Release();
        }
    }
}