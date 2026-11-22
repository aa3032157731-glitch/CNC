using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CNC.TrainAR
{
    /// <summary>
    /// URP 14 Renderer Feature that applies TrainAR-style crisp ink outlines
    /// using Depth + World-Space Normals Roberts-Cross edge detection.
    /// </summary>
    public class TrainAR_EdgeOutlineFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public Shader outlineShader;

            [Header("Ink Style")]
            public Color outlineColor = new Color(0.14f, 0.16f, 0.21f, 1.0f);
            [Range(0.5f, 4.0f)] public float outlineThickness = 1.65f;
            [Range(0.0f, 1.0f)] public float outlineOpacity = 0.92f;

            [Header("Edge Sensitivity")]
            [Range(0.005f, 0.5f)] public float depthThreshold = 0.048f;
            [Range(0.05f, 1.5f)] public float normalThreshold = 0.44f;
            [Range(0.0f, 1.0f)] public float depthNormalThreshold = 0.48f;
            [Range(0.0f, 12.0f)] public float depthNormalScale = 6.5f;
            [Range(0.01f, 0.4f)] public float edgeSoftness = 0.14f;
            public float maxDistance = 24.0f;

            [Header("Runtime Override")]
            public bool renderInSceneView = true;
        }

        public Settings settings = new Settings();

        /// <summary>
        /// Optional static runtime override so Showcase UI can toggle/tune outlines interactively.
        /// </summary>
        public static bool RuntimeEnabled = true;
        public static float RuntimeThicknessMultiplier = 1.0f;
        public static float RuntimeOpacityMultiplier = 1.0f;

        private EdgeOutlinePass _pass;
        private Material _material;

        public override void Create()
        {
            EnsureMaterial();
            _pass = new EdgeOutlinePass(settings)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        private void EnsureMaterial()
        {
            if (settings.outlineShader == null)
                settings.outlineShader = Shader.Find("Hidden/TrainAR/EdgeOutline");

            if (_material == null && settings.outlineShader != null)
                _material = CoreUtils.CreateEngineMaterial(settings.outlineShader);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!RuntimeEnabled)
                return;

            var camType = renderingData.cameraData.cameraType;
            if (camType == CameraType.Preview || camType == CameraType.Reflection)
                return;
            if (camType == CameraType.SceneView && !settings.renderInSceneView)
                return;

            EnsureMaterial();
            if (_material == null)
                return;

            _pass.Setup(_material);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private class EdgeOutlinePass : ScriptableRenderPass
        {
            private const string ProfilerTag = "TrainAR Ink Outline";
            private readonly Settings _settings;
            private Material _mat;
            private RTHandle _tempColor;

            private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
            private static readonly int DepthThresholdId = Shader.PropertyToID("_DepthThreshold");
            private static readonly int NormalThresholdId = Shader.PropertyToID("_NormalThreshold");
            private static readonly int DepthNormalThresholdId = Shader.PropertyToID("_DepthNormalThreshold");
            private static readonly int DepthNormalScaleId = Shader.PropertyToID("_DepthNormalScale");
            private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
            private static readonly int MaxDistanceId = Shader.PropertyToID("_MaxDistance");
            private static readonly int OutlineOpacityId = Shader.PropertyToID("_OutlineOpacity");

            public EdgeOutlinePass(Settings settings)
            {
                _settings = settings;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
            }

            public void Setup(Material mat)
            {
                _mat = mat;
                renderPassEvent = _settings.renderPassEvent;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ResetTarget();
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = (int)DepthBits.None;
                desc.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref _tempColor, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TrainAROutlineTemp");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_mat == null || _tempColor == null)
                    return;

                var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (cameraColorTarget == null)
                    return;

                _mat.SetColor(OutlineColorId, _settings.outlineColor);
                _mat.SetFloat(OutlineThicknessId, _settings.outlineThickness * RuntimeThicknessMultiplier);
                _mat.SetFloat(DepthThresholdId, _settings.depthThreshold);
                _mat.SetFloat(NormalThresholdId, _settings.normalThreshold);
                _mat.SetFloat(DepthNormalThresholdId, _settings.depthNormalThreshold);
                _mat.SetFloat(DepthNormalScaleId, _settings.depthNormalScale);
                _mat.SetFloat(EdgeSoftnessId, _settings.edgeSoftness);
                _mat.SetFloat(MaxDistanceId, _settings.maxDistance);
                _mat.SetFloat(OutlineOpacityId, _settings.outlineOpacity * RuntimeOpacityMultiplier);

                CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);
                using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
                {
                    Blitter.BlitCameraTexture(cmd, cameraColorTarget, _tempColor);
                    Blitter.BlitCameraTexture(cmd, _tempColor, cameraColorTarget, _mat, 0);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                _tempColor?.Release();
                _tempColor = null;
            }
        }
    }
}
