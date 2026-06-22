#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/Art 配下の画像を自動的にUI用Spriteとしてインポート設定する。
/// 手動でInspectorからTexture Typeを変更する必要がなくなる。
/// </summary>
public class AncientTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Art")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        // UI用途のため非圧縮にする（DXT圧縮の「4の倍数」制約による白紙化バグを根本回避）
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(settings);
    }
}
#endif
