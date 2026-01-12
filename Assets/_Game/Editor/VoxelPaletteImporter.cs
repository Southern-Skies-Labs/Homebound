using UnityEngine;
using UnityEditor;

public class VoxelPaletteImporter : AssetPostprocessor
{
    // Escribe aqu� la carpeta donde guardas tus paletas para evitar afectar otras texturas
    private const string PALETTE_FOLDER_KEYWORD = "Palettes";

    // 1. CONFIGURACI�N T�CNICA (Pre-Procesado)
    void OnPreprocessTexture()
    {
        if (!IsPaletteFile(assetPath)) return;

        TextureImporter importer = (TextureImporter)assetImporter;

        // Configuraci�n Sagrada
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.alphaSource = TextureImporterAlphaSource.None; // Vital para ojos
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;

        // Habilitamos lectura para poder "inflarla" en el siguiente paso
        importer.isReadable = true;
    }

    // 2. CIRUG�A DE P�XELES (Post-Procesado)
    void OnPostprocessTexture(Texture2D texture)
    {
        if (!IsPaletteFile(assetPath)) return;

        // Solo actuamos si la textura es peligrosamente fina (1 pixel de alto)
        if (texture.height == 1)
        {
            ExpandTextureHeight(texture, 16); // La inflamos a 16px
        }
    }

    // L�gica auxiliar para rellenar la textura hacia abajo
    private void ExpandTextureHeight(Texture2D original, int newHeight)
    {
        Color[] originalColors = original.GetPixels(0, 0, original.width, 1);
        Color[] newColors = new Color[original.width * newHeight];

        // Copiamos la fila original repetidamente para rellenar el alto
        for (int y = 0; y < newHeight; y++)
        {
            System.Array.Copy(originalColors, 0, newColors, y * original.width, original.width);
        }

        // Redimensionamos y aplicamos (Esto ocurre en memoria, no da�a tu PNG)
        original.Reinitialize(original.width, newHeight);
        original.SetPixels(newColors);
        original.Apply();

        Debug.Log($"[VoxelImporter] Paleta estabilizada (Inflada a {newHeight}px): {assetPath}");
    }

    private bool IsPaletteFile(string path)
    {
        // Filtro de seguridad
        return path.Contains(PALETTE_FOLDER_KEYWORD) || path.Contains("Palette");
    }
}