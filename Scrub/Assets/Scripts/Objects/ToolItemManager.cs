// ToolItemManager.cs
using UnityEngine;
using System.Linq;

public class ToolItemManager : MonoBehaviour
{
    private Transform toolSlot;
    private const string HandSlotTag = "ToolSlot";
    private GameObject currentToolObject;

    void Awake()
    {
        // NO AÑADIR NADA AQUÍ PARA EVITAR CONFLICTOS CON LA CARGA DEL ANIMATOR
    }

    void Start()
    {
        // 🚨 CAMBIO EN LA LÓGICA DE BÚSQUEDA: Buscamos el Transform con el Tag solo en los hijos. 🚨

        // Obtenemos todos los Transforms hijos (pero no el padre).
        // El argumento 'true' en GetComponentsInChildren asegura que busca también en inactivos.
        // Usamos .gameObject.CompareTag para verificar el tag del GameObject.

        toolSlot = transform.GetComponentsInChildren<Transform>(true)
                            .FirstOrDefault(t => t != transform && t.gameObject.CompareTag(HandSlotTag));

        if (toolSlot == null)
        {
            Debug.LogError($"[TOOL MANAGER] ERROR FATAL: No se encontró el slot con el Tag '{HandSlotTag}'. La instanciación fallará. ¡Verifica que RightHandPinky2 tiene el Tag ToolSlot!", this);
        }
        else
        {
            Debug.Log($"[TOOL MANAGER] Slot '{HandSlotTag}' encontrado con éxito: {toolSlot.name}. Listo para instanciar.");
        }
    }

    // =========================================================================
    // MÉTODOS PÚBLICOS PARA EL ONCLICK DE LOS BOTONES
    // =========================================================================

    public void SelectAndInstantiateTool(GameObject toolPrefab)
    {
        DestroyCurrentTool();

        if (toolSlot != null && toolPrefab != null)
        {
            currentToolObject = Instantiate(toolPrefab, toolSlot.position, toolSlot.rotation, toolSlot);
            Debug.Log($"[TOOL MANAGER] Herramienta '{toolPrefab.name}' instanciada en la mano.");
        }
        else
        {
            // El debug que estás viendo: Tool Slot es NULL: True.
            Debug.LogError($"Fallo al instanciar: Tool Slot es NULL: {toolSlot == null}. Prefab es NULL: {toolPrefab == false}.", this);
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