using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

namespace CNC.EditorTools
{
    public static class Step1_URPAndAssetSetup
    {
        private const string SettingsDir = "Assets/_Project/Settings";
        private const string FontsDir = "Assets/_Project/UI/Fonts";
        private const string SpritesDir = "Assets/_Project/UI/Sprites";
        private const string PbrTexDir = "Assets/_Project/Textures/PBR";

        [MenuItem("CNC Optimize/Step 1 - Setup URP, Fonts & Import Settings")]
        public static void Run()
        {
            Debug.Log("[CNC Step1] Starting URP, Fonts & Import Settings setup...");
            Directory.CreateDirectory(SettingsDir);

            ConfigureGeneratedSpritesAndTextures();
            ConfigureLegacyWarehouseTextures();
            ConfigureModelImportSettings();
            CreateChineseTmpFonts();
            SetupUrpPipelineAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CNC Step1] Completed successfully!");
        }

        public static void RunUrpAndFontsOnly()
        {
            Debug.Log("[CNC Step1] Running URP & Chinese TMP Fonts setup...");
            Directory.CreateDirectory(SettingsDir);
            CreateChineseTmpFonts();
            SetupUrpPipelineAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CNC Step1] URP & Fonts setup completed successfully!");
        }

        private static void ConfigureGeneratedSpritesAndTextures()
        {
            // Configure 9-sliced UI sprites
            if (Directory.Exists(SpritesDir))
            {
                var files = Directory.GetFiles(SpritesDir, "*.png");
                foreach (var f in files)
                {
                    string assetPath = f.Replace("\\", "/");
                    var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti == null) continue;

                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.textureCompression = TextureImporterCompression.Uncompressed; // Crisp UI edges on Desktop
                    ti.filterMode = FilterMode.Bilinear;
                    ti.wrapMode = TextureWrapMode.Clamp;

                    string name = Path.GetFileNameWithoutExtension(assetPath);
                    if (name == "ui_panel_glass") ti.spriteBorder = new Vector4(36, 36, 36, 36);
                    else if (name == "ui_card_dark") ti.spriteBorder = new Vector4(24, 24, 24, 24);
                    else if (name.StartsWith("ui_btn_")) ti.spriteBorder = new Vector4(20, 16, 20, 16);
                    else if (name == "ui_world_tag") ti.spriteBorder = new Vector4(24, 16, 24, 16);
                    else if (name == "ui_pill_bar") ti.spriteBorder = new Vector4(12, 8, 12, 8);

                    ti.SaveAndReimport();
                }
            }

            // Configure PBR textures (Normal maps vs Color maps)
            if (Directory.Exists(PbrTexDir))
            {
                var files = Directory.GetFiles(PbrTexDir, "*.png");
                foreach (var f in files)
                {
                    string assetPath = f.Replace("\\", "/");
                    var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti == null) continue;

                    string name = Path.GetFileNameWithoutExtension(assetPath);
                    if (name.EndsWith("_Normal"))
                    {
                        ti.textureType = TextureImporterType.NormalMap;
                    }
                    else
                    {
                        ti.textureType = TextureImporterType.Default;
                        ti.alphaIsTransparency = name.Contains("Mask");
                    }

                    ti.mipmapEnabled = true;
                    ti.wrapMode = TextureWrapMode.Repeat;
                    ti.anisoLevel = 8;
                    ti.maxTextureSize = 1024;
                    ti.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.SaveAndReimport();
                }
            }

            // Ensure Fanyi equipment photos are imported as crisp Sprites (max 1024)
            string[] uiPhotoDirs = { "Assets/Fanyi", "Assets/Scenes/Play" };
            foreach (var dir in uiPhotoDirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir, "*.*").Where(p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                {
                    string assetPath = f.Replace("\\", "/");
                    var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti == null) continue;
                    ti.textureType = TextureImporterType.Sprite;
                    ti.mipmapEnabled = false;
                    ti.maxTextureSize = dir.Contains("Play") ? 2048 : 1024;
                    ti.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.SaveAndReimport();
                }
            }
        }

        private static void ConfigureLegacyWarehouseTextures()
        {
            string texDir = "Assets/Other/UnityWarehouseSceneHDRP/Scene_Warehouse/Background/Textures";
            if (!Directory.Exists(texDir)) return;

            foreach (var f in Directory.GetFiles(texDir, "*.tga"))
            {
                string assetPath = f.Replace("\\", "/");
                var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti == null) continue;

                string name = Path.GetFileNameWithoutExtension(assetPath);
                bool isNormal = name.EndsWith("_N") || name.EndsWith("_Normal");
                ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.maxTextureSize = name.StartsWith("Floor") ? 2048 : 1024;
                ti.mipmapEnabled = true;
                ti.anisoLevel = 8;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.crunchedCompression = true;
                ti.compressionQuality = 75;
                ti.SaveAndReimport();
            }
        }

        private static void ConfigureModelImportSettings()
        {
            string[] modelDirs = { "Assets/M", "Assets/J—Color", "Assets/Other/UnityWarehouseSceneHDRP/Scene_Warehouse/Background/Models" };
            int count = 0;
            foreach (var dir in modelDirs)
            {
                if (!Directory.Exists(dir)) continue;
                var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".obj", StringComparison.OrdinalIgnoreCase));

                foreach (var f in files)
                {
                    string assetPath = f.Replace("\\", "/");
                    var mi = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    if (mi == null) continue;

                    mi.importCameras = false;
                    mi.importLights = false;
                    mi.importAnimation = false;
                    mi.animationType = ModelImporterAnimationType.None;
                    mi.meshCompression = ModelImporterMeshCompression.Medium;
                    mi.optimizeMeshPolygons = true;
                    mi.optimizeMeshVertices = true;
                    mi.isReadable = true; // Needed temporarily for Step2 mesh consolidation; disabled on final assets

                    // Fix zero-normal OBJ model!
                    if (assetPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        mi.importNormals = ModelImporterNormals.Calculate;
                        mi.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
                        mi.normalSmoothingAngle = 45f;
                    }

                    // Generate lightmap UV2 only on reasonably sized building/static models (skip 6669-mesh raw warehouse before Step2 merges it)
                    bool isRawMassive = assetPath.Contains("立体仓库") || assetPath.Contains("产线01") || assetPath.Contains("栅栏");
                    mi.generateSecondaryUV = !isRawMassive && !assetPath.Contains("Collider");

                    mi.SaveAndReimport();
                    count++;
                }
            }
            Debug.Log($"[CNC Step1] Optimized import settings for {count} models.");
        }

        public static void RebuildCrispChineseFonts()
        {
            Debug.Log("[CNC Step1] Rebuilding ultra-crisp 72pt Noto Sans SC SDF fonts...");
            AssetDatabase.Refresh();
            CreateChineseTmpFonts();
            AssetDatabase.SaveAssets();
        }

        private static void CreateChineseTmpFonts()
        {
            string regSrc = File.Exists($"{FontsDir}/CNC_Sans_Regular.otf") ? $"{FontsDir}/CNC_Sans_Regular.otf" : $"{FontsDir}/CNC_Sans_Regular.ttf";
            string boldSrc = File.Exists($"{FontsDir}/CNC_Sans_Bold.otf") ? $"{FontsDir}/CNC_Sans_Bold.otf" : $"{FontsDir}/CNC_Sans_Bold.ttf";
            CreateSingleTmpFont(regSrc, $"{FontsDir}/CNC_Sans_Regular_SDF.asset", true);
            CreateSingleTmpFont(boldSrc, $"{FontsDir}/CNC_Sans_Bold_SDF.asset", false);
        }

        private static void CreateSingleTmpFont(string fontPath, string outAssetPath, bool setAsDefault)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogWarning($"[CNC Step1] Font not found at {fontPath}");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outAssetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(outAssetPath);
            }

            // Ultra-crisp 72pt Dynamic SDFAA_HINTED font asset (Noto Sans SC Medium/Bold — razor-sharp at ALL scales!)
            var tmpFont = TMP_FontAsset.CreateFontAsset(
                font,
                72,                          // High-res samplingPointSize for crystal clarity even at small Game View scale
                7,                           // Generous atlasPadding for clean dilate & subtle drop shadow
                GlyphRenderMode.SDFAA_HINTED,
                2048,                        // atlasWidth
                2048,                        // atlasHeight
                AtlasPopulationMode.Dynamic,
                true                         // enableMultiAtlasSupport
            );

            if (tmpFont == null)
            {
                Debug.LogError($"[CNC Step1] Failed to create TMP_FontAsset for {fontPath}");
                return;
            }

            tmpFont.name = Path.GetFileNameWithoutExtension(outAssetPath);
            AssetDatabase.CreateAsset(tmpFont, outAssetPath);

            // Persist atlas texture & material as sub-assets so they survive reload!
            if (tmpFont.atlasTexture != null)
            {
                tmpFont.atlasTexture.name = tmpFont.name + " Atlas";
                tmpFont.atlasTexture.filterMode = FilterMode.Bilinear;
                AssetDatabase.AddObjectToAsset(tmpFont.atlasTexture, tmpFont);
            }
            if (tmpFont.material != null)
            {
                tmpFont.material.name = tmpFont.name + " Material";
                // Subtle face dilation + soft dark drop shadow => razor-sharp legibility over bright glass & skylights!
                tmpFont.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
                tmpFont.material.EnableKeyword("UNDERLAY_ON");
                tmpFont.material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0.02f, 0.06f, 0.58f));
                tmpFont.material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                tmpFont.material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.55f);
                tmpFont.material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.08f);
                tmpFont.material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
                AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
            }

            EditorUtility.SetDirty(tmpFont);
            Debug.Log($"[CNC Step1] Created Ultra-Crisp 72pt Dynamic Chinese TMP FontAsset: {outAssetPath} from {fontPath}");

            // Hook as default / fallback in TMP Settings if available
            if (setAsDefault && TMP_Settings.instance != null)
            {
                var so = new SerializedObject(TMP_Settings.instance);
                var defProp = so.FindProperty("m_defaultFontAsset");
                if (defProp != null) defProp.objectReferenceValue = tmpFont;
                var fbProp = so.FindProperty("m_fallbackFontAssets");
                if (fbProp != null && fbProp.isArray)
                {
                    bool already = false;
                    for (int i = 0; i < fbProp.arraySize; i++)
                        if (fbProp.GetArrayElementAtIndex(i).objectReferenceValue == tmpFont) already = true;
                    if (!already)
                    {
                        fbProp.InsertArrayElementAtIndex(0);
                        fbProp.GetArrayElementAtIndex(0).objectReferenceValue = tmpFont;
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(TMP_Settings.instance);
            }
        }

        private static void SetupUrpPipelineAssets()
        {
            string rendererPath = $"{SettingsDir}/URP_Desktop_Renderer.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            // Populate default shaders & post-process data on the URP Renderer!
            var postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (postProcessData != null)
                rendererData.postProcessData = postProcessData;

            var rendererSo = new SerializedObject(rendererData);
            var defaultShadersProp = rendererSo.FindProperty("shaders");
            if (defaultShadersProp != null)
            {
                // Reload built-in URP shaders via ResourceReloader
                UnityEngine.Rendering.ResourceReloader.ReloadAllNullIn(rendererData,
                    "Packages/com.unity.render-pipelines.universal");
            }
            rendererData.renderingMode = RenderingMode.Forward;
            rendererData.depthPrimingMode = DepthPrimingMode.Auto;
            rendererData.shadowTransparentReceive = false;

            // Attach ScreenSpaceAmbientOcclusion (SSAO) feature for rich industrial contact shadows!
            bool hasSsao = rendererData.rendererFeatures.Any(f => f != null && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"));
            if (!hasSsao)
            {
                var ssaoType = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if (ssaoType != null)
                {
                    var ssao = ScriptableObject.CreateInstance(ssaoType) as ScriptableRendererFeature;
                    if (ssao != null)
                    {
                        ssao.name = "Industrial_SSAO";
                        UnityEngine.Rendering.ResourceReloader.ReloadAllNullIn(ssao,
                            "Packages/com.unity.render-pipelines.universal");
                        AssetDatabase.AddObjectToAsset(ssao, rendererData);
                        // Configure clean industrial AO parameters via SerializedObject
                        var sso = new SerializedObject(ssao);
                        var settingsProp = sso.FindProperty("m_Settings");
                        if (settingsProp != null)
                        {
                            SetFloat(settingsProp, "Intensity", 0.55f);
                            SetFloat(settingsProp, "Radius", 1.2f);
                            SetFloat(settingsProp, "DirectLightingStrength", 0.25f);
                            SetBool(settingsProp, "Downsample", false);
                            SetBool(settingsProp, "AfterOpaque", false);
                        }
                        sso.ApplyModifiedPropertiesWithoutUndo();

                        var rso = new SerializedObject(rendererData);
                        var featuresProp = rso.FindProperty("m_RendererFeatures");
                        var mapProp = rso.FindProperty("m_RendererFeatureMap");
                        if (featuresProp != null)
                        {
                            int idx = featuresProp.arraySize;
                            featuresProp.InsertArrayElementAtIndex(idx);
                            featuresProp.GetArrayElementAtIndex(idx).objectReferenceValue = ssao;
                            if (mapProp != null)
                            {
                                mapProp.InsertArrayElementAtIndex(idx);
                                mapProp.GetArrayElementAtIndex(idx).longValue = ssao.GetInstanceID();
                            }
                        }
                        rso.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[CNC Step1] Added Industrial SSAO RendererFeature.");
                    }
                }
            }
            EditorUtility.SetDirty(rendererData);

            // Create 3 Desktop Quality URP Assets (Medium / High / Ultra)
            var ultraAsset = CreateOrUpdateUrpAsset($"{SettingsDir}/URP_Desktop_Ultra.asset", rendererData, 4, 2048, 4, 260f, 1.0f);
            var highAsset  = CreateOrUpdateUrpAsset($"{SettingsDir}/URP_Desktop_High.asset",  rendererData, 4, 2048, 2, 200f, 1.0f);
            var medAsset   = CreateOrUpdateUrpAsset($"{SettingsDir}/URP_Desktop_Medium.asset", rendererData, 2, 1024, 2, 140f, 0.9f);

            // Set URP Ultra as project default RenderPipeline
            GraphicsSettings.defaultRenderPipeline = ultraAsset;
            GraphicsSettings.lightsUseLinearIntensity = true;

            // Bind URP assets to QualitySettings levels so runtime Settings UI can switch quality cleanly!
            int origQuality = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (i <= 2) QualitySettings.renderPipeline = medAsset;
                else if (i <= 4) QualitySettings.renderPipeline = highAsset;
                else QualitySettings.renderPipeline = ultraAsset;
            }
            QualitySettings.SetQualityLevel(names.Length - 1, true); // Ultra default for Desktop!
            Debug.Log($"[CNC Step1] Configured URP 14 Desktop pipeline (Default={ultraAsset.name}, Quality={QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        }

        private static UniversalRenderPipelineAsset CreateOrUpdateUrpAsset(
            string path, UniversalRendererData rendererData,
            int msaa, int shadowRes, int cascades, float shadowDist, float renderScale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.supportsHDR = true;
            asset.msaaSampleCount = msaa;
            asset.renderScale = renderScale;
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;
            asset.shadowDistance = shadowDist;
            asset.shadowCascadeCount = cascades;
            asset.useSRPBatcher = true;
            asset.supportsDynamicBatching = false; // SRP Batcher + Static Batching + GPU Instancing is faster!

            var so = new SerializedObject(asset);
            var mainShadowRes = so.FindProperty("m_MainLightShadowmapResolution");
            if (mainShadowRes != null) mainShadowRes.intValue = shadowRes;
            var addShadowRes = so.FindProperty("m_AdditionalLightsShadowmapResolution");
            if (addShadowRes != null) addShadowRes.intValue = Math.Min(shadowRes, 1024);
            var softShadows = so.FindProperty("m_SoftShadowsSupported");
            if (softShadows != null) softShadows.boolValue = true;
            var addLightMode = so.FindProperty("m_AdditionalLightsRenderingMode");
            if (addLightMode != null) addLightMode.intValue = (int)LightRenderingMode.PerPixel;
            var maxAddLights = so.FindProperty("m_AdditionalLightsPerObjectLimit");
            if (maxAddLights != null) maxAddLights.intValue = 8;
            var mainLightMode = so.FindProperty("m_MainLightRenderingMode");
            if (mainLightMode != null) mainLightMode.intValue = (int)LightRenderingMode.PerPixel;
            var mainLightShadows = so.FindProperty("m_MainLightShadowsSupported");
            if (mainLightShadows != null) mainLightShadows.boolValue = true;
            var addLightShadows = so.FindProperty("m_AdditionalLightShadowsSupported");
            if (addLightShadows != null) addLightShadows.boolValue = true;
            var reflBlend = so.FindProperty("m_ReflectionProbeBlending");
            if (reflBlend != null) reflBlend.boolValue = true;
            var reflBox = so.FindProperty("m_ReflectionProbeBoxProjection");
            if (reflBox != null) reflBox.boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetFloat(SerializedProperty parent, string name, float val)
        {
            var p = parent.FindPropertyRelative(name);
            if (p != null) p.floatValue = val;
        }

        private static void SetBool(SerializedProperty parent, string name, bool val)
        {
            var p = parent.FindPropertyRelative(name);
            if (p != null) p.boolValue = val;
        }
    }
}
