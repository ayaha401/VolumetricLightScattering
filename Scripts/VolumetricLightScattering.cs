using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AyahaGraphicComponents.VolumetricLighting
{
    internal class VolumetricLightScattering : ScriptableRendererFeature
    {
        public VolumetricLightScatteringSettings settings;
        private VolumetricLightScatteringPass _renderPass;

        private Material _occluderMaterial;
        private Material _radialBlurMaterial;

        [System.Serializable]
        public class VolumetricLightScatteringSettings
        {
            [Header("Volumetric Properties")]
            [Range(0.1f, 1f)]
            public float resolutionScale = 0.5f;
            [Range(0.0f, 1.0f)]
            public float intensity = 1.0f;
            [Range(0.0f, 1.0f)]
            public float blurWidth = 0.85f;
            [Range(0.0f, 0.5f)]
            public float fadeRange = 0.2f;
            [Range(50, 200)]
            public uint numSamples = 100;
        }

        public override void Create()
        {
            _occluderMaterial = new Material(Shader.Find("Hidden/Occluder"));
            _radialBlurMaterial = new Material(Shader.Find("Hidden/RadialBlur"));
            _renderPass = new VolumetricLightScatteringPass(_occluderMaterial, _radialBlurMaterial, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(_renderPass);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                _renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_occluderMaterial);
            CoreUtils.Destroy(_radialBlurMaterial);
            _renderPass.Dispose();
        }
    }
}