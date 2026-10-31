using UnityEditor;

namespace Pixamor.Editor
{
    // Harita ve ikonların monitörde sıkıştırma lekesi olmadan görünmesini sağlar.
    public class CommandUIAssetImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/CommandUI/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = assetPath.EndsWith("WorldMap.png") ? 4096 : 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = !assetPath.EndsWith("WorldMap.png");
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
