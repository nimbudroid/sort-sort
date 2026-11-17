using UnityEditor;
using UnityEngine;

namespace SortEverything.Editor
{
    /// <summary>
    /// Final-art drop folder: any PNG in Assets/SortEverything/Resources/SortEverything/Art/ named after an asset ID
    /// (apple.png, water_bottle.png, …) imports as a transparent, mobile-sized Sprite and replaces the code-drawn
    /// placeholder automatically at runtime. No other setup needed.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/SortEverything/Resources/SortEverything/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(ArtFolder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 512;
            ti.spritePixelsPerUnit = 100;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.wrapMode = TextureWrapMode.Clamp;
        }

        [MenuItem("Sort Everything/Open Final Art Folder")]
        static void OpenFolder()
        {
            System.IO.Directory.CreateDirectory(ArtFolder);
            AssetDatabase.Refresh();
            var folder = AssetDatabase.LoadAssetAtPath<Object>(ArtFolder.TrimEnd('/'));
            if (folder != null) { Selection.activeObject = folder; EditorGUIUtility.PingObject(folder); }
            EditorUtility.RevealInFinder(ArtFolder);
        }
    }
}
