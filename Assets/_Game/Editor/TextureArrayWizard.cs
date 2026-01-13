using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Homebound.Editor
{
    public class TextureArrayWizard : EditorWindow
    {
        [MenuItem("Homebound/Tools/Texture Array Wizard")]
        static void Init()
        {
            GetWindow<TextureArrayWizard>("Texture Array Packer");
        }

        public List<Texture2D> textures = new List<Texture2D>();
        private string savePath = "Assets/_Game/Art/Materials/WorldTextureArray.asset";

        private void OnGUI()
        {
            GUILayout.Label("Generador de Texture Arrays", EditorStyles.boldLabel);
            GUILayout.Space(10);

            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty stringsProperty = so.FindProperty("textures");

            EditorGUILayout.PropertyField(stringsProperty, true);
            so.ApplyModifiedProperties();

            GUILayout.Space(10);
            GUILayout.Label($"Ruta de guardado: {savePath}", EditorStyles.miniLabel);

            if (GUILayout.Button("Process & Save Texture Array"))
            {
                if (textures.Count == 0)
                {
                    Debug.LogError("¡La lista de texturas está vacía!");
                    return;
                }
                CreateTextureArray();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Asegúrate de que todas las texturas tengan el mismo tamaño (ej: 16x16) y formato.", MessageType.Info);
        }

        private void CreateTextureArray()
        {
            Texture2D t0 = textures[0];
            int width = t0.width;
            int height = t0.height;
            TextureFormat format = t0.format; // Cuidado: normalmente RGBA32 o ARGB32

            // Aseguramos formato consistente
            Texture2DArray textureArray = new Texture2DArray(width, height, textures.Count, format, false);
            textureArray.filterMode = FilterMode.Point; // Pixel Art style
            textureArray.wrapMode = TextureWrapMode.Repeat;

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] == null) continue;

                if (textures[i].width != width || textures[i].height != height)
                {
                    Debug.LogError($"Error: La textura {textures[i].name} tiene dimensiones incorrectas. Se esperaba {width}x{height}.");
                    return;
                }

                // Copiamos los píxeles (requiere textura legible en Import Settings -> Read/Write Enabled)
                Graphics.CopyTexture(textures[i], 0, 0, textureArray, i, 0);
            }

            AssetDatabase.CreateAsset(textureArray, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>Texture Array generado con éxito con {textures.Count} capas.</color>");
            Selection.activeObject = textureArray;
        }
    }
}