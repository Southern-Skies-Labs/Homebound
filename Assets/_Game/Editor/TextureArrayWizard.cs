using UnityEngine;
using UnityEditor;

public class TextureArrayWizard : ScriptableWizard
{
    [MenuItem("Tools/Create Texture Array")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
    }

    [Header("Configuración")]
    public Texture2D[] Textures; // Arrastra tus texturas aquí (Grass, Dirt, Stone...)
    public string FileName = "WorldTextureArray";

    void OnWizardCreate()
    {
        if (Textures.Length == 0) return;

        Texture2D t = Textures[0];
        
        // Creamos el array con formato RGBA32 (Alta calidad)
        Texture2DArray textureArray = new Texture2DArray(t.width, t.height, Textures.Length, TextureFormat.RGBA32, true);
        
        // Configuración Pixel Art
        textureArray.filterMode = FilterMode.Point; 
        textureArray.wrapMode = TextureWrapMode.Repeat;

        for (int i = 0; i < Textures.Length; i++)
        {
            // --- MÉTODO SEGURO (CPU) ---
            // Requiere que las texturas tengan "Read/Write Enabled" en su import settings.
            try 
            {
                // Leemos los colores del original
                Color[] pixels = Textures[i].GetPixels();
                
                // Los escribimos en la "rebanada" (slice) correspondiente del array
                textureArray.SetPixels(pixels, i);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error procesando textura {i} ({Textures[i].name}): {e.Message}. ¿Olvidaste activar 'Read/Write Enabled'?");
                return;
            }
        }

        // Aplicamos los cambios para que se guarden
        textureArray.Apply();

        // Guardar en disco
        string path = $"Assets/_Game/Art/Materials/{FileName}.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log($"✅ Texture Array guardado en: {path}");
    }
}