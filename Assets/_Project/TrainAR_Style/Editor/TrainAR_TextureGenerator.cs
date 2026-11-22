using System.IO;
using UnityEditor;
using UnityEngine;

namespace CNC.TrainAR.Editor
{
    /// <summary>
    /// Generates seamless hand-painted / watercolor / technical-illustration textures
    /// tailored to the TrainAR aesthetic (Coffee & Four-Stroke Engine examples).
    /// </summary>
    public static class TrainAR_TextureGenerator
    {
        public const string TexDir = "Assets/_Project/TrainAR_Style/Textures";

        public static void GenerateAllTextures()
        {
            Directory.CreateDirectory(TexDir);

            SaveTexture($"{TexDir}/Tex_TrainAR_WatercolorBrush.png", CreateWatercolorBrushTex(512), false);
            SaveTexture($"{TexDir}/Tex_TrainAR_CeramicPainted.png", CreateCeramicPaintedTex(512), false);
            SaveTexture($"{TexDir}/Tex_TrainAR_BrushedSteel.png", CreateBrushedSteelTex(512), false);
            SaveTexture($"{TexDir}/Tex_TrainAR_CardClothing.png", CreateCardClothingTex(512), false);
            SaveTexture($"{TexDir}/Tex_TrainAR_CutawayHatch.png", CreateCutawayHatchTex(512), false);
            SaveTexture($"{TexDir}/Tex_TrainAR_CottonFibers.png", CreateCottonFibersTex(512), false);
            SaveTexture($"{TexDir}/Tex_TrainAR_HMI_Screen.png", CreateHmiScreenTex(512), true);
            SaveTexture($"{TexDir}/Tex_TrainAR_CanBadge.png", CreateSliverCanTex(512), false);

            AssetDatabase.Refresh();
        }

        private static void SaveTexture(string assetPath, Texture2D tex, bool clamp)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = !assetPath.EndsWith("WatercolorBrush.png");
                importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static float TileableFbm(float u, float v, int octaves, float baseFreq, float seed)
        {
            float sum = 0f;
            float amp = 0.5f;
            float norm = 0f;
            float freq = baseFreq;

            for (int i = 0; i < octaves; i++)
            {
                // Seamless periodic 2D hash-value noise
                float n = PeriodicNoise(u * freq, v * freq, Mathf.RoundToInt(freq), seed + i * 31.7f);
                sum += n * amp;
                norm += amp;
                amp *= 0.52f;
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

        private static Texture2D CreateWatercolorBrushTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    // Domain-warped soft painterly strokes + watercolor paper grain
                    float warpX = TileableFbm(u, v, 3, 4f, 1.3f) * 0.14f;
                    float warpY = TileableFbm(u, v, 3, 4f, 5.7f) * 0.14f;
                    float blotch = TileableFbm(u + warpX, v + warpY, 4, 4f, 11.1f);
                    float stroke = TileableFbm((u + warpX * 0.5f), v * 3f, 3, 6f, 23.4f);
                    float paper = TileableFbm(u, v, 2, 32f, 41.2f);

                    float val = Mathf.Clamp01(blotch * 0.55f + stroke * 0.30f + paper * 0.15f);
                    // Gentle contrast shaping around 0.5
                    val = Mathf.Clamp01(0.5f + (val - 0.5f) * 1.45f);
                    cols[y * size + x] = new Color(val, val, val, 1f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCeramicPaintedTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float broad = TileableFbm(u, v, 4, 4f, 3.1f);
                    float brush = TileableFbm(u * 2f, v, 3, 8f, 9.8f);
                    float fine = TileableFbm(u, v, 2, 32f, 19.2f);

                    float lum = 0.93f + (broad - 0.5f) * 0.08f + (brush - 0.5f) * 0.05f + (fine - 0.5f) * 0.025f;
                    // Subtle warm-cool painterly shift
                    float warmth = (broad - 0.5f) * 0.025f;
                    cols[y * size + x] = new Color(
                        Mathf.Clamp01(lum + warmth),
                        Mathf.Clamp01(lum),
                        Mathf.Clamp01(lum - warmth * 0.8f),
                        1f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBrushedSteelTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    // Anisotropic horizontal brushed metal + painterly hand-shaded smudges
                    float streaks = PeriodicNoise(u * 6f, v * 64f, 64, 7.2f) * 0.6f +
                                    PeriodicNoise(u * 12f, v * 128f, 128, 13.5f) * 0.4f;
                    float blotch = TileableFbm(u, v, 4, 4f, 27.9f);
                    float lum = 0.90f + (streaks - 0.5f) * 0.14f + (blotch - 0.5f) * 0.12f;
                    cols[y * size + x] = new Color(lum * 0.98f, lum * 0.99f, Mathf.Clamp01(lum * 1.02f), 1f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCardClothingTex(int size)
        {
            // Stylized clean engineering illustration of metallic card-clothing wire strips
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    // Fine parallel metallic wire ribs + micro needle teeth pattern
                    float wireWave = Mathf.Sin(v * Mathf.PI * 2f * 48f) * 0.5f + 0.5f;
                    float toothWave = Mathf.Sin(u * Mathf.PI * 2f * 96f + Mathf.Floor(v * 48f) * 1.7f) * 0.5f + 0.5f;
                    float band = Mathf.Sin(v * Mathf.PI * 2f * 6f) * 0.5f + 0.5f;
                    float wear = TileableFbm(u, v, 3, 4f, 51.3f);

                    float lum = 0.84f + (wireWave - 0.5f) * 0.18f + (toothWave - 0.5f) * 0.08f
                                + (band - 0.5f) * 0.05f + (wear - 0.5f) * 0.10f;
                    lum = Mathf.Clamp01(lum);
                    cols[y * size + x] = new Color(lum * 0.97f, lum * 0.99f, Mathf.Clamp01(lum * 1.02f), 1f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCutawayHatchTex(int size)
        {
            // Clean architectural / engineering cutaway section face:
            // Pale cool ceramic gray with crisp subtle 45-degree technical hatching
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float diag = (u + v) * 28f;
                    float f = diag - Mathf.Floor(diag);
                    // Thin subtle engineering hatch stroke
                    float hatch = (f < 0.11f || f > 0.95f) ? 1f : 0f;
                    float grain = TileableFbm(u, v, 3, 6f, 61.1f);

                    float baseLum = 0.95f + (grain - 0.5f) * 0.05f;
                    float lum = Mathf.Lerp(baseLum, baseLum * 0.83f, hatch * 0.72f);
                    cols[y * size + x] = new Color(lum * 0.96f, lum * 0.98f, Mathf.Clamp01(lum * 1.01f), 1f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCottonFibersTex(int size)
        {
            // Soft warm ivory cotton fleece / aligned carded fiber streaks
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float warp = TileableFbm(u, v, 3, 4f, 77.7f) * 0.08f;
                    float fibers = PeriodicNoise((u + warp) * 8f, (v + warp * 0.3f) * 40f, 40, 81.2f);
                    float puffs = TileableFbm(u, v, 4, 6f, 92.4f);

                    float lum = 0.96f + (fibers - 0.5f) * 0.08f + (puffs - 0.5f) * 0.07f;
                    cols[y * size + x] = new Color(
                        Mathf.Clamp01(lum * 1.01f),
                        Mathf.Clamp01(lum * 0.995f),
                        Mathf.Clamp01(lum * 0.95f),
                        1f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHmiScreenTex(int size)
        {
            // Stylized TrainAR-palette industrial HMI dashboard graphic
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            Color bg = new Color(0.16f, 0.20f, 0.26f, 1f);
            Color header = new Color(0.24f, 0.30f, 0.38f, 1f);
            Color cardBg = new Color(0.21f, 0.26f, 0.33f, 1f);
            Color accentSky = new Color(0.45f, 0.78f, 0.94f, 1f);
            Color accentYellow = new Color(0.92f, 0.78f, 0.36f, 1f);
            Color accentGreen = new Color(0.44f, 0.80f, 0.58f, 1f);
            Color ink = new Color(0.11f, 0.13f, 0.17f, 1f);
            Color cream = new Color(0.93f, 0.92f, 0.88f, 1f);

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    Color c = bg;

                    // Outer dark bezel border
                    if (u < 0.04f || u > 0.96f || v < 0.04f || v > 0.96f)
                    {
                        c = ink;
                    }
                    else if (v > 0.80f)
                    {
                        c = header;
                        // Status green pill top-right
                        if (u > 0.76f && u < 0.92f && v > 0.85f && v < 0.92f) c = accentGreen;
                        // Header title bars top-left
                        if (u > 0.08f && u < 0.45f && v > 0.86f && v < 0.91f) c = cream;
                    }
                    else
                    {
                        // Left schematic box (Carding drum diagram)
                        if (u > 0.07f && u < 0.62f && v > 0.12f && v < 0.74f)
                        {
                            c = cardBg;
                            // Main Cylinder circle
                            float dMain = Vector2.Distance(new Vector2(u, v), new Vector2(0.35f, 0.43f));
                            if (dMain < 0.17f) c = accentSky * 0.85f;
                            if (Mathf.Abs(dMain - 0.17f) < 0.012f) c = cream;
                            if (dMain < 0.045f) c = ink;
                            // Licker-in circle (left)
                            float dLick = Vector2.Distance(new Vector2(u, v), new Vector2(0.15f, 0.37f));
                            if (dLick < 0.065f) c = accentYellow;
                            if (Mathf.Abs(dLick - 0.065f) < 0.01f) c = ink;
                            // Doffer circle (right)
                            float dDoff = Vector2.Distance(new Vector2(u, v), new Vector2(0.53f, 0.40f));
                            if (dDoff < 0.085f) c = accentGreen;
                            if (Mathf.Abs(dDoff - 0.085f) < 0.01f) c = ink;
                            // Flat top arch
                            if (dMain > 0.19f && dMain < 0.225f && v > 0.46f) c = accentYellow;
                        }
                        // Right metrics bars
                        if (u > 0.66f && u < 0.93f && v > 0.12f && v < 0.74f)
                        {
                            c = cardBg;
                            // 3 gauge bars
                            if (v > 0.56f && v < 0.64f && u > 0.70f && u < 0.89f)
                                c = (u < 0.85f) ? accentSky : ink;
                            if (v > 0.38f && v < 0.46f && u > 0.70f && u < 0.89f)
                                c = (u < 0.83f) ? accentGreen : ink;
                            if (v > 0.20f && v < 0.28f && u > 0.70f && u < 0.89f)
                                c = (u < 0.80f) ? accentYellow : ink;
                        }
                    }

                    cols[y * size + x] = c;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSliverCanTex(int size)
        {
            // Inspired by TrainAR's sage-green coffee canister with a charming clean cream badge!
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var cols = new Color[size * size];

            Color sage = new Color(0.32f, 0.47f, 0.39f, 1f);
            Color sageDark = new Color(0.24f, 0.36f, 0.30f, 1f);
            Color cream = new Color(0.95f, 0.91f, 0.80f, 1f);
            Color mustard = new Color(0.89f, 0.75f, 0.38f, 1f);
            Color ink = new Color(0.16f, 0.20f, 0.19f, 1f);

            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float paint = TileableFbm(u, v, 4, 4f, 108.5f);
                    Color c = Color.Lerp(sageDark, sage, 0.55f + (paint - 0.5f) * 0.55f);

                    // Top & bottom subtle decorative industrial stripes
                    if (Mathf.Abs(v - 0.86f) < 0.018f || Mathf.Abs(v - 0.14f) < 0.018f)
                        c = mustard;
                    else if (Mathf.Abs(v - 0.83f) < 0.008f || Mathf.Abs(v - 0.17f) < 0.008f)
                        c = ink;

                    // Circular emblem on the right-front outer wall (around u = 0.09, v = 0.52)
                    float du = (u - 0.09f) * 2.4f;
                    float dv = (v - 0.52f);
                    float dist = Mathf.Sqrt(du * du + dv * dv);
                    if (dist < 0.17f)
                    {
                        c = cream;
                        if (dist > 0.145f) c = mustard;
                        if (Mathf.Abs(dist - 0.17f) < 0.008f || Mathf.Abs(dist - 0.145f) < 0.007f) c = ink;
                        // Stylized spiral cotton sliver symbol inside badge
                        float ang = Mathf.Atan2(dv, du);
                        float spiral = Mathf.Abs(dist - (0.025f + ((ang + Mathf.PI) / (2f * Mathf.PI)) * 0.075f));
                        if (dist < 0.11f && spiral < 0.013f) c = sageDark;
                    }

                    cols[y * size + x] = c;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }
    }
}
