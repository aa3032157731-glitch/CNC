using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CNC.TrainAR.Editor
{
    /// <summary>
    /// End-to-end TrainAR-style URP pipeline + ST-03 Carding Machine (梳棉机) Cutaway Prototype Builder.
    /// Does NOT touch existing SampleScene.unity or Play.unity; installs URP_TrainAR_Renderer at index 1.
    /// </summary>
    public static class TrainAR_CardingMachineBuilder
    {
        public const string RootDir = "Assets/_Project/TrainAR_Style";
        public const string MatDir = RootDir + "/Materials";
        public const string PbrMatDir = RootDir + "/Materials/PBR_Realistic";
        public const string MeshDir = RootDir + "/Models/CardingMachine";
        public const string PrefabDir = RootDir + "/Prefabs";
        public const string SettingsDir = RootDir + "/Settings";
        public const string SceneDir = RootDir + "/Scenes";
        public const string ScenePath = SceneDir + "/TrainAR_HeroShowcase.unity";
        public const string PrefabPath = PrefabDir + "/PF_TrainAR_CardingMachine_Cutaway.prefab";
        public const string RealisticPrefabPath = PrefabDir + "/PF_ST03_CardingMachine_RealisticPBR.prefab";
        public const string RendererAssetPath = SettingsDir + "/URP_TrainAR_Renderer.asset";
        public const string ShowroomRendererAssetPath = SettingsDir + "/URP_RealisticShowroom_Renderer.asset";
        public const string VolumeProfilePath = SettingsDir + "/Profile_RealisticShowroom.asset";

        private static Shader _stylizedLit;
        private static Shader _studioFloor;
        private static Shader _urpLit;

        // Curated TrainAR Stylized Material Palette
        private static Material _matHousingIvory;
        private static Material _matHousingLightGray;
        private static Material _matCutawayHatch;
        private static Material _matChamberInterior;
        private static Material _matFrameSlate;
        private static Material _matDrumSteel;
        private static Material _matCardClothing;
        private static Material _matDarkMetal;
        private static Material _matFlatAluminum;
        private static Material _matAccentMustard;
        private static Material _matAccentSageCan;
        private static Material _matAccentOchre;
        private static Material _matCottonIvory;
        private static Material _matCottonWebAnimated;
        private static Material _matHmiScreen;
        private static Material _matBeltCharcoal;
        private static Material _matBeaconGreen;
        private static Material _matBeaconAmber;
        private static Material _matBeaconRed;
        private static Material _matImpurityMote;
        private static Material _matNepDefect;
        private static Material _matStudioFloor;

        // Realistic Smart-Factory URP/Lit PBR Material Palette (matches SampleScene!)
        private static Material _pbrHousingWhite;
        private static Material _pbrPanelSilver;
        private static Material _pbrCutawayAlloy;
        private static Material _pbrChamberDarkSteel;
        private static Material _pbrCastIron;
        private static Material _pbrMachinedSteel;
        private static Material _pbrCardClothing;
        private static Material _pbrBlackOxide;
        private static Material _pbrFlatAluminum;
        private static Material _pbrAccentTeal;
        private static Material _pbrSliverCan;
        private static Material _pbrMachinedBrass;
        private static Material _pbrCottonFleece;
        private static Material _pbrCottonWeb;
        private static Material _pbrHmiGlass;
        private static Material _pbrRubber;
        private static Material _pbrLedGreen;
        private static Material _pbrLedAmber;
        private static Material _pbrLedRed;
        private static Material _pbrImpurityMote;
        private static Material _pbrNepDefect;
        private static Material _pbrShowroomFloor;

        [MenuItem("CNC TrainAR/1. Build Realistic PBR & TrainAR ST-03 Prototype")]
        public static void BuildAll()
        {
            Debug.Log("[TrainAR] Starting Realistic PBR + TrainAR ST-03 Carding Machine prototype build...");
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PbrMatDir);
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(SceneDir);

            // 1. Generate both Stylized & Realistic PBR textures + Smart-Factory Reflection Cubemap
            TrainAR_TextureGenerator.GenerateAllTextures();
            TrainAR_PbrTextureGenerator.GenerateAllPbrAssets();

            // 2. Build both Stylized and Realistic PBR Material Libraries
            BuildMaterialLibrary();
            BuildRealisticPbrMaterialLibrary();

            // 3. Configure URP_TrainAR_Renderer (index 1 on URP pipeline assets, leaving index 0 untouched)
            SetupTrainARUrpRenderer();

            // 4. Build sculpted multi-part ST-03 Carding Machine Prefabs (both Realistic PBR & TrainAR Stylized)
            GameObject realisticPrefab = BuildCardingMachinePrefabs();

            // 5. Build showcase scene defaulting to Realistic Smart-Factory PBR with 1-click style toggle
            BuildShowcaseScene(realisticPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrainAR] Build completed! Open: " + ScenePath);
        }

        // =========================================================================
        // 1. MATERIAL LIBRARY
        // =========================================================================
        private static void BuildMaterialLibrary()
        {
            _stylizedLit = Shader.Find("TrainAR/URP/StylizedLit");
            _studioFloor = Shader.Find("TrainAR/URP/StudioFloor");
            if (_stylizedLit == null || _studioFloor == null)
                throw new Exception("[TrainAR] Required TrainAR shaders not found! Ensure shaders compiled cleanly.");

            var texBrush = LoadTex("Tex_TrainAR_WatercolorBrush.png");
            var texCeramic = LoadTex("Tex_TrainAR_CeramicPainted.png");
            var texSteel = LoadTex("Tex_TrainAR_BrushedSteel.png");
            var texClothing = LoadTex("Tex_TrainAR_CardClothing.png");
            var texHatch = LoadTex("Tex_TrainAR_CutawayHatch.png");
            var texCotton = LoadTex("Tex_TrainAR_CottonFibers.png");
            var texHmi = LoadTex("Tex_TrainAR_HMI_Screen.png");
            var texCan = LoadTex("Tex_TrainAR_CanBadge.png");

            // Warm porcelain ivory housing (like TrainAR Coffee maker!)
            _matHousingIvory = CreateMat("Mat_TrainAR_HousingIvory", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCeramic, texBrush,
                    baseCol: new Color(0.945f, 0.930f, 0.892f, 1f),
                    shadowCol: new Color(0.76f, 0.78f, 0.84f, 1f),
                    creviceCol: new Color(0.64f, 0.59f, 0.54f, 1f),
                    brushScale: 0.9f, brushStr: 0.11f, grime: 0.28f,
                    specSize: 0.07f, specInt: 0.14f, rimInt: 0.22f);
            });

            // Pale cool industrial casing gray (like TrainAR Four-Stroke Engine outer block!)
            _matHousingLightGray = CreateMat("Mat_TrainAR_HousingLightGray", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCeramic, texBrush,
                    baseCol: new Color(0.84f, 0.865f, 0.90f, 1f),
                    shadowCol: new Color(0.66f, 0.70f, 0.77f, 1f),
                    creviceCol: new Color(0.55f, 0.56f, 0.60f, 1f),
                    brushScale: 1.0f, brushStr: 0.12f, grime: 0.30f,
                    specSize: 0.08f, specInt: 0.12f, rimInt: 0.20f);
            });

            // Architectural cutaway cross-section wall face with 45-deg fine hatching!
            _matCutawayHatch = CreateMat("Mat_TrainAR_CutawaySection", _stylizedLit, m =>
            {
                ConfigureStylized(m, texHatch, texBrush,
                    baseCol: new Color(0.86f, 0.885f, 0.92f, 1f),
                    shadowCol: new Color(0.71f, 0.75f, 0.82f, 1f),
                    creviceCol: new Color(0.58f, 0.60f, 0.66f, 1f),
                    brushScale: 1.2f, brushStr: 0.06f, grime: 0.15f,
                    specSize: 0.05f, specInt: 0.06f, rimInt: 0.10f);
                m.SetFloat("_UseTriplanarBase", 1f);
                m.SetFloat("_TriplanarScale", 1.35f);
                m.SetFloat("_AmbientLift", 0.24f);
            });

            // Warm painterly shaded interior chamber backdrop (directly inspired by TrainAR Engine combustion chamber!)
            _matChamberInterior = CreateMat("Mat_TrainAR_ChamberInterior", _stylizedLit, m =>
            {
                ConfigureStylized(m, texBrush, texBrush,
                    baseCol: new Color(0.56f, 0.51f, 0.46f, 1f),
                    shadowCol: new Color(0.40f, 0.37f, 0.34f, 1f),
                    creviceCol: new Color(0.30f, 0.27f, 0.24f, 1f),
                    brushScale: 0.85f, brushStr: 0.18f, grime: 0.45f,
                    specSize: 0.05f, specInt: 0.04f, rimInt: 0.06f);
                m.SetFloat("_AmbientLift", 0.12f);
            });

            // Cast-iron heavy base plinth & structural slate frame
            _matFrameSlate = CreateMat("Mat_TrainAR_FrameSlate", _stylizedLit, m =>
            {
                ConfigureStylized(m, texSteel, texBrush,
                    baseCol: new Color(0.44f, 0.48f, 0.55f, 1f),
                    shadowCol: new Color(0.30f, 0.33f, 0.40f, 1f),
                    creviceCol: new Color(0.24f, 0.25f, 0.30f, 1f),
                    brushScale: 1.1f, brushStr: 0.13f, grime: 0.35f,
                    specSize: 0.09f, specInt: 0.18f, rimInt: 0.26f);
            });

            // Machined satin steel drum hubs, shafts & pulleys (like TrainAR Engine piston!)
            _matDrumSteel = CreateMat("Mat_TrainAR_DrumSteel", _stylizedLit, m =>
            {
                ConfigureStylized(m, texSteel, texBrush,
                    baseCol: new Color(0.73f, 0.765f, 0.82f, 1f),
                    shadowCol: new Color(0.52f, 0.56f, 0.64f, 1f),
                    creviceCol: new Color(0.42f, 0.44f, 0.50f, 1f),
                    brushScale: 1.3f, brushStr: 0.12f, grime: 0.28f,
                    specSize: 0.11f, specInt: 0.28f, rimInt: 0.30f);
            });

            // Card-clothing fine wire-ribbed metallic cylinder surfaces (Main Cylinder, Doffer, Licker-in)
            _matCardClothing = CreateMat("Mat_TrainAR_CardClothing", _stylizedLit, m =>
            {
                ConfigureStylized(m, texClothing, texBrush,
                    baseCol: new Color(0.67f, 0.715f, 0.78f, 1f),
                    shadowCol: new Color(0.46f, 0.50f, 0.58f, 1f),
                    creviceCol: new Color(0.36f, 0.38f, 0.44f, 1f),
                    brushScale: 1.2f, brushStr: 0.10f, grime: 0.25f,
                    specSize: 0.12f, specInt: 0.26f, rimInt: 0.28f);
            });

            // Dark gunmetal crankshaft/gears/spokes (like TrainAR Engine crankshaft!)
            _matDarkMetal = CreateMat("Mat_TrainAR_DarkMetal", _stylizedLit, m =>
            {
                ConfigureStylized(m, texSteel, texBrush,
                    baseCol: new Color(0.34f, 0.37f, 0.43f, 1f),
                    shadowCol: new Color(0.22f, 0.24f, 0.30f, 1f),
                    creviceCol: new Color(0.16f, 0.18f, 0.22f, 1f),
                    brushScale: 1.2f, brushStr: 0.12f, grime: 0.32f,
                    specSize: 0.10f, specInt: 0.24f, rimInt: 0.32f);
            });

            // Brushed pale aluminum for Revolving Flat-Top Bars
            _matFlatAluminum = CreateMat("Mat_TrainAR_FlatAluminum", _stylizedLit, m =>
            {
                ConfigureStylized(m, texSteel, texBrush,
                    baseCol: new Color(0.82f, 0.85f, 0.89f, 1f),
                    shadowCol: new Color(0.60f, 0.64f, 0.72f, 1f),
                    creviceCol: new Color(0.48f, 0.50f, 0.56f, 1f),
                    brushScale: 1.4f, brushStr: 0.10f, grime: 0.24f,
                    specSize: 0.10f, specInt: 0.22f, rimInt: 0.25f);
            });

            // Warm mustard yellow crown trim (directly echoing TrainAR Coffee maker!)
            _matAccentMustard = CreateMat("Mat_TrainAR_AccentMustard", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCeramic, texBrush,
                    baseCol: new Color(0.94f, 0.80f, 0.38f, 1f),
                    shadowCol: new Color(0.78f, 0.63f, 0.28f, 1f),
                    creviceCol: new Color(0.62f, 0.48f, 0.22f, 1f),
                    brushScale: 1.1f, brushStr: 0.10f, grime: 0.24f,
                    specSize: 0.08f, specInt: 0.16f, rimInt: 0.22f);
            });

            // Muted sage-forest green Sliver Can with TrainAR canister badge!
            _matAccentSageCan = CreateMat("Mat_TrainAR_AccentSageCan", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCan, texBrush,
                    baseCol: Color.white,
                    shadowCol: new Color(0.68f, 0.75f, 0.72f, 1f),
                    creviceCol: new Color(0.48f, 0.52f, 0.48f, 1f),
                    brushScale: 1.0f, brushStr: 0.09f, grime: 0.24f,
                    specSize: 0.09f, specInt: 0.14f, rimInt: 0.22f);
            });

            // Warm terracotta/ochre copper accent (Condenser Trumpet & feed indicator — echoes Coffee filter!)
            _matAccentOchre = CreateMat("Mat_TrainAR_AccentOchre", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCeramic, texBrush,
                    baseCol: new Color(0.87f, 0.58f, 0.34f, 1f),
                    shadowCol: new Color(0.69f, 0.43f, 0.24f, 1f),
                    creviceCol: new Color(0.52f, 0.32f, 0.18f, 1f),
                    brushScale: 1.2f, brushStr: 0.12f, grime: 0.26f,
                    specSize: 0.09f, specInt: 0.20f, rimInt: 0.22f);
            });

            // Soft creamy ivory Cotton Lap & Coiled Sliver
            _matCottonIvory = CreateMat("Mat_TrainAR_CottonIvory", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCotton, texBrush,
                    baseCol: new Color(0.99f, 0.97f, 0.92f, 1f),
                    shadowCol: new Color(0.85f, 0.81f, 0.74f, 1f),
                    creviceCol: new Color(0.74f, 0.69f, 0.62f, 1f),
                    brushScale: 1.6f, brushStr: 0.08f, grime: 0.18f,
                    specSize: 0.04f, specInt: 0.04f, rimInt: 0.18f);
                m.SetFloat("_AmbientLift", 0.22f);
            });

            // Flowing carded cotton web (animated scrolling fiber texture!)
            _matCottonWebAnimated = CreateMat("Mat_TrainAR_CottonWebAnimated", _stylizedLit, m =>
            {
                ConfigureStylized(m, texCotton, texBrush,
                    baseCol: new Color(0.99f, 0.975f, 0.93f, 1f),
                    shadowCol: new Color(0.84f, 0.81f, 0.75f, 1f),
                    creviceCol: new Color(0.75f, 0.71f, 0.64f, 1f),
                    brushScale: 1.4f, brushStr: 0.06f, grime: 0.14f,
                    specSize: 0.04f, specInt: 0.05f, rimInt: 0.22f);
                m.SetVector("_UVScrollSpeed", new Vector4(0f, -0.45f, 0f, 0f));
                m.SetFloat("_AmbientLift", 0.22f);
            });

            // Stylized HMI Screen
            _matHmiScreen = CreateMat("Mat_TrainAR_HmiScreen", _stylizedLit, m =>
            {
                ConfigureStylized(m, texHmi, texBrush,
                    baseCol: Color.white,
                    shadowCol: new Color(0.88f, 0.90f, 0.94f, 1f),
                    creviceCol: new Color(0.80f, 0.82f, 0.86f, 1f),
                    brushScale: 1f, brushStr: 0.02f, grime: 0.04f,
                    specSize: 0.06f, specInt: 0.10f, rimInt: 0.08f);
                m.SetFloat("_AmbientLift", 0.34f);
            });

            // Rubber V-Belts & Dark Rubber Leveling Pads
            _matBeltCharcoal = CreateMat("Mat_TrainAR_BeltCharcoal", _stylizedLit, m =>
            {
                ConfigureStylized(m, texBrush, texBrush,
                    baseCol: new Color(0.25f, 0.26f, 0.30f, 1f),
                    shadowCol: new Color(0.16f, 0.17f, 0.21f, 1f),
                    creviceCol: new Color(0.12f, 0.13f, 0.16f, 1f),
                    brushScale: 1.5f, brushStr: 0.08f, grime: 0.22f,
                    specSize: 0.06f, specInt: 0.08f, rimInt: 0.22f);
            });

            // Status Beacon Lenses (Matte TrainAR pastels)
            _matBeaconGreen = CreateMat("Mat_TrainAR_BeaconGreen", _stylizedLit, m =>
                ConfigureStylized(m, texCeramic, texBrush, new Color(0.42f, 0.80f, 0.56f, 1f), new Color(0.28f, 0.62f, 0.42f, 1f), new Color(0.24f, 0.48f, 0.34f, 1f), 1.5f, 0.05f, 0.10f, 0.12f, 0.25f, 0.28f));
            _matBeaconAmber = CreateMat("Mat_TrainAR_BeaconAmber", _stylizedLit, m =>
                ConfigureStylized(m, texCeramic, texBrush, new Color(0.95f, 0.78f, 0.36f, 1f), new Color(0.76f, 0.60f, 0.24f, 1f), new Color(0.60f, 0.46f, 0.20f, 1f), 1.5f, 0.05f, 0.10f, 0.12f, 0.25f, 0.25f));
            _matBeaconRed = CreateMat("Mat_TrainAR_BeaconRed", _stylizedLit, m =>
                ConfigureStylized(m, texCeramic, texBrush, new Color(0.86f, 0.40f, 0.36f, 1f), new Color(0.66f, 0.28f, 0.26f, 1f), new Color(0.50f, 0.22f, 0.20f, 1f), 1.5f, 0.05f, 0.10f, 0.12f, 0.22f, 0.25f));

            // Cotton seed-hull & leaf impurity motes + fault-mode nep beads
            _matImpurityMote = CreateMat("Mat_TrainAR_ImpurityMote", _stylizedLit, m =>
                ConfigureStylized(m, texBrush, texBrush, new Color(0.36f, 0.26f, 0.18f, 1f), new Color(0.22f, 0.16f, 0.11f, 1f), new Color(0.16f, 0.11f, 0.08f, 1f), 1.6f, 0.10f, 0.20f, 0.05f, 0.04f, 0.12f));
            _matNepDefect = CreateMat("Mat_TrainAR_NepDefect", _stylizedLit, m =>
                ConfigureStylized(m, texCotton, texBrush, new Color(0.86f, 0.68f, 0.44f, 1f), new Color(0.68f, 0.50f, 0.30f, 1f), new Color(0.54f, 0.38f, 0.22f, 1f), 1.6f, 0.08f, 0.16f, 0.05f, 0.04f, 0.16f));

            // Pure White TrainAR Studio Ground Shadow Catcher
            _matStudioFloor = CreateMat("Mat_TrainAR_StudioFloor", _studioFloor, m =>
            {
                m.SetColor("_FloorColor", new Color(0.994f, 0.995f, 0.998f, 1f));
                m.SetColor("_ShadowColor", new Color(0.73f, 0.76f, 0.81f, 1f));
                m.SetFloat("_ShadowStrength", 0.80f);
                m.SetFloat("_FadeStart", 7.0f);
                m.SetFloat("_FadeEnd", 15.0f);
                m.SetFloat("_GridAlpha", 0.0f);
            });
        }

        private static Texture2D LoadTex(string filename) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{TrainAR_TextureGenerator.TexDir}/{filename}");

        private static Texture2D LoadPbrTex(string filename) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{TrainAR_PbrTextureGenerator.PbrDir}/{filename}");

        private static void BuildRealisticPbrMaterialLibrary()
        {
            _urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (_urpLit == null)
                throw new Exception("[TrainAR] Universal Render Pipeline/Lit shader not found!");

            var texPowderAlb = LoadPbrTex("Tex_PBR_PowderCoat_Albedo.png");
            var texPowderNrm = LoadPbrTex("Tex_PBR_PowderCoat_Normal.png");
            var texIronAlb = LoadPbrTex("Tex_PBR_CastIron_Albedo.png");
            var texIronNrm = LoadPbrTex("Tex_PBR_CastIron_Normal.png");
            var texSteelAlb = LoadPbrTex("Tex_PBR_MachinedSteel_Albedo.png");
            var texSteelNrm = LoadPbrTex("Tex_PBR_MachinedSteel_Normal.png");
            var texClothAlb = LoadPbrTex("Tex_PBR_CardClothing_Albedo.png");
            var texClothNrm = LoadPbrTex("Tex_PBR_CardClothing_Normal.png");
            var texCutAlb = LoadPbrTex("Tex_PBR_CutawayMilled_Albedo.png");
            var texCutNrm = LoadPbrTex("Tex_PBR_CutawayMilled_Normal.png");
            var texCotAlb = LoadPbrTex("Tex_PBR_CottonFleece_Albedo.png");
            var texCotNrm = LoadPbrTex("Tex_PBR_CottonFleece_Normal.png");
            var texRubAlb = LoadPbrTex("Tex_PBR_RubberRibbed_Albedo.png");
            var texRubNrm = LoadPbrTex("Tex_PBR_RubberRibbed_Normal.png");
            var texCanAlb = LoadPbrTex("Tex_PBR_SliverCan_Albedo.png");
            var texHmi = LoadPbrTex("Tex_PBR_HMI_SmartFactory.png");

            // Reuse existing smart-factory epoxy floor textures from SampleScene!
            var texFloorAlb = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/PBR/Tex_EpoxyFloor_Albedo.png");
            var texFloorNrm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/PBR/Tex_EpoxyFloor_Normal.png");

            // Balanced Luminous Industrial CAD Value Hierarchy (bright, readable steel & cast iron, zero whiteout glare!)
            _pbrHousingWhite = CreatePbrMat("Mat_PBR_HousingWhite",
                new Color(0.86f, 0.89f, 0.92f, 1f), texPowderAlb, texPowderNrm, 0.10f, 0.08f, 0.60f, new Vector2(2.4f, 2.4f));

            _pbrPanelSilver = CreatePbrMat("Mat_PBR_PanelSilver",
                new Color(0.70f, 0.75f, 0.82f, 1f), texSteelAlb, texSteelNrm, 0.13f, 0.52f, 0.64f, new Vector2(2.0f, 2.0f));

            // Precision CNC-milled physical cutaway exhibition alloy
            _pbrCutawayAlloy = CreatePbrMat("Mat_PBR_CutawayAlloy",
                new Color(0.64f, 0.70f, 0.78f, 1f), texCutAlb, texCutNrm, 0.22f, 0.64f, 0.56f, new Vector2(2.0f, 2.0f));

            _pbrChamberDarkSteel = CreatePbrMat("Mat_PBR_ChamberDarkSteel",
                new Color(0.27f, 0.31f, 0.38f, 1f), texIronAlb, texIronNrm, 0.18f, 0.45f, 0.36f, new Vector2(2.2f, 2.2f));

            _pbrCastIron = CreatePbrMat("Mat_PBR_CastIron",
                new Color(0.31f, 0.36f, 0.44f, 1f), texIronAlb, texIronNrm, 0.20f, 0.54f, 0.44f, new Vector2(2.4f, 2.4f));

            _pbrMachinedSteel = CreatePbrMat("Mat_PBR_MachinedSteel",
                new Color(0.78f, 0.82f, 0.88f, 1f), texSteelAlb, texSteelNrm, 0.14f, 0.82f, 0.72f, new Vector2(2.0f, 2.0f));

            // Sleek gunmetal-steel card wire clothing (bright enough to look metallic & crisp, dark enough that white cotton pops!)
            _pbrCardClothing = CreatePbrMat("Mat_PBR_CardClothing",
                new Color(0.55f, 0.60f, 0.68f, 1f), texClothAlb, texClothNrm, 0.66f, 0.74f, 0.56f, new Vector2(2.4f, 2.4f));

            _pbrBlackOxide = CreatePbrMat("Mat_PBR_BlackOxide",
                new Color(0.28f, 0.33f, 0.40f, 1f), texSteelAlb, texSteelNrm, 0.15f, 0.70f, 0.56f, new Vector2(2.0f, 2.0f));

            _pbrFlatAluminum = CreatePbrMat("Mat_PBR_FlatAluminum",
                new Color(0.75f, 0.80f, 0.86f, 1f), texSteelAlb, texSteelNrm, 0.11f, 0.72f, 0.66f, new Vector2(2.0f, 2.0f));

            // Signature Smart-Factory Cyan-Teal Enamel Trim (vibrant clean enamel matching user's favorite look!)
            _pbrAccentTeal = CreatePbrMat("Mat_PBR_AccentTeal",
                new Color(0.10f, 0.56f, 0.64f, 1f), texPowderAlb, texPowderNrm, 0.09f, 0.24f, 0.70f, new Vector2(2.2f, 2.2f));

            _pbrSliverCan = CreatePbrMat("Mat_PBR_SliverCan",
                new Color(0.96f, 0.97f, 0.99f, 1f), texCanAlb, texPowderNrm, 0.09f, 0.22f, 0.62f, Vector2.one);

            // Precision Turned Industrial Brass/Bronze
            _pbrMachinedBrass = CreatePbrMat("Mat_PBR_MachinedBrass",
                new Color(0.88f, 0.62f, 0.26f, 1f), texSteelAlb, texSteelNrm, 0.13f, 0.84f, 0.72f, new Vector2(2.0f, 2.0f));

            _pbrCottonFleece = CreatePbrMat("Mat_PBR_CottonFleece",
                new Color(0.98f, 0.96f, 0.91f, 1f), texCotAlb, texCotNrm, 0.40f, 0.0f, 0.10f, new Vector2(2.2f, 2.2f));

            _pbrCottonWeb = CreatePbrMat("Mat_PBR_CottonWeb",
                new Color(0.99f, 0.97f, 0.93f, 1f), texCotAlb, texCotNrm, 0.28f, 0.0f, 0.14f, new Vector2(1.8f, 2.6f));

            // Glossy Dark-Glass Smart-Factory HMI Screen with subtle live emission
            _pbrHmiGlass = CreatePbrMat("Mat_PBR_HmiGlass",
                Color.white, texHmi, null, 0f, 0.08f, 0.92f, Vector2.one,
                emissiveMap: texHmi, emissiveCol: new Color(0.96f, 1.02f, 1.08f, 1f));

            _pbrRubber = CreatePbrMat("Mat_PBR_Rubber",
                new Color(0.18f, 0.20f, 0.24f, 1f), texRubAlb, texRubNrm, 0.40f, 0.06f, 0.28f, new Vector2(2f, 4f));

            // Realistic Glossy Polycarbonate LED Beacon Lenses (Active Green Glow!)
            _pbrLedGreen = CreatePbrMat("Mat_PBR_LedGreen",
                new Color(0.14f, 0.86f, 0.46f, 1f), null, null, 0f, 0.08f, 0.92f, Vector2.one,
                emissiveMap: null, emissiveCol: new Color(0.18f, 1.75f, 0.78f, 1f));

            _pbrLedAmber = CreatePbrMat("Mat_PBR_LedAmber",
                new Color(0.92f, 0.66f, 0.12f, 1f), null, null, 0f, 0.08f, 0.90f, Vector2.one,
                emissiveMap: null, emissiveCol: new Color(0.28f, 0.18f, 0.02f, 1f));

            _pbrLedRed = CreatePbrMat("Mat_PBR_LedRed",
                new Color(0.84f, 0.18f, 0.16f, 1f), null, null, 0f, 0.10f, 0.90f, Vector2.one,
                emissiveMap: null, emissiveCol: new Color(0.20f, 0.03f, 0.03f, 1f));

            // Organic cotton seed-hulls / leaf motes (shedding below Licker-in) & nep defect specks (fault mode!)
            _pbrImpurityMote = CreatePbrMat("Mat_PBR_ImpurityMote",
                new Color(0.28f, 0.20f, 0.13f, 1f), texIronAlb, texCotNrm, 0.45f, 0.0f, 0.14f, Vector2.one);
            _pbrNepDefect = CreatePbrMat("Mat_PBR_NepDefect",
                new Color(0.86f, 0.64f, 0.36f, 1f), texCotAlb, texCotNrm, 0.40f, 0.0f, 0.18f, Vector2.one,
                emissiveMap: null, emissiveCol: new Color(0.24f, 0.14f, 0.03f, 1f));

            // Clean bright studio epoxy floor matching user's preferred luminous showroom look
            _pbrShowroomFloor = CreatePbrMat("Mat_PBR_ShowroomFloor",
                new Color(0.78f, 0.82f, 0.87f, 1f), texFloorAlb, texFloorNrm, 0.34f, 0.06f, 0.54f, new Vector2(18f, 18f));
        }

        private static Material CreatePbrMat(string name, Color baseColor, Texture2D albedo, Texture2D normal,
            float bumpScale, float metallic, float smoothness, Vector2 tiling,
            Texture2D emissiveMap = null, Color? emissiveCol = null)
        {
            string path = $"{PbrMatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(_urpLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != _urpLit)
            {
                mat.shader = _urpLit;
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", tiling);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", tiling);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.SetTexture("_BumpMap", null);
                mat.DisableKeyword("_NORMALMAP");
            }

            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_EnvironmentReflections", 1f);
            mat.SetFloat("_SpecularHighlights", 1f);

            // Enable _EMISSION keyword on all PBR materials so MaterialPropertyBlock focus highlights work seamlessly!
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", emissiveMap);
            mat.SetColor("_EmissionColor", emissiveCol ?? Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static List<MaterialStylePair> BuildMaterialStylePairs()
        {
            return new List<MaterialStylePair>
            {
                new MaterialStylePair { realisticPbr = _pbrHousingWhite,     trainArStylized = _matHousingIvory },
                new MaterialStylePair { realisticPbr = _pbrPanelSilver,      trainArStylized = _matHousingLightGray },
                new MaterialStylePair { realisticPbr = _pbrCutawayAlloy,     trainArStylized = _matCutawayHatch },
                new MaterialStylePair { realisticPbr = _pbrChamberDarkSteel, trainArStylized = _matChamberInterior },
                new MaterialStylePair { realisticPbr = _pbrCastIron,         trainArStylized = _matFrameSlate },
                new MaterialStylePair { realisticPbr = _pbrMachinedSteel,    trainArStylized = _matDrumSteel },
                new MaterialStylePair { realisticPbr = _pbrCardClothing,     trainArStylized = _matCardClothing },
                new MaterialStylePair { realisticPbr = _pbrBlackOxide,       trainArStylized = _matDarkMetal },
                new MaterialStylePair { realisticPbr = _pbrFlatAluminum,     trainArStylized = _matFlatAluminum },
                new MaterialStylePair { realisticPbr = _pbrAccentTeal,       trainArStylized = _matAccentMustard },
                new MaterialStylePair { realisticPbr = _pbrSliverCan,        trainArStylized = _matAccentSageCan },
                new MaterialStylePair { realisticPbr = _pbrMachinedBrass,    trainArStylized = _matAccentOchre },
                new MaterialStylePair { realisticPbr = _pbrCottonFleece,     trainArStylized = _matCottonIvory },
                new MaterialStylePair { realisticPbr = _pbrCottonWeb,        trainArStylized = _matCottonWebAnimated },
                new MaterialStylePair { realisticPbr = _pbrHmiGlass,         trainArStylized = _matHmiScreen },
                new MaterialStylePair { realisticPbr = _pbrRubber,           trainArStylized = _matBeltCharcoal },
                new MaterialStylePair { realisticPbr = _pbrLedGreen,         trainArStylized = _matBeaconGreen },
                new MaterialStylePair { realisticPbr = _pbrLedAmber,         trainArStylized = _matBeaconAmber },
                new MaterialStylePair { realisticPbr = _pbrLedRed,           trainArStylized = _matBeaconRed },
                new MaterialStylePair { realisticPbr = _pbrImpurityMote,     trainArStylized = _matImpurityMote },
                new MaterialStylePair { realisticPbr = _pbrNepDefect,        trainArStylized = _matNepDefect },
            };
        }

        private static Material CreateMat(string name, Shader shader, Action<Material> config)
        {
            string path = $"{MatDir}/{name}.mat";
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
            config(mat);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureStylized(Material m, Texture2D baseTex, Texture2D brushTex,
            Color baseCol, Color shadowCol, Color creviceCol,
            float brushScale, float brushStr, float grime,
            float specSize, float specInt, float rimInt)
        {
            m.SetColor("_BaseColor", baseCol);
            m.SetColor("_ShadowColor", shadowCol);
            m.SetColor("_CreviceTint", creviceCol);
            m.SetTexture("_BaseMap", baseTex);
            m.SetTexture("_BrushMap", brushTex);
            m.SetFloat("_BrushScale", brushScale);
            m.SetFloat("_BrushStrength", brushStr);
            m.SetFloat("_GrimeInCrevice", grime);
            m.SetFloat("_ShadowThreshold", 0.06f);
            m.SetFloat("_HighlightThreshold", 0.64f);
            m.SetFloat("_RampSmoothness", 0.14f);
            m.SetFloat("_ShadowReceiveStrength", 0.52f);
            m.SetFloat("_AmbientLift", 0.15f);
            m.SetColor("_SpecColor", new Color(1f, 0.99f, 0.96f, 1f));
            m.SetFloat("_SpecSize", specSize);
            m.SetFloat("_SpecIntensity", specInt);
            m.SetColor("_RimColor", new Color(0.98f, 0.99f, 1f, 1f));
            m.SetFloat("_RimPower", 3.4f);
            m.SetFloat("_RimIntensity", rimInt);
            m.SetVector("_UVScrollSpeed", Vector4.zero);
            m.SetFloat("_UseTriplanarBase", 0f);
            m.SetFloat("_HighlightAmount", 0f);
        }

        // =========================================================================
        // 2. URP TRAINAR RENDERER SETUP (Non-destructive! Registered as Index 1)
        // =========================================================================
        private static void SetupTrainARUrpRenderer()
        {
            const string baseRendererPath = "Assets/_Project/Settings/URP_Desktop_Renderer.asset";
            var trainArRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);

            if (trainArRenderer == null)
            {
                if (File.Exists(baseRendererPath))
                {
                    AssetDatabase.CopyAsset(baseRendererPath, RendererAssetPath);
                    AssetDatabase.Refresh();
                    trainArRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
                }
                else
                {
                    trainArRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(trainArRenderer, RendererAssetPath);
                }
            }

            // Ensure TrainAR_EdgeOutlineFeature is attached and configured cleanly
            TrainAR_EdgeOutlineFeature outlineFeature = null;
            foreach (var f in trainArRenderer.rendererFeatures)
            {
                if (f is TrainAR_EdgeOutlineFeature existing)
                {
                    outlineFeature = existing;
                    break;
                }
            }

            if (outlineFeature == null)
            {
                outlineFeature = ScriptableObject.CreateInstance<TrainAR_EdgeOutlineFeature>();
                outlineFeature.name = "TrainAR_InkOutline";
                AssetDatabase.AddObjectToAsset(outlineFeature, trainArRenderer);

                var so = new SerializedObject(trainArRenderer);
                var featList = so.FindProperty("m_RendererFeatures");
                var mapList = so.FindProperty("m_RendererFeatureMap");
                int idx = featList.arraySize;
                featList.InsertArrayElementAtIndex(idx);
                featList.GetArrayElementAtIndex(idx).objectReferenceValue = outlineFeature;
                if (mapList != null)
                {
                    mapList.InsertArrayElementAtIndex(idx);
                    mapList.GetArrayElementAtIndex(idx).longValue = outlineFeature.GetInstanceID();
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Tune outline parameters to match TrainAR reference image crisp charcoal ink!
            outlineFeature.SetActive(true);
            outlineFeature.settings.outlineShader = Shader.Find("Hidden/TrainAR/EdgeOutline");
            outlineFeature.settings.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            outlineFeature.settings.outlineColor = new Color(0.15f, 0.17f, 0.22f, 1.0f);
            outlineFeature.settings.outlineThickness = 1.65f;
            outlineFeature.settings.outlineOpacity = 0.90f;
            outlineFeature.settings.depthThreshold = 0.042f;
            outlineFeature.settings.normalThreshold = 0.44f;
            outlineFeature.settings.depthNormalThreshold = 0.45f;
            outlineFeature.settings.depthNormalScale = 7.0f;
            outlineFeature.settings.edgeSoftness = 0.14f;
            outlineFeature.settings.maxDistance = 24.0f;
            outlineFeature.settings.renderInSceneView = true;

            // Configure clean, zero-speckle InterleavedGradient High-Sample SSAO on TrainAR Renderer
            ConfigureCleanShowroomSSAO(trainArRenderer, intensity: 0.24f, radius: 0.18f, directStrength: 0.08f);

            // Ensure IntermediateTextureMode = Always (1) for crisp RTHandle blits
            {
                var rso = new SerializedObject(trainArRenderer);
                var inter = rso.FindProperty("m_IntermediateTextureMode");
                if (inter != null) inter.intValue = 1;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(outlineFeature);
            EditorUtility.SetDirty(trainArRenderer);
            trainArRenderer.SetDirty();

            // Also build dedicated Noise-Free Realistic CAD Showroom Renderer (Index 2: Crisp Bilateral Contact AO + Subtle Hairline Technical Creases!)
            var showroomRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ShowroomRendererAssetPath);
            if (showroomRenderer == null)
            {
                if (File.Exists(baseRendererPath))
                {
                    AssetDatabase.CopyAsset(baseRendererPath, ShowroomRendererAssetPath);
                    AssetDatabase.Refresh();
                    showroomRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ShowroomRendererAssetPath);
                }
                else
                {
                    showroomRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(showroomRenderer, ShowroomRendererAssetPath);
                }
            }

            // Attach ultra-restrained CAD technical crease outline (1.05px hairline at 42% slate opacity — KeyShot/SolidWorks clarity without cartoon look!)
            TrainAR_EdgeOutlineFeature cadCreaseFeature = null;
            foreach (var f in showroomRenderer.rendererFeatures)
            {
                if (f is TrainAR_EdgeOutlineFeature existing)
                {
                    cadCreaseFeature = existing;
                    break;
                }
            }
            if (cadCreaseFeature == null)
            {
                cadCreaseFeature = ScriptableObject.CreateInstance<TrainAR_EdgeOutlineFeature>();
                cadCreaseFeature.name = "Showroom_CAD_CreaseLines";
                AssetDatabase.AddObjectToAsset(cadCreaseFeature, showroomRenderer);

                var so = new SerializedObject(showroomRenderer);
                var featList = so.FindProperty("m_RendererFeatures");
                var mapList = so.FindProperty("m_RendererFeatureMap");
                int idx = featList.arraySize;
                featList.InsertArrayElementAtIndex(idx);
                featList.GetArrayElementAtIndex(idx).objectReferenceValue = cadCreaseFeature;
                if (mapList != null)
                {
                    mapList.InsertArrayElementAtIndex(idx);
                    mapList.GetArrayElementAtIndex(idx).longValue = cadCreaseFeature.GetInstanceID();
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            cadCreaseFeature.SetActive(true);
            cadCreaseFeature.settings.outlineShader = Shader.Find("Hidden/TrainAR/EdgeOutline");
            cadCreaseFeature.settings.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            cadCreaseFeature.settings.outlineColor = new Color(0.10f, 0.13f, 0.18f, 1.0f);
            cadCreaseFeature.settings.outlineThickness = 1.0f;
            cadCreaseFeature.settings.outlineOpacity = 0.32f;
            cadCreaseFeature.settings.depthThreshold = 0.046f;
            cadCreaseFeature.settings.normalThreshold = 0.54f;
            cadCreaseFeature.settings.depthNormalThreshold = 0.45f;
            cadCreaseFeature.settings.depthNormalScale = 7.2f;
            cadCreaseFeature.settings.edgeSoftness = 0.15f;
            cadCreaseFeature.settings.maxDistance = 20.0f;
            cadCreaseFeature.settings.renderInSceneView = true;

            ConfigureCleanShowroomSSAO(showroomRenderer, intensity: 0.32f, radius: 0.19f, directStrength: 0.10f);
            {
                var rso = new SerializedObject(showroomRenderer);
                var inter = rso.FindProperty("m_IntermediateTextureMode");
                if (inter != null) inter.intValue = 1;
                rso.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(cadCreaseFeature);
            EditorUtility.SetDirty(showroomRenderer);
            showroomRenderer.SetDirty();

            // Register Renderers [0=SampleScene untouched, 1=TrainAR Outline, 2=Noise-Free Realistic Showroom]
            string[] urpAssets =
            {
                "Assets/_Project/Settings/URP_Desktop_Medium.asset",
                "Assets/_Project/Settings/URP_Desktop_High.asset",
                "Assets/_Project/Settings/URP_Desktop_Ultra.asset"
            };
            foreach (var p in urpAssets)
            {
                var pipe = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(p);
                if (pipe == null) continue;
                var pso = new SerializedObject(pipe);
                var listProp = pso.FindProperty("m_RendererDataList");
                if (listProp != null)
                {
                    if (listProp.arraySize < 3)
                        listProp.arraySize = 3;
                    listProp.GetArrayElementAtIndex(1).objectReferenceValue = trainArRenderer;
                    listProp.GetArrayElementAtIndex(2).objectReferenceValue = showroomRenderer;
                    // Keep default renderer index = 0 so SampleScene/Play remain unchanged!
                    pso.FindProperty("m_DefaultRendererIndex").intValue = 0;
                    pso.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pipe);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureCleanShowroomSSAO(UniversalRendererData renderer, float intensity, float radius, float directStrength)
        {
            if (renderer == null) return;
            foreach (var f in renderer.rendererFeatures)
            {
                if (f != null && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                {
                    var sso = new SerializedObject(f);
                    var s = sso.FindProperty("m_Settings");
                    if (s != null)
                    {
                        SetIntProp(s, "AOMethod", 1);        // 1 = InterleavedGradient (eliminates sandy BlueNoise speckles!)
                        SetBoolProp(s, "Downsample", false); // Full resolution
                        SetIntProp(s, "Samples", 0);         // 0 = High (12 samples per pixel)
                        SetIntProp(s, "BlurQuality", 0);     // 0 = High (Bilateral edge-preserving blur)
                        SetIntProp(s, "NormalSamples", 2);   // 2 = High normal reconstruction
                        SetFloatProp(s, "Intensity", intensity);
                        SetFloatProp(s, "Radius", radius);
                        SetFloatProp(s, "DirectLightingStrength", directStrength);
                    }
                    sso.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(f);
                }
            }
        }

        private static void SetIntProp(SerializedProperty parent, string rel, int val)
        {
            var p = parent.FindPropertyRelative(rel);
            if (p != null) p.intValue = val;
        }
        private static void SetBoolProp(SerializedProperty parent, string rel, bool val)
        {
            var p = parent.FindPropertyRelative(rel);
            if (p != null) p.boolValue = val;
        }
        private static void SetFloatProp(SerializedProperty parent, string rel, float val)
        {
            var p = parent.FindPropertyRelative(rel);
            if (p != null) p.floatValue = val;
        }

        // =========================================================================
        // 3. SCULPTED MULTI-PART ST-03 CARDING MACHINE (梳棉机) MODELING
        // =========================================================================
        private static GameObject BuildCardingMachinePrefabs()
        {
            var root = new GameObject("ST03_CardingMachine_RealisticPBR");
            var animator = root.AddComponent<TrainAR_CardingAnimator>();
            var flowAnim = root.AddComponent<TrainAR_MaterialFlowAnimator>();
            flowAnim.cardingAnimator = animator;
            var switcher = root.AddComponent<TrainAR_StyleSwitcher>();
            switcher.materialPairs = BuildMaterialStylePairs();
            animator.parts = new List<TrainARAssemblyPart>();

            // Reference coordinates matching mechanical specification:
            Vector2 mainCenter = new Vector2(-0.04f, 1.04f);
            Vector2 lickerCenter = new Vector2(-0.88f, 0.94f);
            Vector2 dofferCenter = new Vector2(0.94f, 0.88f);
            Vector2 feedRollerCenter = new Vector2(-1.14f, 0.98f);
            Vector2 stripCenter = new Vector2(1.38f, 0.86f);
            Vector2 calenderUpCenter = new Vector2(1.60f, 0.94f);
            Vector2 calenderLowCenter = new Vector2(1.60f, 0.78f);
            Vector2 coilerCenterXZ = new Vector2(1.96f, 0.0f);

            // --- PART 01: HEAVY CAST-IRON BASE FRAME, BOLTED BEARING PEDESTALS & NAMEPLATE ---
            var p01 = CreateSubRoot(root.transform, "01_BaseFrame");
            AttachMesh(p01, "BaseFrame_Mesh", BuildBaseFrameMesh(lickerCenter, mainCenter, dofferCenter),
                new[] { _matFrameSlate, _matDarkMetal, _matBeltCharcoal, _matDrumSteel, _matAccentOchre });
            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "base_frame",
                displayName = "铸铁机架与轴承座",
                description = "高刚性铸铁底座、防振地脚与滚筒主轴承座，确保高速分梳时的隔距稳定。",
                root = p01.transform,
                explodedOffset = new Vector3(0f, -0.32f, 0f)
            });

            // --- PART 02: ARCHITECTURAL CUTAWAY HOUSING (Back Shell + Arched Hood + Cutaway Wall Frame) ---
            var p02 = CreateSubRoot(root.transform, "02_CutawayHousing");
            AttachMesh(p02, "CutawayHousing_Mesh", BuildCutawayHousingMesh(mainCenter),
                new[] { _matHousingIvory, _matCutawayHatch, _matChamberInterior, _matAccentMustard, _matHousingLightGray });
            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "housing",
                displayName = "半剖视机身外壳",
                description = "仿工程教学半剖视机壳，以45°剖面线清晰区分外壳壁厚与内部梳理腔体。",
                root = p02.transform,
                explodedOffset = new Vector3(0f, 0.68f, 0.55f)
            });

            // --- PART 02b: REMOVABLE SOLID FRONT ENCLOSURE COVER (Hidden in Cutaway & Exploded, Shown in Solid) ---
            var p02b = CreateSubRoot(root.transform, "02b_SolidFrontCover");
            AttachMesh(p02b, "SolidFrontCover_Mesh", BuildSolidFrontCoverMesh(),
                new[] { _matHousingIvory, _matHousingLightGray, _matAccentMustard, _matDarkMetal });
            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "front_cover",
                displayName = "可拆卸前防护罩门",
                description = "完整外形模式下闭合隔尘防飞花，剖视模式自动隐藏以露出内部核心滚筒。",
                root = p02b.transform,
                explodedOffset = new Vector3(0f, 0.15f, -1.35f),
                hideInCutaway = true,
                onlyInSolid = true
            });

            // --- PART 03: FEED CHUTE, DYNAMIC DESCENDING COTTON BATT & FEED ROLLER ---
            var p03 = CreateSubRoot(root.transform, "03_FeedChute_Cotton");
            AttachMesh(p03, "FeedChute_Mesh", BuildFeedChuteStaticMesh(),
                new[] { _matHousingLightGray, _matCutawayHatch, _matAccentMustard, _matCottonIvory, _matDarkMetal });
            var feedRollerObj = CreatePivotChild(p03.transform, "FeedRoller_Rot", new Vector3(feedRollerCenter.x, feedRollerCenter.y, 0f));
            AttachMesh(feedRollerObj, "FeedRoller_Mesh", BuildFlutedRollerMesh(0.095f, 0.96f, 14),
                new[] { _matDrumSteel, _matDarkMetal });
            animator.feedRoller = feedRollerObj.transform;

            // 7 Dynamic Descending Raw Cotton Batt Layers cycling smoothly from top hopper to feed roller!
            Mesh battLayerMesh = SaveMeshAsset("FeedBattLayer_Mesh", BuildSingleFeedBattLayerMesh());
            var battGroup = CreateSubRoot(p03.transform, "DynamicFeedBatt_Group");
            flowAnim.feedBattLayers = SpawnDynamicInstances(battGroup.transform, "FeedBatt", 7, battLayerMesh, _matCottonIvory);

            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "feed_chute",
                displayName = "喂棉箱与给棉罗拉",
                description = "储存并均匀输送松散棉层，经给棉板与沟槽给棉罗拉握持喂入高速刺辊。",
                root = p03.transform,
                explodedOffset = new Vector3(-0.76f, 0.12f, 0f)
            });

            // --- PART 04: HIGH-SPEED LICKER-IN (刺辊) + MOTE KNIVES + DYNAMIC SHEDDING IMPURITIES ---
            var p04 = CreateSubRoot(root.transform, "04_LickerIn");
            AttachMesh(p04, "LickerIn_Static_Mesh", BuildLickerInStaticMesh(lickerCenter),
                new[] { _matDarkMetal, _matFrameSlate, _matAccentOchre });
            var lickerRot = CreatePivotChild(p04.transform, "LickerIn_Drum_Rot", new Vector3(lickerCenter.x, lickerCenter.y, 0f));
            AttachMesh(lickerRot, "LickerIn_Drum_Mesh", BuildLickerInDrumMesh(),
                new[] { _matCardClothing, _matDarkMetal, _matDrumSteel });
            animator.lickerIn = lickerRot.transform;

            // 18 Dynamic Falling Seed-Hull / Leaf Impurity Motes beneath Licker-in mote knives!
            Mesh moteMesh = SaveMeshAsset("ImpurityMote_Mesh", BuildImpurityMoteMesh());
            var moteGroup = CreateSubRoot(p04.transform, "DynamicImpurities_Group");
            flowAnim.impurityMotes = SpawnDynamicInstances(moteGroup.transform, "ImpurityMote", 18, moteMesh, _matImpurityMote);
            flowAnim.lickerCenterXY = lickerCenter;

            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "lickerin",
                displayName = "高速刺辊与除尘刀",
                description = "带锯齿针布的刺辊高速分梳握持棉层，配合下方除尘刀实现大杂质早落少碎。",
                root = p04.transform,
                explodedOffset = new Vector3(-0.36f, -0.06f, -0.54f)
            });

            // --- PART 05: MAIN CYLINDER / SWIFT + DYNAMIC STRETCHING CARDING FIBER STREAM ---
            var p05 = CreateSubRoot(root.transform, "05_MainCylinder");
            AttachMesh(p05, "MainCylinder_Undercasing", BuildMainCylinderUndercasingMesh(mainCenter),
                new[] { _matFrameSlate, _matDarkMetal });
            var mainRot = CreatePivotChild(p05.transform, "MainCylinder_Drum_Rot", new Vector3(mainCenter.x, mainCenter.y, 0f));
            AttachMesh(mainRot, "MainCylinder_Drum_Mesh", BuildMainCylinderDrumMesh(),
                new[] { _matCardClothing, _matFrameSlate, _matDrumSteel });
            animator.mainCylinder = mainRot.transform;

            // 24 Dynamic Cotton Flocks transforming into Stretched Single Fibers around Main Cylinder & Flats!
            Mesh tuftMesh = SaveMeshAsset("CardingTuft_Mesh", BuildCardingTuftMesh());
            var flockGroup = CreateSubRoot(p05.transform, "DynamicCardingFlocks_Group");
            flowAnim.cardingFlocks = SpawnDynamicInstances(flockGroup.transform, "CardingTuft", 24, tuftMesh, _matCottonIvory);
            flowAnim.mainCylinderCenterXY = mainCenter;
            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "main_cylinder",
                displayName = "主锡林大滚筒 (Main Cylinder)",
                description = "梳棉机核心高速滚筒，直径大、针齿密，带走刺辊纤维并与盖板进行精细单纤分梳。",
                root = p05.transform,
                explodedOffset = new Vector3(0f, 0.04f, -0.50f)
            });

            // --- PART 06: REVOLVING FLAT TOPS (26块回转盖板 + 曲轨 + 星轮) ---
            var p06 = CreateSubRoot(root.transform, "06_RevolvingFlats");
            AttachMesh(p06, "Flats_TrackStructure", BuildFlatTrackStructureMesh(mainCenter),
                new[] { _matFrameSlate, _matDarkMetal, _matAccentOchre });

            var rearSprocket = CreatePivotChild(p06.transform, "FlatSprocket_Rear", new Vector3(-0.62f, 1.54f, 0f));
            AttachMesh(rearSprocket, "FlatSprocket_Mesh", BuildFlatSprocketPairMesh(), new[] { _matDarkMetal, _matDrumSteel });
            animator.flatSprocketRear = rearSprocket.transform;

            var frontSprocket = CreatePivotChild(p06.transform, "FlatSprocket_Front", new Vector3(0.54f, 1.54f, 0f));
            AttachMesh(frontSprocket, "FlatSprocketFront_Mesh", BuildFlatSprocketPairMesh(), new[] { _matDarkMetal, _matDrumSteel });
            animator.flatSprocketFront = frontSprocket.transform;

            // Build 1 shared Flat-Top Bar mesh and instantiate 26 bars around the closed loop!
            Mesh flatBarMesh = SaveMeshAsset("FlatTopBar_Mesh", BuildSingleFlatBarMesh());
            var flatBarsRoot = CreateSubRoot(p06.transform, "FlatBars_Loop");
            var bars = new Transform[26];
            for (int i = 0; i < bars.Length; i++)
            {
                var barGo = new GameObject($"FlatBar_{i:D2}");
                barGo.transform.SetParent(flatBarsRoot.transform, false);
                var mf = barGo.AddComponent<MeshFilter>();
                mf.sharedMesh = flatBarMesh;
                var mr = barGo.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { _matFlatAluminum, _matDarkMetal };
                bars[i] = barGo.transform;
            }
            animator.flatTopBars = bars;
            animator.mainCylinderCenterXY = mainCenter;
            animator.flatWorkingRadius = 0.675f;
            animator.flatReturnHeight = 1.94f;
            animator.flatRearX = -0.62f;
            animator.flatFrontX = 0.54f;
            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "revolving_flats",
                displayName = "回转盖板梳理系统",
                description = "26块活动盖板沿曲轨紧贴锡林上方缓慢移动，形成单纤维分梳区并带走短绒棉结。",
                root = p06.transform,
                explodedOffset = new Vector3(0f, 0.56f, -0.18f)
            });

            // --- PART 07: DOFFER + STRIPPING ROLLER + TRUMPET + FLOWING COTTON WEB ---
            var p07 = CreateSubRoot(root.transform, "07_Doffer_Web");
            var dofferRot = CreatePivotChild(p07.transform, "Doffer_Drum_Rot", new Vector3(dofferCenter.x, dofferCenter.y, 0f));
            AttachMesh(dofferRot, "Doffer_Drum_Mesh", BuildDofferDrumMesh(),
                new[] { _matCardClothing, _matFrameSlate, _matDrumSteel });
            animator.doffer = dofferRot.transform;

            var stripRot = CreatePivotChild(p07.transform, "StrippingRoller_Rot", new Vector3(stripCenter.x, stripCenter.y, 0f));
            AttachMesh(stripRot, "StrippingRoller_Mesh", BuildFlutedRollerMesh(0.082f, 0.94f, 12),
                new[] { _matDrumSteel, _matDarkMetal });
            animator.strippingRoller = stripRot.transform;

            var calUpRot = CreatePivotChild(p07.transform, "CalenderUpper_Rot", new Vector3(calenderUpCenter.x, calenderUpCenter.y, 0f));
            AttachMesh(calUpRot, "CalenderRoller_Mesh", BuildSmoothRollerMesh(0.068f, 0.34f), new[] { _matDrumSteel, _matDarkMetal });
            animator.calenderUpper = calUpRot.transform;

            var calLowRot = CreatePivotChild(p07.transform, "CalenderLower_Rot", new Vector3(calenderLowCenter.x, calenderLowCenter.y, 0f));
            AttachMesh(calLowRot, "CalenderRollerLow_Mesh", BuildSmoothRollerMesh(0.068f, 0.34f), new[] { _matDrumSteel, _matDarkMetal });
            animator.calenderLower = calLowRot.transform;

            // Condenser Trumpet (flared warm ochre/copper horn) + Delivery Guide
            AttachMesh(p07, "CondenserTrumpet_Mesh", BuildCondenserAndBracketsMesh(stripCenter, calenderUpCenter, calenderLowCenter),
                new[] { _matAccentOchre, _matFrameSlate, _matDarkMetal });

            // Flowing Carded Cotton Web + Sliver Rope leading up into Coiler!
            var webGo = AttachMesh(p07, "CottonWeb_Mesh", BuildCottonWebAndSliverMesh(dofferCenter, stripCenter, calenderUpCenter, coilerCenterXZ),
                new[] { _matCottonWebAnimated, _matCottonIvory });
            animator.cottonWebRenderer = webGo.GetComponent<MeshRenderer>();
            flowAnim.cottonWebRenderer = animator.cottonWebRenderer;
            flowAnim.dofferCenterXY = dofferCenter;

            // Dynamic traveling sliver fiber pulses along sliver arc + fault-mode nep defect beads on Doffer web!
            Mesh pulseMesh = SaveMeshAsset("SliverPulseNode_Mesh", BuildSliverPulseNodeMesh());
            var pulseGroup = CreateSubRoot(p07.transform, "DynamicSliverFlow_Group");
            flowAnim.sliverFlowNodes = SpawnDynamicInstances(pulseGroup.transform, "SliverNode", 12, pulseMesh, _matCottonIvory);

            Mesh nepMesh = SaveMeshAsset("NepDefect_Mesh", BuildNepDefectMesh());
            var nepGroup = CreateSubRoot(p07.transform, "DynamicNepDefects_Group");
            flowAnim.nepDefectBeads = SpawnDynamicInstances(nepGroup.transform, "NepDefect", 10, nepMesh, _matNepDefect);

            // Fault 3D Visual A: Bulging tangled cotton wad jamming the brass Condenser Trumpet & wrapping Calender Rollers!
            var jamPivot = CreatePivotChild(p07.transform, "Fault_JamCottonClump", new Vector3(1.50f, 0.90f, -0.05f));
            AttachMesh(jamPivot, "FaultJamClump_Mesh", BuildJamCottonClumpMesh(), new[] { _matCottonIvory });
            jamPivot.SetActive(false);
            flowAnim.faultJamCottonClump = jamPivot;

            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "doffer",
                displayName = "道夫滚筒与棉网集束机构",
                description = "低速道夫从锡林表面凝聚均匀棉网，经剥棉罗拉、喇叭口与大压辊收束成连续生条。",
                root = p07.transform,
                explodedOffset = new Vector3(0.46f, 0.02f, -0.50f)
            });

            // --- PART 08: COILER HEAD & CUTAWAY SLIVER CAN WITH LIVE GROWING HELICAL COILS + COMPRESSING SPRING ---
            var p08 = CreateSubRoot(root.transform, "08_Coiler_SliverCan");
            AttachMesh(p08, "CoilerFrameAndCan_Mesh", BuildCoilerAndSliverCanMesh(coilerCenterXZ),
                new[] { _matHousingIvory, _matAccentMustard, _matAccentSageCan, _matDrumSteel, _matCutawayHatch, _matFrameSlate });

            var coilerDisc = CreatePivotChild(p08.transform, "CoilerDisc_Rot", new Vector3(coilerCenterXZ.x, 1.24f, coilerCenterXZ.y));
            AttachMesh(coilerDisc, "CoilerDisc_Mesh", BuildCoilerDiscMesh(), new[] { _matDrumSteel, _matAccentOchre });
            animator.coilerDisc = coilerDisc.transform;
            flowAnim.coilerDisc = coilerDisc.transform;
            flowAnim.coilerCenterXZ = coilerCenterXZ;

            // Moving Spring-Loaded Follower Disc + Visible Compressing Helical Steel Spring inside Cutaway Can!
            var followerObj = CreatePivotChild(p08.transform, "SliverFollowerPlate", new Vector3(coilerCenterXZ.x, 0.30f, coilerCenterXZ.y));
            AttachMesh(followerObj, "SliverFollowerPlate_Mesh", BuildSliverFollowerPlateMesh(), new[] { _matDrumSteel });
            flowAnim.sliverFollowerPlate = followerObj.transform;

            var springObj = CreatePivotChild(p08.transform, "CanInternalSpring", new Vector3(coilerCenterXZ.x, 0.095f, coilerCenterXZ.y));
            AttachMesh(springObj, "CanInternalSpring_Mesh", BuildCanInternalSpringMesh(), new[] { _matDrumSteel });
            flowAnim.canInternalSpring = springObj.transform;

            // 11 Progressively Stacking Helical Cotton Sliver Coils!
            var coilsGroup = CreateSubRoot(p08.transform, "DynamicSliverCoils_Group");
            flowAnim.sliverCoilLayers = new Transform[11];
            for (int c = 0; c < 11; c++)
            {
                Mesh layerMesh = SaveMeshAsset($"SliverCoilLayer_{c:D2}_Mesh", BuildSingleSliverCoilLayerMesh(c));
                var coilGo = new GameObject($"SliverCoil_{c:D2}");
                coilGo.transform.SetParent(coilsGroup.transform, false);
                coilGo.AddComponent<MeshFilter>().sharedMesh = layerMesh;
                var mr = coilGo.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { _matCottonIvory };
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                flowAnim.sliverCoilLayers[c] = coilGo.transform;
            }

            // Dynamic Falling Sliver Strand tracking the rotating eccentric brass Coiler Nozzle!
            var liveStrandObj = CreatePivotChild(p08.transform, "CoilerLiveStrand", new Vector3(coilerCenterXZ.x, 1.05f, coilerCenterXZ.y));
            AttachMesh(liveStrandObj, "CoilerLiveStrand_Mesh", BuildCoilerLiveStrandMesh(), new[] { _matCottonIvory });
            flowAnim.coilerLiveStrand = liveStrandObj.transform;

            // Fault 3D Visual C: Bulging overflowing sliver mass erupting out of the sliver can rim & choking Coiler Nozzle!
            var overflowPivot = CreatePivotChild(p08.transform, "Fault_CanOverflowMass", new Vector3(coilerCenterXZ.x, 1.07f, coilerCenterXZ.y));
            AttachMesh(overflowPivot, "FaultOverflowMass_Mesh", BuildCanOverflowMassMesh(), new[] { _matCottonIvory });
            overflowPivot.SetActive(false);
            flowAnim.faultCanOverflowMass = overflowPivot;

            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "coiler",
                displayName = "圈条器与剖视生条筒",
                description = "仿 TrainAR 绿罐造型打造半剖视条筒，直观展示生条在筒内螺旋盘叠的真实形态。",
                root = p08.transform,
                explodedOffset = new Vector3(0.84f, 0.0f, -0.18f)
            });

            // --- PART 09: CONTROL HMI CONSOLE, OVERHEAD SUCTION DUCT & LIVE 3-TIER SIGNAL BEACON ---
            var p09 = CreateSubRoot(root.transform, "09_Console_Ducts");
            AttachMesh(p09, "ConsoleAndDucts_Mesh", BuildConsoleAndDuctsMesh(mainCenter),
                new[] { _matHousingLightGray, _matFrameSlate, _matDarkMetal, _matHmiScreen, _matAccentMustard, _matBeaconGreen, _matBeaconAmber, _matBeaconRed });

            // Dynamic HDR emissive signal lens sleeves (driven via MaterialPropertyBlock for Green Normal / Amber Warning / Red Alarm!)
            Mesh lensMesh = SaveMeshAsset("BeaconLensOverlay_Mesh", BuildBeaconLensMesh());
            flowAnim.beaconGreenRenderer = SpawnBeaconLens(p09.transform, "Beacon_Green", new Vector3(0.54f, 2.31f, 0.38f), lensMesh, _matBeaconGreen);
            flowAnim.beaconAmberRenderer = SpawnBeaconLens(p09.transform, "Beacon_Amber", new Vector3(0.54f, 2.38f, 0.38f), lensMesh, _matBeaconAmber);
            flowAnim.beaconRedRenderer   = SpawnBeaconLens(p09.transform, "Beacon_Red",   new Vector3(0.54f, 2.45f, 0.38f), lensMesh, _matBeaconRed);

            var haloGo = new GameObject("BeaconHaloLight");
            haloGo.transform.SetParent(p09.transform, false);
            haloGo.transform.localPosition = new Vector3(0.54f, 2.41f, 0.30f);
            var haloLight = haloGo.AddComponent<Light>();
            haloLight.type = LightType.Point;
            haloLight.range = 1.65f;
            haloLight.intensity = 0.85f;
            haloLight.color = new Color(0.20f, 0.92f, 0.54f);
            haloLight.shadows = LightShadows.None;
            flowAnim.beaconGlowLight = haloLight;

            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "console_ducts",
                displayName = "HMI 控制屏与顶部吸尘风管",
                description = "集成工艺监控触摸屏、急停按钮、三色状态灯塔及负压除尘风管系统。",
                root = p09.transform,
                explodedOffset = new Vector3(0.18f, 0.58f, -0.22f)
            });

            // --- PART 10: SIDE DRIVE MOTOR, PULLEYS, GEARS & V-BELTS ---
            var p10 = CreateSubRoot(root.transform, "10_DriveTrain");
            AttachMesh(p10, "DriveTrain_Static_Mesh", BuildDriveTrainMesh(lickerCenter, mainCenter, dofferCenter),
                new[] { _matFrameSlate, _matDarkMetal, _matDrumSteel, _matBeltCharcoal, _matAccentOchre });
            var motorPulley = CreatePivotChild(p10.transform, "MotorPulley_Rot", new Vector3(-0.62f, 0.48f, 0.76f));
            AttachMesh(motorPulley, "MotorPulley_Mesh", BuildPulleyMesh(0.11f, 0.08f), new[] { _matDrumSteel, _matDarkMetal });
            animator.driveMotorPulley = motorPulley.transform;
            animator.parts.Add(new TrainARAssemblyPart
            {
                partId = "drivetrain",
                displayName = "后侧驱动电机与皮带齿轮系",
                description = "独立电机与多级带轮/齿轮传动组，驱动主锡林、刺辊与道夫保持精准工艺速比。",
                root = p10.transform,
                explodedOffset = new Vector3(0f, 0.0f, 0.76f)
            });

            // Position all 26 flat-top bars & dynamic cotton materials right away so Prefabs & EditMode look immediately alive!
            animator.CacheInitialTransforms();
            animator.BuildFlatTrackLut();
            animator.ApplyViewModeImmediate(TrainARViewMode.Cutaway);
            flowAnim.ApplyAllImmediate(0.68f, 0.35f);

            // 1. Save TrainAR Stylized Prefab (preserves original stylized ink-outline look)
            switcher.activeStyle = VisualRenderStyle.TrainARStylized;
            root.name = "ST03_CardingMachine_TrainAR";
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            // 2. Apply 100% native URP/Lit Realistic Smart-Factory PBR materials and save Realistic Prefab!
            switcher.ApplyStyle(VisualRenderStyle.RealisticPBR);
            root.name = "ST03_CardingMachine_RealisticPBR";
            GameObject realisticPrefab = PrefabUtility.SaveAsPrefabAsset(root, RealisticPrefabPath);

            UnityEngine.Object.DestroyImmediate(root);
            return realisticPrefab;
        }

        private static GameObject CreateSubRoot(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreatePivotChild(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        private static GameObject AttachMesh(GameObject host, string assetName, Mesh mesh, Material[] materials)
        {
            Mesh saved = SaveMeshAsset(assetName, mesh);
            var go = new GameObject(assetName);
            go.transform.SetParent(host.transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = saved;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materials;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;

            // Add clean BoxCollider fitted to mesh bounds so 3D mouse hover & click picking work accurately!
            var bc = go.AddComponent<BoxCollider>();
            bc.center = saved.bounds.center;
            bc.size = Vector3.Max(saved.bounds.size, new Vector3(0.05f, 0.05f, 0.05f));
            return go;
        }

        private static Mesh SaveMeshAsset(string assetName, Mesh mesh)
        {
            string path = $"{MeshDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.indexFormat = mesh.indexFormat;
                existing.vertices = mesh.vertices;
                existing.normals = mesh.normals;
                existing.uv = mesh.uv;
                existing.colors = mesh.colors;
                existing.tangents = mesh.tangents;
                existing.subMeshCount = mesh.subMeshCount;
                for (int s = 0; s < mesh.subMeshCount; s++)
                    existing.SetTriangles(mesh.GetTriangles(s), s);
                existing.RecalculateBounds();
                existing.UploadMeshData(false);
                UnityEngine.Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // =========================================================================
        // MESH BUILDERS FOR EACH SUB-ASSEMBLY
        // =========================================================================

        // Submeshes: 0=FrameSlate, 1=DarkMetal, 2=BeltCharcoal (rubber feet), 3=CutawayHatch
        private static Mesh BuildBaseFrameMesh(Vector2 lickerC, Vector2 mainC, Vector2 dofferC)
        {
            var mk = new TrainAR_MeshKit();

            // 6 heavy rubberized leveling feet sitting cleanly on Y = 0.00!
            float[] footXs = { -1.52f, -0.35f, 0.85f, 1.48f };
            float[] footZs = { -0.58f, 0.58f };
            foreach (float fx in footXs)
            {
                foreach (float fz in footZs)
                {
                    if (fx > 1.3f && Mathf.Abs(fz) > 0.5f) { /* keep front narrow plinth feet */ }
                    mk.AddBeveledCylinder(new Vector3(fx, 0.035f, fz), 0.085f, 0.07f, 18, 0.012f, null, 2);
                    mk.AddBeveledCylinder(new Vector3(fx, 0.09f, fz), 0.062f, 0.05f, 14, 0.008f, null, 1);
                }
            }

            // Main cast-iron heavy machine plinth (Y = 0.11..0.35)
            mk.AddBeveledBox(new Vector3(-0.08f, 0.23f, 0f), new Vector3(3.32f, 0.24f, 1.30f), 0.025f, null, 0, 0.78f, 0.98f);

            // Recessed front structural bays & ribs along plinth (-Z front face)
            for (int i = 0; i < 4; i++)
            {
                float rx = -1.25f + i * 0.82f;
                mk.AddBeveledBox(new Vector3(rx, 0.23f, -0.65f), new Vector3(0.56f, 0.14f, 0.04f), 0.01f, null, 1, 0.72f, 0.92f);
            }

            // Side vertical structural cheek rails (Back wall structural columns at Z = +0.56)
            mk.AddBeveledBox(new Vector3(-0.04f, 0.62f, 0.56f), new Vector3(2.96f, 0.56f, 0.12f), 0.018f, null, 0, 0.80f, 0.96f);

            // Front lower cutaway frame rail (low enough so Main Cylinder + Licker-in + Doffer are fully visible!)
            mk.AddBeveledBox(new Vector3(-0.04f, 0.42f, -0.56f), new Vector3(2.96f, 0.18f, 0.12f), 0.016f, null, 0, 0.82f, 0.98f);

            // Pillow-block bearing pedestals on front (+Z = 0.56) and rear (-Z = -0.56) rails supporting the 3 drums!
            AddBearingPedestal(mk, lickerC, 0.55f, 0.11f, 0.35f, 0, 1);
            AddBearingPedestal(mk, lickerC, -0.55f, 0.11f, 0.35f, 0, 1);

            AddBearingPedestal(mk, mainC, 0.56f, 0.16f, 0.35f, 0, 1);
            AddBearingPedestal(mk, mainC, -0.56f, 0.16f, 0.35f, 0, 1);

            AddBearingPedestal(mk, dofferC, 0.55f, 0.13f, 0.35f, 0, 1);
            AddBearingPedestal(mk, dofferC, -0.55f, 0.13f, 0.35f, 0, 1);

            // Front delivery pedestal bridge supporting condenser & calender rollers
            mk.AddBeveledBox(new Vector3(1.52f, 0.54f, -0.34f), new Vector3(0.32f, 0.42f, 0.10f), 0.014f, null, 0, 0.80f, 0.96f);
            mk.AddBeveledBox(new Vector3(1.52f, 0.54f, 0.34f), new Vector3(0.32f, 0.42f, 0.10f), 0.014f, null, 0, 0.80f, 0.96f);

            // Brushed stainless steel machine serial/spec nameplate mounted on front center plinth (-Z = -0.665)
            mk.AddBeveledBox(new Vector3(-0.04f, 0.23f, -0.662f), new Vector3(0.30f, 0.10f, 0.018f), 0.004f, null, 3, 0.92f, 1.0f);
            // 4 corner rivets on nameplate
            float[] rxOffsets = { -0.125f, 0.125f };
            float[] ryOffsets = { -0.032f, 0.032f };
            foreach (float rx in rxOffsets)
                foreach (float ry in ryOffsets)
                    mk.AddBeveledCylinder(new Vector3(-0.04f + rx, 0.23f + ry, -0.673f), 0.007f, 0.010f, 6, 0.002f, Quaternion.Euler(90f, 0f, 0f), 1);

            return mk.BuildMesh("BaseFrame_Mesh", 36f);
        }

        private static void AddBearingPedestal(TrainAR_MeshKit mk, Vector2 centerXY, float zPos, float outerR, float baseY, int colSub, int capSub)
        {
            float legH = Mathf.Max(0.12f, centerXY.y - baseY);
            // Tapered pedestal pillar rising from base rail to bearing centerline
            mk.AddBeveledBox(new Vector3(centerXY.x, baseY + legH * 0.5f, zPos),
                new Vector3(outerR * 1.45f, legH, 0.11f), 0.012f, null, colSub, 0.80f, 0.96f);
            // Circular pillow-block bearing housing + bolted split cap
            Quaternion axisZ = Quaternion.Euler(90f, 0f, 0f);
            mk.AddBeveledCylinder(new Vector3(centerXY.x, centerXY.y, zPos), outerR, 0.135f, 24, 0.012f, axisZ, capSub);
            mk.AddBeveledBox(new Vector3(centerXY.x, centerXY.y + outerR * 0.72f, zPos),
                new Vector3(outerR * 2.15f, outerR * 0.44f, 0.115f), 0.008f, null, capSub, 0.85f, 1.0f);

            // Realistic stainless hex clamping bolts on left/right ears of bearing cap (submesh 3)
            float boltOff = outerR * 0.86f;
            float boltY = centerXY.y + outerR * 0.98f;
            mk.AddBeveledCylinder(new Vector3(centerXY.x - boltOff, boltY, zPos), 0.016f, 0.026f, 6, 0.003f, null, 3);
            mk.AddBeveledCylinder(new Vector3(centerXY.x + boltOff, boltY, zPos), 0.016f, 0.026f, 6, 0.003f, null, 3);
            // Machined brass top lubricator / grease cup right on top of bearing cap (submesh 4)
            mk.AddBeveledCylinder(new Vector3(centerXY.x, centerXY.y + outerR * 1.06f, zPos), 0.014f, 0.034f, 10, 0.003f, null, 4);
        }

        // Submeshes: 0=HousingIvory, 1=CutawayHatch, 2=ChamberInterior, 3=AccentMustard, 4=HousingLightGray
        private static Mesh BuildCutawayHousingMesh(Vector2 mainC)
        {
            var mk = new TrainAR_MeshKit();

            // 1. Sculpted solid rear enclosure wall (Z = +0.51 .. +0.63) matching iconic carding silhouette!
            // Back side (+Z) = HousingIvory (0), Inner chamber face (-Z facing drums) = ChamberInterior (2)
            Vector2[] backSilhouette = new Vector2[]
            {
                new Vector2(-1.62f, 0.34f),
                new Vector2( 1.56f, 0.34f),
                new Vector2( 1.56f, 1.26f),
                new Vector2( 1.18f, 1.38f),
                new Vector2( 0.82f, 1.48f),
                new Vector2( 0.68f, 1.98f),
                new Vector2( 0.42f, 2.12f),
                new Vector2(-0.56f, 2.12f),
                new Vector2(-0.82f, 1.98f),
                new Vector2(-0.96f, 1.42f),
                new Vector2(-1.22f, 1.42f),
                new Vector2(-1.22f, 1.98f),
                new Vector2(-1.62f, 1.98f),
            };
            mk.AddExtrudedPolygonXY(backSilhouette, 0.51f, 0.63f, sideSubmesh: 0, frontCapSubmesh: 0, backCapSubmesh: 2, ao: 0.90f, uvScale: 0.65f);

            // Outer recessed decorative panel lines on rear exterior (+Z = +0.645)
            mk.AddBeveledBox(new Vector3(-0.04f, 1.24f, 0.645f), new Vector3(1.38f, 1.32f, 0.03f), 0.012f, null, 4, 0.86f, 0.98f);
            mk.AddBeveledBox(new Vector3(1.06f, 0.88f, 0.645f), new Vector3(0.64f, 0.82f, 0.03f), 0.012f, null, 4, 0.86f, 0.98f);

            // 2. Top Arched Cutaway Hood Shell (thick double-walled arch spanning Z = -0.22 .. +0.63!)
            // Front sliced edge at Z = -0.22 shows crisp 45° architectural cross-section hatching (Submesh 1)!
            Vector2[] topArchProfile = new Vector2[]
            {
                new Vector2(-0.96f, 1.34f),
                new Vector2(-0.88f, 1.34f),
                new Vector2(-0.75f, 1.93f),
                new Vector2(-0.52f, 2.04f),
                new Vector2( 0.38f, 2.04f),
                new Vector2( 0.61f, 1.93f),
                new Vector2( 0.74f, 1.40f),
                new Vector2( 1.42f, 1.22f),
                new Vector2( 1.56f, 1.22f),
                new Vector2( 1.56f, 1.32f),
                new Vector2( 0.82f, 1.52f),
                new Vector2( 0.70f, 2.00f),
                new Vector2( 0.42f, 2.14f),
                new Vector2(-0.56f, 2.14f),
                new Vector2(-0.84f, 2.00f),
            };
            mk.AddExtrudedPolygonXY(topArchProfile, -0.22f, 0.63f, sideSubmesh: 0, frontCapSubmesh: 0, backCapSubmesh: 1, ao: 0.95f, uvScale: 0.9f);

            // 3. Signature Warm Mustard-Yellow Crown Trim Band (directly echoing TrainAR Coffee maker!)
            Vector2[] mustardCrown = new Vector2[]
            {
                new Vector2(-0.84f, 2.00f),
                new Vector2(-0.56f, 2.14f),
                new Vector2( 0.42f, 2.14f),
                new Vector2( 0.70f, 2.00f),
                new Vector2( 0.68f, 2.07f),
                new Vector2( 0.44f, 2.20f),
                new Vector2(-0.58f, 2.20f),
                new Vector2(-0.82f, 2.07f),
            };
            mk.AddExtrudedPolygonXY(mustardCrown, -0.23f, 0.64f, sideSubmesh: 3, frontCapSubmesh: 3, backCapSubmesh: 3, ao: 0.98f, uvScale: 1.0f);

            // 4. Sculpted Front Architectural Cutaway Framing Cheeks (at Z = -0.62 .. -0.51)
            // Left framing pillar (leaves wide vertical window from X=-1.52..-1.24 so fluffy cotton batt shines clearly!)
            Vector2[] leftOuterPost = new Vector2[]
            {
                new Vector2(-1.62f, 0.346f),
                new Vector2(-1.14f, 0.346f),
                new Vector2(-1.14f, 0.74f),
                new Vector2(-1.52f, 0.74f),
                new Vector2(-1.52f, 1.98f),
                new Vector2(-1.62f, 1.98f),
            };
            mk.AddExtrudedPolygonXY(leftOuterPost, -0.62f, -0.51f, sideSubmesh: 1, frontCapSubmesh: 2, backCapSubmesh: 0, ao: 0.94f, uvScale: 0.85f);

            // Right cutaway delivery nose framing the condenser/calender zone:
            Vector2[] rightCutCheek = new Vector2[]
            {
                new Vector2(1.22f, 0.346f),
                new Vector2(1.56f, 0.346f),
                new Vector2(1.56f, 0.74f),
                new Vector2(1.36f, 0.68f),
                new Vector2(1.22f, 0.56f),
            };
            mk.AddExtrudedPolygonXY(rightCutCheek, -0.62f, -0.51f, sideSubmesh: 1, frontCapSubmesh: 2, backCapSubmesh: 0, ao: 0.92f, uvScale: 0.85f);

            // Lower front cutaway sill strip (clean 45° hatched cross-section edge running along the bottom window lip, zero overlap!)
            mk.AddBeveledBox(new Vector3(0.04f, 0.415f, -0.565f), new Vector3(2.352f, 0.136f, 0.104f), 0.012f, null, 1, 0.85f, 0.98f);

            // Left end wall closing the rear feed chute cleanly (-X = -1.62)
            mk.AddBeveledBox(new Vector3(-1.61f, 1.16f, 0.02f), new Vector3(0.09f, 1.64f, 1.24f), 0.018f, null, 0, 0.86f, 0.98f);

            // Front-right end nose housing wall (+X = 1.56, partial with opening for cotton web!)
            mk.AddBeveledBox(new Vector3(1.54f, 0.54f, 0.02f), new Vector3(0.09f, 0.40f, 1.24f), 0.016f, null, 0, 0.85f, 0.96f);

            // Curved warm-gray inner safety baffle behind main cylinder (adds Four-Stroke Engine depth!)
            mk.AddCurvedArchShellXY(mainC, 0.67f, 0.71f, 150f, 225f, 16, 0.45f, 0.51f, 2, 1, 2, 0.75f);

            return mk.BuildMesh("CutawayHousing_Mesh", 36f);
        }

        // Submeshes: 0=HousingIvory, 1=HousingLightGray, 2=AccentMustard, 3=DarkMetal
        private static Mesh BuildSolidFrontCoverMesh()
        {
            var mk = new TrainAR_MeshKit();

            // Full seamless front enclosure door covering the ENTIRE cutaway opening (sits cleanly at Z = -0.655 .. -0.555 -> zero Z-fighting!)
            Vector2[] frontDoorPoly = new Vector2[]
            {
                new Vector2(-1.625f, 0.355f),
                new Vector2( 1.565f, 0.355f),
                new Vector2( 1.565f, 1.32f),
                new Vector2( 0.82f, 1.52f),
                new Vector2( 0.70f, 2.00f),
                new Vector2( 0.42f, 2.14f),
                new Vector2(-0.56f, 2.14f),
                new Vector2(-0.84f, 2.00f),
                new Vector2(-0.96f, 1.44f),
                new Vector2(-1.20f, 1.44f),
                new Vector2(-1.20f, 1.98f),
                new Vector2(-1.625f, 1.98f),
            };
            mk.AddExtrudedPolygonXY(frontDoorPoly, -0.655f, -0.555f, sideSubmesh: 0, frontCapSubmesh: 1, backCapSubmesh: 0, ao: 0.95f, uvScale: 0.7f);

            // Full stepped roof closure spanning Z = -0.655 .. -0.22 (completely seals both shoulders & top arch!)
            Vector2[] frontRoof = new Vector2[]
            {
                new Vector2(-0.96f, 1.34f),
                new Vector2(-0.88f, 1.34f),
                new Vector2(-0.75f, 1.93f),
                new Vector2(-0.52f, 2.04f),
                new Vector2( 0.38f, 2.04f),
                new Vector2( 0.61f, 1.93f),
                new Vector2( 0.74f, 1.40f),
                new Vector2( 1.42f, 1.22f),
                new Vector2( 1.565f, 1.22f),
                new Vector2( 1.565f, 1.32f),
                new Vector2( 0.82f, 1.52f),
                new Vector2( 0.70f, 2.00f),
                new Vector2( 0.42f, 2.14f),
                new Vector2(-0.56f, 2.14f),
                new Vector2(-0.84f, 2.00f),
            };
            mk.AddExtrudedPolygonXY(frontRoof, -0.655f, -0.22f, sideSubmesh: 0, frontCapSubmesh: 1, backCapSubmesh: 0, ao: 0.96f);

            // Continuation of mustard crown strip all the way to front edge
            Vector2[] mustardFront = new Vector2[]
            {
                new Vector2(-0.84f, 2.00f),
                new Vector2(-0.56f, 2.14f),
                new Vector2( 0.42f, 2.14f),
                new Vector2( 0.70f, 2.00f),
                new Vector2( 0.68f, 2.07f),
                new Vector2( 0.44f, 2.20f),
                new Vector2(-0.58f, 2.20f),
                new Vector2(-0.82f, 2.07f),
            };
            mk.AddExtrudedPolygonXY(mustardFront, -0.66f, -0.23f, sideSubmesh: 2, frontCapSubmesh: 2, backCapSubmesh: 2, ao: 0.98f);

            // Stylized service access door panels & handles on front cover (-Z = -0.675)
            mk.AddBeveledBox(new Vector3(-0.04f, 1.18f, -0.672f), new Vector3(1.22f, 1.06f, 0.032f), 0.015f, null, 1, 0.90f, 1.0f);
            mk.AddBeveledBox(new Vector3(0.96f, 0.92f, -0.672f), new Vector3(0.56f, 0.68f, 0.032f), 0.014f, null, 1, 0.90f, 1.0f);
            // Feed tower inspection panel
            mk.AddBeveledBox(new Vector3(-1.41f, 1.38f, -0.672f), new Vector3(0.28f, 0.84f, 0.030f), 0.012f, null, 1, 0.90f, 1.0f);
            // Door handles + industrial hinges + cooling vent louvers on front solid cover
            mk.AddBeveledBox(new Vector3(0.46f, 1.18f, -0.702f), new Vector3(0.036f, 0.22f, 0.04f), 0.008f, null, 3);
            mk.AddBeveledBox(new Vector3(1.16f, 0.92f, -0.702f), new Vector3(0.036f, 0.18f, 0.04f), 0.008f, null, 3);
            // Heavy-duty vertical door hinges on left side of main door
            mk.AddBeveledCylinder(new Vector3(-0.65f, 1.52f, -0.688f), 0.016f, 0.09f, 12, 0.003f, null, 3);
            mk.AddBeveledCylinder(new Vector3(-0.65f, 0.84f, -0.688f), 0.016f, 0.09f, 12, 0.003f, null, 3);
            // Recessed cooling vent louvers on main door lower corner
            for (int lv = 0; lv < 5; lv++)
                mk.AddBeveledBox(new Vector3(-0.34f, 0.80f + lv * 0.045f, -0.690f), new Vector3(0.36f, 0.018f, 0.016f), 0.003f, null, 3);

            return mk.BuildMesh("SolidFrontCover_Mesh", 36f);
        }

        // Submeshes: 0=HousingLightGray, 1=CutawayHatch, 2=AccentMustard, 3=CottonIvory, 4=DarkMetal
        private static Mesh BuildFeedChuteStaticMesh()
        {
            var mk = new TrainAR_MeshKit();

            // Cutaway Feed Chute trunk walls (back +Z, left -X, right +X partial — wide open on -Z front so fluffy cotton batt is 100% visible!)
            mk.AddBeveledBox(new Vector3(-1.39f, 1.48f, 0.48f), new Vector3(0.42f, 1.08f, 0.06f), 0.01f, null, 0, 0.85f, 0.98f);
            mk.AddBeveledBox(new Vector3(-1.58f, 1.48f, 0.00f), new Vector3(0.06f, 1.08f, 1.02f), 0.01f, null, 0, 0.86f, 0.98f);
            // Right partition wall recessed cleanly behind front opening (Z = +0.02..+0.48) with 45° cutaway face at Z = +0.02!
            mk.AddBeveledBox(new Vector3(-1.19f, 1.54f, 0.24f), new Vector3(0.06f, 0.92f, 0.48f), 0.008f, null, 1, 0.85f, 0.98f);

            // Top flared feed hopper + mustard trim collar
            mk.AddBeveledBox(new Vector3(-1.39f, 2.04f, 0.0f), new Vector3(0.56f, 0.12f, 1.18f), 0.02f, null, 0, 0.90f, 1.0f);
            mk.AddBeveledBox(new Vector3(-1.39f, 2.13f, 0.0f), new Vector3(0.58f, 0.06f, 1.20f), 0.014f, null, 2, 0.94f, 1.0f);

            // Contoured steel Feed Plate (给棉板) guiding cotton from chute bottom into Licker-in!
            Vector2[] feedPlate = new Vector2[]
            {
                new Vector2(-1.56f, 0.78f),
                new Vector2(-1.14f, 0.78f),
                new Vector2(-1.05f, 0.86f),
                new Vector2(-1.12f, 0.89f),
                new Vector2(-1.44f, 0.86f),
                new Vector2(-1.56f, 0.96f),
            };
            mk.AddExtrudedPolygonXY(feedPlate, -0.47f, 0.47f, sideSubmesh: 4, frontCapSubmesh: 4, backCapSubmesh: 4, ao: 0.88f);

            // Note: Stacked raw cotton batt pillows are now 7 individual dynamically descending child layers (FeedBatt_00..06)!
            // Horizontal cotton lap tongue feeding smoothly under the feed roller toward Licker-in!
            Vector2[] lapPath = new Vector2[]
            {
                new Vector2(-1.42f, 1.00f),
                new Vector2(-1.28f, 0.92f),
                new Vector2(-1.14f, 0.89f),
                new Vector2(-1.06f, 0.90f),
            };
            float[] lapHalfWidths = { 0.42f, 0.43f, 0.43f, 0.42f };
            mk.AddTaperedRibbonXY(lapPath, lapHalfWidths, 0.075f, 3);

            return mk.BuildMesh("FeedChute_Mesh", 40f);
        }

        // Submeshes: 0=DrumSteel, 1=DarkMetal
        private static Mesh BuildFlutedRollerMesh(float radius, float length, int flutes)
        {
            var mk = new TrainAR_MeshKit();
            mk.AddGearZ(Vector3.zero, radius, length, flutes, radius * 0.16f, radius * 0.55f, 0);
            mk.AddBeveledCylinder(Vector3.zero, radius * 0.36f, length + 0.14f, 16, 0.008f, Quaternion.Euler(90f, 0f, 0f), 1);
            return mk.BuildMesh("FlutedRoller_Mesh", 36f);
        }

        private static Mesh BuildSmoothRollerMesh(float radius, float length)
        {
            var mk = new TrainAR_MeshKit();
            Quaternion axisZ = Quaternion.Euler(90f, 0f, 0f);
            mk.AddBeveledCylinder(Vector3.zero, radius, length, 24, 0.01f, axisZ, 0);
            mk.AddBeveledCylinder(Vector3.zero, radius * 0.38f, length + 0.22f, 16, 0.006f, axisZ, 1);
            return mk.BuildMesh("SmoothRoller_Mesh", 36f);
        }

        // Submeshes: 0=DarkMetal, 1=FrameSlate, 2=AccentOchre
        private static Mesh BuildLickerInStaticMesh(Vector2 lickerC)
        {
            var mk = new TrainAR_MeshKit();

            // Two crisp angled Mote Knives (除尘刀) under Licker-in!
            for (int k = 0; k < 2; k++)
            {
                float ang = (-132f + k * 24f) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(lickerC.x + Mathf.Cos(ang) * 0.27f, lickerC.y + Mathf.Sin(ang) * 0.27f, 0f);
                mk.AddBeveledBox(pos, new Vector3(0.028f, 0.11f, 0.94f), 0.004f,
                    Quaternion.Euler(0f, 0f, -35f + k * 22f), 2, 0.85f, 1.0f);
            }

            // Curved Licker-in Undercasing grid (刺辊漏底)
            mk.AddCurvedArchShellXY(lickerC, 0.24f, 0.275f, 232f, 326f, 14, -0.47f, 0.47f, 1, 0, 0, 0.80f);

            // Trash drop collector tray sitting low under mote knives (leaving wide open cutaway air gap Y=0.47..0.74 for falling seed-hull motes!)
            mk.AddBeveledBox(new Vector3(lickerC.x - 0.06f, 0.405f, 0f), new Vector3(0.40f, 0.13f, 0.90f), 0.012f, null, 1, 0.72f, 0.90f);

            return mk.BuildMesh("LickerIn_Static_Mesh", 36f);
        }

        // Submeshes: 0=CardClothing, 1=DarkMetal, 2=DrumSteel
        private static Mesh BuildLickerInDrumMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddIndustrialDrumZ(Vector3.zero, radius: 0.215f, length: 0.94f, radialSegments: 36,
                spokeCount: 5, shaftRadius: 0.048f, shaftOverhang: 0.12f,
                drumSubmesh: 0, webSubmesh: 1, shaftSubmesh: 2, toothedGrooves: true);
            return mk.BuildMesh("LickerIn_Drum_Mesh", 38f);
        }

        // Submeshes: 0=FrameSlate, 1=DarkMetal
        private static Mesh BuildMainCylinderUndercasingMesh(Vector2 mainC)
        {
            var mk = new TrainAR_MeshKit();
            // Sleek curved grid screen hugging the underside of the Main Cylinder
            mk.AddCurvedArchShellXY(mainC, 0.642f, 0.678f, 214f, 326f, 24, -0.47f, 0.47f, 0, 1, 1, 0.78f);
            // Cross ribs underneath for rich structural detail
            for (int i = 0; i < 5; i++)
            {
                float ang = Mathf.Lerp(226f, 314f, i / 4f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(mainC.x + Mathf.Cos(ang) * 0.695f, mainC.y + Mathf.Sin(ang) * 0.695f, 0f);
                mk.AddBeveledBox(p, new Vector3(0.032f, 0.048f, 0.92f), 0.005f,
                    Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg), 1, 0.75f, 0.92f);
            }
            return mk.BuildMesh("MainCylinder_Undercasing", 36f);
        }

        // Submeshes: 0=CardClothing, 1=FrameSlate (spoked cast-iron web), 2=DrumSteel (shaft & bolts)
        private static Mesh BuildMainCylinderDrumMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddIndustrialDrumZ(Vector3.zero, radius: 0.62f, length: 0.96f, radialSegments: 56,
                spokeCount: 6, shaftRadius: 0.075f, shaftOverhang: 0.13f,
                drumSubmesh: 0, webSubmesh: 1, shaftSubmesh: 2, toothedGrooves: false);
            return mk.BuildMesh("MainCylinder_Drum_Mesh", 38f);
        }

        // Submeshes: 0=FrameSlate, 1=DarkMetal, 2=AccentOchre
        private static Mesh BuildFlatTrackStructureMesh(Vector2 mainC)
        {
            var mk = new TrainAR_MeshKit();

            // Twin curved Flexible Bends (曲轨) flanking Main Cylinder at Z = -0.50 & +0.50
            mk.AddCurvedArchShellXY(mainC, 0.645f, 0.708f, 36f, 144f, 26, -0.525f, -0.482f, 0, 0, 1, 0.85f);
            mk.AddCurvedArchShellXY(mainC, 0.645f, 0.708f, 36f, 144f, 26,  0.482f,  0.525f, 0, 0, 1, 0.85f);

            // Top return guide rails supporting upper returning flat bars
            mk.AddBeveledBox(new Vector3(-0.04f, 1.90f, -0.505f), new Vector3(1.16f, 0.048f, 0.042f), 0.006f, null, 0);
            mk.AddBeveledBox(new Vector3(-0.04f, 1.90f,  0.505f), new Vector3(1.16f, 0.048f, 0.042f), 0.006f, null, 0);

            // Precision gauge adjustment screws on front cutaway flexible bend (-Z = -0.505)
            float[] adjDegs = { 55f, 90f, 125f };
            foreach (float deg in adjDegs)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector3 pFront = new Vector3(mainC.x + Mathf.Cos(rad) * 0.735f, mainC.y + Mathf.Sin(rad) * 0.735f, -0.505f);
                mk.AddBeveledCylinder(pFront, 0.022f, 0.08f, 10, 0.004f, Quaternion.Euler(0f, 0f, deg - 90f), 2);
            }

            // Twin upper return guide rollers at (-0.28, 1.86) and (+0.20, 1.86)
            Quaternion axisZ = Quaternion.Euler(90f, 0f, 0f);
            mk.AddBeveledCylinder(new Vector3(-0.28f, 1.85f, 0f), 0.055f, 1.04f, 20, 0.008f, axisZ, 1);
            mk.AddBeveledCylinder(new Vector3( 0.20f, 1.85f, 0f), 0.055f, 1.04f, 20, 0.008f, axisZ, 1);

            // A-frame support brackets holding front & rear flat sprockets
            float[] sx = { -0.62f, 0.54f };
            foreach (float x in sx)
            {
                mk.AddBeveledBox(new Vector3(x, 1.72f, -0.505f), new Vector3(0.085f, 0.40f, 0.055f), 0.008f, null, 0);
                mk.AddBeveledBox(new Vector3(x, 1.72f,  0.505f), new Vector3(0.085f, 0.40f, 0.055f), 0.008f, null, 0);
            }

            return mk.BuildMesh("Flats_TrackStructure", 36f);
        }

        private static Mesh BuildFlatSprocketPairMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddGearZ(new Vector3(0, 0, -0.505f), 0.115f, 0.045f, 12, 0.024f, 0.048f, 0);
            mk.AddGearZ(new Vector3(0, 0,  0.505f), 0.115f, 0.045f, 12, 0.024f, 0.048f, 0);
            mk.AddBeveledCylinder(Vector3.zero, 0.032f, 1.14f, 16, 0.005f, Quaternion.Euler(90f, 0f, 0f), 1);
            return mk.BuildMesh("FlatSprocketPair_Mesh", 36f);
        }

        // Single Revolving Flat-Top Bar (T-shaped aluminum beam + dark card-clothing underside + end sliders)
        // Submeshes: 0=FlatAluminum, 1=DarkMetal
        private static Mesh BuildSingleFlatBarMesh()
        {
            var mk = new TrainAR_MeshKit();
            // Card-wire backing plate facing cylinder (-Y)
            mk.AddBeveledBox(new Vector3(0f, -0.012f, 0f), new Vector3(0.058f, 0.020f, 0.96f), 0.003f, null, 1, 0.80f, 0.94f);
            // T-beam rigid aluminum rib (+Y)
            mk.AddBeveledBox(new Vector3(0f, 0.018f, 0f), new Vector3(0.024f, 0.044f, 0.98f), 0.004f, null, 0, 0.90f, 1.0f);
            // End chain links / slider shoes riding on flexible bends
            mk.AddBeveledBox(new Vector3(0f, 0.002f, -0.505f), new Vector3(0.064f, 0.036f, 0.045f), 0.005f, null, 1);
            mk.AddBeveledBox(new Vector3(0f, 0.002f,  0.505f), new Vector3(0.064f, 0.036f, 0.045f), 0.005f, null, 1);
            return mk.BuildMesh("FlatTopBar_Mesh", 36f);
        }

        // Submeshes: 0=CardClothing, 1=FrameSlate, 2=DrumSteel
        private static Mesh BuildDofferDrumMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddIndustrialDrumZ(Vector3.zero, radius: 0.35f, length: 0.96f, radialSegments: 44,
                spokeCount: 5, shaftRadius: 0.056f, shaftOverhang: 0.12f,
                drumSubmesh: 0, webSubmesh: 1, shaftSubmesh: 2, toothedGrooves: false);
            return mk.BuildMesh("Doffer_Drum_Mesh", 38f);
        }

        // Submeshes: 0=AccentOchre (Condenser Trumpet!), 1=FrameSlate, 2=DarkMetal
        private static Mesh BuildCondenserAndBracketsMesh(Vector2 stripC, Vector2 calUp, Vector2 calLow)
        {
            var mk = new TrainAR_MeshKit();

            // Flared warm terracotta-ochre Condenser Trumpet (集棉喇叭口) oriented horizontally along +X!
            Vector2[] trumpetProfile = new Vector2[]
            {
                new Vector2(0.092f, -0.075f),
                new Vector2(0.088f, -0.055f),
                new Vector2(0.052f, -0.015f),
                new Vector2(0.034f,  0.035f),
                new Vector2(0.038f,  0.065f),
                new Vector2(0.022f,  0.065f),
                new Vector2(0.022f, -0.050f),
                new Vector2(0.074f, -0.075f),
            };
            Quaternion alongX = Quaternion.Euler(0f, 0f, -90f);
            mk.AddLathe(trumpetProfile, 24, new Vector3(1.495f, 0.86f, 0f), alongX, 0, 0.88f, 1.0f);

            // Side mounting cheeks for stripping roller & calender crush rollers
            mk.AddBeveledBox(new Vector3(stripC.x, stripC.y, -0.50f), new Vector3(0.14f, 0.14f, 0.06f), 0.01f, null, 1);
            mk.AddBeveledBox(new Vector3(stripC.x, stripC.y,  0.50f), new Vector3(0.14f, 0.14f, 0.06f), 0.01f, null, 1);

            // Calender roller frame housing
            mk.AddBeveledBox(new Vector3(calUp.x, 0.86f, -0.22f), new Vector3(0.19f, 0.34f, 0.06f), 0.01f, null, 1);
            mk.AddBeveledBox(new Vector3(calUp.x, 0.86f,  0.22f), new Vector3(0.19f, 0.34f, 0.06f), 0.01f, null, 1);
            // Calender spring pressure knobs + stacked steel compression coil rings on top
            float[] calZs = { -0.22f, 0.22f };
            foreach (float cz in calZs)
            {
                mk.AddBeveledCylinder(new Vector3(calUp.x, 1.07f, cz), 0.022f, 0.11f, 14, 0.004f, null, 2);
                for (int r = 0; r < 4; r++)
                    mk.AddBeveledCylinder(new Vector3(calUp.x, 1.038f + r * 0.017f, cz), 0.036f, 0.009f, 14, 0.002f, null, 0);
                mk.AddBeveledCylinder(new Vector3(calUp.x, 1.125f, cz), 0.038f, 0.022f, 6, 0.004f, null, 2);
            }

            return mk.BuildMesh("CondenserTrumpet_Mesh", 36f);
        }

        // Submeshes: 0=CottonWebAnimated (wide tapering web), 1=CottonIvory (round sliver rope rising to coiler)
        private static Mesh BuildCottonWebAndSliverMesh(Vector2 dofferC, Vector2 stripC, Vector2 calUp, Vector2 coilerXZ)
        {
            var mk = new TrainAR_MeshKit();

            // Continuous carded cotton web peeling from upper-front of Doffer, gliding over stripping roller,
            // and tapering smoothly into the Condenser Trumpet!
            var webPts = new List<Vector2>();
            var webHalfW = new List<float>();

            // Hug doffer arc from 105 deg -> 12 deg
            for (int i = 0; i <= 10; i++)
            {
                float ang = Mathf.Lerp(105f, 12f, i / 10f) * Mathf.Deg2Rad;
                webPts.Add(dofferC + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.362f);
                webHalfW.Add(0.43f);
            }
            // Bridge across stripping roller & funnel cleanly into Condenser Trumpet!
            webPts.Add(new Vector2(stripC.x, stripC.y + 0.085f)); webHalfW.Add(0.39f);
            webPts.Add(new Vector2(1.43f, 0.89f));                webHalfW.Add(0.24f);
            webPts.Add(new Vector2(1.47f, 0.865f));               webHalfW.Add(0.085f);
            webPts.Add(new Vector2(1.52f, 0.86f));                webHalfW.Add(0.028f);

            mk.AddTaperedRibbonXY(webPts.ToArray(), webHalfW.ToArray(), 0.018f, 0);

            // Continuous round Card Sliver (生条) emerging from calender crush rollers -> arching cleanly OUTSIDE the sliver can & OVER the coiler rim into the top central funnel!
            Vector3[] sliverPath = new Vector3[]
            {
                new Vector3(1.52f, 0.86f, 0.00f),
                new Vector3(1.57f, 0.89f, 0.07f),
                new Vector3(1.58f, 1.08f, 0.16f),
                new Vector3(1.60f, 1.32f, 0.22f),
                new Vector3(1.68f, 1.54f, 0.16f),
                new Vector3(1.83f, 1.62f, 0.06f),
                new Vector3(coilerXZ.x, 1.60f, 0.00f),
                new Vector3(coilerXZ.x, 1.50f, 0.00f),
            };
            float[] sliverRadii = new float[sliverPath.Length];
            for (int i = 0; i < sliverRadii.Length; i++) sliverRadii[i] = 0.023f;
            mk.AddSweptTube(sliverPath, sliverRadii, 12, 1, false, true, 2.2f, 0.98f);

            return mk.BuildMesh("CottonWeb_Mesh", 42f);
        }

        // Submeshes: 0=HousingIvory, 1=AccentMustard, 2=AccentSageCan, 3=DrumSteel, 4=CutawayHatch, 5=FrameSlate
        private static Mesh BuildCoilerAndSliverCanMesh(Vector2 coilerXZ)
        {
            var mk = new TrainAR_MeshKit();
            Vector3 cBase = new Vector3(coilerXZ.x, 0f, coilerXZ.y);

            // 1. Cantilevered Coiler Bridge & Pedestal Column (positioned cleanly behind can at Z = +0.44)
            mk.AddBeveledBox(new Vector3(coilerXZ.x - 0.12f, 0.72f, 0.44f), new Vector3(0.24f, 1.38f, 0.16f), 0.018f, null, 5, 0.82f, 0.98f);
            mk.AddBeveledBox(new Vector3(coilerXZ.x - 0.12f, 0.06f, 0.44f), new Vector3(0.36f, 0.12f, 0.28f), 0.014f, null, 5, 0.78f, 0.94f);
            // Cantilever top arm extending over the sliver can
            mk.AddBeveledBox(new Vector3(coilerXZ.x - 0.06f, 1.36f, 0.18f), new Vector3(0.36f, 0.14f, 0.66f), 0.018f, null, 0, 0.90f, 1.0f);
            // Overhead cylindrical Coiler Head housing + crown ring!
            mk.AddBeveledCylinder(cBase + new Vector3(0f, 1.37f, 0f), 0.34f, 0.22f, 36, 0.02f, null, 0);
            mk.AddBeveledCylinder(cBase + new Vector3(0f, 1.48f, 0f), 0.355f, 0.048f, 36, 0.01f, null, 1);
            // Top sliver funnel cap
            mk.AddBeveledCylinder(cBase + new Vector3(0f, 1.54f, 0f), 0.085f, 0.09f, 20, 0.012f, null, 3);

            // 2. Hollow Cutaway Sliver Can Shell (104° front-right viewing window)
            float canR = 0.33f;
            float canWall = 0.024f;
            float canH = 1.12f;

            Vector2[] canProfile = new Vector2[]
            {
                new Vector2(canR - canWall, 0.08f),
                new Vector2(canR, 0.08f),
                new Vector2(canR, canH - 0.04f),
                new Vector2(canR - canWall, canH - 0.04f),
                new Vector2(canR - canWall, 0.08f),
            };
            mk.AddLathe(canProfile, 36, cBase, Quaternion.identity,
                submesh: 2, aoMin: 0.84f, aoMax: 1.0f, uRepeat: 1.0f, vRepeat: 1.0f,
                startAngleDeg: -18f, endAngleDeg: 238f, capCutEnds: true, cutSubmesh: 4);

            // Brushed stainless steel top rim collar
            Vector2[] rimProfile = new Vector2[]
            {
                new Vector2(canR - canWall - 0.006f, canH - 0.045f),
                new Vector2(canR + 0.014f, canH - 0.045f),
                new Vector2(canR + 0.014f, canH + 0.008f),
                new Vector2(canR - canWall - 0.006f, canH + 0.008f),
                new Vector2(canR - canWall - 0.006f, canH - 0.045f),
            };
            mk.AddLathe(rimProfile, 36, cBase, Quaternion.identity,
                submesh: 3, aoMin: 0.92f, aoMax: 1.0f, uRepeat: 1f, vRepeat: 1f,
                startAngleDeg: -18f, endAngleDeg: 238f, capCutEnds: true, cutSubmesh: 4);

            // Sturdy full circular dark caster base plate sitting cleanly on Y = 0
            // (Moving Follower Plate, Compressing Spring, 11 Coils & Live Strand are dynamic child Transforms!)
            mk.AddBeveledCylinder(cBase + new Vector3(0f, 0.045f, 0f), canR + 0.012f, 0.09f, 36, 0.012f, null, 5);

            return mk.BuildMesh("CoilerFrameAndCan_Mesh", 40f);
        }

        // =========================================================================
        // DYNAMIC RAW-MATERIAL & OUTPUT-PRODUCT MESH BUILDERS
        // =========================================================================
        private static Transform[] SpawnDynamicInstances(Transform parent, string baseName, int count, Mesh mesh, Material mat)
        {
            var list = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"{baseName}_{i:D2}");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { mat };
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                list[i] = go.transform;
            }
            return list;
        }

        private static Mesh BuildSingleFeedBattLayerMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddOrganicPillow(Vector3.zero, new Vector3(0.125f, 0.088f, 0.39f), 1.7f, 0);
            return mk.BuildMesh("FeedBattLayer_Mesh", 55f);
        }

        private static Mesh BuildImpurityMoteMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddIcosphere(Vector3.zero, new Vector3(0.022f, 0.012f, 0.018f), 0, 0, 0.92f);
            return mk.BuildMesh("ImpurityMote_Mesh", 30f);
        }

        private static Mesh BuildCardingTuftMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddIcosphere(Vector3.zero, new Vector3(0.032f, 0.016f, 0.042f), 1, 0, 0.98f);
            return mk.BuildMesh("CardingTuft_Mesh", 55f);
        }

        private static Mesh BuildSliverPulseNodeMesh()
        {
            var mk = new TrainAR_MeshKit();
            // Sleek tapered fibrous pulse bead gliding along the round card sliver rope
            mk.AddIcosphere(Vector3.zero, new Vector3(0.029f, 0.029f, 0.052f), 1, 0, 0.98f);
            return mk.BuildMesh("SliverPulseNode_Mesh", 55f);
        }

        private static Mesh BuildNepDefectMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddIcosphere(Vector3.zero, new Vector3(0.018f, 0.014f, 0.018f), 1, 0, 0.95f);
            return mk.BuildMesh("NepDefect_Mesh", 50f);
        }

        private static Mesh BuildSliverFollowerPlateMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddBeveledCylinder(Vector3.zero, 0.292f, 0.032f, 32, 0.006f, null, 0);
            return mk.BuildMesh("SliverFollowerPlate_Mesh", 36f);
        }

        private static Mesh BuildCanInternalSpringMesh()
        {
            // Unit-height (Y = 0..1) 6-turn helical machined steel compression spring beneath follower disc!
            var mk = new TrainAR_MeshKit();
            int turns = 6;
            int totalPts = turns * 18;
            var pts = new Vector3[totalPts];
            var radii = new float[totalPts];
            for (int i = 0; i < totalPts; i++)
            {
                float t = i / (float)(totalPts - 1);
                float ang = t * turns * Mathf.PI * 2f;
                float r = 0.145f;
                pts[i] = new Vector3(Mathf.Cos(ang) * r, t, Mathf.Sin(ang) * r);
                radii[i] = 0.014f;
            }
            mk.AddSweptTube(pts, radii, 8, 0, false, true, 4f, 0.90f);
            return mk.BuildMesh("CanInternalSpring_Mesh", 45f);
        }

        private static Mesh BuildSingleSliverCoilLayerMesh(int layerIdx)
        {
            // Single 360-deg eccentric cycloidal cotton sliver coil loop centered around local origin
            var mk = new TrainAR_MeshKit();
            int segs = 28;
            float phase = layerIdx * 0.72f;
            float eccX = Mathf.Cos(phase) * 0.048f;
            float eccZ = Mathf.Sin(phase) * 0.048f;
            var pts = new Vector3[segs];
            var radii = new float[segs];
            for (int s = 0; s < segs; s++)
            {
                float ang = (s / (float)segs) * Mathf.PI * 2f;
                float loopR = 0.185f + Mathf.Sin(ang * 2f) * 0.018f;
                pts[s] = new Vector3(eccX + Mathf.Cos(ang) * loopR, (s / (float)segs) * 0.012f, eccZ + Mathf.Sin(ang) * loopR);
                radii[s] = 0.034f;
            }
            mk.AddSweptTube(pts, radii, 10, 0, closedLoop: true, capEnds: false, uvVScale: 2.5f, ao: 0.96f);
            return mk.BuildMesh($"SliverCoilLayer_{layerIdx:D2}_Mesh", 45f);
        }

        private static Mesh BuildCoilerLiveStrandMesh()
        {
            // Unit-length (Y = -0.5..+0.5) cotton sliver strand connecting rotating Coiler Nozzle to top coil
            var mk = new TrainAR_MeshKit();
            mk.AddBeveledCylinder(Vector3.zero, 0.026f, 1.0f, 12, 0.004f, null, 0, 1f, 3f);
            return mk.BuildMesh("CoilerLiveStrand_Mesh", 45f);
        }

        private static Renderer SpawnBeaconLens(Transform parent, string name, Vector3 localPos, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { mat };
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        private static Mesh BuildBeaconLensMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddBeveledCylinder(Vector3.zero, 0.0445f, 0.066f, 20, 0.006f, null, 0);
            return mk.BuildMesh("BeaconLensOverlay_Mesh", 45f);
        }

        private static Mesh BuildJamCottonClumpMesh()
        {
            // Sculpted dramatic tangled cotton wad bulging over Condenser Trumpet & wrapping upper/lower Calender Crush Rollers!
            var mk = new TrainAR_MeshKit();
            mk.AddOrganicPillow(new Vector3(-0.05f, 0.04f, -0.06f), new Vector3(0.125f, 0.130f, 0.21f), 2.3f, 0);
            mk.AddOrganicPillow(new Vector3(0.08f, 0.07f, -0.10f), new Vector3(0.115f, 0.105f, 0.22f), 5.1f, 0);
            mk.AddOrganicPillow(new Vector3(0.08f, -0.09f, -0.05f), new Vector3(0.105f, 0.092f, 0.20f), 8.4f, 0);
            mk.AddIcosphere(new Vector3(-0.14f, 0.06f, -0.15f), new Vector3(0.075f, 0.068f, 0.085f), 1, 0, 0.96f);
            mk.AddIcosphere(new Vector3(0.10f, 0.02f, -0.22f), new Vector3(0.072f, 0.078f, 0.080f), 1, 0, 0.96f);
            return mk.BuildMesh("FaultJamClump_Mesh", 55f);
        }

        private static Mesh BuildCanOverflowMassMesh()
        {
            // Dramatic bulging chaotic sliver overflow bursting over the top rim of the sliver can & choking the Coiler Nozzle!
            var mk = new TrainAR_MeshKit();
            mk.AddOrganicPillow(new Vector3(0.00f, 0.03f, -0.06f), new Vector3(0.24f, 0.085f, 0.22f), 3.2f, 0);
            mk.AddOrganicPillow(new Vector3(0.09f, 0.09f, -0.13f), new Vector3(0.16f, 0.080f, 0.16f), 6.7f, 0);
            mk.AddOrganicPillow(new Vector3(-0.08f, 0.07f, -0.15f), new Vector3(0.15f, 0.075f, 0.14f), 9.1f, 0);
            // Spilling tangled sliver loop hanging outside the front cutaway rim!
            Vector3[] spillPath = new Vector3[]
            {
                new Vector3(0.04f, 0.14f, -0.04f),
                new Vector3(0.15f, 0.08f, -0.22f),
                new Vector3(0.08f, -0.04f, -0.34f),
                new Vector3(-0.08f, -0.11f, -0.33f),
                new Vector3(-0.17f, 0.02f, -0.24f),
                new Vector3(-0.06f, 0.11f, -0.08f),
            };
            float[] spillR = { 0.036f, 0.038f, 0.040f, 0.038f, 0.036f, 0.034f };
            mk.AddSweptTube(spillPath, spillR, 10, 0, closedLoop: false, capEnds: true, uvVScale: 2.5f, ao: 0.96f);
            return mk.BuildMesh("FaultOverflowMass_Mesh", 55f);
        }

        // Submeshes: 0=DrumSteel, 1=AccentOchre
        private static Mesh BuildCoilerDiscMesh()
        {
            var mk = new TrainAR_MeshKit();
            mk.AddBeveledCylinder(Vector3.zero, 0.29f, 0.042f, 32, 0.008f, null, 0);
            // Eccentric angled sliver delivery nozzle
            mk.AddBeveledCylinder(new Vector3(0.08f, -0.02f, -0.02f), 0.042f, 0.09f, 16, 0.008f, Quaternion.Euler(0f, 0f, -16f), 1);
            return mk.BuildMesh("CoilerDisc_Mesh", 36f);
        }

        // Submeshes: 0=HousingLightGray, 1=FrameSlate, 2=DarkMetal, 3=HmiScreen, 4=AccentMustard, 5=Green, 6=Amber, 7=Red
        private static Mesh BuildConsoleAndDuctsMesh(Vector2 mainC)
        {
            var mk = new TrainAR_MeshKit();

            // 1. Sleek Swivel HMI Control Panel mounted cleanly on front-right delivery shoulder (clears Solid cover cleanly!)
            Vector3 armBase = new Vector3(1.28f, 1.24f, -0.66f);
            mk.AddBeveledCylinder(armBase, 0.030f, 0.30f, 14, 0.005f, null, 1);
            Vector3 hmiCenter = new Vector3(1.30f, 1.46f, -0.71f);
            Quaternion hmiRot = Quaternion.Euler(-14f, 160f, 0f); // faces upward toward front-right (-Z/+X) camera cleanly!
            mk.AddBeveledBox(hmiCenter, new Vector3(0.40f, 0.29f, 0.062f), 0.014f, hmiRot, 1, 0.88f, 1.0f);

            // Screen quad with exact 0..1 UVs facing outward (+Z of hmiRot -> toward -Z camera!)
            Vector3 scrO = hmiCenter + hmiRot * new Vector3(0f, 0.015f, 0.034f);
            Vector3 r = hmiRot * Vector3.right * 0.168f;
            Vector3 u = hmiRot * Vector3.up * 0.106f;
            mk.AddQuadUV(scrO - r - u, scrO + r - u, scrO + r + u, scrO - r + u,
                new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), 3, 1.0f, true);

            // Physical tactile Start button (green) & Emergency Stop mushroom button (red + yellow collar)
            Vector3 eStopPos = hmiCenter + hmiRot * new Vector3(-0.13f, -0.114f, 0.038f);
            mk.AddBeveledCylinder(eStopPos, 0.023f, 0.016f, 14, 0.003f, hmiRot * Quaternion.Euler(90f, 0f, 0f), 4);
            mk.AddBeveledCylinder(eStopPos + hmiRot * new Vector3(0, 0, 0.015f), 0.018f, 0.018f, 14, 0.005f, hmiRot * Quaternion.Euler(90f, 0f, 0f), 7);
            Vector3 startPos = hmiCenter + hmiRot * new Vector3(-0.07f, -0.114f, 0.036f);
            mk.AddBeveledCylinder(startPos, 0.014f, 0.014f, 12, 0.003f, hmiRot * Quaternion.Euler(90f, 0f, 0f), 5);

            // 2. Compact Sculptural Overhead Negative-Pressure Suction Hood & Curved Exhaust Duct (tops neatly at Y=2.48!)
            mk.AddBeveledBox(new Vector3(mainC.x, 2.21f, 0.18f), new Vector3(0.50f, 0.09f, 0.44f), 0.016f, null, 0);
            Vector3[] ductPath = new Vector3[]
            {
                new Vector3(mainC.x, 2.23f, 0.18f),
                new Vector3(mainC.x, 2.38f, 0.18f),
                new Vector3(mainC.x - 0.08f, 2.46f, 0.24f),
                new Vector3(mainC.x - 0.26f, 2.49f, 0.34f),
                new Vector3(mainC.x - 0.46f, 2.49f, 0.42f),
            };
            float[] ductR = { 0.115f, 0.112f, 0.112f, 0.112f, 0.112f };
            mk.AddSweptTube(ductPath, ductR, 24, 0, false, true, 1.0f, 0.96f);

            // Bolted ring flanges along duct (with 8 radial hex flange bolts!)
            mk.AddBeveledCylinder(new Vector3(mainC.x, 2.32f, 0.18f), 0.148f, 0.034f, 24, 0.006f, null, 1);
            for (int b = 0; b < 8; b++)
            {
                float a = (b / 8f) * Mathf.PI * 2f;
                mk.AddBeveledCylinder(new Vector3(mainC.x + Mathf.Cos(a) * 0.132f, 2.342f, 0.18f + Mathf.Sin(a) * 0.132f), 0.011f, 0.016f, 6, 0.002f, null, 2);
            }
            mk.AddBeveledCylinder(new Vector3(mainC.x - 0.46f, 2.49f, 0.42f), 0.148f, 0.034f, 24, 0.006f, Quaternion.Euler(0f, -22f, 90f), 1);

            // 3. Sleek 3-Tier Industrial Status Signal Tower on top-right hood corner (tops neatly at Y=2.48!)
            Vector3 towerBase = new Vector3(0.54f, 2.13f, 0.38f);
            mk.AddBeveledCylinder(towerBase + new Vector3(0, 0.07f, 0), 0.018f, 0.16f, 12, 0.003f, null, 2);
            mk.AddBeveledCylinder(towerBase + new Vector3(0, 0.18f, 0), 0.042f, 0.068f, 18, 0.006f, null, 5); // Green
            mk.AddBeveledCylinder(towerBase + new Vector3(0, 0.25f, 0), 0.042f, 0.068f, 18, 0.006f, null, 6); // Amber
            mk.AddBeveledCylinder(towerBase + new Vector3(0, 0.32f, 0), 0.042f, 0.068f, 18, 0.006f, null, 7); // Red
            mk.AddBeveledCylinder(towerBase + new Vector3(0, 0.365f, 0), 0.044f, 0.022f, 18, 0.005f, null, 2); // Cap

            return mk.BuildMesh("ConsoleAndDucts_Mesh", 36f);
        }

        // Submeshes: 0=FrameSlate, 1=DarkMetal, 2=DrumSteel, 3=BeltCharcoal, 4=AccentOchre
        private static Mesh BuildDriveTrainMesh(Vector2 lickerC, Vector2 mainC, Vector2 dofferC)
        {
            var mk = new TrainAR_MeshKit();
            float zPlane = 0.75f;
            Quaternion axisZ = Quaternion.Euler(90f, 0f, 0f);

            // 1. Ribbed Electric Drive Motor on Rear Side Rail (+Z = +0.52)
            Vector3 motorPos = new Vector3(-0.62f, 0.48f, 0.52f);
            mk.AddBeveledCylinder(motorPos, 0.18f, 0.42f, 24, 0.02f, axisZ, 0);
            for (int f = 0; f < 10; f++)
            {
                float ang = (f / 10f) * 360f;
                float rad = ang * Mathf.Deg2Rad;
                Vector3 fp = motorPos + new Vector3(Mathf.Cos(rad) * 0.188f, Mathf.Sin(rad) * 0.188f, 0f);
                mk.AddBeveledBox(fp, new Vector3(0.025f, 0.016f, 0.34f), 0.003f, Quaternion.Euler(0, 0, ang), 1);
            }
            mk.AddBeveledBox(motorPos + new Vector3(0f, 0.20f, -0.02f), new Vector3(0.14f, 0.09f, 0.15f), 0.01f, null, 4);

            // Rear shaft stubs connecting rear bearings out to pulleys & gears
            mk.AddBeveledCylinder(new Vector3(mainC.x, mainC.y, 0.65f), 0.072f, 0.28f, 20, 0.008f, axisZ, 2);
            mk.AddBeveledCylinder(new Vector3(lickerC.x, lickerC.y, 0.65f), 0.048f, 0.26f, 18, 0.008f, axisZ, 2);
            mk.AddBeveledCylinder(new Vector3(dofferC.x, dofferC.y, 0.65f), 0.056f, 0.26f, 18, 0.008f, axisZ, 2);

            // 2. Large Main Cylinder Flywheel Pulley + Licker-in Pulley + Doffer Drive Gear
            mk.AddBeveledCylinder(new Vector3(mainC.x, mainC.y, zPlane), 0.29f, 0.075f, 32, 0.012f, axisZ, 2);
            mk.AddBeveledCylinder(new Vector3(mainC.x, mainC.y, zPlane + 0.02f), 0.22f, 0.095f, 24, 0.008f, axisZ, 1);

            mk.AddBeveledCylinder(new Vector3(lickerC.x, lickerC.y, zPlane), 0.14f, 0.07f, 24, 0.01f, axisZ, 2);
            mk.AddGearZ(new Vector3(dofferC.x, dofferC.y, zPlane), 0.20f, 0.06f, 20, 0.024f, 0.07f, 1);
            mk.AddGearZ(new Vector3(0.56f, 0.84f, zPlane), 0.14f, 0.055f, 14, 0.022f, 0.05f, 4);

            // 3. Tensioned V-Belt loops
            AddBeltLoop(mk, new Vector2(-0.62f, 0.48f), 0.115f, mainC, 0.295f, zPlane, 3);
            AddBeltLoop(mk, lickerC, 0.145f, mainC, 0.24f, zPlane + 0.05f, 3);

            return mk.BuildMesh("DriveTrain_Static_Mesh", 36f);
        }

        private static void AddBeltLoop(TrainAR_MeshKit mk, Vector2 c0, float r0, Vector2 c1, float r1, float z, int submesh)
        {
            Vector2 dir = (c1 - c0).normalized;
            float baseAng = Mathf.Atan2(dir.y, dir.x);
            var pts = new List<Vector3>();

            // Arc around c1 (front semicircle)
            for (int i = 0; i <= 10; i++)
            {
                float a = baseAng - Mathf.PI * 0.5f + (i / 10f) * Mathf.PI;
                pts.Add(new Vector3(c1.x + Mathf.Cos(a) * r1, c1.y + Mathf.Sin(a) * r1, z));
            }
            // Arc around c0 (back semicircle)
            for (int i = 0; i <= 10; i++)
            {
                float a = baseAng + Mathf.PI * 0.5f + (i / 10f) * Mathf.PI;
                pts.Add(new Vector3(c0.x + Mathf.Cos(a) * r0, c0.y + Mathf.Sin(a) * r0, z));
            }

            float[] radii = new float[pts.Count];
            for (int i = 0; i < radii.Length; i++) radii[i] = 0.018f;
            mk.AddSweptTube(pts.ToArray(), radii, 8, submesh, closedLoop: true, capEnds: false, 1f, 0.90f);
        }

        private static Mesh BuildPulleyMesh(float radius, float width)
        {
            var mk = new TrainAR_MeshKit();
            Quaternion axisZ = Quaternion.Euler(90f, 0f, 0f);
            mk.AddBeveledCylinder(Vector3.zero, radius, width, 24, 0.01f, axisZ, 0);
            mk.AddBeveledCylinder(Vector3.zero, radius * 0.45f, width * 1.3f, 16, 0.006f, axisZ, 1);
            return mk.BuildMesh("MotorPulley_Mesh", 36f);
        }

        // =========================================================================
        // 4. REALISTIC SMART-FACTORY SHOWROOM SCENE (WITH 1-CLICK TRAINAR TOGGLE)
        // =========================================================================
        private static VolumeProfile CreateOrUpdateShowroomVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }
            profile.components.Clear();

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.overrideState = true;
            tonemap.mode.value = TonemappingMode.ACES;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.52f; // ONLY HDR LEDs bloom — zero white-housing washout!
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.22f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.56f;

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.08f; // Bright, clean, crisp industrial showroom — neither blown out nor dark!
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 5f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 5f;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void BuildShowcaseScene(GameObject realisticCardingPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var factoryCube = AssetDatabase.LoadAssetAtPath<Cubemap>(TrainAR_PbrTextureGenerator.CubemapPath);

            // Bright, Clean Industrial Showroom Lighting (matches user's favorite luminous balanced look!)
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = factoryCube;
            RenderSettings.reflectionIntensity = 0.95f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.78f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.62f, 0.70f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.40f, 0.46f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.81f, 0.85f, 0.90f, 1f);
            RenderSettings.fogStartDistance = 11.5f;
            RenderSettings.fogEndDistance = 30.0f;

            // Showroom Floor (Real Epoxy PBR Floor by default; switches to Pure-White Shadow Catcher in TrainAR mode!)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Showroom_Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(8f, 1f, 8f);
            var floorMr = floor.GetComponent<MeshRenderer>();
            floorMr.sharedMaterial = _pbrShowroomFloor;
            floorMr.shadowCastingMode = ShadowCastingMode.Off;
            floorMr.receiveShadows = true;

            // Key Directional Light (warm-neutral overhead industrial spotlight casting crisp soft shadows to the left)
            var keyGo = new GameObject("Studio_KeyLight");
            var keyLight = keyGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.98f, 0.95f, 1f);
            keyLight.intensity = 1.40f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.68f;
            keyLight.shadowBias = 0.02f;
            keyLight.shadowNormalBias = 0.20f;
            keyGo.transform.rotation = Quaternion.Euler(35f, -52f, 0f);

            // Cool Factory Rim / Fill Light
            var fillGo = new GameObject("Studio_RimFillLight");
            var fillLight = fillGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.78f, 0.89f, 1.0f, 1f);
            fillLight.intensity = 0.46f;
            fillLight.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(26f, 138f, 0f);

            // Global URP Post-Processing Volume (ACES Tonemapping + delicate LED Bloom)
            var volGo = new GameObject("Showroom_GlobalVolume");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = CreateOrUpdateShowroomVolumeProfile();

            // Instantiate Realistic PBR ST-03 Carding Machine cleanly at origin
            var machineGo = (GameObject)PrefabUtility.InstantiatePrefab(realisticCardingPrefab);
            machineGo.name = "ST03_CardingMachine_RealisticPBR";
            machineGo.transform.position = Vector3.zero;
            machineGo.transform.rotation = Quaternion.identity;

            // Main Camera configured with URP_RealisticShowroom_Renderer (Index 2 = Noise-Free Studio PBR + Clean SSAO!)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.81f, 0.85f, 0.90f, 1f);
            cam.fieldOfView = 31f;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 80f;
            cam.allowMSAA = true;
            camGo.AddComponent<AudioListener>();

            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.SetRenderer(2); // Index 2 = Noise-Free Showroom PBR Renderer (switches to 1 in TrainAR mode!)
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            urpCam.antialiasingQuality = AntialiasingQuality.High;

            camGo.transform.position = new Vector3(2.38f, 2.34f, -6.02f);
            camGo.transform.LookAt(new Vector3(0.08f, 0.96f, 0f));

            // Wire live Realistic PBR <-> TrainAR Style Switcher
            var switcher = machineGo.GetComponent<TrainAR_StyleSwitcher>();
            if (switcher != null)
            {
                switcher.activeStyle = VisualRenderStyle.RealisticPBR;
                switcher.targetCamera = cam;
                switcher.floorRenderer = floorMr;
                switcher.floorRealisticPbr = _pbrShowroomFloor;
                switcher.floorTrainArStudio = _matStudioFloor;
                switcher.factoryReflectionCubemap = factoryCube;
                switcher.keyLight = keyLight;
                switcher.fillLight = fillLight;
                switcher.postProcessVolume = volume;
                switcher.materialPairs = BuildMaterialStylePairs();
                switcher.ApplyStyle(VisualRenderStyle.RealisticPBR);
            }

            // Interactive 4-Module Practical Training Studio Controller + Crisp 72pt Chinese SDF Fonts + StyleA Glass Sprites
            var ctrlGo = new GameObject("TrainAR_ShowcaseController");
            var ctrl = ctrlGo.AddComponent<TrainAR_ShowcaseController>();
            ctrl.targetCamera = cam;
            ctrl.cardingAnimator = machineGo.GetComponent<TrainAR_CardingAnimator>();
            ctrl.materialFlowAnimator = machineGo.GetComponent<TrainAR_MaterialFlowAnimator>();
            ctrl.styleSwitcher = switcher;
            ctrl.uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/CNC_Sans_Regular_SDF.asset");
            ctrl.uiFontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/CNC_Sans_Bold_SDF.asset");
            ctrl.spCardGlass = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_card_glass.png");
            ctrl.spSubcard = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_subcard.png");
            ctrl.spPillGlass = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_pill_glass.png");
            ctrl.spPillActiveSky = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_pill_active_sky.png");
            ctrl.spPillActiveWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_pill_active_white.png");
            ctrl.spPillIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_pill_idle.png");
            ctrl.spPillGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_pill_green.png");
            ctrl.spPillDanger = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/StyleA/va_pill_danger.png");
            ctrl.defaultPivot = new Vector3(0.08f, 0.96f, 0f);
            ctrl.defaultYaw = 158f;
            ctrl.defaultPitch = 13.0f;
            ctrl.defaultDistance = 6.45f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
