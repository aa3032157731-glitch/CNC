using System.IO;
using UnityEditor;
using UnityEngine;

namespace CNC.TrainAR.Editor
{
    /// <summary>
    /// Generates high-craft procedural Industrial PBR Albedo + Normal Maps (tangent-space)
    /// and a Smart-Factory Studio Reflection Cubemap so metallic steel, cast iron, brass,
    /// powder-coated panels, cotton fibers, and glowing HMI glass look photorealistic in URP/Lit
    /// and match the existing CNC Smart Factory SampleScene seamlessly.
    /// </summary>
    public static class TrainAR_PbrTextureGenerator
    {
        public const string PbrDir = "Assets/_Project/TrainAR_Style/Textures/PBR";
        public const string CubemapPath = PbrDir + "/Tex_PBR_FactoryStudio_Cubemap.asset";

        public static void GenerateAllPbrAssets()
        {
            Directory.CreateDirectory(PbrDir);

            // 1. Industrial Automotive-Grade Satin Powder-Coat Enamel (ultra-fine micro orange-peel, zero stucco!)
            GenerateHeightDrivenPbr("PowderCoat", 512, 0.24f,
                (u, v) =>
                {
                    float fine = PeriodicNoise(u * 128f, v * 128f, 128, 1.7f);
                    float micro = PeriodicNoise(u * 256f, v * 256f, 256, 4.1f);
                    return fine * 0.60f + micro * 0.40f;
                },
                (u, v, h) =>
                {
                    float vShade = 0.975f + (h - 0.5f) * 0.025f;
                    return new Color(vShade * 0.99f, vShade * 0.996f, Mathf.Clamp01(vShade * 1.005f), 1f);
                });

            // 2. Refined Machine-Tool Cast Iron (fine satin sand-cast grain, never volcanic rock!)
            GenerateHeightDrivenPbr("CastIron", 512, 0.52f,
                (u, v) =>
                {
                    float grain = PeriodicNoise(u * 96f, v * 96f, 96, 13.3f);
                    float fine = PeriodicNoise(u * 192f, v * 192f, 192, 19.7f);
                    float broad = Fbm(u, v, 3, 8f, 29.1f);
                    return grain * 0.50f + fine * 0.35f + broad * 0.15f;
                },
                (u, v, h) =>
                {
                    float lum = 0.88f + (h - 0.5f) * 0.12f;
                    return new Color(lum * 0.97f, lum * 0.99f, Mathf.Clamp01(lum * 1.02f), 1f);
                });

            // 3. Silky 304 Brushed Stainless Steel (ultra-fine hairline directional sheen)
            GenerateHeightDrivenPbr("MachinedSteel", 512, 0.42f,
                (u, v) =>
                {
                    float streak1 = PeriodicNoise(u * 8f, v * 180f, 180, 31.4f);
                    float streak2 = PeriodicNoise(u * 16f, v * 320f, 320, 47.8f);
                    float sheen = Fbm(u, v, 2, 4f, 53.2f);
                    return streak1 * 0.50f + streak2 * 0.35f + sheen * 0.15f;
                },
                (u, v, h) =>
                {
                    float lum = 0.92f + (h - 0.5f) * 0.09f;
                    return new Color(lum * 0.98f, lum * 0.995f, Mathf.Clamp01(lum * 1.015f), 1f);
                });

            // 4. Hardened Metallic Card-Clothing Wire-Teeth (crisp precision wire ridges!)
            GenerateHeightDrivenPbr("CardClothing", 512, 1.95f,
                (u, v) =>
                {
                    float wireRib = Mathf.Sin(v * Mathf.PI * 2f * 72f) * 0.5f + 0.5f;
                    float sawTooth = Mathf.Pow(Mathf.Sin(u * Mathf.PI * 2f * 144f + Mathf.Floor(v * 72f) * 1.3f) * 0.5f + 0.5f, 1.4f);
                    float seam = Mathf.Sin(v * Mathf.PI * 2f * 8f) * 0.5f + 0.5f;
                    return wireRib * 0.58f + sawTooth * 0.32f + seam * 0.10f;
                },
                (u, v, h) =>
                {
                    float lum = 0.80f + h * 0.18f;
                    return new Color(lum * 0.97f, lum * 0.99f, Mathf.Clamp01(lum * 1.02f), 1f);
                });

            // 5. Sleek Precision CNC-Milled Cutaway Section Alloy (refined satin tool-path, zero zebra stripes!)
            GenerateHeightDrivenPbr("CutawayMilled", 512, 0.32f,
                (u, v) =>
                {
                    float diag = (u + v) * 44f;
                    float f = diag - Mathf.Floor(diag);
                    float groove = Mathf.Sin(f * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float fine = PeriodicNoise(u * 96f, v * 96f, 96, 67.5f);
                    return groove * 0.55f + fine * 0.45f;
                },
                (u, v, h) =>
                {
                    float lum = 0.90f + (h - 0.5f) * 0.075f;
                    return new Color(lum * 0.97f, lum * 0.99f, Mathf.Clamp01(lum * 1.02f), 1f);
                });

            // 6. Soft Downy Natural Raw Cotton Fleece (delicate organic cotton fibers!)
            GenerateHeightDrivenPbr("CottonFleece", 512, 1.25f,
                (u, v) =>
                {
                    float warp = Fbm(u, v, 3, 5f, 71.1f) * 0.09f;
                    float fibers = PeriodicNoise((u + warp) * 12f, (v + warp * 0.35f) * 72f, 72, 79.3f);
                    float tufts = Fbm(u, v, 4, 8f, 83.9f);
                    float micro = Fbm(u, v, 2, 48f, 89.2f);
                    return fibers * 0.42f + tufts * 0.40f + micro * 0.18f;
                },
                (u, v, h) =>
                {
                    float lum = 0.94f + (h - 0.5f) * 0.09f;
                    return new Color(Mathf.Clamp01(lum * 1.01f), Mathf.Clamp01(lum * 0.996f), Mathf.Clamp01(lum * 0.962f), 1f);
                });

            // 7. Realistic Ribbed Industrial Neoprene Rubber (V-Belts & Anti-Vibration Pads)
            GenerateHeightDrivenPbr("RubberRibbed", 256, 1.15f,
                (u, v) =>
                {
                    float rib = Mathf.Sin(v * Mathf.PI * 2f * 32f) * 0.5f + 0.5f;
                    float grain = PeriodicNoise(u * 64f, v * 64f, 64, 97.1f);
                    return rib * 0.60f + grain * 0.40f;
                },
                (u, v, h) =>
                {
                    float lum = 0.80f + (h - 0.5f) * 0.12f;
                    return new Color(lum, lum, lum * 1.02f, 1f);
                });

            // 8. Modern Smart-Factory Industrial Teal Sliver Can Albedo + Satin Polymer Normal
            SavePng($"{PbrDir}/Tex_PBR_SliverCan_Albedo.png", CreateRealisticSliverCanTex(512), false, false);
            SavePng($"{PbrDir}/Tex_PBR_HMI_SmartFactory.png", CreateSmartFactoryHmiTex(512), true, false);

            // 9. Procedural Smart-Factory Overhead-LED Studio Reflection Cubemap (makes PBR metals shine!)
            CreateFactoryStudioCubemap();

            AssetDatabase.Refresh();
        }

        private static void GenerateHeightDrivenPbr(string key, int size, float bumpStrength,
            System.Func<float, float, float> heightFn,
            System.Func<float, float, float, Color> albedoFn)
        {
            float[] heights = new float[size * size];
            Color[] albCols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float h = Mathf.Clamp01(heightFn(u, v));
                    heights[y * size + x] = h;
                    albCols[y * size + x] = albedoFn(u, v, h);
                }
            }

            var albTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            albTex.SetPixels(albCols);
            albTex.Apply();
            SavePng($"{PbrDir}/Tex_PBR_{key}_Albedo.png", albTex, false, false);

            // Sobel tangent-space Normal Map from heightfield
            Color[] nrmCols = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                int ym = (y - 1 + size) % size;
                int yp = (y + 1) % size;
                for (int x = 0; x < size; x++)
                {
                    int xm = (x - 1 + size) % size;
                    int xp = (x + 1) % size;

                    float tl = heights[yp * size + xm];
                    float t  = heights[yp * size + x];
                    float tr = heights[yp * size + xp];
                    float l  = heights[y  * size + xm];
                    float r  = heights[y  * size + xp];
                    float bl = heights[ym * size + xm];
                    float b  = heights[ym * size + x];
                    float br = heights[ym * size + xp];

                    float dX = (tr + 2f * r + br) - (tl + 2f * l + bl);
                    float dY = (tl + 2f * t + tr) - (bl + 2f * b + br);

                    Vector3 n = new Vector3(-dX * bumpStrength, -dY * bumpStrength, 1f).normalized;
                    nrmCols[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }

            var nrmTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            nrmTex.SetPixels(nrmCols);
            nrmTex.Apply();
            SavePng($"{PbrDir}/Tex_PBR_{key}_Normal.png", nrmTex, false, true);
        }

        private static Texture2D CreateRealisticSliverCanTex(int size)
        {
            // Modern smart cotton-mill industrial cyan-teal HDPE sliver can with safety band & metallic badge
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            Color tealBase = new Color(0.11f, 0.45f, 0.51f, 1f);
            Color tealDeep = new Color(0.08f, 0.34f, 0.40f, 1f);
            Color silverBand = new Color(0.76f, 0.82f, 0.87f, 1f);
            Color safetyAmber = new Color(0.92f, 0.70f, 0.14f, 1f);
            Color darkTrim = new Color(0.13f, 0.17f, 0.22f, 1f);

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float grain = Fbm(u, v, 3, 18f, 114.2f);
                    float streaks = PeriodicNoise(u * 4f, v * 64f, 64, 121.5f);
                    Color c = Color.Lerp(tealDeep, tealBase, 0.58f + (grain - 0.5f) * 0.35f + (streaks - 0.5f) * 0.12f);

                    // Industrial reflective identification stripes near top & bottom
                    if (Mathf.Abs(v - 0.87f) < 0.016f || Mathf.Abs(v - 0.14f) < 0.016f)
                        c = silverBand;
                    else if (Mathf.Abs(v - 0.845f) < 0.009f || Mathf.Abs(v - 0.165f) < 0.009f)
                        c = safetyAmber;
                    else if (Mathf.Abs(v - 0.832f) < 0.005f || Mathf.Abs(v - 0.178f) < 0.005f)
                        c = darkTrim;

                    // Crisp metallic industrial spec plaque on visible right-front exterior (u ≈ 0.09, v ≈ 0.52)
                    float du = Mathf.Abs(u - 0.09f) * 2.5f;
                    float dv = Mathf.Abs(v - 0.52f);
                    if (du < 0.14f && dv < 0.11f)
                    {
                        c = darkTrim;
                        if (du < 0.125f && dv < 0.095f) c = silverBand * 0.92f;
                        // Horizontal technical data bars on plaque
                        if (du < 0.095f && dv < 0.065f)
                        {
                            float row = (v - 0.455f) / 0.13f;
                            if (row > 0.72f && row < 0.88f) c = tealDeep;
                            else if (row > 0.45f && row < 0.56f) c = darkTrim;
                            else if (row > 0.22f && row < 0.32f) c = darkTrim;
                        }
                    }

                    cols[y * size + x] = c;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSmartFactoryHmiTex(int size)
        {
            // Dark glass industrial SCADA touchscreen UI matching the user's SampleScene HUD!
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            Color bg = new Color(0.06f, 0.10f, 0.16f, 1f);
            Color panel = new Color(0.10f, 0.16f, 0.24f, 1f);
            Color cyan = new Color(0.12f, 0.78f, 0.94f, 1f);
            Color green = new Color(0.18f, 0.86f, 0.54f, 1f);
            Color amber = new Color(0.96f, 0.72f, 0.18f, 1f);
            Color grid = new Color(0.15f, 0.24f, 0.34f, 1f);
            Color white = new Color(0.88f, 0.94f, 0.98f, 1f);

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    Color c = bg;

                    // Subtle telemetry grid
                    if (((x % 32) == 0 || (y % 32) == 0) && u > 0.04f && u < 0.96f && v > 0.04f && v < 0.96f)
                        c = grid * 0.65f;

                    // Top SCADA status bar
                    if (v > 0.82f && v < 0.96f && u > 0.04f && u < 0.96f)
                    {
                        c = panel;
                        if (u > 0.07f && u < 0.42f && v > 0.87f && v < 0.915f) c = cyan;
                        if (u > 0.76f && u < 0.92f && v > 0.86f && v < 0.92f) c = green;
                    }

                    // Left schematic card (Main Cylinder + Licker-in + Doffer live diagram)
                    if (u > 0.05f && u < 0.61f && v > 0.08f && v < 0.77f)
                    {
                        c = Color.Lerp(panel, c, 0.25f);
                        float dMain = Vector2.Distance(new Vector2(u, v), new Vector2(0.34f, 0.44f));
                        if (dMain < 0.17f) c = cyan * 0.35f;
                        if (Mathf.Abs(dMain - 0.17f) < 0.011f) c = cyan;
                        if (dMain < 0.038f) c = white;

                        float dLick = Vector2.Distance(new Vector2(u, v), new Vector2(0.14f, 0.38f));
                        if (dLick < 0.062f) c = amber * 0.45f;
                        if (Mathf.Abs(dLick - 0.062f) < 0.009f) c = amber;

                        float dDoff = Vector2.Distance(new Vector2(u, v), new Vector2(0.52f, 0.41f));
                        if (dDoff < 0.082f) c = green * 0.45f;
                        if (Mathf.Abs(dDoff - 0.082f) < 0.009f) c = green;

                        if (dMain > 0.19f && dMain < 0.215f && v > 0.48f) c = amber;
                    }

                    // Right real-time telemetry bars (RPM / Load / Temp)
                    if (u > 0.65f && u < 0.95f && v > 0.08f && v < 0.77f)
                    {
                        c = panel;
                        if (v > 0.58f && v < 0.65f && u > 0.69f && u < 0.91f) c = (u < 0.87f) ? cyan : bg;
                        if (v > 0.41f && v < 0.48f && u > 0.69f && u < 0.91f) c = (u < 0.85f) ? green : bg;
                        if (v > 0.24f && v < 0.31f && u > 0.69f && u < 0.91f) c = (u < 0.82f) ? amber : bg;
                        // Thin white labels above bars
                        if ((Mathf.Abs(v - 0.69f) < 0.012f || Mathf.Abs(v - 0.52f) < 0.012f || Mathf.Abs(v - 0.35f) < 0.012f) && u > 0.69f && u < 0.82f)
                            c = white * 0.85f;
                    }

                    cols[y * size + x] = c;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a realistic Smart-Factory Studio Environment Cubemap with bright cool-white
        /// ceiling LED strips, clean factory wall softboxes, and epoxy floor bounce so metallic
        /// PBR steel/iron/brass surfaces show lifelike reflections!
        /// </summary>
        private static void CreateFactoryStudioCubemap()
        {
            const int faceSize = 128;
            var cubemap = new Cubemap(faceSize, TextureFormat.RGBAHalf, true);

            CubemapFace[] faces =
            {
                CubemapFace.PositiveX, CubemapFace.NegativeX,
                CubemapFace.PositiveY, CubemapFace.NegativeY,
                CubemapFace.PositiveZ, CubemapFace.NegativeZ
            };

            foreach (var face in faces)
            {
                var pixels = new Color[faceSize * faceSize];
                for (int y = 0; y < faceSize; y++)
                {
                    float v = ((y + 0.5f) / faceSize) * 2f - 1f;
                    for (int x = 0; x < faceSize; x++)
                    {
                        float u = ((x + 0.5f) / faceSize) * 2f - 1f;
                        Vector3 dir = FaceUVToDir(face, u, v).normalized;
                        pixels[y * faceSize + x] = EvaluateStudioEnv(dir);
                    }
                }
                cubemap.SetPixels(pixels, face);
            }
            cubemap.Apply(true, false);

            var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(CubemapPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(CubemapPath);
            }
            AssetDatabase.CreateAsset(cubemap, CubemapPath);
        }

        private static Vector3 FaceUVToDir(CubemapFace face, float u, float v)
        {
            // Unity cubemap face orientation (v runs top-to-bottom in texture space)
            switch (face)
            {
                case CubemapFace.PositiveX: return new Vector3( 1f, -v, -u);
                case CubemapFace.NegativeX: return new Vector3(-1f, -v,  u);
                case CubemapFace.PositiveY: return new Vector3( u,  1f,  v);
                case CubemapFace.NegativeY: return new Vector3( u, -1f, -v);
                case CubemapFace.PositiveZ: return new Vector3( u, -v,  1f);
                default:                    return new Vector3(-u, -v, -1f);
            }
        }

        private static Color EvaluateStudioEnv(Vector3 d)
        {
            // Base vertical gradient: luminous epoxy floor bounce -> bright clean workshop walls -> bright LED ceiling
            float up = d.y;
            Color floorCol = new Color(0.50f, 0.55f, 0.62f, 1f);
            Color horizonCol = new Color(0.80f, 0.85f, 0.92f, 1f);
            Color ceilingCol = new Color(0.92f, 0.96f, 1.02f, 1f);

            Color col = up >= 0f
                ? Color.Lerp(horizonCol, ceilingCol, Mathf.Pow(up, 0.55f))
                : Color.Lerp(horizonCol, floorCol, Mathf.Pow(-up, 0.5f));

            // Overhead Smart-Factory Linear LED Light Strips (creates authentic crisp metallic highlights!)
            if (d.y > 0.35f)
            {
                float px = d.x / d.y;
                float pz = d.z / d.y;
                // 3 parallel ceiling LED banks
                float strip1 = Mathf.Exp(-Mathf.Pow((pz - 0.65f) * 5.2f, 2f)) * (Mathf.Abs(px) < 1.35f ? 1f : 0f);
                float strip2 = Mathf.Exp(-Mathf.Pow((pz + 0.05f) * 5.2f, 2f)) * (Mathf.Abs(px) < 1.35f ? 1f : 0f);
                float strip3 = Mathf.Exp(-Mathf.Pow((pz + 0.75f) * 5.2f, 2f)) * (Mathf.Abs(px) < 1.35f ? 1f : 0f);
                float led = Mathf.Clamp01(strip1 + strip2 + strip3);
                col += new Color(2.4f, 2.55f, 2.80f, 0f) * led;
            }

            // Warm key softbox on upper-front-right (+X, +Y, -Z) for warm/cool metallic depth contrast
            Vector3 keyDir = new Vector3(0.55f, 0.58f, -0.60f).normalized;
            float keyBox = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(d, keyDir)), 14f);
            col += new Color(2.2f, 2.08f, 1.88f, 0f) * keyBox;

            // Cool cyan rim softbox on left-rear (-X, +Y, +Z) matching SampleScene cool workshop bounce
            Vector3 rimDir = new Vector3(-0.68f, 0.35f, 0.64f).normalized;
            float rimBox = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(d, rimDir)), 10f);
            col += new Color(0.85f, 1.35f, 1.65f, 0f) * rimBox;

            return col;
        }

        private static void SavePng(string assetPath, Texture2D tex, bool clamp, bool isNormal)
        {
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal;
                importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static float Fbm(float u, float v, int octaves, float baseFreq, float seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f, freq = baseFreq;
            for (int i = 0; i < octaves; i++)
            {
                int p = Mathf.Max(2, Mathf.RoundToInt(freq));
                sum += PeriodicNoise(u * p, v * p, p, seed + i * 29.3f) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / Mathf.Max(norm, 1e-4f);
        }

        private static float PeriodicNoise(float x, float y, int period, float seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = x - x0;
            float ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            int ix0 = ((x0 % period) + period) % period;
            int iy0 = ((y0 % period) + period) % period;
            int ix1 = (ix0 + 1) % period;
            int iy1 = (iy0 + 1) % period;

            float v00 = Hash2(ix0, iy0, seed);
            float v10 = Hash2(ix1, iy0, seed);
            float v01 = Hash2(ix0, iy1, seed);
            float v11 = Hash2(ix1, iy1, seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
        }

        private static float Hash2(int x, int y, float seed)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f + seed * 74.7f) * 43758.5453123f;
            return h - Mathf.Floor(h);
        }
    }
}
