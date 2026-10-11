using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerBuffPlanner.UI
{
    // RGBA32 pixels, row 0 at the bottom (Unity texture order).
    public sealed class HudPixels
    {
        public HudPixels(int width, int height, byte[] rgba)
        {
            if (width < 1 || height < 1) throw new ArgumentException("The image is empty.");
            if (rgba == null || rgba.Length != width * height * 4)
                throw new ArgumentException("The pixel buffer does not match the size.", "rgba");
            Width = width;
            Height = height;
            Rgba = rgba;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Rgba { get; private set; }
    }

    public sealed class HudCellArtResult
    {
        public HudCellArtResult(HudPixels pixels, string evidence, float inkOffsetX, float inkOffsetY)
        {
            Pixels = pixels;
            Evidence = evidence;
            InkOffsetX = inkOffsetX;
            InkOffsetY = inkOffsetY;
        }

        public HudPixels Pixels { get; private set; }
        public string Evidence { get; private set; }
        // The drawn glyph's ink centre from the cell centre, in pixels.
        public float InkOffsetX { get; private set; }
        public float InkOffsetY { get; private set; }
    }

    // Native UI finish (UI-09): an owned HUD cell drawn from a native HUD
    // cell's own art. The native cell (frame, stone, state colouring) is kept
    // pixel for pixel; its engraved icon is lifted out (the stone under it
    // is filled in from the surrounding stone); the owned glyph is then
    // engraved in its place with the native icon's own gold and the dark
    // recess the native icons sit in. The same composition over the native
    // hover art gives the hover state, so every state is the native one.
    public static class HudCellArt
    {
        // The share of the cell the glyph's ink spans at its longer side,
        // as the native icons' ink does.
        public const float InkFraction = 0.64f;

        public static HudCellArtResult Compose(HudPixels nativeCell, float[] glyphMask, int maskSize)
        {
            if (nativeCell == null) throw new ArgumentNullException("nativeCell");
            if (glyphMask == null || maskSize < 2 || glyphMask.Length != maskSize * maskSize)
                throw new ArgumentException("The glyph mask does not match its size.", "glyphMask");
            int width = nativeCell.Width, height = nativeCell.Height;
            float[] r = Channel(nativeCell, 0), g = Channel(nativeCell, 1), b = Channel(nativeCell, 2);
            byte[] alpha = Enumerable.Range(0, width * height).Select(i => nativeCell.Rgba[i * 4 + 3]).ToArray();

            // The icon lives inside the frame; the frame ring is never touched.
            int x0 = (int)Math.Round(width * 0.17), x1 = (int)Math.Round(width * 0.83);
            int y0 = (int)Math.Round(height * 0.17), y1 = (int)Math.Round(height * 0.83);
            var ring = new List<float>();
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (x < x0 + 3 || x >= x1 - 3 || y < y0 + 3 || y >= y1 - 3)
                        ring.Add(Luma(r, g, b, y * width + x));
            ring.Sort();
            float stone = ring.Count == 0 ? 0f : ring[ring.Count / 2];
            var gold = new bool[width * height];
            int goldCount = 0;
            float goldR = 0f, goldG = 0f, goldB = 0f;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int i = y * width + x;
                    if (Luma(r, g, b, i) > stone * 1.8f + 0.08f && r[i] > b[i] + 0.04f)
                    {
                        gold[i] = true;
                        goldCount++;
                        goldR += r[i]; goldG += g[i]; goldB += b[i];
                    }
                }
            if (goldCount < 8)
                throw new InvalidOperationException("The native cell shows no engraved icon to match.");
            goldR /= goldCount; goldG /= goldCount; goldB /= goldCount;

            // Lift the icon and its recess out: the stone under it is filled
            // in from the stone around it.
            int recess = Math.Max(2, (int)Math.Round(width * 0.05));
            bool[] lifted = Dilate(gold, width, height, recess);
            for (int i = 0; i < lifted.Length; i++)
            {
                int x = i % width, y = i / width;
                if (x < x0 || x >= x1 || y < y0 || y >= y1) lifted[i] = false;
            }
            float[] stoneR = Inpaint(r, lifted, width, height);
            float[] stoneG = Inpaint(g, lifted, width, height);
            float[] stoneB = Inpaint(b, lifted, width, height);
            // The native gold, with its own grain where the icons overlap.
            bool[] notGold = gold.Select(value => !value).ToArray();
            float[] fieldR = Inpaint(r, notGold, width, height);
            float[] fieldG = Inpaint(g, notGold, width, height);
            float[] fieldB = Inpaint(b, notGold, width, height);

            float[] coverage = Coverage(glyphMask, maskSize, width, height);
            float[] shadow = Spread(coverage, width, height, Math.Max(2, (int)Math.Round(width * 0.035)));
            // Half the grain: the native icons' mottling without the seams
            // where their edges were.
            for (int i = 0; i < width * height; i++)
            {
                fieldR[i] = goldR + 0.5f * (fieldR[i] - goldR);
                fieldG[i] = goldG + 0.5f * (fieldG[i] - goldG);
                fieldB[i] = goldB + 0.5f * (fieldB[i] - goldB);
            }
            var output = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                float darken = 1f - 0.6f * shadow[i];
                float cover = coverage[i];
                output[i * 4] = Byte(stoneR[i] * darken * (1f - cover) + fieldR[i] * cover);
                output[i * 4 + 1] = Byte(stoneG[i] * darken * (1f - cover) + fieldG[i] * cover);
                output[i * 4 + 2] = Byte(stoneB[i] * darken * (1f - cover) + fieldB[i] * cover);
                output[i * 4 + 3] = alpha[i];
            }
            int inkMinX = width, inkMinY = height, inkMaxX = -1, inkMaxY = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (coverage[y * width + x] >= 0.5f)
                    {
                        inkMinX = Math.Min(inkMinX, x); inkMaxX = Math.Max(inkMaxX, x);
                        inkMinY = Math.Min(inkMinY, y); inkMaxY = Math.Max(inkMaxY, y);
                    }
            if (inkMaxX < inkMinX) throw new InvalidOperationException("The glyph mask has no ink.");
            float offsetX = (inkMinX + inkMaxX + 1) * 0.5f - width * 0.5f;
            float offsetY = (inkMinY + inkMaxY + 1) * 0.5f - height * 0.5f;
            string evidence = "icon=" + goldCount + ";lifted=" + lifted.Count(value => value) +
                ";stone=" + Format(stone) + ";gold=" + Format(goldR) + "," + Format(goldG) + "," + Format(goldB) +
                ";ink=" + (inkMaxX - inkMinX + 1) + "x" + (inkMaxY - inkMinY + 1) +
                ";inkOffset=" + Format(offsetX) + "," + Format(offsetY);
            return new HudCellArtResult(new HudPixels(width, height, output), evidence, offsetX, offsetY);
        }

        // The glyph mask (row 0 at the bottom) centred on its ink, its ink's
        // longer side InkFraction of the cell, sampled 4x4 per pixel.
        private static float[] Coverage(float[] mask, int size, int width, int height)
        {
            int minX = size, minY = size, maxX = -1, maxY = -1;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (mask[y * size + x] > 0.05f)
                    {
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
            var result = new float[width * height];
            if (maxX < minX) return result;
            float inkX = (minX + maxX + 1) * 0.5f, inkY = (minY + maxY + 1) * 0.5f;
            float ink = Math.Max(maxX - minX + 1, maxY - minY + 1);
            float scale = ink / (Math.Min(width, height) * InkFraction);
            const int samples = 4;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int sy = 0; sy < samples; sy++)
                        for (int sx = 0; sx < samples; sx++)
                        {
                            float px = (x + (sx + 0.5f) / samples - width * 0.5f) * scale + inkX;
                            float py = (y + (sy + 0.5f) / samples - height * 0.5f) * scale + inkY;
                            int mx = (int)Math.Floor(px), my = (int)Math.Floor(py);
                            if (mx >= 0 && mx < size && my >= 0 && my < size) sum += mask[my * size + mx];
                        }
                    result[y * width + x] = sum / (samples * samples);
                }
            return result;
        }

        // The recess: the glyph's coverage spread outward, fading over radius.
        private static float[] Spread(float[] coverage, int width, int height, int radius)
        {
            var result = new float[coverage.Length];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float best = 0f;
                    for (int dy = -radius; dy <= radius; dy++)
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (distance > radius) continue;
                            float value = coverage[ny * width + nx] * (1f - distance / (radius + 1f));
                            if (value > best) best = value;
                        }
                    result[y * width + x] = best;
                }
            return result;
        }

        private static bool[] Dilate(bool[] source, int width, int height, int radius)
        {
            var result = new bool[source.Length];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (!source[y * width + x]) continue;
                    for (int dy = -radius; dy <= radius; dy++)
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < width && ny < height && dx * dx + dy * dy <= radius * radius)
                                result[ny * width + nx] = true;
                        }
                }
            return result;
        }

        // Fills the unknown pixels inward from the known ones, ring by ring,
        // each the mean of its already known neighbours.
        private static float[] Inpaint(float[] source, bool[] unknown, int width, int height)
        {
            var value = (float[])source.Clone();
            var known = unknown.Select(flag => !flag).ToArray();
            if (!known.Any(flag => flag)) return value;
            var pending = Enumerable.Range(0, value.Length).Where(i => !known[i]).ToList();
            while (pending.Count != 0)
            {
                var ring = new List<KeyValuePair<int, float>>();
                foreach (int i in pending)
                {
                    int x = i % width, y = i / width;
                    float sum = 0f;
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                            int n = ny * width + nx;
                            if (!known[n]) continue;
                            sum += value[n];
                            count++;
                        }
                    if (count != 0) ring.Add(new KeyValuePair<int, float>(i, sum / count));
                }
                if (ring.Count == 0) break;
                foreach (KeyValuePair<int, float> filled in ring)
                {
                    value[filled.Key] = filled.Value;
                    known[filled.Key] = true;
                }
                pending = pending.Where(i => !known[i]).ToList();
            }
            return value;
        }

        private static float[] Channel(HudPixels pixels, int channel)
        {
            var result = new float[pixels.Width * pixels.Height];
            for (int i = 0; i < result.Length; i++) result[i] = pixels.Rgba[i * 4 + channel] / 255f;
            return result;
        }

        private static float Luma(float[] r, float[] g, float[] b, int i)
        {
            return 0.299f * r[i] + 0.587f * g[i] + 0.114f * b[i];
        }

        private static byte Byte(float value)
        {
            return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(value * 255f)));
        }

        private static string Format(float value)
        {
            return value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
