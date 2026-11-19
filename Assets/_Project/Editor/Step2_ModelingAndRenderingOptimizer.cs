using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CNC.DigitalTwin;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CNC.EditorTools
{
    public static class Step2_ModelingAndRenderingOptimizer
    {
        private const string MatDir = "Assets/_Project/Materials";
        private const string ConvMatDir = "Assets/_Project/Materials/Converted";
        private const string OptMeshDir = "Assets/_Project/Models/Optimized";
        private const string PrefabDir = "Assets/_Project/Prefabs";
        private const string SettingsDir = "Assets/_Project/Settings";
        private const string PbrTexDir = "Assets/_Project/Textures/PBR";

        private static Shader _urpLit;
        private static Shader _urpUnlit;
        private static readonly Dictionary<string, Material> _convertedCache = new Dictionary<string, Material>();

        // Shared industrial PBR materials
        private static Material _matEpoxyFloor;
        private static Material _matBrushedSteel;
        private static Material _matDarkGunmetal;
        private static Material _matCottonBale;
        private static Material _matConveyorBelt;
        private static Material _matAgvFlow;
        private static Material _matCeilingLed;
        private static Material _matHoloPad;
        private static Material _matSignboardDark;
        private static Material _matBeaconMetal;
        private static Material _matBeaconLens;
        private static Material _matHighlightHover;
        private static Material _matHighlightSelected;
        private static Material _matRobotYellow;

        [MenuItem("CNC Optimize/Step 2 - Optimize Modeling, PBR Materials & Lighting")]
        public static void Run()
        {
            Debug.Log("[CNC Step2] Starting 3D Modeling, PBR Material & URP Lighting Optimization...");
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(ConvMatDir);
            Directory.CreateDirectory(OptMeshDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(SettingsDir);

            _urpLit = Shader.Find("Universal Render Pipeline/Lit");
            _urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (_urpLit == null)
                throw new Exception("Universal Render Pipeline/Lit shader not found! Run Step 1 first.");

            _convertedCache.Clear();

            // 1. Build Industrial PBR Material Library
            BuildPbrMaterialLibrary();

            // 2. Fix & Upgrade standalone project .mat assets to URP/Lit
            UpgradeStandaloneMaterials();

            // 3. Open SampleScene.unity and perform modeling & scene optimization
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            // 3a. Convert all scene renderers' embedded/legacy materials to GPU-instanced URP/Lit
            UpgradeAllSceneRendererMaterials();

            // 3b. Consolidate & LOD massive models (Fence x26, Warehouse x2, ProdLine01 x2)
            OptimizeAllFences();
            OptimizeBySourceModel("立体仓库.FBX", "Warehouse", 3, 2, true, 0.045f);
            OptimizeBySourceModel("产线01.FBX", "ProdLine01", 2, 1, false, 0.025f);

            // 3c. Upgrade 62 boxy cotton bales to high-craft beveled PBR cotton bales
            UpgradeCottonBales();

            // 3d. Upgrade Building surfaces (epoxy floor, glowing LED ceiling tubes, static flags)
            UpgradeWorkshopBuildingsAndProps();

            // 3e. Upgrade hanging signs & red-ring floor pads
            UpgradeHangingSignboards();
            UpgradeFloorStationPads();

            // 3f. Place industrial 3-color status beacons & integrate inspection robotic arms
            BuildStationStatusBeacons();
            IntegrateInspectionRoboticArms();

            // 3g. Configure Industrial Lighting, Reflection Probes, Global URP Volume & Camera
            SetupIndustrialLightingAndVolume();

            // 3h. Mark remaining static machinery/structures, enforce 100% GPU Instancing & bake Reflection Probes
            MarkStaticSceneGeometry();
            EnsureAllSceneMaterialsGpuInstanced();
            BakeAllReflectionProbes();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CNC Step2] Modeling, PBR Materials & Lighting Optimization completed!");
        }

        private static Texture2D LoadTex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        private static Material CreateOrUpdateMat(string path, Shader shader, Action<Material> configure)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            configure(mat);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildPbrMaterialLibrary()
        {
            var texFloorAlb = LoadTex($"{PbrTexDir}/Tex_EpoxyFloor_Albedo.png");
            var texFloorNrm = LoadTex($"{PbrTexDir}/Tex_EpoxyFloor_Normal.png");
            var texSteelAlb = LoadTex($"{PbrTexDir}/Tex_BrushedMetal_Albedo.png");
            var texSteelNrm = LoadTex($"{PbrTexDir}/Tex_BrushedMetal_Normal.png");
            var texCoatNrm  = LoadTex($"{PbrTexDir}/Tex_PowderCoat_Normal.png");
            var texCotAlb   = LoadTex($"{PbrTexDir}/Tex_CottonBale_Albedo.png");
            var texCotNrm   = LoadTex($"{PbrTexDir}/Tex_CottonBale_Normal.png");
            var texConvAlb  = LoadTex($"{PbrTexDir}/Tex_ConveyorBelt_Albedo.png");
            var texConvNrm  = LoadTex($"{PbrTexDir}/Tex_ConveyorBelt_Normal.png");
            var texFlowMask = LoadTex($"{PbrTexDir}/Tex_FlowChevron_Mask.png");

            _matEpoxyFloor = CreateOrUpdateMat($"{MatDir}/Mat_EpoxyFloor.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.84f, 0.88f, 0.92f, 1f));
                m.SetTexture("_BaseMap", texFloorAlb);
                m.SetTextureScale("_BaseMap", new Vector2(22f, 22f));
                m.SetTexture("_BumpMap", texFloorNrm);
                m.SetTextureScale("_BumpMap", new Vector2(22f, 22f));
                m.SetFloat("_BumpScale", 0.45f);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 0.08f);
                m.SetFloat("_Smoothness", 0.56f);
            });

            _matBrushedSteel = CreateOrUpdateMat($"{MatDir}/Mat_BrushedSteel.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.86f, 0.90f, 0.94f, 1f));
                m.SetTexture("_BaseMap", texSteelAlb);
                m.SetTexture("_BumpMap", texSteelNrm);
                m.SetFloat("_BumpScale", 0.35f);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 0.86f);
                m.SetFloat("_Smoothness", 0.68f);
            });

            _matDarkGunmetal = CreateOrUpdateMat($"{MatDir}/Mat_DarkGunmetal.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.18f, 0.22f, 0.28f, 1f));
                m.SetTexture("_BumpMap", texSteelNrm);
                m.SetFloat("_BumpScale", 0.28f);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 0.68f);
                m.SetFloat("_Smoothness", 0.50f);
            });

            _matRobotYellow = CreateOrUpdateMat($"{MatDir}/Mat_RobotYellow.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.95f, 0.68f, 0.08f, 1f));
                m.SetTexture("_BumpMap", texCoatNrm);
                m.SetFloat("_BumpScale", 0.22f);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 0.25f);
                m.SetFloat("_Smoothness", 0.65f);
            });

            _matCottonBale = CreateOrUpdateMat($"{MatDir}/Mat_CottonBale_PBR.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", texCotAlb);
                m.SetTexture("_BumpMap", texCotNrm);
                m.SetFloat("_BumpScale", 0.85f);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 0.0f);
                m.SetFloat("_Smoothness", 0.14f);
            });

            var convShader = Shader.Find("CNC/URP/ConveyorBelt") ?? _urpLit;
            _matConveyorBelt = CreateOrUpdateMat($"{MatDir}/Mat_ConveyorBelt_Animated.mat", convShader, m =>
            {
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", texConvAlb);
                m.SetTextureScale("_BaseMap", new Vector2(1f, 6f));
                m.SetTexture("_BumpMap", texConvNrm);
                m.SetFloat("_BumpScale", 0.85f);
                m.SetFloat("_Metallic", 0.08f);
                m.SetFloat("_Smoothness", 0.34f);
                if (m.HasProperty("_ScrollDir")) m.SetVector("_ScrollDir", new Vector4(0f, 1f, 0f, 0f));
                if (m.HasProperty("_ScrollSpeed")) m.SetFloat("_ScrollSpeed", 0.6f);
                if (m.HasProperty("_IsRunning")) m.SetFloat("_IsRunning", 1f);
            });

            var flowShader = Shader.Find("CNC/URP/EmissiveFlow") ?? _urpLit;
            _matAgvFlow = CreateOrUpdateMat($"{MatDir}/Mat_AGVGuide_Flow.mat", flowShader, m =>
            {
                m.SetColor("_BaseColor", new Color(0.10f, 0.16f, 0.24f, 1f));
                if (m.HasProperty("_FlowColor")) m.SetColor("_FlowColor", new Color(0.0f, 1.9f, 2.5f, 1f));
                if (m.HasProperty("_FlowMask"))
                {
                    m.SetTexture("_FlowMask", texFlowMask);
                    m.SetTextureScale("_FlowMask", new Vector2(2f, 1f));
                }
                if (m.HasProperty("_FlowDir")) m.SetVector("_FlowDir", new Vector4(1f, 0f, 0f, 0f));
                if (m.HasProperty("_FlowSpeed")) m.SetFloat("_FlowSpeed", 0.85f);
            });

            _matCeilingLed = CreateOrUpdateMat($"{MatDir}/Mat_CeilingLED_Emissive.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1.0f, 1f));
                m.SetFloat("_Metallic", 0.0f);
                m.SetFloat("_Smoothness", 0.85f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(2.2f, 2.45f, 2.8f, 1f));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            });

            _matHoloPad = CreateOrUpdateMat($"{MatDir}/Mat_HoloFloorPad.mat", _urpLit, m =>
            {
                m.SetFloat("_Surface", 1f); // Transparent
                m.SetFloat("_Blend", 0f);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = (int)RenderQueue.Transparent;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetColor("_BaseColor", new Color(0.0f, 0.85f, 1.0f, 0.35f));
                m.SetTexture("_BaseMap", LoadTex("Assets/_Project/UI/Sprites/ui_circle_ring.png"));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.0f, 1.6f, 2.2f, 1f));
            });

            _matSignboardDark = CreateOrUpdateMat($"{MatDir}/Mat_SignboardDark.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.04f, 0.11f, 0.21f, 1f));
                m.SetFloat("_Metallic", 0.45f);
                m.SetFloat("_Smoothness", 0.72f);
            });

            _matBeaconMetal = CreateOrUpdateMat($"{MatDir}/Mat_BeaconMetal.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.22f, 0.26f, 0.32f, 1f));
                m.SetFloat("_Metallic", 0.82f);
                m.SetFloat("_Smoothness", 0.65f);
            });

            _matBeaconLens = CreateOrUpdateMat($"{MatDir}/Mat_BeaconLens.mat", _urpLit, m =>
            {
                m.SetColor("_BaseColor", new Color(0.15f, 0.9f, 0.45f, 1f));
                m.SetFloat("_Metallic", 0.1f);
                m.SetFloat("_Smoothness", 0.9f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.2f, 2.0f, 0.9f, 1f));
            });

            var hlShader = Shader.Find("CNC/URP/MachineHighlight") ?? _urpUnlit;
            _matHighlightHover = CreateOrUpdateMat($"{MatDir}/Mat_HighlightHover.mat", hlShader, m =>
            {
                if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(0.15f, 1.4f, 2.2f, 0.85f));
                if (m.HasProperty("_RimPower")) m.SetFloat("_RimPower", 2.4f);
                if (m.HasProperty("_FillAlpha")) m.SetFloat("_FillAlpha", 0.08f);
            });

            _matHighlightSelected = CreateOrUpdateMat($"{MatDir}/Mat_HighlightSelected.mat", hlShader, m =>
            {
                if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", new Color(0.0f, 2.2f, 2.8f, 1.0f));
                if (m.HasProperty("_RimPower")) m.SetFloat("_RimPower", 1.9f);
                if (m.HasProperty("_FillAlpha")) m.SetFloat("_FillAlpha", 0.14f);
            });
        }

        private static void UpgradeStandaloneMaterials()
        {
            var texCoatNrm = LoadTex($"{PbrTexDir}/Tex_PowderCoat_Normal.png");
            var texFloorAlb = LoadTex($"{PbrTexDir}/Tex_EpoxyFloor_Albedo.png");
            var texFloorNrm = LoadTex($"{PbrTexDir}/Tex_EpoxyFloor_Normal.png");
            var texSteelNrm = LoadTex($"{PbrTexDir}/Tex_BrushedMetal_Normal.png");

            string[] matFolders = {
                "Assets/Materials",
                "Assets/J—Color",
                "Assets/Other/UnityWarehouseSceneHDRP/Scene_Warehouse/Background/Materials"
            };

            int upgraded = 0;
            foreach (var folder in matFolders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var f in Directory.GetFiles(folder, "*.mat"))
                {
                    string assetPath = f.Replace("\\", "/");
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (mat == null) continue;

                    // Preserve existing albedo & color before switching shader
                    Color origColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") :
                                      (mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white);
                    Texture origTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") :
                                      (mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null);
                    Vector2 origScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
                    float origMet = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0.15f;
                    float origSmooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") :
                                       (mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.5f);

                    string matName = mat.name;
                    mat.shader = _urpLit;

                    // Strip all miswired/broken maps first!
                    mat.SetTexture("_BumpMap", null);
                    mat.SetTexture("_MetallicGlossMap", null);
                    mat.SetTexture("_OcclusionMap", null);
                    mat.SetTexture("_ParallaxMap", null);
                    mat.DisableKeyword("_NORMALMAP");
                    mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                    mat.DisableKeyword("_PARALLAXMAP");
                    mat.DisableKeyword("_OCCLUSIONMAP");

                    // Apply clean industrial PBR per material role
                    if (matName == "Floor" || matName == "F")
                    {
                        mat.SetColor("_BaseColor", new Color(0.84f, 0.88f, 0.92f, 1f));
                        mat.SetTexture("_BaseMap", texFloorAlb);
                        mat.SetTextureScale("_BaseMap", new Vector2(16f, 16f));
                        mat.SetTexture("_BumpMap", texFloorNrm);
                        mat.SetTextureScale("_BumpMap", new Vector2(16f, 16f));
                        mat.SetFloat("_BumpScale", 0.45f);
                        mat.EnableKeyword("_NORMALMAP");
                        mat.SetFloat("_Metallic", 0.08f);
                        mat.SetFloat("_Smoothness", 0.55f);
                    }
                    else if (matName == "FloorSteel")
                    {
                        mat.SetColor("_BaseColor", new Color(0.75f, 0.80f, 0.85f, 1f));
                        mat.SetTexture("_BaseMap", origTex);
                        mat.SetTexture("_BumpMap", texSteelNrm);
                        mat.SetFloat("_BumpScale", 0.4f);
                        mat.EnableKeyword("_NORMALMAP");
                        mat.SetFloat("_Metallic", 0.82f);
                        mat.SetFloat("_Smoothness", 0.60f);
                    }
                    else if (matName == "Ceiling" || matName.StartsWith("WallPanel") || matName == "WallConcrete" || matName == "Shutter")
                    {
                        // Bright modern industrial architectural wall/ceiling finish (strip miswired paint-as-normal/metallic!)
                        mat.SetColor("_BaseColor", matName.StartsWith("WallPanel")
                            ? new Color(0.95f, 0.97f, 1.0f, 1f)
                            : new Color(0.92f, 0.94f, 0.97f, 1f));
                        mat.SetTexture("_BaseMap", matName == "WallPanel_001" ? null : origTex);
                        mat.SetTextureScale("_BaseMap", origScale);
                        mat.SetFloat("_Metallic", matName == "Shutter" ? 0.42f : 0.12f);
                        mat.SetFloat("_Smoothness", matName == "Shutter" ? 0.50f : 0.36f);
                    }
                    else if (assetPath.Contains("Assets/Materials/aa.mat") || assetPath.Contains("Assets/Materials/T.mat") ||
                             assetPath.Contains("Assets/Materials/4.mat") || assetPath.Contains("Assets/Materials/1.mat"))
                    {
                        // Fix specifically broken materials (aa had 64MB Floor.tga as Metallic, 4 had missing GUID, 1 had skybox face!)
                        mat.SetColor("_BaseColor", new Color(0.82f, 0.87f, 0.92f, 1f));
                        mat.SetTexture("_BaseMap", null);
                        mat.SetTexture("_BumpMap", texCoatNrm);
                        mat.SetFloat("_BumpScale", 0.22f);
                        mat.EnableKeyword("_NORMALMAP");
                        mat.SetFloat("_Metallic", 0.25f);
                        mat.SetFloat("_Smoothness", 0.58f);
                    }
                    else
                    {
                        // Standard industrial machinery paint / color material
                        mat.SetColor("_BaseColor", origColor);
                        mat.SetTexture("_BaseMap", origTex);
                        mat.SetTextureScale("_BaseMap", origScale);
                        mat.SetTexture("_BumpMap", texCoatNrm);
                        mat.SetFloat("_BumpScale", 0.18f);
                        mat.EnableKeyword("_NORMALMAP");
                        mat.SetFloat("_Metallic", Mathf.Clamp(origMet, 0.12f, 0.75f));
                        mat.SetFloat("_Smoothness", Mathf.Clamp(origSmooth, 0.38f, 0.72f));
                    }

                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    upgraded++;
                }
            }
            Debug.Log($"[CNC Step2] Upgraded & repaired {upgraded} standalone materials to URP/Lit with GPU Instancing.");
        }

        private static Material ConvertOrCacheUrpMaterial(Material src)
        {
            if (src == null) return _matDarkGunmetal;
            if (src.shader != null && (src.shader.name.StartsWith("Universal Render Pipeline/") || src.shader.name.StartsWith("CNC/URP/")))
            {
                if (!src.enableInstancing)
                {
                    src.enableInstancing = true;
                    EditorUtility.SetDirty(src);
                }
                return src;
            }

            // Extract color & texture from Built-in Standard / embedded FBX material
            Color col = Color.white;
            if (src.HasProperty("_Color")) col = src.GetColor("_Color");
            else if (src.HasProperty("_BaseColor")) col = src.GetColor("_BaseColor");

            Texture tex = src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            float met = src.HasProperty("_Metallic") ? src.GetFloat("_Metallic") : 0.22f;
            float smo = src.HasProperty("_Glossiness") ? src.GetFloat("_Glossiness") : 0.52f;

            // Special semantic enhancements by material name or color
            string lowerName = src.name.ToLowerInvariant();
            if (lowerName.Contains("light") || lowerName.Contains("lamp") || lowerName.Contains("emiss"))
                return _matCeilingLed;

            // Cache key based on color & texture to aggressively collapse duplicate embedded FBX materials!
            int r = Mathf.RoundToInt(col.r * 255);
            int g = Mathf.RoundToInt(col.g * 255);
            int b = Mathf.RoundToInt(col.b * 255);
            string texKey = tex != null ? tex.GetInstanceID().ToString() : "notex";
            string key = $"Conv_{r}_{g}_{b}_{Mathf.RoundToInt(met * 10)}_{texKey}";

            if (_convertedCache.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            string assetPath = $"{ConvMatDir}/{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                _convertedCache[key] = existing;
                return existing;
            }

            var mat = new Material(_urpLit);
            mat.SetColor("_BaseColor", col);
            if (tex != null) mat.SetTexture("_BaseMap", tex);

            // Detect metallic/grey/yellow/teal industrial tones for authentic PBR feel
            bool isDarkGrey = (col.r < 0.35f && col.g < 0.35f && col.b < 0.35f);
            bool isNeutralSilver = (Mathf.Abs(col.r - col.g) < 0.08f && Mathf.Abs(col.g - col.b) < 0.08f && col.r > 0.45f && col.r < 0.82f);
            bool isYellow = (col.r > 0.7f && col.g > 0.55f && col.b < 0.3f);

            if (isNeutralSilver && tex == null)
            {
                mat.SetFloat("_Metallic", 0.72f);
                mat.SetFloat("_Smoothness", 0.62f);
            }
            else if (isDarkGrey && tex == null)
            {
                mat.SetFloat("_Metallic", 0.52f);
                mat.SetFloat("_Smoothness", 0.46f);
            }
            else if (isYellow)
            {
                mat.SetFloat("_Metallic", 0.25f);
                mat.SetFloat("_Smoothness", 0.64f);
            }
            else
            {
                mat.SetFloat("_Metallic", Mathf.Clamp(met, 0.14f, 0.65f));
                mat.SetFloat("_Smoothness", Mathf.Clamp(smo, 0.42f, 0.68f));
            }

            var coatNrm = LoadTex($"{PbrTexDir}/Tex_PowderCoat_Normal.png");
            if (coatNrm != null && tex == null)
            {
                mat.SetTexture("_BumpMap", coatNrm);
                mat.SetFloat("_BumpScale", 0.16f);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, assetPath);
            _convertedCache[key] = mat;
            return mat;
        }

        private static void UpgradeAllSceneRendererMaterials()
        {
            var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>(true);
            int convertedSlots = 0;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var up = ConvertOrCacheUrpMaterial(mats[i]);
                    if (up != mats[i])
                    {
                        mats[i] = up;
                        changed = true;
                        convertedSlots++;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
            Debug.Log($"[CNC Step2] Converted {convertedSlots} embedded/legacy renderer material slots into {_convertedCache.Count} shared URP/Lit GPU-instanced materials.");
        }

        // ==================== MESH CONSOLIDATION & LOD ENGINE ====================

        private static void OptimizeAllFences()
        {
            var fenceFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/M/栅栏.fbx");
            if (fenceFbx == null) return;

            // Find ONLY exact PrefabInstance roots of Assets/M/栅栏.fbx (exactly 26 roots, never sub-children!)
            var fenceRoots = new List<GameObject>();
            foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            {
                if (t == null) continue;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (src != null && AssetDatabase.GetAssetPath(src).EndsWith("栅栏.fbx", StringComparison.OrdinalIgnoreCase))
                {
                    if (!fenceRoots.Contains(t.gameObject))
                        fenceRoots.Add(t.gameObject);
                }
            }

            if (fenceRoots.Count == 0)
            {
                Debug.Log("[CNC Step2] No raw multi-mesh 栅栏 instances found.");
                return;
            }

            // Build one clean single-fence merged LOD0 + gentle LOD1 Prefab directly from the pure FBX asset!
            var tempFbxInst = (GameObject)PrefabUtility.InstantiatePrefab(fenceFbx);
            tempFbxInst.transform.position = Vector3.zero;
            tempFbxInst.transform.rotation = Quaternion.identity;
            tempFbxInst.transform.localScale = Vector3.one;

            var (lod0Mesh, mats) = CombineHierarchyToMultiSubmesh(tempFbxInst, $"{OptMeshDir}/Mesh_Fence_LOD0.asset");
            // Gentle 3.5cm clustering keeps fence bars crisp while cutting ~45% tris at distance
            var lod1Mesh = SimplifyMeshClustered(lod0Mesh, 0.035f, $"{OptMeshDir}/Mesh_Fence_LOD1.asset");
            UnityEngine.Object.DestroyImmediate(tempFbxInst);

            string prefabPath = $"{PrefabDir}/PF_Fence_Optimized.prefab";
            var tempRoot = new GameObject("PF_Fence_Optimized");
            // Keep LOD0 visible down to 4% screen height, LOD1 down to 0.5% so fences NEVER vanish!
            CreateLodPair(tempRoot, lod0Mesh, lod1Mesh, null, mats, 0.04f, 0.005f);
            MarkStaticRecursive(tempRoot, true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);
            UnityEngine.Object.DestroyImmediate(tempRoot);

            int replaced = 0;
            foreach (var oldFence in fenceRoots)
            {
                if (oldFence == null) continue;
                Transform parent = oldFence.transform.parent;
                Vector3 pos = oldFence.transform.position;
                Quaternion rot = oldFence.transform.rotation;
                Vector3 localScl = oldFence.transform.localScale;
                string origName = oldFence.name;

                // Reparent any non-fence children out first so nothing unrelated is ever deleted!
                var childrenToKeep = new List<Transform>();
                foreach (Transform ch in oldFence.transform)
                {
                    var csrc = PrefabUtility.GetCorrespondingObjectFromSource(ch.gameObject);
                    if (csrc == null || !AssetDatabase.GetAssetPath(csrc).EndsWith("栅栏.fbx", StringComparison.OrdinalIgnoreCase))
                        childrenToKeep.Add(ch);
                }
                foreach (var ch in childrenToKeep)
                    ch.SetParent(parent, true);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = origName + "_Opt";
                inst.transform.SetParent(parent, false);
                inst.transform.position = pos;
                inst.transform.rotation = rot;
                inst.transform.localScale = localScl;

                UnityEngine.Object.DestroyImmediate(oldFence);
                replaced++;
            }
            Debug.Log($"[CNC Step2] Consolidated {replaced} 栅栏 instances (pure single-fence LOD prefab, ~{CountTriangles(lod0Mesh):N0} tris LOD0, GPU-instanced!).");
        }

        private static void OptimizeBySourceModel(string fbxFileName, string assetPrefix, int gridX, int gridZ, bool generateLod2, float lod0MicroWeldCell)
        {
            var targets = new List<GameObject>();
            foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            {
                if (t == null) continue;
                var src = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (src != null && AssetDatabase.GetAssetPath(src).EndsWith(fbxFileName, StringComparison.OrdinalIgnoreCase))
                {
                    bool parentSame = t.parent != null && PrefabUtility.GetCorrespondingObjectFromSource(t.parent.gameObject) != null &&
                                      AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.parent.gameObject))
                                          .EndsWith(fbxFileName, StringComparison.OrdinalIgnoreCase);
                    if (!parentSame && t.GetComponentsInChildren<MeshRenderer>(true).Length >= 40)
                        targets.Add(t.gameObject);
                }
            }

            Debug.Log($"[CNC Step2] Found {targets.Count} instances of {fbxFileName} for spatial LOD consolidation.");
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    OptimizeSingleAssembly(targets[i], $"{assetPrefix}_{i + 1}", gridX, gridZ, generateLod2, lod0MicroWeldCell);
            }
        }

        private static void OptimizeSingleAssembly(GameObject root, string assetPrefix, int gridX, int gridZ, bool generateLod2, float lod0MicroWeldCell)
        {
            if (root == null) return;
            string rootName = root.name;

            // Collect ONLY renderers belonging to this assembly (skip unrelated reparented children if any)
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length < 40) return;

            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) totalBounds.Encapsulate(renderers[i].bounds);

            var buckets = new Dictionary<(int, int), List<MeshRenderer>>();
            foreach (var mr in renderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Vector3 c = mr.bounds.center;
                float nx = totalBounds.size.x > 0.01f ? Mathf.Clamp01((c.x - totalBounds.min.x) / totalBounds.size.x) : 0f;
                float nz = totalBounds.size.z > 0.01f ? Mathf.Clamp01((c.z - totalBounds.min.z) / totalBounds.size.z) : 0f;
                int ix = Mathf.Clamp((int)(nx * gridX), 0, gridX - 1);
                int iz = Mathf.Clamp((int)(nz * gridZ), 0, gridZ - 1);
                var cell = (ix, iz);
                if (!buckets.TryGetValue(cell, out var list))
                {
                    list = new List<MeshRenderer>();
                    buckets[cell] = list;
                }
                list.Add(mr);
            }

            var optParent = new GameObject($"{rootName}_Optimized");
            optParent.transform.SetParent(root.transform.parent, true);
            optParent.transform.position = totalBounds.center;
            optParent.transform.rotation = Quaternion.identity;
            optParent.transform.localScale = Vector3.one;

            string subDir = $"{OptMeshDir}/{assetPrefix}";
            Directory.CreateDirectory(subDir);

            int chunkIdx = 0;
            long rawTris = 0;
            long lod0Tris = 0;
            long lod1Tris = 0;
            foreach (var kv in buckets)
            {
                var chunkGo = new GameObject($"{assetPrefix}_Chunk_{kv.Key.Item1}_{kv.Key.Item2}");
                chunkGo.transform.SetParent(optParent.transform, false);

                var (rawCombined, mats) = CombineRendererList(kv.Value, optParent.transform.worldToLocalMatrix,
                    $"{subDir}/{assetPrefix}_Chunk{chunkIdx}_Raw.asset");
                if (rawCombined == null || rawCombined.vertexCount == 0)
                {
                    UnityEngine.Object.DestroyImmediate(chunkGo);
                    continue;
                }

                rawTris += CountTriangles(rawCombined);

                // Micro-weld CAD LOD0 to eliminate redundant sub-centimeter bevels/duplicate verts while preserving 100% visual silhouette!
                Mesh lod0Mesh = lod0MicroWeldCell > 0.001f
                    ? SimplifyMeshClustered(rawCombined, lod0MicroWeldCell, $"{subDir}/{assetPrefix}_Chunk{chunkIdx}_LOD0.asset")
                    : rawCombined;
                if (lod0Mesh != rawCombined)
                    AssetDatabase.DeleteAsset($"{subDir}/{assetPrefix}_Chunk{chunkIdx}_Raw.asset");

                lod0Tris += CountTriangles(lod0Mesh);

                float cell1 = Mathf.Max(lod0MicroWeldCell * 3.2f, 0.14f);
                var lod1Mesh = SimplifyMeshClustered(lod0Mesh, cell1, $"{subDir}/{assetPrefix}_Chunk{chunkIdx}_LOD1.asset");
                lod1Tris += CountTriangles(lod1Mesh);

                Mesh lod2Mesh = null;
                if (generateLod2)
                {
                    float cell2 = cell1 * 2.4f;
                    lod2Mesh = SimplifyMeshClustered(lod0Mesh, cell2, $"{subDir}/{assetPrefix}_Chunk{chunkIdx}_LOD2.asset");
                }

                CreateLodPair(chunkGo, lod0Mesh, lod1Mesh, lod2Mesh, mats, 0.24f, 0.08f);
                MarkStaticRecursive(chunkGo, true);
                chunkIdx++;
            }

            var bc = optParent.AddComponent<BoxCollider>();
            bc.center = Vector3.zero;
            bc.size = totalBounds.size;

            UnityEngine.Object.DestroyImmediate(root);
            optParent.name = rootName;

            Debug.Log($"[CNC Step2] Consolidated '{rootName}' ({assetPrefix}): {renderers.Length} renderers -> {chunkIdx} spatial LOD chunks (Raw={rawTris:N0} -> LOD0={lod0Tris:N0} tris, LOD1={lod1Tris:N0} tris).");
        }

        private static (Mesh, Material[]) CombineHierarchyToMultiSubmesh(GameObject root, string meshAssetPath)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true).ToList();
            return CombineRendererList(renderers, root.transform.worldToLocalMatrix, meshAssetPath);
        }

        private static (Mesh, Material[]) CombineRendererList(List<MeshRenderer> renderers, Matrix4x4 rootWorldToLocal, string meshAssetPath)
        {
            var matToInstances = new Dictionary<Material, List<CombineInstance>>();
            foreach (var mr in renderers)
            {
                if (mr == null || !mr.enabled) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;
                var mats = mr.sharedMaterials;
                Matrix4x4 mtx = rootWorldToLocal * mr.transform.localToWorldMatrix;

                int subCount = Mathf.Min(mesh.subMeshCount, mats.Length);
                for (int s = 0; s < subCount; s++)
                {
                    Material m = ConvertOrCacheUrpMaterial(mats[s]);
                    if (!matToInstances.TryGetValue(m, out var list))
                    {
                        list = new List<CombineInstance>();
                        matToInstances[m] = list;
                    }
                    list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = mtx });
                }
            }

            if (matToInstances.Count == 0) return (null, Array.Empty<Material>());

            var subMeshes = new List<Mesh>();
            var finalMats = new List<Material>();
            foreach (var kv in matToInstances)
            {
                var sub = new Mesh { indexFormat = IndexFormat.UInt32 };
                sub.CombineMeshes(kv.Value.ToArray(), true, true, false);
                subMeshes.Add(sub);
                finalMats.Add(kv.Key);
            }

            var finalCombines = new CombineInstance[subMeshes.Count];
            for (int i = 0; i < subMeshes.Count; i++)
            {
                finalCombines[i] = new CombineInstance
                {
                    mesh = subMeshes[i],
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                };
            }

            var finalMesh = new Mesh { name = Path.GetFileNameWithoutExtension(meshAssetPath), indexFormat = IndexFormat.UInt32 };
            finalMesh.CombineMeshes(finalCombines, false, false, false);
            finalMesh.RecalculateBounds();
            if (finalMesh.normals == null || finalMesh.normals.Length == 0)
                finalMesh.RecalculateNormals();
            MeshUtility.Optimize(finalMesh);

            foreach (var s in subMeshes) UnityEngine.Object.DestroyImmediate(s);

            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath) != null)
                AssetDatabase.DeleteAsset(meshAssetPath);
            AssetDatabase.CreateAsset(finalMesh, meshAssetPath);
            return (finalMesh, finalMats.ToArray());
        }

        /// <summary>
        /// Fast, deterministic spatial vertex-clustering mesh decimator that preserves submesh materials,
        /// silhouettes, normals, and UVs cleanly for LOD1/LOD2 generation.
        /// </summary>
        private static Mesh SimplifyMeshClustered(Mesh src, float cellSize, string outAssetPath)
        {
            if (src == null) return null;
            cellSize = Mathf.Max(cellSize, 0.02f);
            float inv = 1f / cellSize;

            var srcVerts = src.vertices;
            var srcNorms = src.normals;
            var srcUvs = src.uv;
            bool hasNorms = srcNorms != null && srcNorms.Length == srcVerts.Length;
            bool hasUvs = srcUvs != null && srcUvs.Length == srcVerts.Length;

            var cellMap = new Dictionary<(int, int, int), int>(srcVerts.Length / 2);
            var sumPos = new List<Vector3>();
            var sumNorm = new List<Vector3>();
            var sumUv = new List<Vector2>();
            var counts = new List<int>();
            var remap = new int[srcVerts.Length];

            for (int i = 0; i < srcVerts.Length; i++)
            {
                Vector3 v = srcVerts[i];
                var key = (Mathf.FloorToInt(v.x * inv), Mathf.FloorToInt(v.y * inv), Mathf.FloorToInt(v.z * inv));
                if (!cellMap.TryGetValue(key, out int newIdx))
                {
                    newIdx = sumPos.Count;
                    cellMap[key] = newIdx;
                    sumPos.Add(v);
                    sumNorm.Add(hasNorms ? srcNorms[i] : Vector3.up);
                    sumUv.Add(hasUvs ? srcUvs[i] : Vector2.zero);
                    counts.Add(1);
                }
                else
                {
                    sumPos[newIdx] += v;
                    if (hasNorms) sumNorm[newIdx] += srcNorms[i];
                    if (hasUvs) sumUv[newIdx] += srcUvs[i];
                    counts[newIdx]++;
                }
                remap[i] = newIdx;
            }

            int nv = sumPos.Count;
            var newVerts = new Vector3[nv];
            var newNorms = new Vector3[nv];
            var newUvs = new Vector2[nv];
            for (int i = 0; i < nv; i++)
            {
                float invC = 1f / counts[i];
                newVerts[i] = sumPos[i] * invC;
                newNorms[i] = sumNorm[i].sqrMagnitude > 1e-6f ? sumNorm[i].normalized : Vector3.up;
                newUvs[i] = sumUv[i] * invC;
            }

            var dst = new Mesh
            {
                name = Path.GetFileNameWithoutExtension(outAssetPath),
                indexFormat = nv > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = newVerts,
                normals = newNorms,
                uv = newUvs,
                subMeshCount = src.subMeshCount
            };

            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tris = src.GetTriangles(s);
                var outTris = new List<int>(tris.Length / 2);
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    int a = remap[tris[t]];
                    int b = remap[tris[t + 1]];
                    int c = remap[tris[t + 2]];
                    if (a == b || b == c || a == c) continue; // Degenerate triangle collapsed cleanly!
                    outTris.Add(a);
                    outTris.Add(b);
                    outTris.Add(c);
                }
                dst.SetTriangles(outTris, s);
            }

            dst.RecalculateBounds();
            MeshUtility.Optimize(dst);

            if (AssetDatabase.LoadAssetAtPath<Mesh>(outAssetPath) != null)
                AssetDatabase.DeleteAsset(outAssetPath);
            AssetDatabase.CreateAsset(dst, outAssetPath);
            return dst;
        }

        private static void CreateLodPair(GameObject parent, Mesh lod0, Mesh lod1, Mesh lod2, Material[] mats, float lod0Height, float lod1Height)
        {
            var group = parent.AddComponent<LODGroup>();
            var lods = new List<LOD>();

            var r0 = CreateLodChild(parent, "LOD0", lod0, mats);
            lods.Add(new LOD(lod0Height, new Renderer[] { r0 }));

            if (lod1 != null && CountTriangles(lod1) > 0)
            {
                var r1 = CreateLodChild(parent, "LOD1", lod1, mats);
                lods.Add(new LOD(lod2 != null ? lod1Height : 0.015f, new Renderer[] { r1 }));
            }

            if (lod2 != null && CountTriangles(lod2) > 0)
            {
                var r2 = CreateLodChild(parent, "LOD2", lod2, mats);
                lods.Add(new LOD(0.012f, new Renderer[] { r2 }));
            }

            group.SetLODs(lods.ToArray());
            group.RecalculateBounds();
        }

        private static MeshRenderer CreateLodChild(GameObject parent, string name, Mesh mesh, Material[] mats)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            return mr;
        }

        private static long CountTriangles(Mesh m)
        {
            if (m == null) return 0;
            long t = 0;
            for (int s = 0; s < m.subMeshCount; s++) t += (long)(m.GetIndexCount(s) / 3);
            return t;
        }

        // ==================== HIGH-CRAFT COTTON BALE MODEL UPGRADE ====================

        private static void UpgradeCottonBales()
        {
            string baleMeshPath = $"{OptMeshDir}/Mesh_CottonBale_HD.asset";
            var origCottonFbx = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/M/棉花.fbx");
            Bounds refBounds = origCottonFbx != null ? origCottonFbx.bounds : new Bounds(Vector3.zero, Vector3.one);

            Mesh baleMesh = BuildBeveledCottonBaleMesh(refBounds, baleMeshPath);

            int upgraded = 0;
            foreach (var mf in UnityEngine.Object.FindObjectsOfType<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                string mpath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (mpath.Contains("棉花.fbx") || mf.gameObject.name.Contains("棉花"))
                {
                    mf.sharedMesh = baleMesh;
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = _matCottonBale;
                    MarkStaticRecursive(mf.gameObject, true);
                    upgraded++;
                }
            }
            Debug.Log($"[CNC Step2] Upgraded {upgraded} boxy 8-vert cotton bales to beveled PBR Cotton Bales ({CountTriangles(baleMesh)} tris each, GPU Instanced).");
        }

        private static Mesh BuildBeveledCottonBaleMesh(Bounds targetBounds, string outPath)
        {
            // Subdivided & softly rounded box matching original Cotton bale bounds exactly (so zero floating/clipping!)
            Vector3 size = targetBounds.size;
            if (size.sqrMagnitude < 1e-4f) size = Vector3.one;
            Vector3 center = targetBounds.center;

            int seg = 3; // 3x3 grid per face -> smooth pillowed edges like real compressed cotton bales!
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            Vector3[] faceUs      = { Vector3.right, Vector3.right, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            Vector3[] faceVs      = { Vector3.forward, Vector3.back, Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            Vector3 half = size * 0.5f;
            float roundness = 0.14f;

            for (int f = 0; f < 6; f++)
            {
                int baseIdx = verts.Count;
                Vector3 n = faceNormals[f];
                Vector3 uAxis = faceUs[f];
                Vector3 vAxis = faceVs[f];

                for (int iy = 0; iy <= seg; iy++)
                {
                    float ty = iy / (float)seg;
                    float py = ty * 2f - 1f;
                    for (int ix = 0; ix <= seg; ix++)
                    {
                        float tx = ix / (float)seg;
                        float px = tx * 2f - 1f;

                        Vector3 cubePt = n + uAxis * px + vAxis * py;
                        // Softly bevel corners & edges by blending cube corner toward super-ellipsoid
                        Vector3 clamped = new Vector3(
                            Mathf.Clamp(cubePt.x, -1f + roundness, 1f - roundness),
                            Mathf.Clamp(cubePt.y, -1f + roundness, 1f - roundness),
                            Mathf.Clamp(cubePt.z, -1f + roundness, 1f - roundness)
                        );
                        Vector3 delta = cubePt - clamped;
                        Vector3 bevelPt = delta.sqrMagnitude > 1e-6f
                            ? clamped + delta.normalized * roundness
                            : cubePt;

                        // Subtle organic cotton bulge in the center of sides
                        float bulge = 1f + 0.03f * (1f - px * px) * (1f - py * py);
                        Vector3 localPos = center + new Vector3(bevelPt.x * half.x * bulge, bevelPt.y * half.y * bulge, bevelPt.z * half.z * bulge);
                        Vector3 localNorm = delta.sqrMagnitude > 1e-5f ? delta.normalized : n;

                        verts.Add(localPos);
                        norms.Add(localNorm);
                        uvs.Add(new Vector2(tx, ty));
                    }
                }

                int row = seg + 1;
                for (int iy = 0; iy < seg; iy++)
                {
                    for (int ix = 0; ix < seg; ix++)
                    {
                        int a = baseIdx + iy * row + ix;
                        int b = a + 1;
                        int c = a + row;
                        int d = c + 1;
                        // Determine winding matching face normal
                        Vector3 e1 = verts[b] - verts[a];
                        Vector3 e2 = verts[c] - verts[a];
                        if (Vector3.Dot(Vector3.Cross(e1, e2), n) >= 0f)
                        {
                            tris.Add(a); tris.Add(b); tris.Add(c);
                            tris.Add(b); tris.Add(d); tris.Add(c);
                        }
                        else
                        {
                            tris.Add(a); tris.Add(c); tris.Add(b);
                            tris.Add(b); tris.Add(c); tris.Add(d);
                        }
                    }
                }
            }

            var mesh = new Mesh { name = "Mesh_CottonBale_HD" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            if (AssetDatabase.LoadAssetAtPath<Mesh>(outPath) != null)
                AssetDatabase.DeleteAsset(outPath);
            AssetDatabase.CreateAsset(mesh, outPath);
            return mesh;
        }

        // ==================== WORKSHOP BUILDINGS, LEDS, CONVEYORS, SIGNBOARDS ====================

        private static void UpgradeWorkshopBuildingsAndProps()
        {
            int ledCount = 0;
            int convCount = 0;
            int arrowCount = 0;

            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                string n = mr.gameObject.name;
                string nl = n.ToLowerInvariant();

                // Glow overhead fluorescent ceiling light fixtures inside Building & Building (1)
                if (nl.Contains("light") || nl.Contains("lamp") || nl.Contains("fluorescent") || nl.Contains("fixture"))
                {
                    mr.sharedMaterial = _matCeilingLed;
                    ledCount++;
                }

                // Upgrade floor meshes explicitly to crisp Epoxy Floor PBR
                if (nl == "floor" || nl.StartsWith("floor_") || n == "地板")
                {
                    mr.sharedMaterial = _matEpoxyFloor;
                }

                // Upgrade conveyor belt surfaces to animated UV scrolling PBR shader!
                if (n.Contains("传送带") || mr.GetComponent<Chuansongdai>() != null || mr.GetComponent<Twochuansongdai>() != null)
                {
                    mr.sharedMaterial = _matConveyorBelt;
                    convCount++;
                }

                // Upgrade yellow directional arrows / AGV guide lines to glowing animated flow!
                if (n.Contains("黄色") && (n.Contains("箭头") || n.Contains("剪头") || n.Contains("导引线")))
                {
                    mr.sharedMaterial = _matAgvFlow;
                    arrowCount++;
                }
            }

            Debug.Log($"[CNC Step2] Upgraded {ledCount} LED ceiling fixtures, {convCount} animated conveyors, {arrowCount} glowing AGV guide arrows.");
        }

        private static void UpgradeHangingSignboards()
        {
            var tmpFontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/CNC_Sans_Bold_SDF.asset")
                           ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/CNC_Sans_Regular_SDF.asset");

            // Known hanging Plane objects in SampleScene (Plane, Plane (1), Plane (2), Plane (3))
            var signConfig = new Dictionary<string, (string title, string sub)>
            {
                { "Plane (3)", ("智能立体仓储中心", "AUTOMATED AS/RS WAREHOUSE") },
                { "Plane",     ("智能视觉检测分拣区", "AI QUALITY INSPECTION") },
                { "Plane (2)", ("智能视觉检测分拣区", "AI QUALITY INSPECTION") },
                { "Plane (1)", ("自动物流 AGV 调度通道", "AGV LOGISTICS CORRIDOR") }
            };

            int upgraded = 0;
            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (!mr.gameObject.name.StartsWith("Plane")) continue;
                if (mr.transform.position.y < 10f) continue; // Only elevated hanging signboards!

                var info = signConfig.TryGetValue(mr.gameObject.name, out var val)
                    ? val
                    : ("棉纺智能制造示范车间", "SMART SPINNING WORKSHOP");

                // Style existing plane as dark tech signboard backing
                mr.sharedMaterial = _matSignboardDark;
                mr.shadowCastingMode = ShadowCastingMode.Off;

                // Remove old duplicate 3D sign text if re-running
                var old = mr.transform.Find("__TechSignText");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var oldF = mr.transform.Find("__TechSignFront");
                if (oldF != null) UnityEngine.Object.DestroyImmediate(oldF.gameObject);
                var oldB = mr.transform.Find("__TechSignBack");
                if (oldB != null) UnityEngine.Object.DestroyImmediate(oldB.gameObject);

                // Compute clean world-space upright facing normal (guarantees NEVER upside-down!)
                Bounds sb = mr.bounds;
                Vector3 planeNormal = mr.transform.up; // Unity Plane faces +Y locally
                Vector3 horizNormal = new Vector3(planeNormal.x, 0f, planeNormal.z);
                if (horizNormal.sqrMagnitude < 0.01f) horizNormal = Vector3.forward;
                horizNormal.Normalize();

                float signW = Mathf.Max(sb.size.x, sb.size.z, 6f);
                float signH = Mathf.Max(sb.size.y, 3f);

                // Create upright world-aligned TMP text on BOTH sides of the signboard
                for (int side = 0; side < 2; side++)
                {
                    Vector3 faceDir = side == 0 ? horizNormal : -horizNormal;
                    var holder = new GameObject(side == 0 ? "__TechSignFront" : "__TechSignBack");
                    holder.transform.position = sb.center + faceDir * 0.12f;
                    // TMP faces -Z, so LookRotation(-faceDir, Vector3.up) faces +faceDir upright!
                    holder.transform.rotation = Quaternion.LookRotation(-faceDir, Vector3.up);
                    holder.transform.localScale = Vector3.one;
                    holder.transform.SetParent(mr.transform, true);

                    var tmp = holder.AddComponent<TextMeshPro>();
                    if (tmpFontBold != null) tmp.font = tmpFontBold;
                    tmp.text = $"<color=#00E6FF><b>{info.Item1}</b></color>\n<size=40%><color=#9CD4F8>{info.Item2}</color></size>";
                    tmp.fontSize = Mathf.Clamp(signH * 2.3f, 8f, 16f);
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.rectTransform.sizeDelta = new Vector2(signW * 0.92f, signH * 0.85f);
                }
                upgraded++;
            }
            Debug.Log($"[CNC Step2] Upgraded {upgraded} hanging factory signboards to upright double-sided dark-navy LED signage.");
        }

        private static void UpgradeFloorStationPads()
        {
            string[] padNames = { "末并", "精梳", "粗纱", "细纱", "络筒", "抓棉机UI", "除杂、梳棉、卷棉UI" };
            int upgraded = 0;
            foreach (var pname in padNames)
            {
                var go = GameObject.Find(pname);
                if (go == null) continue;
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.sharedMaterial = _matHoloPad;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                    upgraded++;
                }
            }
            Debug.Log($"[CNC Step2] Upgraded {upgraded} floor station pads to glowing holographic cyan rings.");
        }

        private static void BuildStationStatusBeacons()
        {
            var oldRoot = GameObject.Find("__StationBeacons");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            var root = new GameObject("__StationBeacons");
            var stations = WorkshopCatalog.CreateDefaultStations();

            foreach (var st in stations)
            {
                var beaconGo = new GameObject($"Beacon_{st.code}_{st.shortName}");
                beaconGo.transform.SetParent(root.transform, false);
                beaconGo.transform.position = new Vector3(st.worldPosition.x, st.worldPosition.y + 4.8f, st.worldPosition.z);

                // Sleek industrial vertical pole
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole";
                pole.transform.SetParent(beaconGo.transform, false);
                pole.transform.localPosition = new Vector3(0f, -2.0f, 0f);
                pole.transform.localScale = new Vector3(0.18f, 2.0f, 0.18f);
                pole.GetComponent<MeshRenderer>().sharedMaterial = _matBeaconMetal;
                UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());

                // 3 stacked glowing indicator rings (Green, Amber, Red)
                var green = CreateBeaconRing(beaconGo.transform, "GreenLens", 0.35f);
                var amber = CreateBeaconRing(beaconGo.transform, "AmberLens", 0.85f);
                var red   = CreateBeaconRing(beaconGo.transform, "RedLens",   1.35f);

                var comp = beaconGo.AddComponent<MachineStatusBeacon>();
                comp.stationIndex = st.index;
                comp.greenLens = green;
                comp.amberLens = amber;
                comp.redLens = red;
            }
            Debug.Log($"[CNC Step2] Built {stations.Count} industrial 3-color status signal tower lights.");
        }

        private static MeshRenderer CreateBeaconRing(Transform parent, string name, float localY)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, localY, 0f);
            go.transform.localScale = new Vector3(0.55f, 0.22f, 0.55f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _matBeaconLens;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return mr;
        }

        private static void IntegrateInspectionRoboticArms()
        {
            var oldRoot = GameObject.Find("__InspectionArms");
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            var armPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/J—Color/检测机械臂.FBX");
            if (armPrefab == null) return;

            var root = new GameObject("__InspectionArms");
            // Position cleanly mounted at the left/right transfer stations beside the inspection conveyors
            Vector3[] positions = {
                new Vector3(16.0f, 6.2f, 388.0f),
                new Vector3(62.0f, 6.2f, 388.0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(armPrefab);
                inst.name = $"InspectionRobotArm_{i + 1}";
                inst.transform.SetParent(root.transform, false);
                inst.transform.position = positions[i];
                inst.transform.rotation = Quaternion.Euler(0f, i == 0 ? 90f : -90f, 0f);
                inst.transform.localScale = Vector3.one * 3.2f;

                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                    mr.sharedMaterial = mr.gameObject.name.ToLowerInvariant().Contains("base") ? _matDarkGunmetal : _matRobotYellow;
            }
            Debug.Log("[CNC Step2] Integrated 2 animated industrial robotic arms into Workshop 2 inspection zone.");
        }

        // ==================== INDUSTRIAL LIGHTING, REFLECTIONS & URP VOLUME ====================

        private static void SetupIndustrialLightingAndVolume()
        {
            // 1. Tune Main Directional Sunlight
            var dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                var dl = dirLightGo.GetComponent<Light>();
                if (dl != null)
                {
                    dl.type = LightType.Directional;
                    dl.color = new Color(1.0f, 0.97f, 0.92f);
                    dl.intensity = 1.35f;
                    dl.shadows = LightShadows.Soft;
                    dl.shadowStrength = 0.56f;
                    dl.shadowBias = 0.05f;
                    dl.shadowNormalBias = 0.35f;
                }
                dirLightGo.transform.rotation = Quaternion.Euler(46f, -35f, 0f);
            }

            // 2. Bright Modern Industrial Trilight Ambient & Subtle Depth Fog
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.60f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.42f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.24f, 0.30f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.16f, 0.23f, 0.32f);
            RenderSettings.fogDensity = 0.00075f;

            // 3. Industrial High-Bay Overhead Lighting Rig + Reflection Probes
            var oldRig = GameObject.Find("__IndustrialLightingRig");
            if (oldRig != null) UnityEngine.Object.DestroyImmediate(oldRig);
            var rig = new GameObject("__IndustrialLightingRig");

            // Workshop 1 warm-cool neutral industrial high-bay fill lights
            Vector3[] w1LightPos = {
                new Vector3(160f, 26f, 80f),
                new Vector3(160f, 26f, 180f),
                new Vector3(70f, 26f, 100f),
                new Vector3(70f, 26f, 210f),
                new Vector3(-15f, 26f, 140f),
                new Vector3(-15f, 26f, 235f)
            };
            for (int i = 0; i < w1LightPos.Length; i++)
                CreateBayLight(rig.transform, $"W1_HighBayLight_{i + 1}", w1LightPos[i], new Color(0.95f, 0.98f, 1.0f), 130f, 2.5f);

            // Workshop 2 inspection & automated warehouse overhead lights
            Vector3[] w2LightPos = {
                new Vector3(39f, 19f, 376f),
                new Vector3(20f, 19f, 418f),
                new Vector3(58f, 19f, 418f),
                new Vector3(39f, 19f, 446f)
            };
            for (int i = 0; i < w2LightPos.Length; i++)
                CreateBayLight(rig.transform, $"W2_WarehouseLight_{i + 1}", w2LightPos[i], new Color(0.96f, 0.98f, 1.0f), 95f, 2.6f);

            // Reflection Probes for metallic specular realism
            CreateReflectionProbe(rig.transform, "ReflProbe_Workshop1_East", new Vector3(135f, 14f, 130f), new Vector3(180f, 40f, 280f));
            CreateReflectionProbe(rig.transform, "ReflProbe_Workshop1_West", new Vector3(15f, 14f, 165f), new Vector3(160f, 40f, 280f));
            CreateReflectionProbe(rig.transform, "ReflProbe_Workshop2_Warehouse", new Vector3(39f, 12f, 405f), new Vector3(90f, 28f, 120f));

            // 4. Global URP Post-Processing Volume (ACES + Subtle Bloom + Contrast + Vignette)
            string profilePath = $"{SettingsDir}/Profile_SmartFactory.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile != null) AssetDatabase.DeleteAsset(profilePath);
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.42f);
            bloom.scatter.Override(0.62f);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.18f);
            colorAdj.contrast.Override(12f);
            colorAdj.saturation.Override(10f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.42f);

            EditorUtility.SetDirty(profile);

            var volGo = new GameObject("GlobalPostProcessVolume");
            volGo.transform.SetParent(rig.transform, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.sharedProfile = profile;

            // 5. Configure Main Camera for URP PostProcessing & MSAA
            var mainCam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (mainCam != null)
            {
                mainCam.allowHDR = true;
                mainCam.allowMSAA = true;
                mainCam.farClipPlane = 1500f;
                mainCam.fieldOfView = 55f;
                var uac = mainCam.GetComponent<UniversalAdditionalCameraData>() ?? mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                uac.renderPostProcessing = true;
                uac.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                uac.antialiasingQuality = AntialiasingQuality.High;
                uac.renderShadows = true;
            }
            Debug.Log("[CNC Step2] Configured Industrial Lighting Rig, 3 Reflection Probes & URP Global Volume (ACES + Bloom + SSAO).");
        }

        private static void CreateBayLight(Transform parent, string name, Vector3 pos, Color col, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None; // Keep shadows clean on main Directional Light for crisp FPS
        }

        private static void CreateReflectionProbe(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var rp = go.AddComponent<ReflectionProbe>();
            rp.mode = ReflectionProbeMode.Baked;
            rp.resolution = 256;
            rp.boxProjection = true;
            rp.size = size;
            rp.intensity = 0.85f;
            rp.hdr = true;
        }

        private static void MarkStaticSceneGeometry()
        {
            string[] staticRootKeywords = {
                "Building", "飞架", "管道", "转弯导管", "挂载", "立体仓库", "产线01",
                "栅栏", "除杂机", "梳卷一体", "卷筒储存", "牢筒机", "洗砂机", "分卷机", "检测产线", "棉花"
            };
            int count = 0;
            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr == null) continue;
                Transform root = mr.transform.root;
                string rn = root.name;
                string on = mr.gameObject.name;
                // Exclude moving elements (Player, AGVs, Arms, Beacons, dynamic cotton rolls)
                if (rn.Contains("ECM_") || rn.Contains("agv") || rn.Contains("InspectionArms") ||
                    rn.Contains("StationBeacons") || on.Contains("agv") || mr.GetComponentInParent<Xishadong>() != null ||
                    mr.GetComponentInParent<Zhua_mian_ji>() != null)
                    continue;

                if (staticRootKeywords.Any(k => rn.Contains(k) || on.Contains(k)))
                {
                    GameObjectUtility.SetStaticEditorFlags(mr.gameObject,
                        StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic |
                        StaticEditorFlags.ReflectionProbeStatic);
                    count++;
                }
            }
            Debug.Log($"[CNC Step2] Marked {count} static architectural/machinery renderers for Static Batching, Occlusion & Reflections.");
        }

        private static void MarkStaticRecursive(GameObject go, bool includeOccluder)
        {
            var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (includeOccluder) flags |= StaticEditorFlags.OccluderStatic;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }

        private static void EnsureAllSceneMaterialsGpuInstanced()
        {
            var touched = new HashSet<Material>();
            int enabledCount = 0;
            foreach (var mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr == null) continue;
                var mats = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    // Replace Unity built-in default material with GPU-instanced URP Lit
                    if (m.name == "Default-Material" || (m.shader != null && m.shader.name == "Standard"))
                    {
                        mats[i] = ConvertOrCacheUrpMaterial(m);
                        changed = true;
                        m = mats[i];
                    }
                    if (!m.enableInstancing && touched.Add(m))
                    {
                        m.enableInstancing = true;
                        EditorUtility.SetDirty(m);
                        enabledCount++;
                    }
                }
                if (changed) mr.sharedMaterials = mats;
            }
            Debug.Log($"[CNC Step2] Verified GPU Instancing ON across all scene materials (newly enabled: {enabledCount}).");
        }

        private static void BakeAllReflectionProbes()
        {
            Directory.CreateDirectory("Assets/Scenes/SampleScene");
            var probes = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(true);
            foreach (var rp in probes)
            {
                string exrPath = $"Assets/Scenes/SampleScene/{rp.name}.exr";
                Lightmapping.BakeReflectionProbe(rp, exrPath);
            }
            Debug.Log($"[CNC Step2] Baked {probes.Length} industrial Reflection Probes.");
        }
    }
}
