using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    // 0.4.2 diagnostic (runtime-test sessions only): the exact geometry of
    // the native paper, frame and shadow sprites the game draws its service
    // windows with, a preview of a bounded set of them, and the native UI
    // sound table, so a donor is chosen from verified runtime facts. The
    // previews stay in the guarded evidence directory; nothing is packaged.
    internal static class NativePaperDonorInventory
    {
        private const int MaximumPreviews = 24;
        private const int MaximumPreviewSide = 4096;

        // Paper, frame, edge, shadow and lighting sprites named by the
        // recorded native UI contract (runtime-evidence native-ui-contract).
        internal static readonly string[] PreviewNames =
        {
            "dialogue_backsheet", "Papers", "Card_Big", "Card_Small",
            "Inventory_Book_Clear", "Map_back", "map_mask_frame", "map_mask_back",
            "spellbook_metamagicback", "Character_Frame", "Character_Background",
            "Inventory_Background_1195_1446", "GradientEffect", "journal_back1",
            "journal_back3", "back_blockscroll", "kingdom_shadow", "map_chuckleft_25%",
            "spellbook_2505_1820", "ServiceWindow_TableBackGruond_3840_2022",
            "blockscroll_bottom", "WeaponSets_Frame", "map_caption_back", "main_menu_back"
        };

        internal static string CaptureTo(string evidenceDirectory)
        {
            if (StaticCanvas.Instance == null)
                throw new InvalidOperationException("StaticCanvas is not ready.");
            var report = new NativePaperDonorReport
            {
                SchemaVersion = 1,
                CapturedAtUtc = DateTime.UtcNow.ToString("o"),
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                ColorSpace = QualitySettings.activeColorSpace.ToString()
            };
            var seen = new Dictionary<string, NativePaperSpriteRecord>(StringComparer.Ordinal);
            foreach (Image image in StaticCanvas.Instance.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.sprite == null) continue;
                Sprite sprite = image.sprite;
                NativePaperSpriteRecord record;
                if (!seen.TryGetValue(sprite.name, out record))
                {
                    record = Describe(sprite);
                    seen[sprite.name] = record;
                }
                if (record.Uses.Count < 6) record.Uses.Add(Use(image));
                record.UseCount++;
            }
            report.Sprites = seen.Values.OrderBy(value => value.Name, StringComparer.Ordinal).ToList();
            int previews = 0;
            foreach (string name in PreviewNames)
            {
                NativePaperSpriteRecord record;
                if (!seen.TryGetValue(name, out record) || previews >= MaximumPreviews) continue;
                Sprite sprite = FindSprite(name);
                if (sprite == null) continue;
                try
                {
                    record.PreviewFile = WritePreview(sprite, evidenceDirectory);
                    previews++;
                }
                catch (Exception exception)
                {
                    record.PreviewError = exception.GetType().Name + ":" + exception.Message;
                }
            }
            report.UiSounds = ReadUiSounds();
            string path = Path.Combine(evidenceDirectory, "native-paper-donors.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
            return "sprites=" + report.Sprites.Count + ";previews=" + previews +
                ";sounds=" + report.UiSounds.Count;
        }

        private static Sprite FindSprite(string name)
        {
            return StaticCanvas.Instance.GetComponentsInChildren<Image>(true)
                .Where(image => image != null && image.sprite != null &&
                    string.Equals(image.sprite.name, name, StringComparison.Ordinal))
                .Select(image => image.sprite).FirstOrDefault();
        }

        private static NativePaperSpriteRecord Describe(Sprite sprite)
        {
            var record = new NativePaperSpriteRecord
            {
                Name = sprite.name,
                Rect = Format(sprite.rect),
                Border = Format(sprite.border),
                PixelsPerUnit = sprite.pixelsPerUnit,
                Packed = sprite.packed,
                TextureName = sprite.texture == null ? string.Empty : sprite.texture.name,
                TextureSize = sprite.texture == null ? string.Empty
                    : sprite.texture.width + "x" + sprite.texture.height,
                Uses = new List<NativePaperSpriteUse>()
            };
            try { record.TextureRect = Format(sprite.textureRect); }
            catch (Exception exception) { record.TextureRect = "unavailable:" + exception.GetType().Name; }
            return record;
        }

        private static NativePaperSpriteUse Use(Image image)
        {
            RectTransform rect = image.rectTransform;
            return new NativePaperSpriteUse
            {
                Path = HierarchyPath(image.transform),
                ActiveInHierarchy = image.gameObject.activeInHierarchy,
                SiblingIndex = image.transform.GetSiblingIndex(),
                Size = rect.rect.width.ToString("F1") + "x" + rect.rect.height.ToString("F1"),
                AnchorMin = rect.anchorMin.x.ToString("F3") + "," + rect.anchorMin.y.ToString("F3"),
                AnchorMax = rect.anchorMax.x.ToString("F3") + "," + rect.anchorMax.y.ToString("F3"),
                Type = image.type.ToString(),
                Color = image.color.r.ToString("F3") + "," + image.color.g.ToString("F3") + "," +
                    image.color.b.ToString("F3") + "," + image.color.a.ToString("F3"),
                Material = image.material == null ? string.Empty : image.material.name,
                PreserveAspect = image.preserveAspect,
                Scale = image.transform.localScale.x.ToString("F3") + "," +
                    image.transform.localScale.y.ToString("F3")
            };
        }

        // GPU copy of the sprite's own texture region (works for textures
        // that are not CPU-readable); the PNG is a local inspection aid.
        private static string WritePreview(Sprite sprite, string evidenceDirectory)
        {
            Texture2D texture = sprite.texture;
            if (texture == null) throw new InvalidOperationException("texture-null");
            Rect region = sprite.packed ? sprite.textureRect : sprite.rect;
            int width = Mathf.RoundToInt(region.width);
            int height = Mathf.RoundToInt(region.height);
            if (width < 1 || height < 1 || width > MaximumPreviewSide || height > MaximumPreviewSide)
                throw new InvalidOperationException("preview-size:" + width + "x" + height);
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
                string file = "donor-" + Safe(sprite.name) + ".png";
                File.WriteAllBytes(System.IO.Path.Combine(evidenceDirectory, file), read.EncodeToPNG());
                return file;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (read != null) UnityEngine.Object.Destroy(read);
            }
        }

        // UISoundManager.Sounds: the native UISoundType -> Wwise event table.
        private static List<NativeUiSoundRecord> ReadUiSounds()
        {
            var result = new List<NativeUiSoundRecord>();
            object manager = Game.Instance == null || Game.Instance.UI == null ||
                Game.Instance.UI.Common == null ? null : Game.Instance.UI.Common.UISound;
            if (manager == null) return result;
            FieldInfo soundsField = manager.GetType().GetField("Sounds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var sounds = soundsField == null ? null : soundsField.GetValue(manager) as IEnumerable;
            if (sounds == null) return result;
            foreach (object sound in sounds)
            {
                if (sound == null) continue;
                Type type = sound.GetType();
                result.Add(new NativeUiSoundRecord
                {
                    Type = ReadMember(type, sound, "Type"),
                    Id = ReadMember(type, sound, "Id")
                });
            }
            return result;
        }

        private static string ReadMember(Type type, object owner, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo field = type.GetField(name, flags);
            if (field != null) return Convert.ToString(field.GetValue(owner));
            PropertyInfo property = type.GetProperty(name, flags);
            return property == null ? string.Empty : Convert.ToString(property.GetValue(owner, null));
        }

        private static string Safe(string name)
        {
            var chars = name.Select(value => char.IsLetterOrDigit(value) || value == '_' ? value : '-')
                .ToArray();
            return new string(chars);
        }

        private static string HierarchyPath(Transform transform)
        {
            var parts = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string Format(Rect value)
        {
            return value.x.ToString("F1") + "," + value.y.ToString("F1") + "," +
                value.width.ToString("F1") + "," + value.height.ToString("F1");
        }

        private static string Format(Vector4 value)
        {
            return value.x.ToString("F1") + "," + value.y.ToString("F1") + "," +
                value.z.ToString("F1") + "," + value.w.ToString("F1");
        }
    }

    internal sealed class NativePaperDonorReport
    {
        public int SchemaVersion { get; set; }
        public string CapturedAtUtc { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public string ColorSpace { get; set; }
        public List<NativePaperSpriteRecord> Sprites { get; set; }
        public List<NativeUiSoundRecord> UiSounds { get; set; }
    }

    internal sealed class NativePaperSpriteRecord
    {
        public string Name { get; set; }
        public string Rect { get; set; }
        public string TextureRect { get; set; }
        public string Border { get; set; }
        public float PixelsPerUnit { get; set; }
        public bool Packed { get; set; }
        public string TextureName { get; set; }
        public string TextureSize { get; set; }
        public int UseCount { get; set; }
        public List<NativePaperSpriteUse> Uses { get; set; }
        public string PreviewFile { get; set; }
        public string PreviewError { get; set; }
    }

    internal sealed class NativePaperSpriteUse
    {
        public string Path { get; set; }
        public bool ActiveInHierarchy { get; set; }
        public int SiblingIndex { get; set; }
        public string Size { get; set; }
        public string AnchorMin { get; set; }
        public string AnchorMax { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string Material { get; set; }
        public bool PreserveAspect { get; set; }
        public string Scale { get; set; }
    }

    internal sealed class NativeUiSoundRecord
    {
        public string Type { get; set; }
        public string Id { get; set; }
    }
}
