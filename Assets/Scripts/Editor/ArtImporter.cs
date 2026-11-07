using UnityEditor;
using UnityEngine;

namespace PaintSort.EditorTools
{
    /// <summary>
    /// Import settings for game art, applied automatically to every PNG dropped into Resources/Art or Art/Store, so art never needs manual import setup.
    /// Sprite, no mip maps (UI), bilinear, transparency on. The two 9-slice shapes (round, roundsoft) get their borders and pixel density here,
    /// so a button keeps clean rounded ends at any size.
    /// </summary>
    public sealed class ArtImporter : AssetPostprocessor
    {
        const string GameArt = "Assets/Resources/Art/", StoreArt = "Assets/Art/Store/";
        const float CornerCanvasPixels = 45f;           // on-screen corner size of a 9-slice shape, in canvas pixels

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(GameArt) && !assetPath.StartsWith(StoreArt)) return;
            var importer = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;
            importer.textureCompression = assetPath.StartsWith(StoreArt) ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;

            if (name == "round" || name == "roundsoft")
            {
                importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                float border = Mathf.Floor(Mathf.Min(w, h) * 0.5f);            // half the height: fully rounded ends
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.spritePixelsPerUnit = border * 100f / CornerCanvasPixels;
            }

            // full-rect meshes (not tight) so Image.Sliced and sprite sizes always match the picture exactly
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
