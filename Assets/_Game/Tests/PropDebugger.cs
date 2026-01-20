using UnityEngine;

public class PropDebugger : MonoBehaviour
{
    void Start()
    {
        var render = GetComponentInChildren<Renderer>();

        Debug.LogWarning($"--- INICIO REPORTE DE HACHA ({transform.root.name}) ---");

        // 1. Verificar Capa (Layer) vs Cámara
        Debug.Log($"[LAYER] Objeto en Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");

        // 2. Verificar Escala GLOBAL (LossyScale). El inspector solo muestra Local.
        Debug.Log($"[SCALE] Local: {transform.localScale} | GLOBAL (Lossy): {transform.lossyScale}");

        if (render != null)
        {
            // 3. Verificar si el motor cree que es visible
            Debug.Log($"[RENDER] Enabled: {render.enabled} | IsVisible: {render.isVisible} | Bounds: {render.bounds}");

            // 4. Verificar Materiales y Color (¿Alpha es 0?)
            var mat = render.material; // Crea instancia temporal segura
            if (mat != null)
            {
                Debug.Log($"[MATERIAL] Shader: {mat.shader.name} | Color: {mat.color}");
                if (mat.HasProperty("_BaseColor"))
                    Debug.Log($"[URP COLOR] BaseColor: {mat.GetColor("_BaseColor")}");
            }
            else
            {
                Debug.LogError("[MATERIAL] ¡El Renderer no tiene material asignado!");
            }
        }
        else
        {
            Debug.LogError("[RENDER] ¡No se encontró componente Renderer en el hacha!");
        }

        Debug.LogWarning("--- FIN REPORTE ---");
    }
}