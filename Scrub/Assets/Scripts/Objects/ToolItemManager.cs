// ToolItemManager.cs
using UnityEngine;

public class ToolItemManager : MonoBehaviour
{
    // 🚨 CAMBIO CRÍTICO: Variable pública para arrastrar un objeto de escena 🚨
    [Header("Posición de la Herramienta")]
    [Tooltip("Arrastra un GameObject vacío de la Escena (el punto de anclaje) aquí.")]
    public Transform toolSlot;

    private GameObject currentToolObject;

    // Eliminamos Awake(), Start(), Singleton y Coroutine.

    // =========================================================================
    // LÓGICA DE INSTANCIACIÓN Y DESTRUCCIÓN
    // =========================================================================

    public void SelectAndInstantiateTool(GameObject toolPrefab)
    {
        // 1. Destruir anterior
        DestroyCurrentTool();

        if (toolSlot != null && toolPrefab != null)
        {
            // 2. Instanciar en el punto de la escena
            currentToolObject = Instantiate(toolPrefab, toolSlot.position, toolSlot.rotation, toolSlot);
            Debug.Log($"[TOOL MANAGER] 🟢 ¡ÉXITO! Herramienta '{toolPrefab.name}' instanciada en el punto de escena.");

            // 3. Opcional: Cerrar panel
            // GetComponent<ToolPanelIdea>()?.TogglePause(); 
        }
        else
        {
            // Este error te dirá si olvidaste arrastrar el objeto vacío o el Prefab al botón.
            Debug.LogError($"Fallo al instanciar. Verifica que el Prefab del botón y el Tool Slot (Inspector) estén asignados. Slot es NULL: {toolSlot == null}.", this);
        }
    }

    public void DestroyCurrentTool()
    {
        if (currentToolObject != null)
        {
            Destroy(currentToolObject);
            currentToolObject = null;
            Debug.Log("[TOOL MANAGER] Herramienta destruida al soltar.");
        }
    }

    public GameObject GetCurrentTool()
    {
        return currentToolObject;
    }
}