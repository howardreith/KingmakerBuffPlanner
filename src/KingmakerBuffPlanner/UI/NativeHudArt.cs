using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    // Native UI finish (UI-09): the native HUD art the owned cells and frame
    // are made from. The game's textures are not CPU-readable, so a native
    // sprite is read back through the GPU (the route the runtime donor
    // previews proved, kbpnui-facts-03); composed pixels become owned
    // textures and sprites the HUD controller destroys with its root.
    internal static class NativeHudArt
    {
        private const int MaximumSide = 512;

        internal static HudPixels Read(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                throw new InvalidOperationException("native-sprite-missing");
            Texture2D texture = sprite.texture;
            Rect region = sprite.packed ? sprite.textureRect : sprite.rect;
            int width = Mathf.RoundToInt(region.width);
            int height = Mathf.RoundToInt(region.height);
            if (width < 1 || height < 1 || width > MaximumSide || height > MaximumSide)
                throw new InvalidOperationException("native-sprite-size:" + width + "x" + height);
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                RenderTextureFormat.ARGB32);
            Texture2D read = null;
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;
                read = new Texture2D(width, height, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(region.x, region.y, width, height), 0, 0, false);
                read.Apply(false, false);
                Color32[] pixels = read.GetPixels32();
                var rgba = new byte[width * height * 4];
                for (int index = 0; index < pixels.Length; index++)
                {
                    rgba[index * 4] = pixels[index].r;
                    rgba[index * 4 + 1] = pixels[index].g;
                    rgba[index * 4 + 2] = pixels[index].b;
                    rgba[index * 4 + 3] = pixels[index].a;
                }
                return new HudPixels(width, height, rgba);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (read != null) UnityEngine.Object.Destroy(read);
            }
        }

        // An owned sprite of composed pixels, mipmapped like the native cell
        // art it is drawn beside at the HUD's reduced size.
        internal static Sprite CreateSprite(HudPixels pixels, string name, float pixelsPerUnit,
            List<Texture2D> ownedTextures, List<Sprite> ownedSprites)
        {
            var texture = new Texture2D(pixels.Width, pixels.Height, TextureFormat.RGBA32, true);
            texture.name = name;
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var colors = new Color32[pixels.Width * pixels.Height];
            for (int index = 0; index < colors.Length; index++)
                colors[index] = new Color32(pixels.Rgba[index * 4], pixels.Rgba[index * 4 + 1],
                    pixels.Rgba[index * 4 + 2], pixels.Rgba[index * 4 + 3]);
            texture.SetPixels32(colors);
            texture.Apply(true, true);
            ownedTextures.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, pixels.Width, pixels.Height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            ownedSprites.Add(sprite);
            return sprite;
        }

        // A piece of a native texture as an owned sprite over the same
        // texture (no pixels copied).
        internal static Sprite CreatePiece(Sprite native, HudFramePiece piece, List<Sprite> ownedSprites)
        {
            Rect region = native.packed ? native.textureRect : native.rect;
            Sprite sprite = Sprite.Create(native.texture,
                new Rect(region.x + piece.SourceX, region.y + piece.SourceY, piece.SourceWidth, piece.SourceHeight),
                Vector2.zero, native.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = "KingmakerBuffPlanner.Frame." + piece.Name;
            ownedSprites.Add(sprite);
            return sprite;
        }
    }
}
