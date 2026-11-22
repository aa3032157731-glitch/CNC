using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CNC.TrainAR
{
    public enum VisualRenderStyle
    {
        RealisticPBR = 0,
        TrainARStylized = 1
    }

    [Serializable]
    public class MaterialStylePair
    {
        public Material realisticPbr;
        public Material trainArStylized;
    }

    /// <summary>
    /// Allows real-time 1-click switching between Photorealistic Smart-Factory PBR
    /// (100% native URP/Lit matching SampleScene) and TrainAR Stylized Cel + Ink Outline mode.
    /// </summary>
    public class TrainAR_StyleSwitcher : MonoBehaviour
    {
        public VisualRenderStyle activeStyle = VisualRenderStyle.RealisticPBR;
        public Camera targetCamera;
        public MeshRenderer floorRenderer;
        public Material floorRealisticPbr;
        public Material floorTrainArStudio;
        public Cubemap factoryReflectionCubemap;
        public Light keyLight;
        public Light fillLight;
        public Volume postProcessVolume;
        public List<MaterialStylePair> materialPairs = new List<MaterialStylePair>();

        public void ApplyStyle(VisualRenderStyle style)
        {
            activeStyle = style;
            bool isPbr = (style == VisualRenderStyle.RealisticPBR);

            // 1. Swap all renderer materials on this machine cleanly
            var lookup = new Dictionary<Material, Material>();
            foreach (var pair in materialPairs)
            {
                if (pair == null || pair.realisticPbr == null || pair.trainArStylized == null) continue;
                if (isPbr)
                {
                    lookup[pair.trainArStylized] = pair.realisticPbr;
                    lookup[pair.realisticPbr] = pair.realisticPbr;
                }
                else
                {
                    lookup[pair.realisticPbr] = pair.trainArStylized;
                    lookup[pair.trainArStylized] = pair.trainArStylized;
                }
            }

            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && lookup.TryGetValue(mats[i], out var target) && target != mats[i])
                    {
                        mats[i] = target;
                        changed = true;
                    }
                }
                if (changed) mr.sharedMaterials = mats;
            }

            // 2. Switch Camera URP Renderer & Backdrop
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                var urp = targetCamera.GetComponent<UniversalAdditionalCameraData>();
                if (urp != null)
                    urp.SetRenderer(isPbr ? 2 : 1); // 2 = Noise-Free Showroom PBR Renderer, 1 = TrainAR Ink Outline Renderer

                targetCamera.backgroundColor = isPbr
                    ? new Color(0.81f, 0.85f, 0.90f, 1f)   // Bright clean modern industrial showroom backdrop (matches user's favorite bright look!)
                    : new Color(0.994f, 0.995f, 0.998f, 1f); // Pure white TrainAR backdrop
            }

            // 3. Switch Floor Material
            if (floorRenderer != null)
            {
                floorRenderer.sharedMaterial = isPbr ? floorRealisticPbr : floorTrainArStudio;
            }

            // 4. Switch Environment Reflections & Lighting Tuning
            if (isPbr)
            {
                if (factoryReflectionCubemap != null)
                {
                    RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                    RenderSettings.customReflectionTexture = factoryReflectionCubemap;
                    RenderSettings.reflectionIntensity = 0.95f;
                }
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.72f, 0.78f, 0.86f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.56f, 0.62f, 0.70f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.36f, 0.40f, 0.46f, 1f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.81f, 0.85f, 0.90f, 1f);
                RenderSettings.fogStartDistance = 11.5f;
                RenderSettings.fogEndDistance = 30.0f;
                if (postProcessVolume != null) postProcessVolume.enabled = true;
                if (keyLight != null)
                {
                    keyLight.intensity = 1.40f;
                    keyLight.shadowStrength = 0.68f;
                }
                if (fillLight != null) fillLight.intensity = 0.46f;
            }
            else
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.fog = false;
                if (postProcessVolume != null) postProcessVolume.enabled = false;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.95f, 0.96f, 0.99f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.88f, 0.90f, 0.94f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.78f, 0.80f, 0.84f, 1f);
                if (keyLight != null)
                {
                    keyLight.intensity = 1.12f;
                    keyLight.shadowStrength = 0.78f;
                }
                if (fillLight != null) fillLight.intensity = 0.36f;
            }
        }
    }
}
